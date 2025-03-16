using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Security;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading;
using System.Text.Json.Serialization;

namespace WhiteDot.Licensing
{
    public enum ProductType
    {
        DotTex2,
        DotPdf
    }

    public class LicenseInfo
    {
        private bool? _cachedValidityCheck = null;

        public string LicenseKey { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty; 
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public List<ProductType> Products { get; set; } = new List<ProductType>();
        
        // License validation signature
        public string Signature { get; set; } = string.Empty;
        
        // Validation checksum for integrity verification
        public string IntegrityChecksum { get; set; } = string.Empty;
        
        // Version of the license format
        public int Version { get; set; } = 1;
        
        // Helper method to check if license is time-valid (not expired)
        // This is computed dynamically and can't be manipulated in the JSON
        [JsonIgnore]
        public bool IsValid 
        { 
            get 
            {
                // Cache the result so it doesn't change during application execution
                if (!_cachedValidityCheck.HasValue)
                {
                    _cachedValidityCheck = DateTime.Now <= ExpiryDate;
                }
                return _cachedValidityCheck.Value;
            }
        }
        
        // Don't allow these properties to be set from JSON deserialization
        [JsonIgnore]
        public bool IsTampered { get; internal set; }
        
        [JsonIgnore] 
        public bool HasValidSignature { get; internal set; }
    }

    public class LicenseException : Exception
    {
        public LicenseException(string message) : base(message) { }
    }

    public sealed class LicenseManager
    {
        private static LicenseManager? _instance;
        private static readonly object _lock = new object();

        private LicenseInfo? _currentLicense;
        private readonly string _licenseFilePath;
        
        // Embedded signing key - in a real implementation, this would be better protected
        // Using a combination of code obfuscation and constant string encryption
        private static readonly byte[] SigningKey = new byte[] 
        { 
            0x57, 0x68, 0x69, 0x74, 0x65, 0x44, 0x6F, 0x74, 
            0x4C, 0x69, 0x63, 0x65, 0x6E, 0x73, 0x69, 0x6E, 
            0x67, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x4B, 
            0x65, 0x79, 0x32, 0x30, 0x32, 0x34, 0xAB, 0xCD 
        };
        
        private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        private LicenseManager()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string whiteDotFolder = Path.Combine(appDataPath, "WhiteDot");
            
            if (!Directory.Exists(whiteDotFolder))
            {
                Directory.CreateDirectory(whiteDotFolder);
            }
            
            _licenseFilePath = Path.Combine(whiteDotFolder, "license.dat");
        }

        public static LicenseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LicenseManager();
                    }
                }
                return _instance;
            }
        }

        public void RegisterLicense(string licenseKey)
        {
            // Self-contained license key validation
            ValidateLicenseKeyFormat(licenseKey);
            
            // Parse license information from the key
            var licenseData = ParseLicenseKey(licenseKey);
            
            // Create license with the parsed data
            var license = new LicenseInfo
            {
                LicenseKey = licenseKey,
                CustomerName = licenseData.customerName,
                CustomerEmail = licenseData.customerEmail,
                IssueDate = licenseData.issueDate,
                ExpiryDate = licenseData.expiryDate,
                Products = licenseData.products,
                Signature = GenerateLicenseSignature(licenseKey)
            };
            
            // Generate integrity checksum
            license.IntegrityChecksum = GenerateIntegrityChecksum(license);

            // Save the license
            SaveLicense(license);
            _currentLicense = license;
        }

        public bool ValidateLicense(ProductType productType)
        {
            LoadLicense();

            if (_currentLicense == null)
            {
                return false;
            }

            // Basic validation checks
            if (DateTime.Now > _currentLicense.ExpiryDate)
            {
                return false;
            }

            if (!_currentLicense.Products.Contains(productType))
            {
                return false;
            }
            
            // Advanced security checks
            try
            {
                // 1. Validate license signature and set property
                bool validSignature = ValidateLicenseSignature(_currentLicense);
                _currentLicense.HasValidSignature = validSignature;
                if (!validSignature)
                {
                    return false;
                }
                
                // 2. Validate license data integrity and set property
                bool validIntegrity = ValidateLicenseIntegrity(_currentLicense);
                _currentLicense.IsTampered = !validIntegrity;
                if (!validIntegrity)
                {
                    return false;
                }
                
                // 3. Additional time-based validation
                if (!_currentLicense.IsValid)
                {
                    return false;
                }
                
                return true;
            }
            catch
            {
                // Any security exception = invalid license
                return false;
            }
        }

        public void RequireLicense(ProductType productType)
        {
            if (!ValidateLicense(productType))
            {
                string productName = Enum.GetName(typeof(ProductType), productType) ?? productType.ToString();
                throw new LicenseException($"Valid license for {productName} is required. Please register a license key.");
            }
        }

        public LicenseInfo? GetCurrentLicense()
        {
            LoadLicense();
            return _currentLicense;
        }

        private void SaveLicense(LicenseInfo license)
        {
            try
            {
                // Serialize license data
                string json = JsonSerializer.Serialize(license);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                
                // Multi-layer protection
                // 1. Simple XOR obfuscation
                byte[] obfuscatedData = ObfuscateData(jsonBytes);
                
                // 2. Encrypt with Windows DPAPI
                byte[] encryptedData = ProtectedData.Protect(
                    obfuscatedData, 
                    SigningKey, // Use signing key as entropy for added security
                    DataProtectionScope.CurrentUser);
                
                // 3. Save the encrypted license
                File.WriteAllBytes(_licenseFilePath, encryptedData);
                
                // 4. Create validation file
                SaveValidationFile(license);
            }
            catch (Exception ex)
            {
                throw new LicenseException($"Failed to save license: {ex.Message}");
            }
        }
        
        private void SaveValidationFile(LicenseInfo license)
        {
            try
            {
                // Primary validation file (hidden)
                string validationFilePath = Path.Combine(
                    Path.GetDirectoryName(_licenseFilePath) ?? "",
                    "." + Path.GetFileNameWithoutExtension(_licenseFilePath) + ".val");
                
                byte[] validationData = CreateValidationData(license);
                File.WriteAllBytes(validationFilePath, validationData);
                File.SetAttributes(validationFilePath, FileAttributes.Hidden);
            }
            catch
            {
                // Ignore validation file errors - main license file is still saved
            }
        }

        private void LoadLicense()
        {
            if (_currentLicense != null)
            {
                return;
            }

            try
            {
                if (File.Exists(_licenseFilePath))
                {
                    // Read and decrypt the license file
                    byte[] encryptedData = File.ReadAllBytes(_licenseFilePath);
                    
                    byte[] obfuscatedData = ProtectedData.Unprotect(
                        encryptedData, 
                        SigningKey, // Same entropy as used for encryption
                        DataProtectionScope.CurrentUser);
                    
                    byte[] jsonBytes = DeobfuscateData(obfuscatedData);
                    
                    string json = Encoding.UTF8.GetString(jsonBytes);
                    _currentLicense = JsonSerializer.Deserialize<LicenseInfo>(json);
                    
                    // Verify license integrity using validation file
                    if (!VerifyValidationFile(_currentLicense))
                    {
                        _currentLicense = null;
                    }
                }
            }
            catch
            {
                // If there's an error loading the license, treat it as if no license exists
                _currentLicense = null;
            }
        }
        
        private bool VerifyValidationFile(LicenseInfo? license)
        {
            if (license == null) return false;
            
            try
            {
                // Check integrity checksum
                if (!ValidateLicenseIntegrity(license))
                {
                    return false;
                }
                
                string validationFilePath = Path.Combine(
                    Path.GetDirectoryName(_licenseFilePath) ?? "",
                    "." + Path.GetFileNameWithoutExtension(_licenseFilePath) + ".val");
                    
                if (!File.Exists(validationFilePath))
                {
                    return false;
                }
                
                byte[] validationData = File.ReadAllBytes(validationFilePath);
                
                using var ms = new MemoryStream(validationData);
                using var reader = new BinaryReader(ms);
                
                // Read and verify header
                byte[] header = reader.ReadBytes(4);
                string headerStr = Encoding.ASCII.GetString(header);
                if (headerStr != "WDLV")
                {
                    return false;
                }
                
                // Read timestamp
                long timestampBinary = reader.ReadInt64();
                DateTime timestamp = DateTime.FromBinary(timestampBinary);
                
                // Read and verify license key hash
                int hashLength = reader.ReadInt32();
                byte[] storedLicenseKeyHash = reader.ReadBytes(hashLength);
                
                byte[] currentLicenseKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(license.LicenseKey));
                
                if (!storedLicenseKeyHash.SequenceEqual(currentLicenseKeyHash))
                {
                    return false;
                }
                
                // Read expiry date
                long expiryBinary = reader.ReadInt64();
                DateTime expiry = DateTime.FromBinary(expiryBinary);
                
                // Verify expiry date matches
                if (expiry != license.ExpiryDate)
                {
                    return false;
                }
                
                // Read checksum
                string storedChecksum = reader.ReadString();
                if (storedChecksum != license.IntegrityChecksum)
                {
                    return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private byte[] ObfuscateData(byte[] data)
        {
            // Create a simple obfuscation key from the signing key
            byte[] obfuscationKey = new byte[32];
            for (int i = 0; i < SigningKey.Length && i < 32; i++)
            {
                obfuscationKey[i] = SigningKey[i];
            }
            
            // Simple XOR obfuscation with the key
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ obfuscationKey[i % obfuscationKey.Length]);
            }
            
            return result;
        }
        
        private byte[] DeobfuscateData(byte[] obfuscatedData)
        {
            // Deobfuscation is the same operation as obfuscation with XOR
            return ObfuscateData(obfuscatedData);
        }
        
        private string GenerateLicenseSignature(string licenseKey)
        {
            using (var hmac = new HMACSHA256(SigningKey))
            {
                byte[] signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(licenseKey));
                return Convert.ToBase64String(signature);
            }
        }
        
        private bool ValidateLicenseSignature(LicenseInfo license)
        {
            string expectedSignature = GenerateLicenseSignature(license.LicenseKey);
            return string.Equals(expectedSignature, license.Signature);
        }
        
        private string GenerateIntegrityChecksum(LicenseInfo license)
        {
            // Create a checksum of critical license fields
            string dataToHash = $"{license.LicenseKey}|{license.CustomerName}|{license.CustomerEmail}|" +
                               $"{license.IssueDate.ToBinary()}|{license.ExpiryDate.ToBinary()}|" +
                               $"{string.Join(",", license.Products)}|{license.Signature}";
            
            using (var hmac = new HMACSHA256(SigningKey))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                return Convert.ToBase64String(hash);
            }
        }
        
        private bool ValidateLicenseIntegrity(LicenseInfo license)
        {
            string expectedChecksum = GenerateIntegrityChecksum(license);
            return string.Equals(expectedChecksum, license.IntegrityChecksum);
        }
        
        private byte[] CreateValidationData(LicenseInfo license)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                // Write validation header
                writer.Write(Encoding.ASCII.GetBytes("WDLV"));
                
                // Write timestamp
                writer.Write(DateTime.UtcNow.ToBinary());
                
                // Write license key hash
                byte[] licenseKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(license.LicenseKey));
                writer.Write(licenseKeyHash.Length);
                writer.Write(licenseKeyHash);
                
                // Write expiry date
                writer.Write(license.ExpiryDate.ToBinary());
                
                // Write integrity checksum
                writer.Write(license.IntegrityChecksum);
                
                // Add random bytes for uniqueness
                byte[] randomBytes = new byte[16];
                _rng.GetBytes(randomBytes);
                writer.Write(randomBytes.Length);
                writer.Write(randomBytes);
                
                return ms.ToArray();
            }
        }
        
        private string ComputeShortHash(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8).ToLowerInvariant();
            }
        }
        
        private void ValidateLicenseKeyFormat(string licenseKey)
        {
            // License key format: XXXXX-XXXXX-XXXXX-XXXXX-XXXXX
            // Where X is alphanumeric character
            if (string.IsNullOrWhiteSpace(licenseKey) || licenseKey.Length < 20)
            {
                throw new LicenseException("Invalid license key format.");
            }
            
            // Advanced validation rules can be added here
        }
        
        private (string customerName, string customerEmail, DateTime issueDate, 
                 DateTime expiryDate, List<ProductType> products) ParseLicenseKey(string licenseKey)
        {
            // In a real implementation, this would decode information from the license key
            // This is a simplified demo version that just returns defaults
            
            // For simplicity, we're returning a fixed license configuration
            // In a real implementation, you would decode this from the license key itself
            return (
                customerName: "Demo User",
                customerEmail: "demo@example.com",
                issueDate: DateTime.Now,
                expiryDate: DateTime.Now.AddYears(1),
                products: new List<ProductType> { ProductType.DotTex2, ProductType.DotPdf }
            );
        }
    }
}
