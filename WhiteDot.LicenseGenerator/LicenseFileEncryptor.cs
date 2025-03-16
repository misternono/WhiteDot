using System;
using System.Security.Cryptography;
using System.Text;

namespace WhiteDot.LicenseGenerator
{
    /// <summary>
    /// Utility class for encrypting license files to distribute them securely
    /// </summary>
    internal class LicenseFileEncryptor
    {
        // This is a transport key used only to encrypt the license file for secure distribution
        // It's separate from the internal license validation key
        private static readonly byte[] TransportKey = new byte[] 
        { 
            0x46, 0xA2, 0xC5, 0xD9, 0x7B, 0x3F, 0x8E, 0x51, 
            0x9C, 0x34, 0x10, 0x87, 0x6D, 0xF2, 0xA7, 0xE1,
            0x90, 0x23, 0x5F, 0xB8, 0x76, 0xC4, 0x0D, 0xE9,
            0x12, 0x67, 0xF8, 0x3A, 0xD1, 0x59, 0xB2, 0x8E
        };

        /// <summary>
        /// Encrypt a license file for secure transport
        /// </summary>
        /// <param name="jsonLicenseContent">The license file content as JSON</param>
        /// <param name="outputFilePath">The path to write the encrypted license file</param>
        public static void EncryptLicenseFile(string jsonLicenseContent, string outputFilePath)
        {
            // Convert the license content to bytes
            byte[] licenseData = Encoding.UTF8.GetBytes(jsonLicenseContent);
            
            // Create a new AES instance
            using (var aes = Aes.Create())
            {
                aes.Key = TransportKey;
                aes.GenerateIV(); // Generate a random IV
                
                // Write the IV to the beginning of the file
                using (var outputFileStream = new FileStream(outputFilePath, FileMode.Create))
                {
                    // Write a marker to identify this as an encrypted license file
                    byte[] marker = Encoding.ASCII.GetBytes("WDLENC");
                    outputFileStream.Write(marker, 0, marker.Length);
                    
                    // Write the IV
                    outputFileStream.Write(aes.IV, 0, aes.IV.Length);
                    
                    // Create encryptor
                    using (var encryptor = aes.CreateEncryptor())
                    using (var cryptoStream = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write))
                    {
                        // Write the license data
                        cryptoStream.Write(licenseData, 0, licenseData.Length);
                    }
                }
            }
            
            Console.WriteLine($"Encrypted license file created at: {Path.GetFullPath(outputFilePath)}");
        }
        
        /// <summary>
        /// Generates a custom license activation code for distribution to customers
        /// </summary>
        /// <param name="licenseKey">The license key</param>
        /// <param name="customerEmail">The customer email</param>
        /// <returns>An activation code that can be shared with customers</returns>
        public static string GenerateActivationCode(string licenseKey, string customerEmail)
        {
            // Create a more human-friendly activation code derived from the license key and email
            // This is just for distribution purposes, not for security
            
            // Create a hash of the license key and email combination
            using (var sha = SHA256.Create())
            {
                byte[] keyEmailBytes = Encoding.UTF8.GetBytes($"{licenseKey}|{customerEmail.ToLower()}");
                byte[] hash = sha.ComputeHash(keyEmailBytes);
                
                // Take first 8 bytes and convert to a more readable format (Base32 variant)
                const string base32Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
                StringBuilder sb = new StringBuilder();
                
                // Get the first 5 bytes (40 bits) and encode as 8 5-bit chunks
                for (int i = 0; i < 5; i++)
                {
                    byte b = hash[i];
                    // Each byte provides enough bits for 1.6 Base32 chars
                    sb.Append(base32Chars[(b >> 3) % 32]); // Take 5 bits from the high end
                    
                    if (i < 4) // For all but the last byte
                    {
                        // Take 3 bits from the current byte and 2 from the next byte
                        int index = ((b & 0x07) << 2) | ((hash[i + 1] >> 6) & 0x03);
                        sb.Append(base32Chars[index]);
                    }
                }
                
                // Return activation code with hyphens for readability
                string code = sb.ToString();
                return $"{code.Substring(0, 4)}-{code.Substring(4, 4)}";
            }
        }
    }
} 