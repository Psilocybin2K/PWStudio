namespace PlaywrightStudio.Events;

/// <summary>
/// Event published when a new page is created
/// </summary>
/// <param name="PageId">The ID of the created page</param>
/// <param name="Url">The initial URL of the page, if any</param>
public record PageCreatedEvent(string PageId, string? Url) : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string EventType => nameof(PageCreatedEvent);
}
