namespace PlaywrightStudio.Events;

/// <summary>
/// Event published when the browser is initialized and ready
/// </summary>
/// <param name="BrowserType">The type of browser that was initialized</param>
public record BrowserInitializedEvent(string BrowserType) : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string EventType => nameof(BrowserInitializedEvent);
}
