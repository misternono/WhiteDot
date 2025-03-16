using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Xunit;
using WhiteDot.Licensing;

namespace WhiteDot.Tests
{
    public class LicenseFileTests : IDisposable
    {
        private readonly string _encryptedLicensePath;
        private readonly string _decryptedLicensePath;
        private readonly byte[] _transportKey = new byte[] 
        { 
            0x46, 0xA2, 0xC5, 0xD9, 0x7B, 0x3F, 0x8E, 0x51, 
            0x9C, 0x34, 0x10, 0x87, 0x6D, 0xF2, 0xA7, 0xE1,
            0x90, 0x23, 0x5F, 0xB8, 0x76, 0xC4, 0x0D, 0xE9,
            0x12, 0x67, 0xF8, 0x3A, 0xD1, 0x59, 0xB2, 0x8E
        };

        public LicenseFileTests()
        {
            // Set up test license files with unique names to avoid conflicts
            _encryptedLicensePath = Path.Combine(Path.GetTempPath(), $"test-license-{Guid.NewGuid()}.lic");
            _decryptedLicensePath = Path.Combine(Path.GetTempPath(), $"test-license-{Guid.NewGuid()}.json");
            
            // Create test license files
            CreateTestLicenseFiles();
        }

        public void Dispose()
        {
            // Clean up test files
            try
            {
                if (File.Exists(_encryptedLicensePath))
                {
                    File.Delete(_encryptedLicensePath);
                }
                
                if (File.Exists(_decryptedLicensePath))
                {
                    File.Delete(_decryptedLicensePath);
                }
            }
            catch (IOException)
            {
                // Ignore file access errors during cleanup
            }
        }

        [Fact]
        public void Initialize_WithEncryptedLicense_ReturnsTrue()
        {
            try
            {
                // Act
                bool result = LicenseManagerExtensions.Initialize(_encryptedLicensePath);

                // Assert
                Assert.True(result);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed: {ex.Message}");
            }
        }

        [Fact]
        public void Initialize_WithDecryptedLicense_ReturnsTrue()
        {
            try
            {
                // Act
                bool result = LicenseManagerExtensions.Initialize(_decryptedLicensePath);

                // Assert
                Assert.True(result);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed: {ex.Message}");
            }
        }

        [Fact]
        public void TamperedEncryptedLicense_IsDetected()
        {
            // Arrange - Create a tampered encrypted license file
            string tamperedPath = Path.Combine(Path.GetTempPath(), $"tampered-license-{Guid.NewGuid()}.lic");
            
            try
            {
                File.Copy(_encryptedLicensePath, tamperedPath, true);
                
                // Tamper with the file (modify a byte in the middle)
                byte[] fileBytes = File.ReadAllBytes(tamperedPath);
                int middleIndex = fileBytes.Length / 2;
                fileBytes[middleIndex] = (byte)(fileBytes[middleIndex] + 1);
                File.WriteAllBytes(tamperedPath, fileBytes);

                // Act
                bool result = LicenseManagerExtensions.Initialize(tamperedPath);

                // Assert
                Assert.False(result);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tamperedPath))
                {
                    try
                    {
                        File.Delete(tamperedPath);
                    }
                    catch (IOException)
                    {
                        // Ignore file access errors during cleanup
                    }
                }
            }
        }

        [Fact(Skip = "Integration test that requires file system access")]
        public void CreateAndLoadEncryptedLicense_WorksCorrectly()
        {
            // This is an integration test that creates and loads a real encrypted license file
            // It's skipped by default since it requires file system access and encryption capabilities

            // Arrange - Create a license
            var license = CreateValidLicense();
            string licensePath = Path.Combine(Path.GetTempPath(), "integration-test.lic");
            
            // Create an encrypted license file
            string json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = true });
            
            // Act - Encrypt and save the license
            using (var aes = Aes.Create())
            {
                aes.Key = _transportKey;
                aes.GenerateIV();
                
                using (var outputFileStream = new FileStream(licensePath, FileMode.Create))
                {
                    // Write marker
                    byte[] marker = Encoding.ASCII.GetBytes("WDLENC");
                    outputFileStream.Write(marker, 0, marker.Length);
                    
                    // Write IV
                    outputFileStream.Write(aes.IV, 0, aes.IV.Length);
                    
                    // Write encrypted data
                    byte[] licenseData = Encoding.UTF8.GetBytes(json);
                    using (var encryptor = aes.CreateEncryptor())
                    using (var cryptoStream = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(licenseData, 0, licenseData.Length);
                    }
                }
            }
            
            // Test loading the encrypted license
            bool result = LicenseManagerExtensions.Initialize(licensePath);
            
            // Assert
            Assert.True(result);
            
            // Cleanup
            if (File.Exists(licensePath))
            {
                File.Delete(licensePath);
            }
        }

        #region Test Helpers

        private void CreateTestLicenseFiles()
        {
            try
            {
                // Create a valid license object
                var license = CreateValidLicense();
                
                // Save as JSON with proper serialization options
                var options = new JsonSerializerOptions { 
                    WriteIndented = true,
                    PropertyNamingPolicy = null, // Use exact property names
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                string json = JsonSerializer.Serialize(license, options);
                File.WriteAllText(_decryptedLicensePath, json);
                
                // Also create an encrypted version - make sure we're using the correct format
                using (var aes = Aes.Create())
                {
                    aes.Key = _transportKey;
                    aes.GenerateIV();
                    
                    using (var outputFileStream = new FileStream(_encryptedLicensePath, FileMode.Create))
                    {
                        // Write marker "WDLENC" to identify this as an encrypted license file
                        byte[] marker = Encoding.ASCII.GetBytes("WDLENC");
                        outputFileStream.Write(marker, 0, marker.Length);
                        
                        // Write the IV (16 bytes)
                        outputFileStream.Write(aes.IV, 0, aes.IV.Length);
                        
                        // Encrypt the license data
                        byte[] licenseData = Encoding.UTF8.GetBytes(json);
                        
                        using (var encryptor = aes.CreateEncryptor())
                        using (var memoryStream = new MemoryStream())
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(licenseData, 0, licenseData.Length);
                            cryptoStream.FlushFinalBlock();
                            
                            byte[] encryptedData = memoryStream.ToArray();
                            outputFileStream.Write(encryptedData, 0, encryptedData.Length);
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Assert.Fail($"Failed to create test license files: {ex.Message}");
            }
        }

        private LicenseInfo CreateValidLicense()
        {
            var license = new LicenseInfo
            {
                LicenseKey = "TEST-ENCRYPTED-LICENSE-12345",
                CustomerName = "Encrypted License Customer",
                CustomerEmail = "encrypted@example.com",
                IssueDate = DateTime.Now.AddDays(-1),
                ExpiryDate = DateTime.Now.AddYears(1),
                Products = new List<ProductType> { ProductType.DotPdf, ProductType.DotTex2 },
                Version = 1
            };

            // Generate signature and integrity checksum for a valid license
            var signingKey = GetSigningKey();
            license.Signature = GenerateLicenseSignature(license.LicenseKey, signingKey);
            license.IntegrityChecksum = GenerateIntegrityChecksum(license, signingKey);
            
            return license;
        }

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

        private static string GenerateLicenseSignature(string licenseKey, byte[] signingKey)
        {
            using (var hmac = new HMACSHA256(signingKey))
            {
                byte[] signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(licenseKey));
                return Convert.ToBase64String(signature);
            }
        }

        private static string GenerateIntegrityChecksum(LicenseInfo license, byte[] signingKey)
        {
            string dataToHash = $"{license.LicenseKey}|{license.CustomerName}|{license.CustomerEmail}|" +
                               $"{license.IssueDate.ToBinary()}|{license.ExpiryDate.ToBinary()}|" +
                               $"{string.Join(",", license.Products)}|{license.Signature}";
            
            using (var hmac = new HMACSHA256(signingKey))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                return Convert.ToBase64String(hash);
            }
        }

        #endregion
    }
} 