namespace PlaywrightStudio.Commands;

/// <summary>
/// Command to create a new page
/// </summary>
/// <param name="Url">Optional URL to navigate to immediately after creating the page</param>
public record CreatePageCommand(string? Url = null) : ICommand
{
    public Guid CommandId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
