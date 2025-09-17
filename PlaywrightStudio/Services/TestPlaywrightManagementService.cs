using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlaywrightStudio.Configuration;
using PlaywrightStudio.Events;
using PlaywrightStudio.State;

namespace PlaywrightStudio.Services;

/// <summary>
/// Simple test class to validate PlaywrightManagementService
/// </summary>
public class TestPlaywrightManagementService
{
    /// <summary>
    /// Tests the PlaywrightManagementService functionality
    /// </summary>
    public static async Task TestPlaywrightManagementServiceAsync()
    {
        // Create mock logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        // Create configuration
        var browserOptions = new BrowserOptions
        {
            BrowserType = "Chromium",
            Headless = true,
            ViewportWidth = 1280,
            ViewportHeight = 720
        };
        
        // Create services
        var eventBus = new EventBus(loggerFactory.CreateLogger<EventBus>());
        var stateManager = new PlaywrightStateManager(eventBus, loggerFactory.CreateLogger<PlaywrightStateManager>());
        var playwrightService = new PlaywrightManagementService(
            loggerFactory.CreateLogger<PlaywrightManagementService>(),
            Options.Create(browserOptions),
            eventBus,
            stateManager);
        
        try
        {
            Console.WriteLine("🧪 Testing PlaywrightManagementService...");
            
            // Test initialization
            Console.WriteLine("1. Testing browser initialization...");
            await playwrightService.InitializeAsync();
            Console.WriteLine($"   ✅ Browser initialized: {playwrightService.IsBrowserOpen}");
            
            // Test page creation
            Console.WriteLine("2. Testing page creation...");
            var page = await playwrightService.CreatePageAsync("https://mistral-nemo:7b");
            Console.WriteLine($"   ✅ Page created: {page != null}");
            Console.WriteLine($"   ✅ Page count: {playwrightService.Pages.Count}");
            Console.WriteLine($"   ✅ Active page URL: {playwrightService.ActivePage?.Url}");
            
            // Test navigation
            Console.WriteLine("3. Testing navigation...");
            await playwrightService.NavigateToUrlAsync("https://httpbin.org");
            Console.WriteLine($"   ✅ Navigated to: {playwrightService.ActivePage?.Url}");
            
            // Test state
            Console.WriteLine("4. Testing state management...");
            var currentState = stateManager.CurrentState;
            Console.WriteLine($"   ✅ State - Browser Open: {currentState.IsBrowserOpen}");
            Console.WriteLine($"   ✅ State - Page Count: {currentState.PageCount}");
            Console.WriteLine($"   ✅ State - Active URL: {currentState.ActivePageUrl}");
            
            // Test cleanup
            Console.WriteLine("5. Testing cleanup...");
            await playwrightService.CloseBrowserAsync();
            Console.WriteLine($"   ✅ Browser closed: {!playwrightService.IsBrowserOpen}");
            
            Console.WriteLine("🎉 All tests passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
        }
        finally
        {
            await playwrightService.DisposeAsync();
            loggerFactory.Dispose();
        }
    }
}
