using System;
using System.CommandLine;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WhiteDot.Licensing;

namespace WhiteDot.LicenseGenerator
{
    // This internal class mimics the LicenseInfo class to generate compatible licenses
    internal class LicenseGenerator
    {
        // Embedded signing key - MUST match the one in WhiteDot.Licensing.LicenseManager
        private static readonly byte[] SigningKey = new byte[] 
        { 
            0x57, 0x68, 0x69, 0x74, 0x65, 0x44, 0x6F, 0x74, 
            0x4C, 0x69, 0x63, 0x65, 0x6E, 0x73, 0x69, 0x6E, 
            0x67, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x4B, 
            0x65, 0x79, 0x32, 0x30, 0x32, 0x34, 0xAB, 0xCD 
        };

        // Generate a random license key with a format like XXXXX-XXXXX-XXXXX-XXXXX-XXXXX
        public static string GenerateLicenseKey()
        {
            const string validChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Omitting confusing chars like I, O, 0, 1
            var random = new Random();
            var segments = new string[5];
            
            for (int s = 0; s < 5; s++)
            {
                var segmentChars = new char[5];
                for (int i = 0; i < 5; i++)
                {
                    segmentChars[i] = validChars[random.Next(validChars.Length)];
                }
                segments[s] = new string(segmentChars);
            }
            
            return string.Join("-", segments);
        }

        // Calculate a license signature using HMACSHA256
        public static string GenerateLicenseSignature(string licenseKey)
        {
            using (var hmac = new HMACSHA256(SigningKey))
            {
                byte[] signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(licenseKey));
                return Convert.ToBase64String(signature);
            }
        }

        // Generate a checksum for the license integrity verification
        public static string GenerateIntegrityChecksum(string licenseKey, string customerName, 
                                                      string customerEmail, DateTime issueDate,
                                                      DateTime expiryDate, List<ProductType> products,
                                                      string signature)
        {
            // Create a checksum of critical license fields
            string dataToHash = $"{licenseKey}|{customerName}|{customerEmail}|" +
                               $"{issueDate.ToBinary()}|{expiryDate.ToBinary()}|" +
                               $"{string.Join(",", products)}|{signature}";
            
            using (var hmac = new HMACSHA256(SigningKey))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
                return Convert.ToBase64String(hash);
            }
        }

        // Create a complete license and export it to a file
        public static void GenerateAndExportLicense(string customerName, string customerEmail, 
                                                   int durationYears, bool includeDotTex2, 
                                                   bool includeDotPdf, string outputPath,
                                                   bool encrypt)
        {
            // Generate license key
            string licenseKey = GenerateLicenseKey();
            
            // Create product list
            var products = new List<ProductType>();
            if (includeDotTex2) products.Add(ProductType.DotTex2);
            if (includeDotPdf) products.Add(ProductType.DotPdf);
            
            // Set dates
            DateTime issueDate = DateTime.Now;
            DateTime expiryDate = issueDate.AddYears(durationYears);
            
            // Generate signature
            string signature = GenerateLicenseSignature(licenseKey);
            
            // Create license info
            var license = new LicenseInfo
            {
                LicenseKey = licenseKey,
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                IssueDate = issueDate,
                ExpiryDate = expiryDate,
                Products = products,
                Signature = signature
            };
            
            // Generate integrity checksum
            license.IntegrityChecksum = GenerateIntegrityChecksum(
                license.LicenseKey, license.CustomerName, license.CustomerEmail,
                license.IssueDate, license.ExpiryDate, license.Products, license.Signature);
            
            // Serialize to JSON
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(license, options);
            
            // Export to file - either as plain JSON or encrypted
            if (encrypt)
            {
                string encryptedPath = outputPath;
                if (!encryptedPath.EndsWith(".lic"))
                {
                    encryptedPath = Path.ChangeExtension(encryptedPath, "lic");
                }
                
                LicenseFileEncryptor.EncryptLicenseFile(json, encryptedPath);
                outputPath = encryptedPath;
            }
            else
            {
                File.WriteAllText(outputPath, json);
                Console.WriteLine($"License file created at: {Path.GetFullPath(outputPath)}");
            }
            
            // Generate activation code
            string activationCode = LicenseFileEncryptor.GenerateActivationCode(licenseKey, customerEmail);
            
            // Display information
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("License generated successfully:");
            Console.ResetColor();
            Console.WriteLine($"License Key: {licenseKey}");
            Console.WriteLine($"Activation Code: {activationCode}");
            Console.WriteLine($"Customer: {customerName}");
            Console.WriteLine($"Email: {customerEmail}");
            Console.WriteLine($"Issue Date: {issueDate:yyyy-MM-dd}");
            Console.WriteLine($"Expiry Date: {expiryDate:yyyy-MM-dd}");
            Console.WriteLine($"Products: {string.Join(", ", products)}");
            Console.WriteLine($"Output File: {Path.GetFullPath(outputPath)}");
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("INSTRUCTIONS FOR CUSTOMER");
            Console.WriteLine("------------------------");
            Console.ResetColor();
            Console.WriteLine("Provide the customer with the following information:");
            Console.WriteLine($"1. The license key: {licenseKey}");
            Console.WriteLine($"2. The activation code: {activationCode}");
            Console.WriteLine($"3. The license file at: {Path.GetFullPath(outputPath)}");
            Console.WriteLine();
            Console.WriteLine("They should either:");
            Console.WriteLine("a) Run the license tool with: WhiteDot.LicenseTool register <license-key>");
            Console.WriteLine("   - or -");
            Console.WriteLine("b) Enter the license key when prompted by the application");
        }
    }

    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Create root command
            var rootCommand = new RootCommand("WhiteDot License Generator");
            
            // Add options for all license parameters
            var customerNameOption = new Option<string>(
                "--name",
                description: "Customer name")
            { IsRequired = true };
            
            var customerEmailOption = new Option<string>(
                "--email",
                description: "Customer email")
            { IsRequired = true };
            
            var durationYearsOption = new Option<int>(
                "--years",
                description: "License duration in years",
                getDefaultValue: () => 1);
            
            var includeDotTex2Option = new Option<bool>(
                "--dot-tex2",
                description: "Include DotTex2 license",
                getDefaultValue: () => false);
            
            var includeDotPdfOption = new Option<bool>(
                "--dot-pdf",
                description: "Include DotPdf license",
                getDefaultValue: () => false);
            
            var outputPathOption = new Option<string>(
                "--output",
                description: "Output file path",
                getDefaultValue: () => "license.json");
                
            var encryptOption = new Option<bool>(
                "--encrypt",
                description: "Encrypt the license file for secure distribution",
                getDefaultValue: () => true);
            
            // Add options to command
            rootCommand.AddOption(customerNameOption);
            rootCommand.AddOption(customerEmailOption);
            rootCommand.AddOption(durationYearsOption);
            rootCommand.AddOption(includeDotTex2Option);
            rootCommand.AddOption(includeDotPdfOption);
            rootCommand.AddOption(outputPathOption);
            rootCommand.AddOption(encryptOption);
            
            // Setup the command handler using action
            rootCommand.SetHandler((string name, string email, int years, bool dotTex2, bool dotPdf, string output, bool encrypt) => 
            {
                int exitCode = ProcessLicenseGeneration(name, email, years, dotTex2, dotPdf, output, encrypt);
                Environment.ExitCode = exitCode;
            },
            customerNameOption, customerEmailOption, durationYearsOption, 
            includeDotTex2Option, includeDotPdfOption, outputPathOption, encryptOption);
            
            // Parse and invoke
            return await rootCommand.InvokeAsync(args);
        }
        
        private static int ProcessLicenseGeneration(string name, string email, int years, 
                                                  bool dotTex2, bool dotPdf, string output, bool encrypt)
        {
            // Validate inputs
            bool isValid = true;
            
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Error: Customer name is required");
                isValid = false;
            }
            
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                Console.WriteLine("Error: Valid customer email is required");
                isValid = false;
            }
            
            if (years <= 0 || years > 10)
            {
                Console.WriteLine("Error: Duration must be between 1 and 10 years");
                isValid = false;
            }
            
            if (!dotTex2 && !dotPdf)
            {
                Console.WriteLine("Error: At least one product must be selected");
                isValid = false;
            }
            
            if (!isValid)
            {
                return 1;
            }
            
            try
            {
                // Generate the license
                LicenseGenerator.GenerateAndExportLicense(
                    name, email, years, dotTex2, dotPdf, output, encrypt);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating license: {ex.Message}");
                return 1;
            }
        }
    }
}
