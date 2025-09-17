using PlaywrightStudio.Models;

namespace PlaywrightStudio.Interfaces;

/// <summary>
/// Repository interface for managing PageObjectModel instances
/// </summary>
public interface IModelRepository
{
    /// <summary>
    /// Gets all available page object models
    /// </summary>
    /// <returns>Collection of all models</returns>
    Task<IEnumerable<PageObjectModel>> GetAllModelsAsync();

    /// <summary>
    /// Gets a specific model by name
    /// </summary>
    /// <param name="name">The name of the model to retrieve</param>
    /// <returns>The model if found, null otherwise</returns>
    Task<PageObjectModel?> GetModelByNameAsync(string name);

    /// <summary>
    /// Checks if a model exists by name
    /// </summary>
    /// <param name="name">The name of the model to check</param>
    /// <returns>True if the model exists, false otherwise</returns>
    Task<bool> ModelExistsAsync(string name);

    /// <summary>
    /// Gets the count of available models
    /// </summary>
    /// <returns>Number of available models</returns>
    Task<int> GetModelCountAsync();
}
