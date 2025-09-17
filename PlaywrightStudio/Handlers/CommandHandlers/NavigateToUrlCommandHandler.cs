using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using PlaywrightStudio.Commands;
using PlaywrightStudio.Interfaces;

namespace PlaywrightStudio.Handlers.CommandHandlers;

/// <summary>
/// Handler for NavigateToUrlCommand
/// </summary>
public class NavigateToUrlCommandHandler : ICommandHandler<NavigateToUrlCommand>
{
    private readonly IPlaywrightManagementService _playwrightService;
    private readonly ILogger<NavigateToUrlCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the NavigateToUrlCommandHandler class
    /// </summary>
    /// <param name="playwrightService">Playwright management service</param>
    /// <param name="logger">Logger instance</param>
    public NavigateToUrlCommandHandler(
        IPlaywrightManagementService playwrightService,
        ILogger<NavigateToUrlCommandHandler> logger)
    {
        _playwrightService = playwrightService ?? throw new ArgumentNullException(nameof(playwrightService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task HandleAsync(NavigateToUrlCommand command)
    {
        _logger.LogInformation("Handling NavigateToUrlCommand {CommandId} to URL: {Url}", 
            command.CommandId, command.Url);
        
        try
        {
            if (string.IsNullOrEmpty(command.PageId))
            {
                // Navigate active page
                await _playwrightService.NavigateToUrlAsync(command.Url);
            }
            else
            {
                // Find specific page and navigate
                var pages = _playwrightService.Pages;
                var targetPage = pages.FirstOrDefault(p => GetPageId(p) == command.PageId) ?? throw new InvalidOperationException($"Page with ID {command.PageId} not found");
                await _playwrightService.NavigateToUrlAsync(targetPage, command.Url);
            }
            
            _logger.LogInformation("Navigation completed for command {CommandId}", command.CommandId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to handle NavigateToUrlCommand {CommandId}", command.CommandId);
            throw;
        }
    }

    private static string GetPageId(IPage page)
    {
        return $"page-{page.GetHashCode()}";
    }
}
