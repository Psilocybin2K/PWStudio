using Microsoft.Extensions.Logging;

namespace PlaywrightStudio.Events;

/// <summary>
/// Simple test class to validate CQRS infrastructure
/// </summary>
public class TestEventBus
{
    /// <summary>
    /// Tests the event bus functionality
    /// </summary>
    public static async Task TestEventBusAsync()
    {
        // Create a mock logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<EventBus>();
        
        var eventBus = new EventBus(logger);
        
        // Test event
        var testEvent = new PageNavigatedEvent("https://mistral-nemo:7b", "page-1");
        
        // Subscribe to events
        var receivedEvents = new List<PageNavigatedEvent>();
        using var subscription = eventBus.Subscribe<PageNavigatedEvent>(evt =>
        {
            receivedEvents.Add(evt);
            Console.WriteLine($"Received event: {evt.EventType} - URL: {evt.Url}");
        });
        
        // Publish event
        await eventBus.PublishAsync(testEvent);
        
        // Verify event was received
        if (receivedEvents.Count == 1 && receivedEvents[0].Url == "https://mistral-nemo:7b")
        {
            Console.WriteLine("✅ EventBus test passed!");
        }
        else
        {
            Console.WriteLine("❌ EventBus test failed!");
        }
        
        loggerFactory.Dispose();
    }
}
