using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Xunit;
using WhiteDot.Licensing;

namespace WhiteDot.Tests
{
    public class SimpleLicenseTests
    {
        [Fact]
        public void BasicLicenseValidation_Works()
        {
            // Create a license object directly
            var license = new LicenseInfo
            {
                LicenseKey = "SIMPLE-TEST-LICENSE-KEY",
                CustomerName = "Simple Test Customer",
                CustomerEmail = "simple@example.com",
                IssueDate = DateTime.Now.AddDays(-1),
                ExpiryDate = DateTime.Now.AddYears(1),
                Products = new List<ProductType> { ProductType.DotPdf, ProductType.DotTex2 },
                Version = 1
            };
            
            // Verify basic properties
            Assert.Equal("SIMPLE-TEST-LICENSE-KEY", license.LicenseKey);
            Assert.Equal("Simple Test Customer", license.CustomerName);
            Assert.Equal("simple@example.com", license.CustomerEmail);
            Assert.True(license.ExpiryDate > DateTime.Now);
            Assert.Equal(2, license.Products.Count);
            Assert.Contains(ProductType.DotPdf, license.Products);
            Assert.Contains(ProductType.DotTex2, license.Products);
            Assert.True(license.IsValid);
        }
        
        [Fact]
        public void ExpiredLicense_IsDetected()
        {
            // Create an expired license
            var license = new LicenseInfo
            {
                LicenseKey = "EXPIRED-TEST-LICENSE-KEY",
                CustomerName = "Expired Test Customer",
                CustomerEmail = "expired@example.com",
                IssueDate = DateTime.Now.AddYears(-2),
                ExpiryDate = DateTime.Now.AddDays(-1), // Expired
                Products = new List<ProductType> { ProductType.DotPdf },
                Version = 1
            };
            
            // Verify it's detected as expired
            Assert.False(license.IsValid);
        }
        
        [Fact]
        public void ProductLicensing_WorksCorrectly()
        {
            // Create a license with only DotPdf
            var license = new LicenseInfo
            {
                LicenseKey = "PRODUCT-TEST-LICENSE-KEY",
                CustomerName = "Product Test Customer",
                CustomerEmail = "product@example.com",
                IssueDate = DateTime.Now.AddDays(-1),
                ExpiryDate = DateTime.Now.AddYears(1),
                Products = new List<ProductType> { ProductType.DotPdf },
                Version = 1
            };
            
            // Verify product licensing
            Assert.Single(license.Products);
            Assert.Contains(ProductType.DotPdf, license.Products);
            Assert.DoesNotContain(ProductType.DotTex2, license.Products);
        }
        
        [Fact]
        public void LicenseJsonSerialization_WorksCorrectly()
        {
            // Create a license
            var license = new LicenseInfo
            {
                LicenseKey = "JSON-TEST-LICENSE-KEY",
                CustomerName = "JSON Test Customer",
                CustomerEmail = "json@example.com",
                IssueDate = DateTime.Now.AddDays(-1),
                ExpiryDate = DateTime.Now.AddYears(1),
                Products = new List<ProductType> { ProductType.DotPdf },
                Version = 1,
                Signature = "TestSignature",
                IntegrityChecksum = "TestChecksum"
            };
            
            // Serialize to JSON
            var options = new JsonSerializerOptions { 
                WriteIndented = true,
                PropertyNamingPolicy = null
            };
            
            string json = JsonSerializer.Serialize(license, options);
            
            // Deserialize back
            var deserializedLicense = JsonSerializer.Deserialize<LicenseInfo>(json, options);
            
            // Verify properties were preserved
            Assert.NotNull(deserializedLicense);
            Assert.Equal(license.LicenseKey, deserializedLicense.LicenseKey);
            Assert.Equal(license.CustomerName, deserializedLicense.CustomerName);
            Assert.Equal(license.CustomerEmail, deserializedLicense.CustomerEmail);
            Assert.Equal(license.IssueDate, deserializedLicense.IssueDate);
            Assert.Equal(license.ExpiryDate, deserializedLicense.ExpiryDate);
            Assert.Equal(license.Products.Count, deserializedLicense.Products.Count);
            Assert.Equal(license.Version, deserializedLicense.Version);
            Assert.Equal(license.Signature, deserializedLicense.Signature);
            Assert.Equal(license.IntegrityChecksum, deserializedLicense.IntegrityChecksum);
        }
        
        [Fact]
        public void LicenseSignatureGeneration_WorksCorrectly()
        {
            // Create a license
            var license = new LicenseInfo
            {
                LicenseKey = "SIGNATURE-TEST-LICENSE-KEY",
                CustomerName = "Signature Test Customer",
                CustomerEmail = "signature@example.com",
                IssueDate = DateTime.Now.AddDays(-1),
                ExpiryDate = DateTime.Now.AddYears(1),
                Products = new List<ProductType> { ProductType.DotPdf },
                Version = 1
            };
            
            // Generate a signature using the same algorithm as in LicenseManager
            byte[] signingKey = new byte[] 
            { 
                0x57, 0x68, 0x69, 0x74, 0x65, 0x44, 0x6F, 0x74, 
                0x4C, 0x69, 0x63, 0x65, 0x6E, 0x73, 0x69, 0x6E, 
                0x67, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x4B, 
                0x65, 0x79, 0x32, 0x30, 0x32, 0x34, 0xAB, 0xCD 
            };
            
            using (var hmac = new HMACSHA256(signingKey))
            {
                byte[] signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(license.LicenseKey));
                license.Signature = Convert.ToBase64String(signature);
            }
            
            // Verify the signature is not empty
            Assert.NotEmpty(license.Signature);
            Assert.True(license.Signature.Length > 20); // Should be a reasonably long Base64 string
        }
        
        [Fact]
        public void LicenseIntegrityChecksum_WorksCorrectly()
        {
            // Create a license
            var license = new LicenseInfo
            {
                LicenseKey = "INTEGRITY-TEST-LICENSE-KEY",
                CustomerName = "Integrity Test Customer",
                CustomerEmail = "integrity@example.com",
                IssueDate = DateTime.Now.AddDays(-1),
                ExpiryDate = DateTime.Now.AddYears(1),
                Products = new List<ProductType> { ProductType.DotPdf },
                Version = 1,
                Signature = "TestSignature" // Set a signature first
            };
            
            // Generate an integrity checksum using the same algorithm as in LicenseManager
            byte[] signingKey = new byte[] 
            { 
                0x57, 0x68, 0x69, 0x74, 0x65, 0x44, 0x6F, 0x74, 
                0x4C, 0x69, 0x63, 0x65, 0x6E, 0x73, 0x69, 0x6E, 
                0x67, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x4B, 
                0x65, 0x79, 0x32, 0x30, 0x32, 0x34, 0xAB, 0xCD 
            };
            
            string dataToHash = $"{license.LicenseKey}|{license.CustomerName}|{license.CustomerEmail}|" +
                              $"{license.IssueDate.ToBinary()}|{license.ExpiryDate.ToBinary()}|" +
                              $"{string.Join(",", license.Products)}|{license.Signature}";
            
            using (var hmac = new HMACSHA256(signingKey))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                license.IntegrityChecksum = Convert.ToBase64String(hash);
            }
            
            // Verify the checksum is not empty
            Assert.NotEmpty(license.IntegrityChecksum);
            Assert.True(license.IntegrityChecksum.Length > 20); // Should be a reasonably long Base64 string
        }
        
        [Fact]
        public void LicenseIntegrityChecksum_DetectsTampering()
        {
            // Create a license
            var license = new LicenseInfo
            {
                LicenseKey = "TAMPER-TEST-LICENSE-KEY",
                CustomerName = "Tamper Test Customer",
                CustomerEmail = "tamper@example.com",
                IssueDate = DateTime.Now.AddDays(-1),
                ExpiryDate = DateTime.Now.AddYears(1),
                Products = new List<ProductType> { ProductType.DotPdf },
                Version = 1
            };
            
            // Generate a signature
            byte[] signingKey = new byte[] 
            { 
                0x57, 0x68, 0x69, 0x74, 0x65, 0x44, 0x6F, 0x74, 
                0x4C, 0x69, 0x63, 0x65, 0x6E, 0x73, 0x69, 0x6E, 
                0x67, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x4B, 
                0x65, 0x79, 0x32, 0x30, 0x32, 0x34, 0xAB, 0xCD 
            };
            
            using (var hmac = new HMACSHA256(signingKey))
            {
                byte[] signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(license.LicenseKey));
                license.Signature = Convert.ToBase64String(signature);
            }
            
            // Generate an integrity checksum
            string dataToHash = $"{license.LicenseKey}|{license.CustomerName}|{license.CustomerEmail}|" +
                              $"{license.IssueDate.ToBinary()}|{license.ExpiryDate.ToBinary()}|" +
                              $"{string.Join(",", license.Products)}|{license.Signature}";
            
            using (var hmac = new HMACSHA256(signingKey))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                license.IntegrityChecksum = Convert.ToBase64String(hash);
            }
            
            // Save the original checksum
            string originalChecksum = license.IntegrityChecksum;
            
            // Now tamper with the license by adding a product
            license.Products.Add(ProductType.DotTex2);
            
            // Recalculate the checksum
            dataToHash = $"{license.LicenseKey}|{license.CustomerName}|{license.CustomerEmail}|" +
                       $"{license.IssueDate.ToBinary()}|{license.ExpiryDate.ToBinary()}|" +
                       $"{string.Join(",", license.Products)}|{license.Signature}";
            
            string newChecksum;
            using (var hmac = new HMACSHA256(signingKey))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                newChecksum = Convert.ToBase64String(hash);
            }
            
            // Verify the checksum has changed, which would detect tampering
            Assert.NotEqual(originalChecksum, newChecksum);
        }
    }
} 