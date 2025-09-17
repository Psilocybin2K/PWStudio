namespace PlaywrightStudio.Events;

/// <summary>
/// Event bus interface for publishing and subscribing to events
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all subscribers
    /// </summary>
    /// <typeparam name="TEvent">Type of event to publish</typeparam>
    /// <param name="event">The event to publish</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;

    /// <summary>
    /// Subscribes to events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">Type of event to subscribe to</typeparam>
    /// <param name="handler">Event handler</param>
    /// <returns>Disposable subscription</returns>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;

    /// <summary>
    /// Subscribes to events of a specific type with synchronous handler
    /// </summary>
    /// <typeparam name="TEvent">Type of event to subscribe to</typeparam>
    /// <param name="handler">Event handler</param>
    /// <returns>Disposable subscription</returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
}
