using Microsoft.Extensions.Logging;
using PlaywrightStudio.Commands;
using PlaywrightStudio.Interfaces;

namespace PlaywrightStudio.Handlers.CommandHandlers;

/// <summary>
/// Handler for CloseBrowserCommand
/// </summary>
public class CloseBrowserCommandHandler : ICommandHandler<CloseBrowserCommand>
{
    private readonly IPlaywrightManagementService _playwrightService;
    private readonly ILogger<CloseBrowserCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the CloseBrowserCommandHandler class
    /// </summary>
    /// <param name="playwrightService">Playwright management service</param>
    /// <param name="logger">Logger instance</param>
    public CloseBrowserCommandHandler(
        IPlaywrightManagementService playwrightService,
        ILogger<CloseBrowserCommandHandler> logger)
    {
        _playwrightService = playwrightService ?? throw new ArgumentNullException(nameof(playwrightService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task HandleAsync(CloseBrowserCommand command)
    {
        _logger.LogInformation("Handling CloseBrowserCommand {CommandId}", command.CommandId);
        
        try
        {
            await _playwrightService.CloseBrowserAsync();
            _logger.LogInformation("Browser closed successfully for command {CommandId}", command.CommandId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle CloseBrowserCommand {CommandId}", command.CommandId);
            throw;
        }
    }
}
