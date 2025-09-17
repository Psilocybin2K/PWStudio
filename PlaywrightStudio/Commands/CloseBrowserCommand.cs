namespace PlaywrightStudio.Commands;

/// <summary>
/// Command to close the browser and all pages
/// </summary>
public record CloseBrowserCommand() : ICommand
{
    public Guid CommandId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
