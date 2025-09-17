using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlaywrightStudio.Configuration;
using PlaywrightStudio.Exceptions;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.Models;

namespace PlaywrightStudio.Repositories;

/// <summary>
/// File system implementation of IModelRepository
/// </summary>
public class FileSystemModelRepository : IModelRepository
{
    private readonly IModelLoader _modelLoader;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FileSystemModelRepository> _logger;
    private readonly PlaywrightStudioOptions _options;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private bool _isLoaded = false;

    private const string CacheKeyPrefix = "PlaywrightStudio_Model_";
    private const string AllModelsCacheKey = "PlaywrightStudio_AllModels";
    private const string ModelCountCacheKey = "PlaywrightStudio_ModelCount";

    /// <summary>
    /// Initializes a new instance of the FileSystemModelRepository class
    /// </summary>
    /// <param name="modelLoader">Model loader instance</param>
    /// <param name="cache">Memory cache instance</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="options">Configuration options</param>
    public FileSystemModelRepository(
        IModelLoader modelLoader,
        IMemoryCache cache,
        ILogger<FileSystemModelRepository> logger,
        IOptions<PlaywrightStudioOptions> options)
    {
        _modelLoader = modelLoader ?? throw new ArgumentNullException(nameof(modelLoader));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PageObjectModel>> GetAllModelsAsync()
    {
        if (_options.EnableCaching && _cache.TryGetValue(AllModelsCacheKey, out IEnumerable<PageObjectModel>? cachedModels))
        {
            _logger.LogDebug("Returning cached models");
            return cachedModels ?? Enumerable.Empty<PageObjectModel>();
        }

        await EnsureModelsLoadedAsync();

        var models = await LoadModelsFromDirectoryAsync();
        
        if (_options.EnableCaching)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.GetCacheExpirationTimeSpan(),
                Priority = CacheItemPriority.Normal
            };
            
            _cache.Set(AllModelsCacheKey, models, cacheOptions);
            _logger.LogDebug("Cached {ModelCount} models", models.Count());
        }

        return models;
    }

    /// <inheritdoc />
    public async Task<PageObjectModel?> GetModelByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Model name is null or empty");
            return null;
        }

        var cacheKey = $"{CacheKeyPrefix}{name}";
        
        if (_options.EnableCaching && _cache.TryGetValue(cacheKey, out PageObjectModel? cachedModel))
        {
            _logger.LogDebug("Returning cached model: {ModelName}", name);
            return cachedModel;
        }

        await EnsureModelsLoadedAsync();

        var allModels = await GetAllModelsAsync();
        var model = allModels.FirstOrDefault(m => 
            string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));

        if (model != null && _options.EnableCaching)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.GetCacheExpirationTimeSpan(),
                Priority = CacheItemPriority.Normal
            };
            
            _cache.Set(cacheKey, model, cacheOptions);
            _logger.LogDebug("Cached model: {ModelName}", name);
        }

        return model;
    }

    /// <inheritdoc />
    public async Task<bool> ModelExistsAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var model = await GetModelByNameAsync(name);
        return model != null;
    }

    /// <inheritdoc />
    public async Task<int> GetModelCountAsync()
    {
        if (_options.EnableCaching && _cache.TryGetValue(ModelCountCacheKey, out int cachedCount))
        {
            return cachedCount;
        }

        var models = await GetAllModelsAsync();
        var count = models.Count();

        if (_options.EnableCaching)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.GetCacheExpirationTimeSpan(),
                Priority = CacheItemPriority.Normal
            };
            
            _cache.Set(ModelCountCacheKey, count, cacheOptions);
        }

        return count;
    }

    private async Task EnsureModelsLoadedAsync()
    {
        if (_isLoaded)
        {
            return;
        }

        await _loadSemaphore.WaitAsync();
        try
        {
            if (_isLoaded)
            {
                return;
            }

            _logger.LogDebug("Loading models from directory: {DirectoryPath}", _options.ModelsDirectory);
            
            var models = await LoadModelsFromDirectoryAsync();
            _isLoaded = true;
            
            _logger.LogInformation("Successfully loaded {ModelCount} models", models.Count());
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    private async Task<IEnumerable<PageObjectModel>> LoadModelsFromDirectoryAsync()
    {
        try
        {
            var models = await _modelLoader.LoadModelsFromDirectoryAsync(_options.ModelsDirectory);
            return models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load models from directory: {DirectoryPath}", _options.ModelsDirectory);
            throw new ModelNotFoundException("Failed to load models", ex);
        }
    }
}
