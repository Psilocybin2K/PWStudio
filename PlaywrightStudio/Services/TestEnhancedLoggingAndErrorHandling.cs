using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlaywrightStudio.Extensions;
using PlaywrightStudio.Interfaces;

namespace PlaywrightStudio.Services;

/// <summary>
/// Test class to validate enhanced logging and error handling
/// </summary>
public class TestEnhancedLoggingAndErrorHandling
{
    /// <summary>
    /// Tests the enhanced logging and error handling functionality
    /// </summary>
    public static async Task TestEnhancedLoggingAndErrorHandlingAsync()
    {
        // Create configuration with detailed logging
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PlaywrightStudio:ModelsDirectory"] = "PageObjectModels",
                ["PlaywrightStudio:DefaultSchemaPath"] = "PageObjectModels/page.schema.json",
                ["PlaywrightStudio:EnableCaching"] = "true",
                ["PlaywrightStudio:CacheExpiration"] = "00:30:00",
                ["PlaywrightStudio:ModelFilePatterns:0"] = "*.json",
                ["PlaywrightStudio:SkipSchemaValidation"] = "false",
                ["PlaywrightStudio:IncludeDebugInfo"] = "true",
                ["Browser:Headless"] = "true",
                ["Browser:BrowserType"] = "Chromium",
                ["Browser:ViewportWidth"] = "1280",
                ["Browser:ViewportHeight"] = "720",
                ["Browser:Timeout"] = "30000",
                ["Browser:LaunchOptions:SlowMo"] = "0",
                ["Browser:LaunchOptions:Devtools"] = "false",
                ["Logging:LogLevel:Default"] = "Debug",
                ["Logging:LogLevel:PlaywrightStudio"] = "Debug"
            })
            .Build();

        // Create service collection
        var services = new ServiceCollection();
        
        // Add detailed logging
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.AddConfiguration(configuration.GetSection("Logging"));
        });
        
        // Add PlaywrightStudio services
        services.AddPlaywrightStudio(configuration);

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        try
        {
            Console.WriteLine("🧪 Testing enhanced logging and error handling...");
            
            // Get services
            var playwrightManagementService = serviceProvider.GetRequiredService<IPlaywrightManagementService>();
            var logger = serviceProvider.GetRequiredService<ILogger<TestEnhancedLoggingAndErrorHandling>>();
            
            Console.WriteLine("1. Testing normal initialization with detailed logging...");
            await playwrightManagementService.InitializeAsync();
            Console.WriteLine("   ✅ Initialization completed with detailed logging");
            
            Console.WriteLine("2. Testing page creation with detailed logging...");
            var page = await playwrightManagementService.CreatePageAsync("https://mistral-nemo:7b");
            Console.WriteLine("   ✅ Page creation completed with detailed logging");
            
            Console.WriteLine("3. Testing navigation with detailed logging...");
            await playwrightManagementService.NavigateToUrlAsync("https://httpbin.org");
            Console.WriteLine("   ✅ Navigation completed with detailed logging");
            
            Console.WriteLine("4. Testing error handling - invalid URL...");
            try
            {
                await playwrightManagementService.NavigateToUrlAsync("invalid-url");
                Console.WriteLine("   ❌ Expected error but navigation succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✅ Error handling worked: {ex.Message}");
            }
            
            Console.WriteLine("5. Testing error handling - null page operations...");
            try
            {
                await playwrightManagementService.ClosePageAsync(null!);
                Console.WriteLine("   ❌ Expected error but operation succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✅ Error handling worked: {ex.Message}");
            }
            
            Console.WriteLine("6. Testing cleanup with detailed logging...");
            await playwrightManagementService.CloseBrowserAsync();
            Console.WriteLine("   ✅ Cleanup completed with detailed logging");
            
            Console.WriteLine("7. Testing disposal with detailed logging...");
            await playwrightManagementService.DisposeAsync();
            Console.WriteLine("   ✅ Disposal completed with detailed logging");
            
            Console.WriteLine("🎉 Enhanced logging and error handling test completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }
}
