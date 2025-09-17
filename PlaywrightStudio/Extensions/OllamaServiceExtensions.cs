using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using OllamaSharp;
using PlaywrightStudio.Configuration;

namespace PlaywrightStudio.Extensions;

/// <summary>
/// Extension methods for configuring Ollama services
/// </summary>
public static class OllamaServiceExtensions
{
    /// <summary>
    /// Adds Ollama chat completion services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddOllamaServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind Ollama configuration
        services.Configure<OllamaOptions>(configuration.GetSection("Ollama"));

        // Register Ollama API client
        services.AddSingleton<OllamaApiClient>(serviceProvider =>
        {
            var ollamaOptions = serviceProvider.GetRequiredService<IOptions<OllamaOptions>>().Value;
            
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(ollamaOptions.BaseAddress),
                Timeout = ollamaOptions.GetTimeoutTimeSpan()
            };

            return new OllamaApiClient(httpClient, ollamaOptions.DefaultModelName);
        });

        services.AddOllamaChatCompletion();
        services.AddOllamaEmbeddingGenerator();

        return services;
    }

    /// <summary>
    /// Adds Ollama chat completion services with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddOllamaServices(
        this IServiceCollection services,
        Action<OllamaOptions> configureOptions)
    {
        // Configure options
        services.Configure(configureOptions);

        // Register Ollama API client
        services.AddSingleton<OllamaApiClient>(serviceProvider =>
        {
            var ollamaOptions = serviceProvider.GetRequiredService<IOptions<OllamaOptions>>().Value;
            
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(ollamaOptions.BaseAddress),
                Timeout = ollamaOptions.GetTimeoutTimeSpan()
            };

            return new OllamaApiClient(httpClient, ollamaOptions.DefaultModelName);
        });

        // OllamaApiClient is already registered above and can be used directly

        return services;
    }
}
