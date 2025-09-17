using PlaywrightStudio.Commands;

namespace PlaywrightStudio.Handlers;

/// <summary>
/// Base interface for command handlers
/// </summary>
/// <typeparam name="TCommand">Type of command to handle</typeparam>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Handles the command
    /// </summary>
    /// <param name="command">The command to handle</param>
    /// <returns>Task representing the async operation</returns>
    Task HandleAsync(TCommand command);
}
