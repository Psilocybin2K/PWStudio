namespace PlaywrightStudio.Events;

/// <summary>
/// Base interface for all events in the CQRS system
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Type of the event
    /// </summary>
    string EventType { get; }
}
