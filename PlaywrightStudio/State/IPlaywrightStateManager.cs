namespace PlaywrightStudio.State;

/// <summary>
/// Manages the state of Playwright operations and publishes state change events
/// </summary>
public interface IPlaywrightStateManager
{
    /// <summary>
    /// Gets the current state
    /// </summary>
    PlaywrightState CurrentState { get; }

    /// <summary>
    /// Updates the state and publishes appropriate events
    /// </summary>
    /// <param name="updateAction">Action to update the state</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateStateAsync(Func<PlaywrightState, PlaywrightState> updateAction);

    /// <summary>
    /// Updates the state with a new state value
    /// </summary>
    /// <param name="newState">The new state value</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateStateAsync(PlaywrightState newState);

    /// <summary>
    /// Subscribes to state change events
    /// </summary>
    /// <param name="handler">Event handler for state changes</param>
    /// <returns>Disposable subscription</returns>
    IDisposable SubscribeToStateChanges(Action<PlaywrightState> handler);

    /// <summary>
    /// Resets the state to empty
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task ResetStateAsync();
}
