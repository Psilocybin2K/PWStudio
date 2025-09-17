using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlaywrightStudio.Extensions;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.Services;

namespace PlaywrightStudio;

/// <summary>
/// Test class to validate the updated Program.cs architecture
/// </summary>
public class ProgramTest
{
    /// <summary>
    /// Tests the updated Program.cs functionality
    /// </summary>
    public static async Task TestUpdatedProgramAsync()
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
                ["Browser:LaunchOptions:Devtools"] = "false",
                ["Ollama:BaseAddress"] = "http://localhost:11434/v1",
                ["Ollama:TimeoutMinutes"] = "20",
                ["Ollama:Model"] = "llama3.2",
                ["Ollama:Temperature"] = "0.7",
                ["Ollama:MaxTokens"] = "2048",
                ["Ollama:DefaultModelName"] = "llama3.1:8b"
            })
            .Build();

        // Create service collection
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add PlaywrightStudio services
        services.AddPlaywrightStudio(configuration);

        // Add Ollama services
        services.AddOllamaServices(configuration);

        // Add Semantic Kernel
        services.AddSingleton(serviceProvider => new Microsoft.SemanticKernel.Kernel(serviceProvider));

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        try
        {
            Console.WriteLine("🧪 Testing updated Program.cs architecture...");

            // Get services
            var playwrightService = serviceProvider.GetRequiredService<PlaywrightStudioService>()
                ?? throw new Exception("PlaywrightStudioService not found");
            IPlaywrightManagementService playwrightManagementService = serviceProvider.GetRequiredService<IPlaywrightManagementService>()
                ?? throw new Exception("PlaywrightManagementService not found");
            var logger = serviceProvider.GetRequiredService<ILogger<ProgramTest>>();

            Console.WriteLine("1. Testing service resolution...");
            Console.WriteLine($"   ✅ PlaywrightStudioService resolved: {playwrightService != null}");
            Console.WriteLine($"   ✅ PlaywrightManagementService resolved: {playwrightManagementService != null}");

            Console.WriteLine("2. Testing PlaywrightStudio initialization...");
            var result = await playwrightService.InitializeAsync();

            if (result.Success)
            {
                Console.WriteLine($"   ✅ Initialization successful: {result.Message}");
                Console.WriteLine($"   ✅ Models loaded: {result.Models.Count}");
                Console.WriteLine($"   ✅ Plugins created: {result.Plugins.Count}");
                Console.WriteLine($"   ✅ Browser open: {playwrightManagementService.IsBrowserOpen}");
                Console.WriteLine($"   ✅ Pages count: {playwrightManagementService.Pages.Count}");
                Console.WriteLine($"   ✅ Active page URL: {playwrightManagementService.ActivePage?.Url ?? "null"}");
            }
            else
            {
                Console.WriteLine($"   ❌ Initialization failed: {result.Message}");
            }

            Console.WriteLine("3. Testing cleanup...");
            await playwrightManagementService.DisposeAsync();
            Console.WriteLine($"   ✅ Browser closed: {!playwrightManagementService.IsBrowserOpen}");

            Console.WriteLine("🎉 Updated Program.cs test completed!");
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
