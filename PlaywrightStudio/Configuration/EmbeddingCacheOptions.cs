using System.ComponentModel.DataAnnotations;

namespace PlaywrightStudio.Configuration;

/// <summary>
/// Configuration options for embedding cache
/// </summary>
public class EmbeddingCacheOptions
{
    /// <summary>
    /// Whether to enable embedding caching
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Directory path for storing embedding cache files
    /// </summary>
    [Required]
    public string CacheDirectory { get; set; } = "Cache/Embeddings";

    /// <summary>
    /// Cache expiration time in hours
    /// </summary>
    public int ExpirationHours { get; set; } = 24;

    /// <summary>
    /// Maximum cache size in MB
    /// </summary>
    public int MaxCacheSizeMB { get; set; } = 100;

    /// <summary>
    /// Gets the cache expiration as a TimeSpan
    /// </summary>
    public TimeSpan GetCacheExpirationTimeSpan()
    {
        return TimeSpan.FromHours(ExpirationHours);
    }

    /// <summary>
    /// Gets the maximum cache size in bytes
    /// </summary>
    public long GetMaxCacheSizeBytes()
    {
        return MaxCacheSizeMB * 1024L * 1024L;
    }
}
