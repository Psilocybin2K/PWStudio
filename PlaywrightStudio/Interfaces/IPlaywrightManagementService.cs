using Microsoft.Playwright;

namespace PlaywrightStudio.Interfaces;

/// <summary>
/// Service interface for managing Playwright browser and page operations
/// </summary>
public interface IPlaywrightManagementService
{
    /// <summary>
    /// Gets the current active page, if any
    /// </summary>
    IPage? ActivePage { get; }

    /// <summary>
    /// Gets all currently open pages
    /// </summary>
    IReadOnlyList<IPage> Pages { get; }

    /// <summary>
    /// Gets whether the browser is currently open
    /// </summary>
    bool IsBrowserOpen { get; }

    /// <summary>
    /// Initializes and launches the browser
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task InitializeAsync();

    /// <summary>
    /// Creates a new page
    /// </summary>
    /// <param name="url">Optional URL to navigate to immediately</param>
    /// <returns>The created page</returns>
    Task<IPage> CreatePageAsync(string? url = null);

    /// <summary>
    /// Navigates the active page to a URL
    /// </summary>
    /// <param name="url">URL to navigate to</param>
    /// <returns>Task representing the async operation</returns>
    Task NavigateToUrlAsync(string url);

    /// <summary>
    /// Navigates a specific page to a URL
    /// </summary>
    /// <param name="page">The page to navigate</param>
    /// <param name="url">URL to navigate to</param>
    /// <returns>Task representing the async operation</returns>
    Task NavigateToUrlAsync(IPage page, string url);

    /// <summary>
    /// Closes a specific page
    /// </summary>
    /// <param name="page">The page to close</param>
    /// <returns>Task representing the async operation</returns>
    Task ClosePageAsync(IPage page);

    /// <summary>
    /// Closes all pages and the browser
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task CloseBrowserAsync();

    /// <summary>
    /// Disposes of all resources
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task DisposeAsync();
}
