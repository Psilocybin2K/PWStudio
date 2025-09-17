namespace PlaywrightStudio.Events;

/// <summary>
/// Event published when a page navigates to a new URL
/// </summary>
/// <param name="Url">The URL that was navigated to</param>
/// <param name="PageId">The ID of the page that navigated</param>
public record PageNavigatedEvent(string Url, string PageId) : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string EventType => nameof(PageNavigatedEvent);
}
