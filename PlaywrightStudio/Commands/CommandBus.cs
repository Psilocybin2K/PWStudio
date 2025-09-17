using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlaywrightStudio.Commands;
using PlaywrightStudio.Handlers;

namespace PlaywrightStudio.Commands;

/// <summary>
/// Command bus implementation for routing commands to handlers
/// </summary>
public class CommandBus : ICommandBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandBus> _logger;

    /// <summary>
    /// Initializes a new instance of the CommandBus class
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving handlers</param>
    /// <param name="logger">Logger instance</param>
    public CommandBus(IServiceProvider serviceProvider, ILogger<CommandBus> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        try
        {
            _logger.LogDebug("Sending command {CommandType} with ID {CommandId}", 
                typeof(TCommand).Name, command.CommandId);

            // Get the handler for this command type
            var handler = _serviceProvider.GetService<ICommandHandler<TCommand>>();
            if (handler == null)
            {
                _logger.LogError("No handler found for command type {CommandType}", typeof(TCommand).Name);
                throw new InvalidOperationException($"No handler found for command type {typeof(TCommand).Name}");
            }

            // Execute the command
            await handler.HandleAsync(command);

            _logger.LogDebug("Command {CommandType} with ID {CommandId} handled successfully", 
                typeof(TCommand).Name, command.CommandId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error handling command {CommandType} with ID {CommandId}", 
                typeof(TCommand).Name, command.CommandId);
            throw;
        }
    }
}
