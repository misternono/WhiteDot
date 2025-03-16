using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Xunit;
using WhiteDot.Licensing;

namespace WhiteDot.Tests
{
    public class LicenseSecurityTests : IDisposable
    {
        private readonly string _baseLicensePath;

        public LicenseSecurityTests()
        {
            _baseLicensePath = Path.Combine(Path.GetTempPath(), $"security-test-license-{Guid.NewGuid()}.json");
        }

        public void Dispose()
        {
            // Clean up test files
            if (File.Exists(_baseLicensePath))
            {
                try
                {
                    File.Delete(_baseLicensePath);
                }
                catch (IOException)
                {
                    // Ignore file access errors during cleanup
                }
            }
        }

        [Fact]
        public void ManuallyModifiedLicense_ExpiredDate_IsDetected()
        {
            try
            {
                // Arrange
                var licenseWithFutureExpiryJson = CreateLicenseJson(DateTime.Now.AddYears(1));
                File.WriteAllText(_baseLicensePath, licenseWithFutureExpiryJson);
                
                // Act - first confirm the license is valid
                bool initialResult = LicenseManagerExtensions.Initialize(_baseLicensePath);
                Assert.True(initialResult, "The initial license should be valid");
                
                // Now manually modify the JSON to change the expiry date
                var jsonDoc = JsonDocument.Parse(licenseWithFutureExpiryJson);
                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream))
                    {
                        writer.WriteStartObject();
                        foreach (var property in jsonDoc.RootElement.EnumerateObject())
                        {
                            if (property.Name == "ExpiryDate")
                            {
                                // Maliciously modify the expiry date to be far in the future
                                writer.WriteString("ExpiryDate", DateTime.Now.AddYears(100).ToString("o"));
                            }
                            else
                            {
                                property.WriteTo(writer);
                            }
                        }
                        writer.WriteEndObject();
                    }
                    
                    string tamperedJson = Encoding.UTF8.GetString(stream.ToArray());
                    
                    // Write to a different file to avoid file access conflicts
                    string tamperedPath = Path.Combine(Path.GetTempPath(), $"tampered-expiry-{Guid.NewGuid()}.json");
                    File.WriteAllText(tamperedPath, tamperedJson);
                    
                    // Act - attempt to load the tampered license
                    bool tamperedResult = LicenseManagerExtensions.Initialize(tamperedPath);
                    
                    // Assert - the tampered license should be detected
                    Assert.False(tamperedResult, "The license with a tampered expiry date should be detected as invalid");
                    
                    // Cleanup
                    if (File.Exists(tamperedPath))
                    {
                        File.Delete(tamperedPath);
                    }
                }
            }
            catch (IOException ex)
            {
                Assert.Fail($"File access error: {ex.Message}");
            }
        }

        [Fact]
        public void ManuallyModifiedLicense_AddedProduct_IsDetected()
        {
            try
            {
                // Arrange - Create a license with only DotPdf
                var validLicense = new LicenseInfo
                {
                    LicenseKey = "SECURITY-TEST-LICENSE-KEY",
                    CustomerName = "Security Test Customer",
                    CustomerEmail = "security@example.com",
                    IssueDate = DateTime.Now.AddDays(-1),
                    ExpiryDate = DateTime.Now.AddYears(1),
                    Products = new List<ProductType> { ProductType.DotPdf },
                    Version = 1
                };
                
                // Generate signature and integrity checksum for a valid license
                var signingKey = GetSigningKey();
                validLicense.Signature = GenerateLicenseSignature(validLicense.LicenseKey, signingKey);
                validLicense.IntegrityChecksum = GenerateIntegrityChecksum(validLicense, signingKey);
                
                // Serialize the valid license
                var options = new JsonSerializerOptions { 
                    WriteIndented = true,
                    PropertyNamingPolicy = null, // Use exact property names
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                string validLicenseJson = JsonSerializer.Serialize(validLicense, options);
                File.WriteAllText(_baseLicensePath, validLicenseJson);
                
                // Act - first confirm the license is valid for DotPdf but not DotTex2
                bool initialResult = LicenseManagerExtensions.Initialize(_baseLicensePath);
                Assert.True(initialResult, "The initial license should be valid");

                // Verify the correct products are licensed
                bool dotPdfLicensed = LicenseManagerExtensions.IsProductLicensed(LicenseManager.Instance, ProductType.DotPdf);
                bool dotTex2Licensed = LicenseManagerExtensions.IsProductLicensed(LicenseManager.Instance, ProductType.DotTex2);
                
                Assert.True(dotPdfLicensed, "DotPdf should be licensed");
                Assert.False(dotTex2Licensed, "DotTex2 should not be licensed");
                
                // Now create a tampered license that adds DotTex2 to the Products array
                string tamperedLicensePath = Path.Combine(Path.GetTempPath(), $"tampered-license-{Guid.NewGuid()}.json");
                
                // Create a tampered version by manually modifying the JSON
                string tamperedJson = validLicenseJson.Replace("\"Products\": [", "\"Products\": [0,");
                File.WriteAllText(tamperedLicensePath, tamperedJson);
                
                // Act - attempt to load the tampered license
                bool tamperedResult = LicenseManagerExtensions.Initialize(tamperedLicensePath);
                
                // Assert - the tampered license should be detected
                Assert.False(tamperedResult, "The license with tampered Products list should be detected as invalid");
                
                // Cleanup
                if (File.Exists(tamperedLicensePath))
                {
                    File.Delete(tamperedLicensePath);
                }
            }
            catch (IOException ex)
            {
                Assert.Fail($"File access error: {ex.Message}");
            }
        }

        [Fact]
        public void ManuallyModifiedLicense_IsValidProperty_IsDetected()
        {
            try
            {
                // This test verifies that the IsValid property can't be manipulated
                // in the JSON since it's computed dynamically
                
                // Arrange - Create a license that's expired
                var expiredLicenseJson = CreateLicenseJson(DateTime.Now.AddDays(-1));
                
                // Try to inject a malicious IsValid property
                string tamperedJson = expiredLicenseJson.Replace("\"ExpiryDate\":", "\"IsValid\":true,\"ExpiryDate\":");
                File.WriteAllText(_baseLicensePath, tamperedJson);
                
                // Act - attempt to load the tampered license
                bool result = LicenseManagerExtensions.Initialize(_baseLicensePath);
                
                // Assert - the license should still be detected as invalid
                Assert.False(result, "The expired license with injected IsValid property should still be detected as invalid");
            }
            catch (IOException ex)
            {
                Assert.Fail($"File access error: {ex.Message}");
            }
        }

        #region Test Helpers

        private string CreateLicenseJson(DateTime expiryDate, ProductType[] products = null)
        {
            products ??= new[] { ProductType.DotPdf, ProductType.DotTex2 };
            
            var license = new LicenseInfo
            {
                LicenseKey = "SECURITY-TEST-LICENSE-KEY",
                CustomerName = "Security Test Customer",
                CustomerEmail = "security@example.com",
                IssueDate = DateTime.Now.AddDays(-1),
                ExpiryDate = expiryDate,
                Products = new List<ProductType>(products),
                Version = 1
            };

            // Generate signature and integrity checksum for a valid license
            var signingKey = GetSigningKey();
            
            // First generate the signature
            license.Signature = GenerateLicenseSignature(license.LicenseKey, signingKey);
            
            // Then generate the integrity checksum which includes the signature
            license.IntegrityChecksum = GenerateIntegrityChecksum(license, signingKey);

            // Use JsonSerializerOptions to ensure proper serialization
            var options = new JsonSerializerOptions { 
                WriteIndented = true,
                PropertyNamingPolicy = null, // Use exact property names
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            
            return JsonSerializer.Serialize(license, options);
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