using Microsoft.Extensions.Logging;
using PlaywrightStudio.State;

namespace PlaywrightStudio.Handlers.EventHandlers;

/// <summary>
/// Handler for state change events
/// </summary>
public class StateChangeEventHandler : IEventHandler<StateChangedEvent>
{
    private readonly ILogger<StateChangeEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the StateChangeEventHandler class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public StateChangeEventHandler(ILogger<StateChangeEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task HandleAsync(StateChangedEvent @event)
    {
        _logger.LogDebug("Handling state change event {EventId}", @event.EventId);
        
        try
        {
            // Log state changes for debugging
            var oldState = @event.OldState;
            var newState = @event.NewState;

            if (oldState.ActivePageUrl != newState.ActivePageUrl)
            {
                _logger.LogInformation("Active page URL changed from {OldUrl} to {NewUrl}", 
                    oldState.ActivePageUrl ?? "null", newState.ActivePageUrl ?? "null");
            }

            if (oldState.PageCount != newState.PageCount)
            {
                _logger.LogInformation("Page count changed from {OldCount} to {NewCount}", 
                    oldState.PageCount, newState.PageCount);
            }

            if (oldState.IsBrowserOpen != newState.IsBrowserOpen)
            {
                _logger.LogInformation("Browser state changed from {OldState} to {NewState}", 
                    oldState.IsBrowserOpen ? "open" : "closed", 
                    newState.IsBrowserOpen ? "open" : "closed");
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling state change event {EventId}", @event.EventId);
        }
    }
}
