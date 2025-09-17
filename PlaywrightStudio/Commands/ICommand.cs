namespace PlaywrightStudio.Commands;

/// <summary>
/// Base interface for all commands in the CQRS system
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Unique identifier for the command
    /// </summary>
    Guid CommandId { get; }

    /// <summary>
    /// Timestamp when the command was created
    /// </summary>
    DateTime Timestamp { get; }
}
