using System.ComponentModel.DataAnnotations;

namespace PlaywrightStudio.Configuration;

/// <summary>
/// Configuration options for browser settings
/// </summary>
public class BrowserOptions
{
    /// <summary>
    /// Whether to run the browser in headless mode
    /// </summary>
    public bool Headless { get; set; } = false;

    /// <summary>
    /// Type of browser to launch (Chromium, Firefox, WebKit)
    /// </summary>
    [Required]
    public string BrowserType { get; set; } = "Chromium";

    /// <summary>
    /// Additional launch options for the browser
    /// </summary>
    public Dictionary<string, object> LaunchOptions { get; set; } = new();

    /// <summary>
    /// Default viewport width
    /// </summary>
    [Range(100, 4096)]
    public int ViewportWidth { get; set; } = 1280;

    /// <summary>
    /// Default viewport height
    /// </summary>
    [Range(100, 4096)]
    public int ViewportHeight { get; set; } = 720;

    /// <summary>
    /// User agent string to use
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Timeout for page operations in milliseconds
    /// </summary>
    [Range(1000, 300000)]
    public int Timeout { get; set; } = 30000;
}
