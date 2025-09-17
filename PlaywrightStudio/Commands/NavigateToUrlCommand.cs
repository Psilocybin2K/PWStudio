namespace PlaywrightStudio.Commands;

/// <summary>
/// Command to navigate to a specific URL
/// </summary>
/// <param name="Url">The URL to navigate to</param>
/// <param name="PageId">Optional page ID to navigate (if null, uses active page)</param>
public record NavigateToUrlCommand(string Url, string? PageId = null) : ICommand
{
    public Guid CommandId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
