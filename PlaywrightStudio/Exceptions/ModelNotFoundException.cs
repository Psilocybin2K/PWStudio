namespace PlaywrightStudio.Exceptions;

/// <summary>
/// Exception thrown when a requested model is not found
/// </summary>
public class ModelNotFoundException : Exception
{
    /// <summary>
    /// The name of the model that was not found
    /// </summary>
    public string ModelName { get; }

    /// <summary>
    /// Initializes a new instance of the ModelNotFoundException class
    /// </summary>
    /// <param name="modelName">The name of the model that was not found</param>
    public ModelNotFoundException(string modelName) 
        : base($"Model '{modelName}' not found")
    {
        ModelName = modelName;
    }

    /// <summary>
    /// Initializes a new instance of the ModelNotFoundException class
    /// </summary>
    /// <param name="modelName">The name of the model that was not found</param>
    /// <param name="innerException">The inner exception</param>
    public ModelNotFoundException(string modelName, Exception innerException) 
        : base($"Model '{modelName}' not found", innerException)
    {
        ModelName = modelName;
    }
}
