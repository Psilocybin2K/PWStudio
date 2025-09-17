using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlaywrightStudio.Extensions;
using PlaywrightStudio.Interfaces;

namespace PlaywrightStudio.Services;

/// <summary>
/// Test class to validate the refactored PlaywrightStudioService
/// </summary>
public class TestRefactoredPlaywrightStudioService
{
    /// <summary>
    /// Tests the refactored PlaywrightStudioService functionality
    /// </summary>
    public static async Task TestRefactoredServiceAsync()
    {
        // Create configuration
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
                ["Browser:LaunchOptions:Devtools"] = "false"
            })
            .Build();

        // Create service collection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add PlaywrightStudio services
        services.AddPlaywrightStudio(configuration);

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        try
        {
            Console.WriteLine("🧪 Testing refactored PlaywrightStudioService...");
            
            // Get the service
            var playwrightStudioService = serviceProvider.GetRequiredService<PlaywrightStudioService>();
            var playwrightManagementService = serviceProvider.GetRequiredService<IPlaywrightManagementService>();
            
            // Test initialization without providing a page
            Console.WriteLine("1. Testing initialization without page parameter...");
            var result = await playwrightStudioService.InitializeAsync();
            
            if (result.Success)
            {
                Console.WriteLine($"   ✅ Initialization successful: {result.Message}");
                Console.WriteLine($"   ✅ Models loaded: {result.Models.Count}");
                Console.WriteLine($"   ✅ Plugins created: {result.Plugins.Count}");
                Console.WriteLine($"   ✅ Browser open: {playwrightManagementService.IsBrowserOpen}");
                Console.WriteLine($"   ✅ Pages count: {playwrightManagementService.Pages.Count}");
                Console.WriteLine($"   ✅ Active page URL: {playwrightManagementService.ActivePage?.Url}");
            }
            else
            {
                Console.WriteLine($"   ❌ Initialization failed: {result.Message}");
            }
            
            // Test plugin creation
            Console.WriteLine("2. Testing plugin creation...");
            var plugin = await playwrightStudioService.CreatePluginForModelAsync("login");
            if (plugin != null)
            {
                Console.WriteLine($"   ✅ Plugin created successfully with {plugin.Count()} functions");
            }
            else
            {
                Console.WriteLine("   ⚠️ No plugin created (this is expected if no login model exists)");
            }
            
            // Test cleanup
            Console.WriteLine("3. Testing cleanup...");
            await playwrightManagementService.CloseBrowserAsync();
            Console.WriteLine($"   ✅ Browser closed: {!playwrightManagementService.IsBrowserOpen}");
            
            Console.WriteLine("🎉 Refactored service test completed!");
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
