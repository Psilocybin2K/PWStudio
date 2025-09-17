using PlaywrightStudio.Events;

namespace PlaywrightStudio.Handlers;

/// <summary>
/// Base interface for event handlers
/// </summary>
/// <typeparam name="TEvent">Type of event to handle</typeparam>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Handles the event
    /// </summary>
    /// <param name="event">The event to handle</param>
    /// <returns>Task representing the async operation</returns>
    Task HandleAsync(TEvent @event);
}
