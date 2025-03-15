// See https://aka.ms/new-console-template for more information
using WhiteDot.Licensing;

try
{
    // Check for a valid license when the program starts
    LicenseManager.Instance.RequireLicense(ProductType.DotTex2);
    
    Console.WriteLine("DotTex2 is licensed and ready to use!");
    // Main program logic here...
}
catch (LicenseException ex)
{
    Console.WriteLine($"License Error: {ex.Message}");
    Console.WriteLine("Please enter a valid license key to continue:");
    string licenseKey = Console.ReadLine() ?? string.Empty;
    
    try
    {
        LicenseManager.Instance.RegisterLicense(licenseKey);
        Console.WriteLine("License registered successfully!");
        Console.WriteLine("Please restart the application to continue.");
    }
    catch (LicenseException registerEx)
    {
        Console.WriteLine($"Failed to register license: {registerEx.Message}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
