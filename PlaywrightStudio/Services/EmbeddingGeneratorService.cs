using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlaywrightStudio.Configuration;

namespace PlaywrightStudio.Services;

public class EmbeddingGeneratorService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerationService;
    private readonly ILogger<EmbeddingGeneratorService> _logger;
    private readonly EmbeddingCacheService _cacheService;
    private readonly OllamaOptions _ollamaOptions;

    public EmbeddingGeneratorService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerationService, 
        ILogger<EmbeddingGeneratorService> logger,
        EmbeddingCacheService cacheService,
        IOptions<OllamaOptions> ollamaOptions)
    {
        _embeddingGenerationService = embeddingGenerationService ?? throw new ArgumentNullException(nameof(embeddingGenerationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _ollamaOptions = ollamaOptions?.Value ?? throw new ArgumentNullException(nameof(ollamaOptions));
    }

    public async Task<Embedding<float>> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        }

        _logger.LogDebug("Generating embedding for text: {TextLength} characters", text.Length);

        // Try to get from cache first
        var cachedEmbedding = await _cacheService.GetCachedEmbeddingAsync(text);
        if (cachedEmbedding != null)
        {
            _logger.LogDebug("Using cached embedding for text");
            return cachedEmbedding;
        }

        // Generate new embedding
        _logger.LogDebug("Generating new embedding (cache miss) using model: {Model}", _ollamaOptions.EmbeddingModel);
        var embedding = await _embeddingGenerationService.GenerateAsync(text, new EmbeddingGenerationOptions
        {
            ModelId = _ollamaOptions.EmbeddingModel
        });
        
        // Store in cache
        await _cacheService.StoreEmbeddingAsync(text, embedding);
        
        _logger.LogDebug("Generated and cached new embedding");
        return embedding;
    }

    /// <summary>
    /// Gets cache statistics for monitoring purposes
    /// </summary>
    public async Task<CacheStatistics> GetCacheStatisticsAsync()
    {
        return await _cacheService.GetCacheStatisticsAsync();
    }
}
