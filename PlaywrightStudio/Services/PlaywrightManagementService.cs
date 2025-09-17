using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using PlaywrightStudio.Configuration;
using PlaywrightStudio.Events;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.State;

namespace PlaywrightStudio.Services;

/// <summary>
/// Service for managing Playwright browser and page operations with CQRS integration
/// </summary>
public class PlaywrightManagementService : IPlaywrightManagementService
{
    private readonly ILogger<PlaywrightManagementService> _logger;
    private readonly BrowserOptions _browserOptions;
    private readonly IEventBus _eventBus;
    private readonly IPlaywrightStateManager _stateManager;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly List<IPage> _pages = new();
    private IPage? _activePage;
    private readonly object _pagesLock = new();

    /// <summary>
    /// Initializes a new instance of the PlaywrightManagementService class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="browserOptions">Browser configuration options</param>
    /// <param name="eventBus">Event bus for publishing events</param>
    /// <param name="stateManager">State manager for tracking state</param>
    public PlaywrightManagementService(
        ILogger<PlaywrightManagementService> logger,
        IOptions<BrowserOptions> browserOptions,
        IEventBus eventBus,
        IPlaywrightStateManager stateManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _browserOptions = browserOptions?.Value ?? throw new ArgumentNullException(nameof(browserOptions));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
    }

    /// <inheritdoc />
    public IPage? ActivePage => _activePage;

    /// <inheritdoc />
    public IReadOnlyList<IPage> Pages
    {
        get
        {
            lock (_pagesLock)
            {
                return _pages.ToList().AsReadOnly();
            }
        }
    }

    /// <inheritdoc />
    public bool IsBrowserOpen => _browser?.IsConnected == true;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_browser != null && _browser.IsConnected)
        {
            _logger.LogWarning("Browser is already initialized and connected");
            return;
        }

        try
        {
            _logger.LogInformation("Initializing Playwright browser with type {BrowserType}, headless: {Headless}", 
                _browserOptions.BrowserType, _browserOptions.Headless);

            _playwright = await Playwright.CreateAsync();
            _logger.LogDebug("Playwright instance created successfully");

            _browser = await CreateBrowserAsync(_playwright, _browserOptions);
            _logger.LogDebug("Browser launched successfully");

            // Update state
            await _stateManager.UpdateStateAsync(state => state.With(
                browserType: _browserOptions.BrowserType,
                isBrowserOpen: true));

            // Publish event
            await _eventBus.PublishAsync(new BrowserInitializedEvent(_browserOptions.BrowserType));

            _logger.LogInformation("Browser initialized successfully with type {BrowserType}", _browserOptions.BrowserType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize browser with type {BrowserType}", _browserOptions.BrowserType);
            
            // Cleanup on failure
            try
            {
                _playwright?.Dispose();
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, "Error during cleanup after initialization failure");
            }
            
            _browser = null;
            _playwright = null;
            
            throw new InvalidOperationException($"Failed to initialize Playwright browser: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IPage> CreatePageAsync(string? url = null)
    {
        if (_browser == null || !_browser.IsConnected)
        {
            _logger.LogError("Browser not initialized or disconnected. Call InitializeAsync() first.");
            throw new InvalidOperationException("Browser not initialized or disconnected. Call InitializeAsync() first.");
        }

        try
        {
            _logger.LogInformation("Creating new page{Url}", url != null ? $" with URL: {url}" : "");

            var page = await _browser.NewPageAsync();
            _logger.LogDebug("New page created with ID {PageId}", GetPageId(page));

            // Set viewport if configured
            if (_browserOptions.ViewportWidth > 0 && _browserOptions.ViewportHeight > 0)
            {
                await page.SetViewportSizeAsync(_browserOptions.ViewportWidth, _browserOptions.ViewportHeight);
                _logger.LogDebug("Viewport set to {Width}x{Height}", _browserOptions.ViewportWidth, _browserOptions.ViewportHeight);
            }

            // Set user agent if configured
            if (!string.IsNullOrEmpty(_browserOptions.UserAgent))
            {
                await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
                {
                    ["User-Agent"] = _browserOptions.UserAgent
                });
                _logger.LogDebug("User agent set to {UserAgent}", _browserOptions.UserAgent);
            }

            // Add to pages list
            lock (_pagesLock)
            {
                _pages.Add(page);
            }

            // Set as active page if it's the first page
            if (_activePage == null)
            {
                _activePage = page;
                _logger.LogDebug("Set as active page");
            }

            // Navigate to URL if provided
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    await NavigateToUrlAsync(page, url);
                }
                catch (Exception navEx)
                {
                    _logger.LogWarning(navEx, "Failed to navigate to URL {Url} during page creation, but page was created successfully", url);
                    // Don't rethrow - page creation succeeded, navigation failed
                }
            }

            // Update state
            await _stateManager.UpdateStateAsync(state => state.With(
                pageCount: _pages.Count,
                activePageUrl: url));

            // Publish event
            await _eventBus.PublishAsync(new PageCreatedEvent(GetPageId(page), url));

            _logger.LogInformation("Page created successfully{Url}", url != null ? $" and navigated to {url}" : "");
            return page;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create page{Url}", url != null ? $" with URL: {url}" : "");
            throw new InvalidOperationException($"Failed to create page: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task NavigateToUrlAsync(string url)
    {
        await NavigateToUrlAsync(_activePage, url);
    }

    /// <inheritdoc />
    public async Task NavigateToUrlAsync(IPage page, string url)
    {
        if (page == null)
        {
            _logger.LogError("Page is null for navigation to {Url}", url);
            throw new ArgumentNullException(nameof(page));
        }

        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("URL is null or empty for page {PageId}", GetPageId(page));
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        try
        {
            _logger.LogInformation("Navigating page {PageId} to {Url}", GetPageId(page), url);

            var response = await page.GotoAsync(url);
            _logger.LogDebug("Navigation response status: {StatusCode}", response?.Status);

            // Update active page if this is the current active page
            if (page == _activePage)
            {
                await _stateManager.UpdateStateAsync(state => state.With(activePageUrl: url));
                _logger.LogDebug("Updated state with new active page URL: {Url}", url);
            }

            // Publish event
            await _eventBus.PublishAsync(new PageNavigatedEvent(url, GetPageId(page)));

            _logger.LogInformation("Successfully navigated page {PageId} to {Url}", GetPageId(page), url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to navigate page {PageId} to {Url}", GetPageId(page), url);
            throw new InvalidOperationException($"Failed to navigate to {url}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task ClosePageAsync(IPage page)
    {
        if (page == null)
        {
            _logger.LogError("Cannot close null page");
            throw new ArgumentNullException(nameof(page));
        }

        var pageId = GetPageId(page);
        
        try
        {
            _logger.LogInformation("Closing page {PageId}", pageId);

            // Check if page is still valid
            if (page.IsClosed)
            {
                _logger.LogWarning("Page {PageId} is already closed", pageId);
            }
            else
            {
                await page.CloseAsync();
                _logger.LogDebug("Page {PageId} closed successfully", pageId);
            }

            lock (_pagesLock)
            {
                _pages.Remove(page);
            }

            // If this was the active page, set a new active page or null
            if (page == _activePage)
            {
                _activePage = _pages.FirstOrDefault();
                _logger.LogDebug("Active page changed to {NewActivePageId}", _activePage != null ? GetPageId(_activePage) : "null");
            }

            // Update state
            await _stateManager.UpdateStateAsync(state => state.With(
                pageCount: _pages.Count,
                activePageUrl: _activePage?.Url));

            _logger.LogInformation("Page {PageId} closed successfully. Remaining pages: {PageCount}", pageId, _pages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close page {PageId}", pageId);
            throw new InvalidOperationException($"Failed to close page {pageId}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task CloseBrowserAsync()
    {
        try
        {
            _logger.LogInformation("Closing browser and all pages");

            if (_browser != null)
            {
                if (_browser.IsConnected)
                {
                    await _browser.CloseAsync();
                    _logger.LogDebug("Browser closed successfully");
                }
                else
                {
                    _logger.LogWarning("Browser was already disconnected");
                }
                _browser = null;
            }
            else
            {
                _logger.LogWarning("Browser was already null");
            }

            lock (_pagesLock)
            {
                var pageCount = _pages.Count;
                _pages.Clear();
                _logger.LogDebug("Cleared {PageCount} pages from internal list", pageCount);
            }

            _activePage = null;

            // Update state
            await _stateManager.UpdateStateAsync(state => state.With(
                isBrowserOpen: false,
                pageCount: 0,
                activePageUrl: null));

            // Publish event
            await _eventBus.PublishAsync(new BrowserClosedEvent());

            _logger.LogInformation("Browser closed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close browser");
            throw new InvalidOperationException($"Failed to close browser: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        try
        {
            _logger.LogInformation("Disposing PlaywrightManagementService");

            // Close browser first
            await CloseBrowserAsync();

            // Dispose Playwright instance
            if (_playwright != null)
            {
                _playwright.Dispose();
                _playwright = null;
                _logger.LogDebug("Playwright instance disposed");
            }

            _logger.LogInformation("PlaywrightManagementService disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PlaywrightManagementService disposal");
            // Don't rethrow during disposal to avoid masking other disposal errors
        }
    }

    private async Task<IBrowser> CreateBrowserAsync(IPlaywright playwright, BrowserOptions options)
    {
        _logger.LogDebug("Creating browser with type {BrowserType}, headless: {Headless}", 
            options.BrowserType, options.Headless);

        var browserType = options.BrowserType.ToLowerInvariant() switch
        {
            "firefox" => playwright.Firefox,
            "webkit" => playwright.Webkit,
            _ => playwright.Chromium
        };

        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = options.Headless
        };

        // Apply custom launch options
        foreach (var kvp in options.LaunchOptions)
        {
            switch (kvp.Key.ToLowerInvariant())
            {
                case "slowmo":
                    if (kvp.Value is int slowMo)
                    {
                        launchOptions.SlowMo = slowMo;
                        _logger.LogDebug("Applied slowMo: {SlowMo}ms", slowMo);
                    }
                    break;
                default:
                    _logger.LogDebug("Ignored unknown launch option: {Key} = {Value}", kvp.Key, kvp.Value);
                    break;
            }
        }

        try
        {
            var browser = await browserType.LaunchAsync(launchOptions);
            _logger.LogDebug("Browser launched successfully with type {BrowserType}", options.BrowserType);
            return browser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch browser with type {BrowserType}", options.BrowserType);
            throw;
        }
    }

    private static string GetPageId(IPage page)
    {
        // Use a simple identifier for the page
        return $"page-{page.GetHashCode()}";
    }
}
