using PlaywrightStudio.Commands;

namespace PlaywrightStudio.Commands;

/// <summary>
/// Command bus interface for routing commands to handlers
/// </summary>
public interface ICommandBus
{
    /// <summary>
    /// Sends a command to its appropriate handler
    /// </summary>
    /// <typeparam name="TCommand">Type of command to send</typeparam>
    /// <param name="command">The command to send</param>
    /// <returns>Task representing the async operation</returns>
    Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand;
}
