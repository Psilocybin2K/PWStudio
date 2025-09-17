namespace PlaywrightStudio.Exceptions;

/// <summary>
/// Exception thrown when model validation fails
/// </summary>
public class ModelValidationException : Exception
{
    /// <summary>
    /// The file path of the model that failed validation
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The validation errors that occurred
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the ModelValidationException class
    /// </summary>
    /// <param name="filePath">The file path of the model that failed validation</param>
    /// <param name="validationErrors">The validation errors that occurred</param>
    public ModelValidationException(string filePath, IReadOnlyList<string> validationErrors) 
        : base($"Model validation failed for '{filePath}': {string.Join(", ", validationErrors)}")
    {
        FilePath = filePath;
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Initializes a new instance of the ModelValidationException class
    /// </summary>
    /// <param name="filePath">The file path of the model that failed validation</param>
    /// <param name="validationErrors">The validation errors that occurred</param>
    /// <param name="innerException">The inner exception</param>
    public ModelValidationException(string filePath, IReadOnlyList<string> validationErrors, Exception innerException) 
        : base($"Model validation failed for '{filePath}': {string.Join(", ", validationErrors)}", innerException)
    {
        FilePath = filePath;
        ValidationErrors = validationErrors;
    }
}
