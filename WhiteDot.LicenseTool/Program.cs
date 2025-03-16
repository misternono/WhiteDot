using WhiteDot.Licensing;

namespace WhiteDot.LicenseTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("WhiteDot License Management Tool");
            Console.WriteLine("================================");

            if (args.Length == 0)
            {
                DisplayHelp();
                return;
            }

            try
            {
                switch (args[0].ToLower())
                {
                    case "register":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("Error: License key is required.");
                            DisplayHelp();
                            return;
                        }
                        RegisterLicense(args[1]);
                        break;

                    case "info":
                        DisplayLicenseInfo();
                        break;

                    case "validate":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("Error: Product name is required.");
                            DisplayHelp();
                            return;
                        }
                        ValidateLicense(args[1]);
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {args[0]}");
                        DisplayHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void DisplayHelp()
        {
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  WhiteDot.LicenseTool register <license-key>  - Register a new license");
            Console.WriteLine("  WhiteDot.LicenseTool info                   - Display current license information");
            Console.WriteLine("  WhiteDot.LicenseTool validate <product>      - Check if a product is licensed");
            Console.WriteLine("\nAvailable products:");
            Console.WriteLine("  DotTex2");
            Console.WriteLine("  DotPdf");
        }

        static void RegisterLicense(string licenseKey)
        {
            try
            {
                LicenseManager.Instance.RegisterLicense(licenseKey);
                Console.WriteLine("License registered successfully!");
                DisplayLicenseInfo();
            }
            catch (LicenseException ex)
            {
                Console.WriteLine($"Failed to register license: {ex.Message}");
            }
        }

        static void DisplayLicenseInfo()
        {
            var license = LicenseManager.Instance.GetCurrentLicense();

            if (license == null)
            {
                Console.WriteLine("No license is currently installed.");
                return;
            }

            Console.WriteLine("\nLicense Information:");
            Console.WriteLine($"Customer: {license.CustomerName}");
            Console.WriteLine($"Email: {license.CustomerEmail}");
            Console.WriteLine($"Issue Date: {license.IssueDate:yyyy-MM-dd}");
            Console.WriteLine($"Expiry Date: {license.ExpiryDate:yyyy-MM-dd}");
            Console.WriteLine($"Status: {(license.IsValid ? "Valid" : "Expired")}");

            Console.WriteLine("\nLicensed Products:");
            foreach (var product in license.Products)
            {
                Console.WriteLine($"  - {product}");
            }
        }

        static void ValidateLicense(string productName)
        {
            if (!Enum.TryParse<ProductType>(productName, true, out var productType))
            {
                Console.WriteLine($"Invalid product name: {productName}");
                Console.WriteLine("Valid product names are: DotTex2, DotPdf");
                return;
            }

            bool isValid = LicenseManager.Instance.ValidateLicense(productType);
            
            Console.WriteLine($"License for {productName} is {(isValid ? "valid" : "invalid")}.");
            
            if (!isValid)
            {
                Console.WriteLine("Please register a valid license using the 'register' command.");
            }
        }
    }
}
