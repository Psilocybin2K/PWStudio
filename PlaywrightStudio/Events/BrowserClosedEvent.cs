namespace PlaywrightStudio.Events;

/// <summary>
/// Event published when the browser is closed
/// </summary>
public record BrowserClosedEvent() : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string EventType => nameof(BrowserClosedEvent);
}
