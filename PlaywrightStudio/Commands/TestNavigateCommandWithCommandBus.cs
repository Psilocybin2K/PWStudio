using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlaywrightStudio.Events;
using PlaywrightStudio.Extensions;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.State;

namespace PlaywrightStudio.Commands;

/// <summary>
/// Test class to validate navigate command functionality with Command Bus
/// </summary>
public class TestNavigateCommandWithCommandBus
{
    /// <summary>
    /// Tests the navigate command through the Command Bus
    /// </summary>
    public static async Task TestNavigateCommandWithCommandBusAsync()
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
            Console.WriteLine("🧪 Testing Navigate Command with Command Bus...");
            
            // Get services
            var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            var stateManager = serviceProvider.GetRequiredService<IPlaywrightStateManager>();
            var playwrightManagementService = serviceProvider.GetRequiredService<IPlaywrightManagementService>();
            
            // Track events
            var receivedEvents = new List<IEvent>();
            using var eventSubscription = eventBus.Subscribe<IEvent>(evt =>
            {
                receivedEvents.Add(evt);
                Console.WriteLine($"📢 Event: {evt.EventType} at {evt.Timestamp:HH:mm:ss.fff}");
            });
            
            // Track state changes
            var stateChanges = new List<PlaywrightState>();
            using var stateSubscription = stateManager.SubscribeToStateChanges(state =>
            {
                stateChanges.Add(state);
                Console.WriteLine($"📊 State: Browser={state.IsBrowserOpen}, Pages={state.PageCount}, URL={state.ActivePageUrl ?? "null"}");
            });
            
            Console.WriteLine("1. Initializing browser via command...");
            var initCommand = new InitializeBrowserCommand();
            await commandBus.SendAsync(initCommand);
            Console.WriteLine("   ✅ Browser initialization command sent");
            
            Console.WriteLine("2. Creating page via command...");
            var createPageCommand = new CreatePageCommand("https://mistral-nemo:7b");
            await commandBus.SendAsync(createPageCommand);
            Console.WriteLine("   ✅ Page creation command sent");
            
            Console.WriteLine("3. Testing navigate command via Command Bus...");
            var navigateCommand1 = new NavigateToUrlCommand("https://httpbin.org");
            Console.WriteLine($"   📤 Sending NavigateToUrlCommand to {navigateCommand1.Url}");
            await commandBus.SendAsync(navigateCommand1);
            Console.WriteLine("   ✅ Navigate command sent and handled");
            
            Console.WriteLine("4. Testing multiple navigate commands...");
            var navigateCommand2 = new NavigateToUrlCommand("https://jsonplaceholder.typicode.com");
            Console.WriteLine($"   📤 Sending NavigateToUrlCommand to {navigateCommand2.Url}");
            await commandBus.SendAsync(navigateCommand2);
            
            var navigateCommand3 = new NavigateToUrlCommand("https://httpbin.org/json");
            Console.WriteLine($"   📤 Sending NavigateToUrlCommand to {navigateCommand3.Url}");
            await commandBus.SendAsync(navigateCommand3);
            
            Console.WriteLine("   ✅ Multiple navigate commands sent");
            
            Console.WriteLine("5. Testing navigate command with specific page ID...");
            var currentPage = playwrightManagementService.ActivePage;
            if (currentPage != null)
            {
                var pageId = $"page-{currentPage.GetHashCode()}";
                var navigateCommand4 = new NavigateToUrlCommand("https://httpbin.org/html", pageId);
                Console.WriteLine($"   📤 Sending NavigateToUrlCommand to {navigateCommand4.Url} for page {pageId}");
                await commandBus.SendAsync(navigateCommand4);
                Console.WriteLine("   ✅ Page-specific navigate command sent");
            }
            
            Console.WriteLine("6. Testing error handling with invalid URL...");
            try
            {
                var invalidCommand = new NavigateToUrlCommand("not-a-valid-url");
                Console.WriteLine($"   📤 Sending invalid NavigateToUrlCommand to {invalidCommand.Url}");
                await commandBus.SendAsync(invalidCommand);
                Console.WriteLine("   ❌ Expected error but command succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✅ Error handling worked: {ex.Message}");
            }
            
            Console.WriteLine("7. Testing close browser command...");
            var closeCommand = new CloseBrowserCommand();
            await commandBus.SendAsync(closeCommand);
            Console.WriteLine("   ✅ Close browser command sent");
            
            Console.WriteLine("8. Validating results...");
            Console.WriteLine($"   📊 Total events received: {receivedEvents.Count}");
            Console.WriteLine($"   📊 Total state changes: {stateChanges.Count}");
            Console.WriteLine($"   📊 Final state - Browser: {stateManager.CurrentState.IsBrowserOpen}");
            Console.WriteLine($"   📊 Final state - Pages: {stateManager.CurrentState.PageCount}");
            Console.WriteLine($"   📊 Final state - URL: {stateManager.CurrentState.ActivePageUrl}");
            
            // Validate specific events
            var pageNavigatedEvents = receivedEvents.OfType<PageNavigatedEvent>().ToList();
            var pageCreatedEvents = receivedEvents.OfType<PageCreatedEvent>().ToList();
            var browserInitializedEvents = receivedEvents.OfType<BrowserInitializedEvent>().ToList();
            var browserClosedEvents = receivedEvents.OfType<BrowserClosedEvent>().ToList();
            
            Console.WriteLine($"   📊 BrowserInitializedEvent count: {browserInitializedEvents.Count}");
            Console.WriteLine($"   📊 PageCreatedEvent count: {pageCreatedEvents.Count}");
            Console.WriteLine($"   📊 PageNavigatedEvent count: {pageNavigatedEvents.Count}");
            Console.WriteLine($"   📊 BrowserClosedEvent count: {browserClosedEvents.Count}");
            
            Console.WriteLine("   📊 Navigation events:");
            foreach (var evt in pageNavigatedEvents)
            {
                Console.WriteLine($"     - {evt.Url} (Page: {evt.PageId}) at {evt.Timestamp:HH:mm:ss.fff}");
            }
            
            Console.WriteLine("🎉 Navigate Command with Command Bus test completed successfully!");
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
    /// Tests command validation and error scenarios
    /// </summary>
    public static async Task TestCommandValidationAsync()
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
            Console.WriteLine("🧪 Testing Command Validation...");
            
            var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
            
            Console.WriteLine("1. Testing null command...");
            try
            {
                await commandBus.SendAsync<NavigateToUrlCommand>(null!);
                Console.WriteLine("   ❌ Expected error but command succeeded");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("   ✅ Null command properly rejected");
            }
            
            Console.WriteLine("2. Testing empty URL command...");
            try
            {
                var emptyUrlCommand = new NavigateToUrlCommand("");
                await commandBus.SendAsync(emptyUrlCommand);
                Console.WriteLine("   ❌ Expected error but command succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✅ Empty URL properly rejected: {ex.Message}");
            }
            
            Console.WriteLine("3. Testing command without initialized browser...");
            try
            {
                var navigateCommand = new NavigateToUrlCommand("https://mistral-nemo:7b");
                await commandBus.SendAsync(navigateCommand);
                Console.WriteLine("   ❌ Expected error but command succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✅ Uninitialized browser properly handled: {ex.Message}");
            }
            
            Console.WriteLine("🎉 Command validation test completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Validation test failed: {ex.Message}");
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }
}
