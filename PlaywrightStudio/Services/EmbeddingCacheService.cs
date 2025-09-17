using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlaywrightStudio.Configuration;

namespace PlaywrightStudio.Services;

/// <summary>
/// Service for managing file-based embedding cache
/// </summary>
public class EmbeddingCacheService
{
    private readonly EmbeddingCacheOptions _options;
    private readonly ILogger<EmbeddingCacheService> _logger;
    private readonly string _cacheDirectory;

    public EmbeddingCacheService(IOptions<EmbeddingCacheOptions> options, ILogger<EmbeddingCacheService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _cacheDirectory = Path.GetFullPath(_options.CacheDirectory);
        
        // Ensure cache directory exists
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
            _logger.LogInformation("Created embedding cache directory: {CacheDirectory}", _cacheDirectory);
        }
    }

    /// <summary>
    /// Generates a cache key for the given text
    /// </summary>
    public string GenerateCacheKey(string text)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Gets the cache file path for a given cache key
    /// </summary>
    private string GetCacheFilePath(string cacheKey)
    {
        return Path.Combine(_cacheDirectory, $"{cacheKey}.json");
    }

    /// <summary>
    /// Retrieves a cached embedding if it exists and is not expired
    /// </summary>
    public async Task<Embedding<float>?> GetCachedEmbeddingAsync(string text)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        try
        {
            var cacheKey = GenerateCacheKey(text);
            var cacheFilePath = GetCacheFilePath(cacheKey);

            if (!File.Exists(cacheFilePath))
            {
                return null;
            }

            var fileInfo = new FileInfo(cacheFilePath);
            var fileAge = DateTime.UtcNow - fileInfo.CreationTimeUtc;
            
            if (fileAge > _options.GetCacheExpirationTimeSpan())
            {
                _logger.LogDebug("Cache entry expired for key: {CacheKey}", cacheKey);
                File.Delete(cacheFilePath);
                return null;
            }

            var jsonContent = await File.ReadAllTextAsync(cacheFilePath);
            var cacheEntry = JsonSerializer.Deserialize<EmbeddingCacheEntry>(jsonContent);
            
            if (cacheEntry == null)
            {
                _logger.LogWarning("Failed to deserialize cache entry for key: {CacheKey}", cacheKey);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return new Embedding<float>(cacheEntry.Values);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached embedding for text: {Text}", text);
            return null;
        }
    }

    /// <summary>
    /// Stores an embedding in the cache
    /// </summary>
    public async Task StoreEmbeddingAsync(string text, Embedding<float> embedding)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            var cacheKey = GenerateCacheKey(text);
            var cacheFilePath = GetCacheFilePath(cacheKey);

            var cacheEntry = new EmbeddingCacheEntry
            {
                Text = text,
                Values = embedding.Vector.ToArray(),
                CreatedAt = DateTime.UtcNow
            };

            var jsonContent = JsonSerializer.Serialize(cacheEntry, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            await File.WriteAllTextAsync(cacheFilePath, jsonContent);
            _logger.LogDebug("Stored embedding in cache with key: {CacheKey}", cacheKey);

            // Clean up old cache entries if cache size limit is exceeded
            await CleanupCacheIfNeededAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing embedding in cache for text: {Text}", text);
        }
    }

    /// <summary>
    /// Cleans up expired cache entries and enforces size limits
    /// </summary>
    private Task CleanupCacheIfNeededAsync()
    {
        try
        {
            var cacheFiles = Directory.GetFiles(_cacheDirectory, "*.json");
            var totalSize = cacheFiles.Sum(file => new FileInfo(file).Length);
            var maxSize = _options.GetMaxCacheSizeBytes();

            if (totalSize <= maxSize)
            {
                return Task.CompletedTask;
            }

            _logger.LogInformation("Cache size limit exceeded. Current size: {CurrentSizeMB}MB, Max: {MaxSizeMB}MB", 
                totalSize / (1024 * 1024), maxSize / (1024 * 1024));

            // Get files sorted by creation time (oldest first)
            var filesWithInfo = cacheFiles
                .Select(file => new FileInfo(file))
                .OrderBy(f => f.CreationTimeUtc)
                .ToList();

            var currentSize = totalSize;
            var filesToDelete = new List<FileInfo>();

            // Remove expired files first
            foreach (var file in filesWithInfo)
            {
                var fileAge = DateTime.UtcNow - file.CreationTimeUtc;
                if (fileAge > _options.GetCacheExpirationTimeSpan())
                {
                    filesToDelete.Add(file);
                    currentSize -= file.Length;
                }
            }

            // If still over limit, remove oldest files
            if (currentSize > maxSize)
            {
                var remainingFiles = filesWithInfo.Except(filesToDelete).ToList();
                foreach (var file in remainingFiles)
                {
                    filesToDelete.Add(file);
                    currentSize -= file.Length;
                    if (currentSize <= maxSize)
                    {
                        break;
                    }
                }
            }

            // Delete selected files
            foreach (var file in filesToDelete)
            {
                try
                {
                    File.Delete(file.FullName);
                    _logger.LogDebug("Deleted cache file: {FileName}", file.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete cache file: {FileName}", file.Name);
                }
            }

            if (filesToDelete.Count > 0)
            {
                _logger.LogInformation("Cleaned up {DeletedCount} cache files", filesToDelete.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    public Task<CacheStatistics> GetCacheStatisticsAsync()
    {
        try
        {
            var cacheFiles = Directory.GetFiles(_cacheDirectory, "*.json");
            var totalSize = cacheFiles.Sum(file => new FileInfo(file).Length);
            var expiredCount = 0;
            var validCount = 0;

            foreach (var file in cacheFiles)
            {
                var fileInfo = new FileInfo(file);
                var fileAge = DateTime.UtcNow - fileInfo.CreationTimeUtc;
                
                if (fileAge > _options.GetCacheExpirationTimeSpan())
                {
                    expiredCount++;
                }
                else
                {
                    validCount++;
                }
            }

            return Task.FromResult(new CacheStatistics
            {
                TotalFiles = cacheFiles.Length,
                ValidFiles = validCount,
                ExpiredFiles = expiredCount,
                TotalSizeBytes = totalSize,
                MaxSizeBytes = _options.GetMaxCacheSizeBytes(),
                CacheDirectory = _cacheDirectory
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return Task.FromResult(new CacheStatistics
            {
                CacheDirectory = _cacheDirectory
            });
        }
    }
}

/// <summary>
/// Represents a cached embedding entry
/// </summary>
public class EmbeddingCacheEntry
{
    public string Text { get; set; } = string.Empty;
    public float[] Values { get; set; } = Array.Empty<float>();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Cache statistics information
/// </summary>
public class CacheStatistics
{
    public int TotalFiles { get; set; }
    public int ValidFiles { get; set; }
    public int ExpiredFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public long MaxSizeBytes { get; set; }
    public string CacheDirectory { get; set; } = string.Empty;
    
    public double TotalSizeMB => TotalSizeBytes / (1024.0 * 1024.0);
    public double MaxSizeMB => MaxSizeBytes / (1024.0 * 1024.0);
    public double UsagePercentage => MaxSizeBytes > 0 ? (double)TotalSizeBytes / MaxSizeBytes * 100 : 0;
}
