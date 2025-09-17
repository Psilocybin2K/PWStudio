using Microsoft.Extensions.Logging;

namespace PlaywrightStudio.Events;

/// <summary>
/// Simple in-memory event bus implementation
/// </summary>
public class EventBus : IEventBus
{
    private readonly ILogger<EventBus> _logger;
    private readonly Dictionary<Type, List<Func<IEvent, Task>>> _handlers = new();
    private readonly object _handlersLock = new();

    /// <summary>
    /// Initializes a new instance of the EventBus class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        _logger.LogDebug("Publishing event {EventType} with ID {EventId}", @event.EventType, @event.EventId);

        List<Func<IEvent, Task>> handlers;
        lock (_handlersLock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out handlers) || handlers == null || handlers.Count == 0)
            {
                _logger.LogDebug("No handlers registered for event type {EventType}", typeof(TEvent).Name);
                return;
            }

            // Create a copy to avoid issues with concurrent modifications
            handlers = new List<Func<IEvent, Task>>(handlers);
        }

        // Execute all handlers
        var tasks = handlers.Select(handler => 
        {
            try
            {
                return handler(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing event handler for {EventType}", @event.EventType);
                return Task.CompletedTask;
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        // Wrap the typed handler in a generic handler
        Func<IEvent, Task> genericHandler = @event =>
        {
            if (@event is TEvent typedEvent)
            {
                return handler(typedEvent);
            }
            return Task.CompletedTask;
        };

        lock (_handlersLock)
        {
            if (!_handlers.ContainsKey(typeof(TEvent)))
            {
                _handlers[typeof(TEvent)] = new List<Func<IEvent, Task>>();
            }
            _handlers[typeof(TEvent)].Add(genericHandler);
        }

        _logger.LogDebug("Subscribed handler for event type {EventType}", typeof(TEvent).Name);

        return new EventSubscription(() =>
        {
            lock (_handlersLock)
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
                {
                    handlers.Remove(genericHandler);
                    if (handlers.Count == 0)
                    {
                        _handlers.Remove(typeof(TEvent));
                    }
                }
            }
            _logger.LogDebug("Unsubscribed handler for event type {EventType}", typeof(TEvent).Name);
        });
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return Subscribe<TEvent>(@event =>
        {
            handler(@event);
            return Task.CompletedTask;
        });
    }
}

/// <summary>
/// Represents a subscription to an event
/// </summary>
public class EventSubscription : IDisposable
{
    private readonly Action _unsubscribeAction;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the EventSubscription class
    /// </summary>
    /// <param name="unsubscribeAction">Action to perform when unsubscribing</param>
    public EventSubscription(Action unsubscribeAction)
    {
        _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _unsubscribeAction();
            _disposed = true;
        }
    }
}
