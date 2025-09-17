namespace PlaywrightStudio.Commands;

/// <summary>
/// Command to initialize and launch the browser
/// </summary>
public record InitializeBrowserCommand() : ICommand
{
    public Guid CommandId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
