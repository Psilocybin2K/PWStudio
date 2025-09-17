namespace PlaywrightStudio.Exceptions;

/// <summary>
/// Exception thrown when plugin creation fails
/// </summary>
public class PluginCreationException : Exception
{
    /// <summary>
    /// The name of the model for which plugin creation failed
    /// </summary>
    public string ModelName { get; }

    /// <summary>
    /// Initializes a new instance of the PluginCreationException class
    /// </summary>
    /// <param name="modelName">The name of the model for which plugin creation failed</param>
    /// <param name="message">The error message</param>
    public PluginCreationException(string modelName, string message) 
        : base($"Plugin creation failed for model '{modelName}': {message}")
    {
        ModelName = modelName;
    }

    /// <summary>
    /// Initializes a new instance of the PluginCreationException class
    /// </summary>
    /// <param name="modelName">The name of the model for which plugin creation failed</param>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    public PluginCreationException(string modelName, string message, Exception innerException) 
        : base($"Plugin creation failed for model '{modelName}': {message}", innerException)
    {
        ModelName = modelName;
    }
}
