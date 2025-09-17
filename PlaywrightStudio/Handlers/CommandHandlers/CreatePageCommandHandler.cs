using Microsoft.Extensions.Logging;
using PlaywrightStudio.Commands;
using PlaywrightStudio.Interfaces;

namespace PlaywrightStudio.Handlers.CommandHandlers;

/// <summary>
/// Handler for CreatePageCommand
/// </summary>
public class CreatePageCommandHandler : ICommandHandler<CreatePageCommand>
{
    private readonly IPlaywrightManagementService _playwrightService;
    private readonly ILogger<CreatePageCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the CreatePageCommandHandler class
    /// </summary>
    /// <param name="playwrightService">Playwright management service</param>
    /// <param name="logger">Logger instance</param>
    public CreatePageCommandHandler(
        IPlaywrightManagementService playwrightService,
        ILogger<CreatePageCommandHandler> logger)
    {
        _playwrightService = playwrightService ?? throw new ArgumentNullException(nameof(playwrightService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task HandleAsync(CreatePageCommand command)
    {
        _logger.LogInformation("Handling CreatePageCommand {CommandId} with URL: {Url}", 
            command.CommandId, command.Url ?? "none");
        
        try
        {
            var page = await _playwrightService.CreatePageAsync(command.Url);
            _logger.LogInformation("Page created successfully for command {CommandId}", command.CommandId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle CreatePageCommand {CommandId}", command.CommandId);
            throw;
        }
    }
}
