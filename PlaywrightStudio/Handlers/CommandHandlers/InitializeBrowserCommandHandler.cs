using Microsoft.Extensions.Logging;
using PlaywrightStudio.Commands;
using PlaywrightStudio.Interfaces;

namespace PlaywrightStudio.Handlers.CommandHandlers;

/// <summary>
/// Handler for InitializeBrowserCommand
/// </summary>
public class InitializeBrowserCommandHandler : ICommandHandler<InitializeBrowserCommand>
{
    private readonly IPlaywrightManagementService _playwrightService;
    private readonly ILogger<InitializeBrowserCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the InitializeBrowserCommandHandler class
    /// </summary>
    /// <param name="playwrightService">Playwright management service</param>
    /// <param name="logger">Logger instance</param>
    public InitializeBrowserCommandHandler(
        IPlaywrightManagementService playwrightService,
        ILogger<InitializeBrowserCommandHandler> logger)
    {
        _playwrightService = playwrightService ?? throw new ArgumentNullException(nameof(playwrightService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task HandleAsync(InitializeBrowserCommand command)
    {
        _logger.LogInformation("Handling InitializeBrowserCommand {CommandId}", command.CommandId);
        
        try
        {
            await _playwrightService.InitializeAsync();
            _logger.LogInformation("Browser initialization completed for command {CommandId}", command.CommandId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle InitializeBrowserCommand {CommandId}", command.CommandId);
            throw;
        }
    }
}
