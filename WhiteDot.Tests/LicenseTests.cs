using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Xunit;
using WhiteDot.Licensing;

namespace WhiteDot.Tests
{
    public class LicenseTests : IDisposable
    {
        private readonly string _testLicensePath;
        private readonly string _testInvalidLicensePath;

        public LicenseTests()
        {
            // Set up test license files with unique names to avoid file access conflicts
            _testLicensePath = Path.Combine(Path.GetTempPath(), $"test-license-{Guid.NewGuid()}.json");
            _testInvalidLicensePath = Path.Combine(Path.GetTempPath(), $"invalid-license-{Guid.NewGuid()}.json");
            
            // Create test license files
            CreateTestLicenseFile();
            CreateInvalidLicenseFile();
        }

        public void Dispose()
        {
            // Clean up after tests
            CleanupFiles();
        }

        // Cleanup method to be called from Dispose and also from test methods if needed
        private void CleanupFiles()
        {
            if (File.Exists(_testLicensePath))
            {
                try
                {
                    File.Delete(_testLicensePath);
                }
                catch (IOException)
                {
                    // Ignore file access errors during cleanup
                }
            }

            if (File.Exists(_testInvalidLicensePath))
            {
                try
                {
                    File.Delete(_testInvalidLicensePath);
                }
                catch (IOException)
                {
                    // Ignore file access errors during cleanup
                }
            }
        }

        [Fact]
        public void Initialize_WithValidLicense_ReturnsTrue()
        {
            // Act
            bool result = LicenseManagerExtensions.Initialize(_testLicensePath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Initialize_WithInvalidLicense_ReturnsFalse()
        {
            // Act
            bool result = LicenseManagerExtensions.Initialize(_testInvalidLicensePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Initialize_WithNonExistentFile_ReturnsFalse()
        {
            // Act
            bool result = LicenseManagerExtensions.Initialize("non-existent-file.json");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateLicense_WithValidProduct_ReturnsTrue()
        {
            // Arrange
            LicenseManagerExtensions.Initialize(_testLicensePath);

            // Act
            bool result = LicenseManagerExtensions.IsProductLicensed(
                LicenseManager.Instance, ProductType.DotPdf);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateLicense_WithInvalidProduct_ReturnsFalse()
        {
            try
            {
                // Arrange - Create a test license file with only DotPdf
                var license = new LicenseInfo
                {
                    LicenseKey = "TEST-LICENSE-KEY-12345",
                    CustomerName = "Test Customer",
                    CustomerEmail = "test@example.com",
                    IssueDate = DateTime.Now.AddDays(-1),
                    ExpiryDate = DateTime.Now.AddYears(1),
                    Products = new List<ProductType> { ProductType.DotPdf },
                    Version = 1
                };

                var signingKey = GetSigningKey();
                
                // First generate the signature
                license.Signature = GenerateLicenseSignature(license.LicenseKey, signingKey);
                
                // Then generate the integrity checksum which includes the signature
                license.IntegrityChecksum = GenerateIntegrityChecksum(license, signingKey);

                var tempPath = Path.Combine(Path.GetTempPath(), $"product-test-{Guid.NewGuid()}.json");
                
                // Use JsonSerializerOptions to ensure proper serialization
                var options = new JsonSerializerOptions { 
                    WriteIndented = true,
                    PropertyNamingPolicy = null, // Use exact property names
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                string json = JsonSerializer.Serialize(license, options);
                File.WriteAllText(tempPath, json);

                // Act - Initialize license and check if DotTex2 is licensed (shouldn't be)
                bool initResult = LicenseManagerExtensions.Initialize(tempPath);
                Assert.True(initResult, "License initialization failed");
                
                // Verify DotPdf is licensed (should be)
                bool dotPdfResult = LicenseManagerExtensions.IsProductLicensed(
                    LicenseManager.Instance, ProductType.DotPdf);
                Assert.True(dotPdfResult, "DotPdf should be licensed");
                
                // Verify DotTex2 is not licensed (shouldn't be)
                bool dotTex2Result = LicenseManagerExtensions.IsProductLicensed(
                    LicenseManager.Instance, ProductType.DotTex2);
                Assert.False(dotTex2Result, "DotTex2 should not be licensed");

                // Cleanup
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (IOException ex)
            {
                Assert.Fail($"Test failed due to file access error: {ex.Message}");
            }
        }

        [Fact]
        public void LicenseSignature_IsCorrectlyValidated()
        {
            try
            {
                // Create a simple valid license file
                var tempPath = Path.Combine(Path.GetTempPath(), $"signature-test-{Guid.NewGuid()}.json");
                
                // Create a license with a valid signature
                var license = new LicenseInfo
                {
                    LicenseKey = "SIGNATURE-TEST-LICENSE-KEY",
                    CustomerName = "Signature Test Customer",
                    CustomerEmail = "signature@example.com",
                    IssueDate = DateTime.Now.AddDays(-1),
                    ExpiryDate = DateTime.Now.AddYears(1),
                    Products = new List<ProductType> { ProductType.DotPdf, ProductType.DotTex2 },
                    Version = 1
                };
                
                // Generate a valid signature
                var signingKey = GetSigningKey();
                license.Signature = GenerateLicenseSignature(license.LicenseKey, signingKey);
                license.IntegrityChecksum = GenerateIntegrityChecksum(license, signingKey);
                
                // Serialize with proper options
                var options = new JsonSerializerOptions { 
                    WriteIndented = true,
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                string json = JsonSerializer.Serialize(license, options);
                File.WriteAllText(tempPath, json);
                
                // Act - Initialize the license
                bool result = LicenseManagerExtensions.Initialize(tempPath);
                
                // Assert
                Assert.True(result, "License initialization should succeed with a valid signature");
                
                // Cleanup
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed: {ex.Message}");
            }
        }

        [Fact]
        public void LicenseIntegrity_IsCorrectlyValidated()
        {
            try
            {
                // Arrange - Create a test license file with valid integrity
                var license = new LicenseInfo
                {
                    LicenseKey = "TEST-LICENSE-KEY-12345",
                    CustomerName = "Test Customer",
                    CustomerEmail = "test@example.com",
                    IssueDate = DateTime.Now.AddDays(-1),
                    ExpiryDate = DateTime.Now.AddYears(1),
                    Products = new List<ProductType> { ProductType.DotPdf },
                    Version = 1
                };

                var signingKey = GetSigningKey();
                
                // First generate the signature
                license.Signature = GenerateLicenseSignature(license.LicenseKey, signingKey);
                
                // Then generate the integrity checksum which includes the signature
                license.IntegrityChecksum = GenerateIntegrityChecksum(license, signingKey);

                var tempPath = Path.Combine(Path.GetTempPath(), $"integrity-test-{Guid.NewGuid()}.json");
                
                // Use JsonSerializerOptions to ensure proper serialization
                var options = new JsonSerializerOptions { 
                    WriteIndented = true,
                    PropertyNamingPolicy = null, // Use exact property names
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                string json = JsonSerializer.Serialize(license, options);
                File.WriteAllText(tempPath, json);

                // Act
                bool result = LicenseManagerExtensions.Initialize(tempPath);
                
                // Assert
                Assert.True(result, "License initialization failed");
                
                if (result)
                {
                    var loadedLicense = LicenseManager.Instance.GetCurrentLicense();
                    Assert.NotNull(loadedLicense);
                    Assert.False(loadedLicense.IsTampered, "License integrity validation failed");
                }

                // Cleanup
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (IOException ex)
            {
                Assert.Fail($"Test failed due to file access error: {ex.Message}");
            }
        }

        [Fact]
        public void ExpiredLicense_IsDetected()
        {
            // Arrange - Create an expired license
            var expiredLicensePath = Path.Combine(Path.GetTempPath(), $"expired-license-{Guid.NewGuid()}.json");
            CreateExpiredLicenseFile(expiredLicensePath);

            try
            {
                // Act
                bool result = LicenseManagerExtensions.Initialize(expiredLicensePath);

                // Assert
                Assert.False(result);
            }
            finally
            {
                // Cleanup
                if (File.Exists(expiredLicensePath))
                {
                    try
                    {
                        File.Delete(expiredLicensePath);
                    }
                    catch (IOException)
                    {
                        // Ignore file access errors during cleanup
                    }
                }
            }
        }

        #region Test Helpers

        private void CreateTestLicenseFile()
        {
            try
            {
                var license = new LicenseInfo
                {
                    LicenseKey = "TEST-LICENSE-KEY-12345",
                    CustomerName = "Test Customer",
                    CustomerEmail = "test@example.com",
                    IssueDate = DateTime.Now.AddDays(-1),
                    ExpiryDate = DateTime.Now.AddYears(1),
                    Products = new List<ProductType> { ProductType.DotPdf },
                    Version = 1
                };

                // Generate signature and integrity checksum for a valid license
                var signingKey = GetSigningKey();
                license.Signature = GenerateLicenseSignature(license.LicenseKey, signingKey);
                license.IntegrityChecksum = GenerateIntegrityChecksum(license, signingKey);

                string json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_testLicensePath, json);
            }
            catch (IOException ex)
            {
                Assert.Fail($"Failed to create test license file: {ex.Message}");
            }
        }

        private void CreateInvalidLicenseFile()
        {
            var license = new LicenseInfo
            {
                LicenseKey = "INVALID-LICENSE-KEY",
                CustomerName = "Test Customer",
                CustomerEmail = "test@example.com",
                IssueDate = DateTime.Now.AddDays(-1),
                ExpiryDate = DateTime.Now.AddYears(1),
                Products = new List<ProductType> { ProductType.DotPdf },
                Version = 1,
                // Invalid signature and checksum
                Signature = "InvalidSignature",
                IntegrityChecksum = "InvalidChecksum"
            };

            string json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_testInvalidLicensePath, json);
        }

        private void CreateExpiredLicenseFile(string path)
        {
            var license = new LicenseInfo
            {
                LicenseKey = "EXPIRED-LICENSE-KEY",
                CustomerName = "Test Customer",
                CustomerEmail = "test@example.com",
                IssueDate = DateTime.Now.AddYears(-2),
                ExpiryDate = DateTime.Now.AddDays(-1), // Expired
                Products = new List<ProductType> { ProductType.DotPdf },
                Version = 1
            };

            // Generate valid signature and integrity checksum
            var signingKey = GetSigningKey();
            license.Signature = GenerateLicenseSignature(license.LicenseKey, signingKey);
            license.IntegrityChecksum = GenerateIntegrityChecksum(license, signingKey);

            string json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
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
            // This must match exactly the format in LicenseManager and LicenseManagerExtensions
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