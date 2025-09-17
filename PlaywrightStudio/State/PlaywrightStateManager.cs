using Microsoft.Extensions.Logging;
using PlaywrightStudio.Events;

namespace PlaywrightStudio.State;

/// <summary>
/// Manages the state of Playwright operations and publishes state change events
/// </summary>
public class PlaywrightStateManager : IPlaywrightStateManager
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<PlaywrightStateManager> _logger;
    private PlaywrightState _currentState;
    private readonly object _stateLock = new();

    /// <summary>
    /// Initializes a new instance of the PlaywrightStateManager class
    /// </summary>
    /// <param name="eventBus">Event bus for publishing events</param>
    /// <param name="logger">Logger instance</param>
    public PlaywrightStateManager(IEventBus eventBus, ILogger<PlaywrightStateManager> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentState = PlaywrightState.Empty;
    }

    /// <inheritdoc />
    public PlaywrightState CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
    }

    /// <inheritdoc />
    public async Task UpdateStateAsync(Func<PlaywrightState, PlaywrightState> updateAction)
    {
        if (updateAction == null)
        {
            throw new ArgumentNullException(nameof(updateAction));
        }

        PlaywrightState oldState;
        PlaywrightState newState;

        lock (_stateLock)
        {
            oldState = _currentState;
            newState = updateAction(_currentState);
            _currentState = newState;
        }

        _logger.LogDebug("State updated from {OldState} to {NewState}", oldState, newState);

        // Publish state change event
        await _eventBus.PublishAsync(new StateChangedEvent(oldState, newState));
    }

    /// <inheritdoc />
    public async Task UpdateStateAsync(PlaywrightState newState)
    {
        if (newState == null)
        {
            throw new ArgumentNullException(nameof(newState));
        }

        PlaywrightState oldState;

        lock (_stateLock)
        {
            oldState = _currentState;
            _currentState = newState;
        }

        _logger.LogDebug("State updated from {OldState} to {NewState}", oldState, newState);

        // Publish state change event
        await _eventBus.PublishAsync(new StateChangedEvent(oldState, newState));
    }

    /// <inheritdoc />
    public IDisposable SubscribeToStateChanges(Action<PlaywrightState> handler)
    {
        return _eventBus.Subscribe<StateChangedEvent>(e => handler(e.NewState));
    }

    /// <inheritdoc />
    public async Task ResetStateAsync()
    {
        await UpdateStateAsync(PlaywrightState.Empty);
    }
}

/// <summary>
/// Event published when the Playwright state changes
/// </summary>
/// <param name="OldState">The previous state</param>
/// <param name="NewState">The new state</param>
public record StateChangedEvent(PlaywrightState OldState, PlaywrightState NewState) : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string EventType => nameof(StateChangedEvent);
}
