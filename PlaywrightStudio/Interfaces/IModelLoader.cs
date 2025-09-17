using PlaywrightStudio.Models;

namespace PlaywrightStudio.Interfaces;

/// <summary>
/// Interface for loading PageObjectModel instances from various sources
/// </summary>
public interface IModelLoader
{
    /// <summary>
    /// Loads a single model from a file path
    /// </summary>
    /// <param name="filePath">Path to the model file</param>
    /// <returns>The loaded model or null if loading fails</returns>
    Task<PageObjectModel?> LoadModelAsync(string filePath);

    /// <summary>
    /// Loads all models from a directory
    /// </summary>
    /// <param name="directoryPath">Path to the directory containing model files</param>
    /// <returns>Collection of loaded models</returns>
    Task<IEnumerable<PageObjectModel>> LoadModelsFromDirectoryAsync(string directoryPath);

    /// <summary>
    /// Validates a model file before loading
    /// </summary>
    /// <param name="filePath">Path to the model file</param>
    /// <returns>True if the file is valid, false otherwise</returns>
    Task<bool> IsValidModelFileAsync(string filePath);
}
