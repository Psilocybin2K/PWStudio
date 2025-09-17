using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlaywrightStudio.Commands;
using PlaywrightStudio.Configuration;
using PlaywrightStudio.Events;
using PlaywrightStudio.Factories;
using PlaywrightStudio.Handlers;
using PlaywrightStudio.Handlers.CommandHandlers;
using PlaywrightStudio.Handlers.EventHandlers;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.Loaders;
using PlaywrightStudio.Parsers;
using PlaywrightStudio.Repositories;
using PlaywrightStudio.Services;
using PlaywrightStudio.State;
using PlaywrightStudio.Utils;

namespace PlaywrightStudio.Extensions;

/// <summary>
/// Extension methods for configuring PlaywrightStudio services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PlaywrightStudio services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPlaywrightStudio(
        this IServiceCollection services, 
        Action<PlaywrightStudioOptions>? configureOptions = null)
    {
        // Register configuration
        services.Configure<PlaywrightStudioOptions>(configureOptions ?? (opts => { }));

        // Register CQRS infrastructure
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<ICommandBus, CommandBus>();
        services.AddSingleton<IPlaywrightStateManager, PlaywrightStateManager>();
        
        // Register Playwright management service
        services.AddSingleton<IPlaywrightManagementService, PlaywrightManagementService>();
        
        // Register command handlers
        services.AddTransient<ICommandHandler<InitializeBrowserCommand>, InitializeBrowserCommandHandler>();
        services.AddTransient<ICommandHandler<CreatePageCommand>, CreatePageCommandHandler>();
        services.AddTransient<ICommandHandler<NavigateToUrlCommand>, NavigateToUrlCommandHandler>();
        services.AddTransient<ICommandHandler<CloseBrowserCommand>, CloseBrowserCommandHandler>();
        
        // Register event handlers
        services.AddTransient<IEventHandler<StateChangedEvent>, StateChangeEventHandler>();
        services.AddTransient<PageNavigationEventHandler>();

        // Register core services
        services.AddSingleton<ISchemaValidator, JsonSchemaValidator>();
        services.AddSingleton<IVariableParser, RegexVariableParser>();
        services.AddSingleton<IModelLoader, JsonModelLoader>();
        services.AddSingleton<IModelRepository, FileSystemModelRepository>();
        services.AddSingleton<IPluginFactory, DynamicPluginFactory>();
        services.AddSingleton<PlaywrightStudioService>();
        
        // Register embedding services
        services.AddSingleton<EmbeddingCacheService>();
        services.AddSingleton<EmbeddingGeneratorService>();
        
        // Register search services
        services.AddSingleton<PageObjectModelSearchService>();
        
        // Register memory cache if not already registered
        services.AddMemoryCache();

        // Register logging if not already registered
        services.AddLogging();

        return services;
    }

    /// <summary>
    /// Adds PlaywrightStudio services with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">Configuration options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPlaywrightStudio(
        this IServiceCollection services, 
        PlaywrightStudioOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));
        return services.AddPlaywrightStudio((Action<PlaywrightStudioOptions>?)null);
    }

    /// <summary>
    /// Adds PlaywrightStudio services with custom implementations
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Configuration action</param>
    /// <param name="configureServices">Service configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPlaywrightStudio(
        this IServiceCollection services,
        Action<PlaywrightStudioOptions>? configureOptions = null,
        Action<IServiceCollection>? configureServices = null)
    {
        services.AddPlaywrightStudio(configureOptions);
        configureServices?.Invoke(services);
        return services;
    }

    /// <summary>
    /// Adds PlaywrightStudio services with configuration from IConfiguration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPlaywrightStudio(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration sections
        services.Configure<PlaywrightStudioOptions>(configuration.GetSection("PlaywrightStudio"));
        services.Configure<BrowserOptions>(configuration.GetSection("Browser"));
        services.Configure<EmbeddingCacheOptions>(configuration.GetSection("PlaywrightStudio:EmbeddingCache"));

        // Register CQRS infrastructure
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<ICommandBus, CommandBus>();
        services.AddSingleton<IPlaywrightStateManager, PlaywrightStateManager>();
        
        // Register Playwright management service
        services.AddSingleton<IPlaywrightManagementService, PlaywrightManagementService>();
        
        // Register command handlers
        services.AddTransient<ICommandHandler<InitializeBrowserCommand>, InitializeBrowserCommandHandler>();
        services.AddTransient<ICommandHandler<CreatePageCommand>, CreatePageCommandHandler>();
        services.AddTransient<ICommandHandler<NavigateToUrlCommand>, NavigateToUrlCommandHandler>();
        services.AddTransient<ICommandHandler<CloseBrowserCommand>, CloseBrowserCommandHandler>();
        
        // Register event handlers
        services.AddTransient<IEventHandler<StateChangedEvent>, StateChangeEventHandler>();
        services.AddTransient<PageNavigationEventHandler>();

        // Register core services
        services.AddSingleton<ISchemaValidator, JsonSchemaValidator>();
        services.AddSingleton<IVariableParser, RegexVariableParser>();
        services.AddSingleton<IModelLoader, JsonModelLoader>();
        services.AddSingleton<IModelRepository, FileSystemModelRepository>();
        services.AddSingleton<IPluginFactory, DynamicPluginFactory>();
        services.AddSingleton<PlaywrightStudioService>();
        
        // Register embedding services
        services.AddSingleton<EmbeddingCacheService>();
        services.AddSingleton<EmbeddingGeneratorService>();
        
        // Register search services
        services.AddSingleton<PageObjectModelSearchService>();
        
        // Register memory cache if not already registered
        services.AddMemoryCache();

        // Register logging if not already registered
        services.AddLogging();

        return services;
    }
}
