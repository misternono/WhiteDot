using System;
using WhiteDot.Licensing;

namespace WhiteDot.LicenseDemo
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("WhiteDot License Demo");
            Console.WriteLine("====================");
            
            // Path to the license file generated with WhiteDot.LicenseGenerator
            // Replace this with the actual path to your license file
            string licensePath = args.Length > 0 ? args[0] : "license.json";
            
            Console.WriteLine($"Using license file: {licensePath}");
            
            try
            {
                // Initialize license using the static method
                bool licenseValid = LicenseManagerExtensions.Initialize(licensePath);
                
                if (!licenseValid)
                {
                    Console.WriteLine("License initialization failed. License is invalid or could not be loaded.");
                    return;
                }
                
                Console.WriteLine("License loaded successfully!");
                
                // Display information about the loaded license
                var license = LicenseManager.Instance.GetCurrentLicense();
                if (license != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("License Information:");
                    Console.WriteLine($"  Customer: {license.CustomerName}");
                    Console.WriteLine($"  Email: {license.CustomerEmail}");
                    Console.WriteLine($"  License Key: {license.LicenseKey}");
                    Console.WriteLine($"  Issue Date: {license.IssueDate:yyyy-MM-dd}");
                    Console.WriteLine($"  Expiry Date: {license.ExpiryDate:yyyy-MM-dd}");
                    Console.WriteLine($"  Products: {string.Join(", ", license.Products)}");
                    Console.WriteLine($"  Is Valid: {license.IsValid}");
                }
                
                // Check for specific product licenses
                Console.WriteLine();
                Console.WriteLine("Checking product licenses:");
                
                if (LicenseManagerExtensions.IsProductLicensed(LicenseManager.Instance, ProductType.DotTex2))
                {
                    Console.WriteLine("  DotTex2 is licensed - You can use DotTex2 features");
                    
                    // Example of using DotTex2 functionality
                    UseDotTex2Example();
                }
                else
                {
                    Console.WriteLine("  DotTex2 is not licensed");
                }
                
                if (LicenseManagerExtensions.IsProductLicensed(LicenseManager.Instance, ProductType.DotPdf))
                {
                    Console.WriteLine("  DotPdf is licensed - You can use DotPdf features");
                    
                    // Example of using DotPdf functionality
                    UseDotPdfExample();
                }
                else
                {
                    Console.WriteLine("  DotPdf is not licensed");
                }
            }
            catch (LicenseException ex)
            {
                Console.WriteLine($"License error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        // Example of using DotTex2 (this would normally use the actual DotTex2 APIs)
        static void UseDotTex2Example()
        {
            // This is placeholder code - in a real application you would use
            // the actual DotTex2 classes and methods
            Console.WriteLine("  [DotTex2 API] Rendering LaTeX document...");
            
            // The important part is that license validation happens automatically
            // when you use the DotTex2 API, for example:
            // var renderer = new DotTex2.LatexRenderer();
            // renderer.RenderDocument("document.tex", "output.pdf");
        }
        
        // Example of using DotPdf (this would normally use the actual DotPdf APIs)
        static void UseDotPdfExample()
        {
            // This is placeholder code - in a real application you would use
            // the actual DotPdf classes and methods
            Console.WriteLine("  [DotPdf API] Creating PDF document...");
            
            // The important part is that license validation happens automatically
            // when you use the DotPdf API, for example:
            // var document = new DotPdf.Document();
            // document.AddPage();
            // document.Save("output.pdf");
        }
    }
} 