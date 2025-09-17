using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlaywrightStudio.Events;
using PlaywrightStudio.Extensions;
using PlaywrightStudio.Handlers;
using PlaywrightStudio.Handlers.CommandHandlers;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.State;

namespace PlaywrightStudio.Commands;

/// <summary>
/// Test class to validate navigate command functionality
/// </summary>
public class TestNavigateCommand
{
    /// <summary>
    /// Tests the navigate command through the CQRS system
    /// </summary>
    public static async Task TestNavigateCommandAsync()
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
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:LogLevel:PlaywrightStudio"] = "Debug"
            })
            .Build();

        // Create service collection
        var services = new ServiceCollection();
        
        // Add logging
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
            Console.WriteLine("🧪 Testing Navigate Command through CQRS system...");
            
            // Get services
            var playwrightManagementService = serviceProvider.GetRequiredService<IPlaywrightManagementService>();
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            var stateManager = serviceProvider.GetRequiredService<IPlaywrightStateManager>();
            var navigateCommandHandler = serviceProvider.GetRequiredService<ICommandHandler<NavigateToUrlCommand>>();
            var logger = serviceProvider.GetRequiredService<ILogger<TestNavigateCommand>>();
            
            // Track events
            var receivedEvents = new List<IEvent>();
            using var eventSubscription = eventBus.Subscribe<IEvent>(evt =>
            {
                receivedEvents.Add(evt);
                Console.WriteLine($"📢 Event received: {evt.EventType} at {evt.Timestamp:HH:mm:ss.fff}");
            });
            
            // Track state changes
            var stateChanges = new List<PlaywrightState>();
            using var stateSubscription = stateManager.SubscribeToStateChanges(state =>
            {
                stateChanges.Add(state);
                Console.WriteLine($"📊 State changed: Browser={state.IsBrowserOpen}, Pages={state.PageCount}, URL={state.ActivePageUrl ?? "null"}");
            });
            
            Console.WriteLine("1. Initializing browser...");
            await playwrightManagementService.InitializeAsync();
            Console.WriteLine("   ✅ Browser initialized");
            
            Console.WriteLine("2. Creating initial page...");
            var page = await playwrightManagementService.CreatePageAsync("https://mistral-nemo:7b");
            Console.WriteLine($"   ✅ Page created and navigated to: {page.Url}");
            
            Console.WriteLine("3. Testing direct command handler...");
            var navigateCommand = new NavigateToUrlCommand("https://httpbin.org");
            Console.WriteLine($"   📤 Sending command: NavigateToUrlCommand to {navigateCommand.Url}");
            
            await navigateCommandHandler.HandleAsync(navigateCommand);
            Console.WriteLine("   ✅ Command handled successfully");
            
            Console.WriteLine("4. Testing command through event bus...");
            var navigateCommand2 = new NavigateToUrlCommand("https://jsonplaceholder.typicode.com");
            Console.WriteLine($"   📤 Publishing command: NavigateToUrlCommand to {navigateCommand2.Url}");
            
            // Note: In a real CQRS system, you'd have a command bus that routes commands to handlers
            // For this test, we'll call the handler directly since we don't have a command bus
            await navigateCommandHandler.HandleAsync(navigateCommand2);
            Console.WriteLine("   ✅ Command published and handled");
            
            Console.WriteLine("5. Testing navigation with specific page ID...");
            var currentPage = playwrightManagementService.ActivePage;
            if (currentPage != null)
            {
                var pageId = $"page-{currentPage.GetHashCode()}";
                var navigateCommand3 = new NavigateToUrlCommand("https://httpbin.org/json", pageId);
                Console.WriteLine($"   📤 Sending command: NavigateToUrlCommand to {navigateCommand3.Url} for page {pageId}");
                
                await navigateCommandHandler.HandleAsync(navigateCommand3);
                Console.WriteLine("   ✅ Page-specific command handled");
            }
            
            Console.WriteLine("6. Testing error handling with invalid URL...");
            try
            {
                var invalidCommand = new NavigateToUrlCommand("not-a-valid-url");
                Console.WriteLine($"   📤 Sending invalid command: NavigateToUrlCommand to {invalidCommand.Url}");
                
                await navigateCommandHandler.HandleAsync(invalidCommand);
                Console.WriteLine("   ❌ Expected error but command succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✅ Error handling worked: {ex.Message}");
            }
            
            Console.WriteLine("7. Validating results...");
            Console.WriteLine($"   📊 Total events received: {receivedEvents.Count}");
            Console.WriteLine($"   📊 Total state changes: {stateChanges.Count}");
            Console.WriteLine($"   📊 Final state - Browser: {stateManager.CurrentState.IsBrowserOpen}");
            Console.WriteLine($"   📊 Final state - Pages: {stateManager.CurrentState.PageCount}");
            Console.WriteLine($"   📊 Final state - URL: {stateManager.CurrentState.ActivePageUrl}");
            
            // Validate events
            var pageNavigatedEvents = receivedEvents.OfType<PageNavigatedEvent>().ToList();
            Console.WriteLine($"   📊 PageNavigatedEvent count: {pageNavigatedEvents.Count}");
            
            foreach (var evt in pageNavigatedEvents)
            {
                Console.WriteLine($"     - Navigated to: {evt.Url} (Page: {evt.PageId})");
            }
            
            Console.WriteLine("8. Testing cleanup...");
            await playwrightManagementService.CloseBrowserAsync();
            Console.WriteLine("   ✅ Browser closed");
            
            Console.WriteLine("🎉 Navigate Command test completed successfully!");
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
    
    /// <summary>
    /// Tests the navigate command with event bus integration
    /// </summary>
    public static async Task TestNavigateCommandWithEventBusAsync()
    {
        // Create configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Browser:Headless"] = "true",
                ["Browser:BrowserType"] = "Chromium",
                ["Logging:LogLevel:Default"] = "Information"
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
            Console.WriteLine("🧪 Testing Navigate Command with Event Bus integration...");
            
            // Get services
            var playwrightManagementService = serviceProvider.GetRequiredService<IPlaywrightManagementService>();
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            
            // Track navigation events
            var navigationEvents = new List<PageNavigatedEvent>();
            using var navSubscription = eventBus.Subscribe<PageNavigatedEvent>(evt =>
            {
                navigationEvents.Add(evt);
                Console.WriteLine($"🌐 Navigation event: {evt.Url} (Page: {evt.PageId})");
            });
            
            // Initialize and create page
            await playwrightManagementService.InitializeAsync();
            await playwrightManagementService.CreatePageAsync("https://mistral-nemo:7b");
            
            Console.WriteLine("1. Testing navigation through service...");
            await playwrightManagementService.NavigateToUrlAsync("https://httpbin.org");
            
            Console.WriteLine("2. Testing multiple navigations...");
            await playwrightManagementService.NavigateToUrlAsync("https://jsonplaceholder.typicode.com");
            await playwrightManagementService.NavigateToUrlAsync("https://httpbin.org/json");
            
            Console.WriteLine($"3. Validation - Navigation events received: {navigationEvents.Count}");
            foreach (var evt in navigationEvents)
            {
                Console.WriteLine($"   - {evt.Url} at {evt.Timestamp:HH:mm:ss.fff}");
            }
            
            Console.WriteLine("🎉 Event Bus integration test completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Event Bus test failed: {ex.Message}");
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }
}
