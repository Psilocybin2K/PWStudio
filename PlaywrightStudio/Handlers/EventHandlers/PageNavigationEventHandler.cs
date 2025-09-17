using Microsoft.Extensions.Logging;
using PlaywrightStudio.Events;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.Services;
using PlaywrightStudio.State;

namespace PlaywrightStudio.Handlers.EventHandlers;

/// <summary>
/// Handler for page navigation events that updates the active page object model
/// </summary>
public class PageNavigationEventHandler : IEventHandler<PageNavigatedEvent>
{
    private readonly PageObjectModelSearchService _searchService;
    private readonly IPlaywrightStateManager _stateManager;
    private readonly ILogger<PageNavigationEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the PageNavigationEventHandler class
    /// </summary>
    /// <param name="searchService">Page object model search service</param>
    /// <param name="stateManager">State manager for updating state</param>
    /// <param name="logger">Logger instance</param>
    public PageNavigationEventHandler(
        PageObjectModelSearchService searchService,
        IPlaywrightStateManager stateManager,
        ILogger<PageNavigationEventHandler> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task HandleAsync(PageNavigatedEvent @event)
    {
        _logger.LogDebug("Handling page navigation event for URL '{Url}' on page '{PageId}'", 
            @event.Url, @event.PageId);

        try
        {
            // Find matching page object model
            var matchingModel = await _searchService.FindModelByUrlAsync(@event.Url);

            // Update state with the matching model
            await _stateManager.UpdateStateAsync(state => state.With(
                activePageObjectModel: matchingModel));

            if (matchingModel != null)
            {
                _logger.LogInformation("Updated active page object model to '{ModelName}' for URL '{Url}'", 
                    matchingModel.Name, @event.Url);
            }
            else
            {
                _logger.LogInformation("No matching page object model found for URL '{Url}', cleared active model", 
                    @event.Url);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling page navigation event for URL '{Url}'", @event.Url);
        }
    }
}
