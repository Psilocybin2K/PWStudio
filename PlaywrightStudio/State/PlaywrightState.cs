using PlaywrightStudio.Models;

namespace PlaywrightStudio.State;

/// <summary>
/// Immutable state object representing the current state of Playwright management
/// </summary>
/// <param name="ActivePageUrl">The URL of the currently active page, null if no page is active</param>
/// <param name="BrowserType">The type of browser being used (Chromium, Firefox, WebKit)</param>
/// <param name="PageCount">The number of pages currently open</param>
/// <param name="IsBrowserOpen">Whether the browser is currently open</param>
/// <param name="LastUpdated">Timestamp of the last state update</param>
/// <param name="ActivePageObjectModel">The currently active page object model, null if no model matches</param>
public record PlaywrightState(
    string? ActivePageUrl,
    string BrowserType,
    int PageCount,
    bool IsBrowserOpen,
    DateTime LastUpdated,
    PageObjectModel? ActivePageObjectModel
)
{
    /// <summary>
    /// Creates an initial empty state
    /// </summary>
    public static PlaywrightState Empty => new(
        ActivePageUrl: null,
        BrowserType: "Chromium",
        PageCount: 0,
        IsBrowserOpen: false,
        LastUpdated: DateTime.UtcNow,
        ActivePageObjectModel: null
    );

    /// <summary>
    /// Creates a new state with updated values
    /// </summary>
    public PlaywrightState With(
        string? activePageUrl = null,
        string? browserType = null,
        int? pageCount = null,
        bool? isBrowserOpen = null,
        PageObjectModel? activePageObjectModel = null)
    {
        return this with
        {
            ActivePageUrl = activePageUrl ?? ActivePageUrl,
            BrowserType = browserType ?? BrowserType,
            PageCount = pageCount ?? PageCount,
            IsBrowserOpen = isBrowserOpen ?? IsBrowserOpen,
            LastUpdated = DateTime.UtcNow,
            ActivePageObjectModel = activePageObjectModel ?? ActivePageObjectModel
        };
    }
}
