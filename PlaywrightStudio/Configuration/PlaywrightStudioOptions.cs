using System.ComponentModel.DataAnnotations;

namespace PlaywrightStudio.Configuration;

/// <summary>
/// Configuration options for PlaywrightStudio
/// </summary>
public class PlaywrightStudioOptions
{
    /// <summary>
    /// Directory path containing page object model files
    /// </summary>
    [Required]
    public string ModelsDirectory { get; set; } = "PageObjectModels";

    /// <summary>
    /// Default schema file path for validation
    /// </summary>
    [Required]
    public string DefaultSchemaPath { get; set; } = "PageObjectModels/login.schema.json";

    /// <summary>
    /// Whether to enable caching of loaded models
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time for loaded models (in format "HH:mm:ss")
    /// </summary>
    public string CacheExpiration { get; set; } = "00:30:00";

    /// <summary>
    /// File patterns to include when loading models
    /// </summary>
    public string[] ModelFilePatterns { get; set; } = { "*.json" };

    /// <summary>
    /// Whether to skip schema validation during model loading
    /// </summary>
    public bool SkipSchemaValidation { get; set; } = false;

    /// <summary>
    /// Whether to include debug information in generated plugins
    /// </summary>
    public bool IncludeDebugInfo { get; set; } = false;

    /// <summary>
    /// Gets the cache expiration as a TimeSpan
    /// </summary>
    public TimeSpan GetCacheExpirationTimeSpan()
    {
        return TimeSpan.TryParse(CacheExpiration, out var result) ? result : TimeSpan.FromMinutes(30);
    }
}
