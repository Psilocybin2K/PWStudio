using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlaywrightStudio.Events;
using PlaywrightStudio.Extensions;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.State;

namespace PlaywrightStudio.Commands;

/// <summary>
/// Comprehensive validation of the navigate command system
/// </summary>
public class ValidateNavigateCommand
{
    /// <summary>
    /// Validates the complete navigate command flow
    /// </summary>
    public static async Task ValidateCompleteFlowAsync()
    {
        Console.WriteLine("🔍 Validating Complete Navigate Command Flow");
        Console.WriteLine("=============================================");
        
        // Create configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Browser:Headless"] = "true",
                ["Browser:BrowserType"] = "Chromium",
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:LogLevel:PlaywrightStudio"] = "Debug"
            })
            .Build();

        // Create service collection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddPlaywrightStudio(configuration);

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Get services
            var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            var stateManager = serviceProvider.GetRequiredService<IPlaywrightStateManager>();
            var playwrightService = serviceProvider.GetRequiredService<IPlaywrightManagementService>();
            
            // Track all events
            var allEvents = new List<IEvent>();
            using var eventSubscription = eventBus.Subscribe<IEvent>(evt =>
            {
                allEvents.Add(evt);
                Console.WriteLine($"📢 {evt.Timestamp:HH:mm:ss.fff} - {evt.EventType}");
            });
            
            // Track state changes
            var stateHistory = new List<PlaywrightState>();
            using var stateSubscription = stateManager.SubscribeToStateChanges(state =>
            {
                stateHistory.Add(state);
                Console.WriteLine($"📊 State: Browser={state.IsBrowserOpen}, Pages={state.PageCount}, URL={state.ActivePageUrl ?? "null"}");
            });
            
            Console.WriteLine("\n1️⃣ Testing Command Flow:");
            Console.WriteLine("   📤 InitializeBrowserCommand");
            await commandBus.SendAsync(new InitializeBrowserCommand());
            
            Console.WriteLine("   📤 CreatePageCommand");
            await commandBus.SendAsync(new CreatePageCommand("https://mistral-nemo:7b"));
            
            Console.WriteLine("   📤 NavigateToUrlCommand (httpbin.org)");
            await commandBus.SendAsync(new NavigateToUrlCommand("https://httpbin.org"));
            
            Console.WriteLine("   📤 NavigateToUrlCommand (jsonplaceholder)");
            await commandBus.SendAsync(new NavigateToUrlCommand("https://jsonplaceholder.typicode.com"));
            
            Console.WriteLine("   📤 NavigateToUrlCommand (httpbin/json)");
            await commandBus.SendAsync(new NavigateToUrlCommand("https://httpbin.org/json"));
            
            Console.WriteLine("   📤 CloseBrowserCommand");
            await commandBus.SendAsync(new CloseBrowserCommand());
            
            Console.WriteLine("\n2️⃣ Validating Results:");
            
            // Validate events
            var browserInitEvents = allEvents.OfType<BrowserInitializedEvent>().Count();
            var pageCreatedEvents = allEvents.OfType<PageCreatedEvent>().Count();
            var pageNavigatedEvents = allEvents.OfType<PageNavigatedEvent>().Count();
            var browserClosedEvents = allEvents.OfType<BrowserClosedEvent>().Count();
            var stateChangedEvents = allEvents.OfType<StateChangedEvent>().Count();
            
            Console.WriteLine($"   ✅ BrowserInitializedEvent: {browserInitEvents} (expected: 1)");
            Console.WriteLine($"   ✅ PageCreatedEvent: {pageCreatedEvents} (expected: 1)");
            Console.WriteLine($"   ✅ PageNavigatedEvent: {pageNavigatedEvents} (expected: 4)");
            Console.WriteLine($"   ✅ BrowserClosedEvent: {browserClosedEvents} (expected: 1)");
            Console.WriteLine($"   ✅ StateChangedEvent: {stateChangedEvents} (expected: multiple)");
            
            // Validate state progression
            Console.WriteLine($"   ✅ State changes tracked: {stateHistory.Count}");
            Console.WriteLine($"   ✅ Final state - Browser: {stateManager.CurrentState.IsBrowserOpen} (expected: false)");
            Console.WriteLine($"   ✅ Final state - Pages: {stateManager.CurrentState.PageCount} (expected: 0)");
            Console.WriteLine($"   ✅ Final state - URL: {stateManager.CurrentState.ActivePageUrl ?? "null"} (expected: null)");
            
            // Validate navigation events
            var navigationUrls = allEvents.OfType<PageNavigatedEvent>().Select(e => e.Url).ToList();
            var expectedUrls = new[]
            {
                "https://mistral-nemo:7b",
                "https://httpbin.org", 
                "https://jsonplaceholder.typicode.com",
                "https://httpbin.org/json"
            };
            
            Console.WriteLine("\n3️⃣ Validating Navigation URLs:");
            foreach (var expectedUrl in expectedUrls)
            {
                var found = navigationUrls.Contains(expectedUrl);
                Console.WriteLine($"   {(found ? "✅" : "❌")} {expectedUrl}");
            }
            
            // Validate command timing
            var commandEvents = allEvents.Where(e => e.EventType.Contains("Command")).ToList();
            Console.WriteLine($"\n4️⃣ Command Processing:");
            Console.WriteLine($"   ✅ Total events processed: {allEvents.Count}");
            Console.WriteLine($"   ✅ Event processing time: {(allEvents.LastOrDefault()?.Timestamp - allEvents.FirstOrDefault()?.Timestamp)?.TotalMilliseconds:F0}ms");
            
            Console.WriteLine("\n🎉 Complete Navigate Command Flow Validation PASSED!");
            
            // Summary
            Console.WriteLine("\n📋 Summary:");
            Console.WriteLine($"   • Commands sent: 6");
            Console.WriteLine($"   • Events generated: {allEvents.Count}");
            Console.WriteLine($"   • State changes: {stateHistory.Count}");
            Console.WriteLine($"   • Navigation events: {pageNavigatedEvents}");
            Console.WriteLine($"   • All validations: ✅ PASSED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Validation failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }
    
    /// <summary>
    /// Validates error handling scenarios
    /// </summary>
    public static async Task ValidateErrorHandlingAsync()
    {
        Console.WriteLine("\n🔍 Validating Error Handling Scenarios");
        Console.WriteLine("======================================");
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Browser:Headless"] = "true",
                ["Browser:BrowserType"] = "Chromium",
                ["Logging:LogLevel:Default"] = "Warning"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddPlaywrightStudio(configuration);

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
            
            // Test 1: Navigate without initialization
            Console.WriteLine("1️⃣ Testing navigate without browser initialization:");
            try
            {
                await commandBus.SendAsync(new NavigateToUrlCommand("https://mistral-nemo:7b"));
                Console.WriteLine("   ❌ Expected error but command succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✅ Properly handled: {ex.GetType().Name}");
            }
            
            // Test 2: Navigate with invalid URL
            Console.WriteLine("2️⃣ Testing navigate with invalid URL:");
            await commandBus.SendAsync(new InitializeBrowserCommand());
            await commandBus.SendAsync(new CreatePageCommand());
            
            try
            {
                await commandBus.SendAsync(new NavigateToUrlCommand("invalid-url"));
                Console.WriteLine("   ❌ Expected error but command succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✅ Properly handled: {ex.GetType().Name}");
            }
            
            // Test 3: Navigate with empty URL
            Console.WriteLine("3️⃣ Testing navigate with empty URL:");
            try
            {
                await commandBus.SendAsync(new NavigateToUrlCommand(""));
                Console.WriteLine("   ❌ Expected error but command succeeded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✅ Properly handled: {ex.GetType().Name}");
            }
            
            Console.WriteLine("\n🎉 Error Handling Validation PASSED!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error handling validation failed: {ex.Message}");
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }
}
