using System.ComponentModel.DataAnnotations;

namespace PlaywrightStudio.Configuration;

/// <summary>
/// Configuration options for Ollama integration
/// </summary>
public class OllamaOptions
{
    /// <summary>
    /// Base address of the Ollama API server
    /// </summary>
    [Required]
    public string BaseAddress { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Request timeout in minutes
    /// </summary>
    [Range(1, 120)]
    public int TimeoutMinutes { get; set; } = 20;

    /// <summary>
    /// Model name to use for chat completion
    /// </summary>
    [Required]
    public string Model { get; set; } = "llama3.2";

    /// <summary>
    /// Temperature for text generation (0.0 to 2.0)
    /// </summary>
    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    [Range(1, 8192)]
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Default model name for Ollama API client
    /// </summary>
    [Required]
    public string DefaultModelName { get; set; } = "llama3.2";

    /// <summary>
    /// Model name to use for embedding generation
    /// </summary>
    [Required]
    public string EmbeddingModel { get; set; } = "bge-large";

    /// <summary>
    /// Gets the timeout as a TimeSpan
    /// </summary>
    public TimeSpan GetTimeoutTimeSpan()
    {
        return TimeSpan.FromMinutes(TimeoutMinutes);
    }
}
