using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WhiteDot.Licensing
{
    /// <summary>
    /// Extensions for the LicenseManager to support additional licensing scenarios
    /// </summary>
    public static class LicenseManagerExtensions
    {
        // Transport key for decrypting license files - must match the one in WhiteDot.LicenseGenerator
        private static readonly byte[] TransportKey = new byte[] 
        { 
            0x46, 0xA2, 0xC5, 0xD9, 0x7B, 0x3F, 0x8E, 0x51, 
            0x9C, 0x34, 0x10, 0x87, 0x6D, 0xF2, 0xA7, 0xE1,
            0x90, 0x23, 0x5F, 0xB8, 0x76, 0xC4, 0x0D, 0xE9,
            0x12, 0x67, 0xF8, 0x3A, 0xD1, 0x59, 0xB2, 0x8E
        };

        /// <summary>
        /// Initialize licensing from a license file
        /// </summary>
        /// <param name="manager">The license manager instance</param>
        /// <param name="licenseFilePath">Path to the license file (.json or .lic)</param>
        /// <returns>True if license was loaded and is valid, false otherwise</returns>
        public static bool LoadLicenseFromFile(this LicenseManager manager, string licenseFilePath)
        {
            if (string.IsNullOrEmpty(licenseFilePath) || !File.Exists(licenseFilePath))
            {
                return false;
            }

            try
            {
                string extension = Path.GetExtension(licenseFilePath).ToLowerInvariant();
                LicenseInfo license;

                if (extension == ".lic")
                {
                    // Encrypted license file - more secure
                    license = DecryptLicenseFile(licenseFilePath);
                }
                else if (extension == ".json")
                {
                    // Plain JSON license file - less secure, verify more carefully
                    string json = File.ReadAllText(licenseFilePath);
                    
                    // Use JsonSerializerOptions to control deserialization behavior
                    var options = new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        // Don't allow unknown properties that might be trying to inject malicious data
                        IgnoreReadOnlyProperties = true
                    };
                    
                    license = JsonSerializer.Deserialize<LicenseInfo>(json, options);
                    
                    if (license == null)
                    {
                        Console.WriteLine("Error: Invalid license format");
                        return false;
                    }
                }
                else
                {
                    // Unsupported file format
                    Console.WriteLine($"Error: Unsupported license file format: {extension}");
                    return false;
                }

                if (license != null)
                {
                    // Perform additional validation checks on the license
                    try
                    {
                        ValidateLicense(license);
                        
                        // If we got this far, the license is valid, so register it
                        RegisterLicenseInfo(manager, license);
                        return true;
                    }
                    catch (LicenseException ex)
                    {
                        Console.WriteLine($"License validation failed: {ex.Message}");
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading license: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper method to check if a product is licensed
        /// </summary>
        /// <param name="manager">The license manager instance</param>
        /// <param name="product">The product to check</param>
        /// <returns>True if product is licensed, false otherwise</returns>
        public static bool IsProductLicensed(this LicenseManager manager, ProductType product)
        {
            return manager.ValidateLicense(product);
        }

        /// <summary>
        /// Initialize the license system from a license file
        /// </summary>
        /// <param name="licenseFilePath">Path to the license file</param>
        /// <returns>True if license was loaded and is valid, false otherwise</returns>
        public static bool Initialize(string licenseFilePath)
        {
            return LicenseManager.Instance.LoadLicenseFromFile(licenseFilePath);
        }

        // Private helper methods

        private static LicenseInfo DecryptLicenseFile(string licenseFilePath)
        {
            byte[] fileData = File.ReadAllBytes(licenseFilePath);

            // Check for license file marker
            const string marker = "WDLENC";
            byte[] markerBytes = Encoding.ASCII.GetBytes(marker);

            if (fileData.Length <= markerBytes.Length)
            {
                throw new LicenseException("Invalid license file format");
            }

            // Verify marker
            for (int i = 0; i < markerBytes.Length; i++)
            {
                if (fileData[i] != markerBytes[i])
                {
                    throw new LicenseException("Invalid license file format");
                }
            }

            // Read the IV
            int ivLength = 16; // AES IV length is 16 bytes
            byte[] iv = new byte[ivLength];
            Array.Copy(fileData, markerBytes.Length, iv, 0, ivLength);

            // Get encrypted data
            int encryptedDataLength = fileData.Length - markerBytes.Length - ivLength;
            byte[] encryptedData = new byte[encryptedDataLength];
            Array.Copy(fileData, markerBytes.Length + ivLength, encryptedData, 0, encryptedDataLength);

            // Decrypt the data
            using (var aes = Aes.Create())
            {
                aes.Key = TransportKey;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var memoryStream = new MemoryStream(encryptedData))
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cryptoStream))
                {
                    string json = reader.ReadToEnd();
                    return JsonSerializer.Deserialize<LicenseInfo>(json);
                }
            }
        }

        private static void ValidateLicense(LicenseInfo license)
        {
            if (license == null)
            {
                throw new LicenseException("License is null");
            }

            if (string.IsNullOrEmpty(license.LicenseKey))
            {
                throw new LicenseException("License key is missing");
            }

            if (string.IsNullOrEmpty(license.Signature))
            {
                throw new LicenseException("License signature is missing");
            }

            if (string.IsNullOrEmpty(license.IntegrityChecksum))
            {
                throw new LicenseException("License integrity checksum is missing");
            }

            if (license.Products == null || license.Products.Count == 0)
            {
                throw new LicenseException("No licensed products");
            }

            // Check expiration date regardless of IsValid property
            if (DateTime.Now > license.ExpiryDate)
            {
                throw new LicenseException($"License expired on {license.ExpiryDate:yyyy-MM-dd}");
            }

            // Validate license signature to ensure license key hasn't been tampered with
            string expectedSignature = GenerateLicenseSignature(license.LicenseKey);
            bool hasValidSignature = string.Equals(expectedSignature, license.Signature);
            license.HasValidSignature = hasValidSignature;
            
            if (!hasValidSignature)
            {
                throw new LicenseException("Invalid license signature - license has been tampered with");
            }

            // Validate integrity checksum to ensure license data hasn't been modified
            string expectedChecksum = GenerateIntegrityChecksum(license);
            bool hasValidIntegrity = string.Equals(expectedChecksum, license.IntegrityChecksum);
            license.IsTampered = !hasValidIntegrity;
            
            if (!hasValidIntegrity)
            {
                throw new LicenseException("Invalid license integrity - license data has been tampered with");
            }
        }

        // Add methods to validate license signatures and integrity
        private static string GenerateLicenseSignature(string licenseKey)
        {
            // This should match the algorithm in LicenseManager
            using (var hmac = new HMACSHA256(GetSigningKey()))
            {
                byte[] signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(licenseKey));
                return Convert.ToBase64String(signature);
            }
        }

        private static string GenerateIntegrityChecksum(LicenseInfo license)
        {
            // Create a checksum of critical license fields
            string dataToHash = $"{license.LicenseKey}|{license.CustomerName}|{license.CustomerEmail}|" +
                               $"{license.IssueDate.ToBinary()}|{license.ExpiryDate.ToBinary()}|" +
                               $"{string.Join(",", license.Products)}|{license.Signature}";
            
            using (var hmac = new HMACSHA256(GetSigningKey()))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                return Convert.ToBase64String(hash);
            }
        }

        // Retrieve signing key - must match the key in LicenseManager
        private static byte[] GetSigningKey()
        {
            // This should match the key in LicenseManager
            return new byte[] 
            { 
                0x57, 0x68, 0x69, 0x74, 0x65, 0x44, 0x6F, 0x74, 
                0x4C, 0x69, 0x63, 0x65, 0x6E, 0x73, 0x69, 0x6E, 
                0x67, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x4B, 
                0x65, 0x79, 0x32, 0x30, 0x32, 0x34, 0xAB, 0xCD 
            };
        }

        private static void RegisterLicenseInfo(LicenseManager manager, LicenseInfo license)
        {
            // Use reflection to access the private method and set the license
            var type = typeof(LicenseManager);
            var method = type.GetMethod("RegisterLicense", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(manager, new object[] { license.LicenseKey });
            }
            else
            {
                // Fallback if reflection fails - directly set the field using reflection
                var currentLicenseField = type.GetField("_currentLicense", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (currentLicenseField != null)
                {
                    currentLicenseField.SetValue(manager, license);
                }
            }
        }
    }
} 