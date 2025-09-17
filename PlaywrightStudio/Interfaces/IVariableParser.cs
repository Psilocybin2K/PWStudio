namespace PlaywrightStudio.Interfaces;

/// <summary>
/// Interface for parsing and processing variable references in strings
/// </summary>
public interface IVariableParser
{
    /// <summary>
    /// Parses a value string and extracts variable references in the format {@varName}
    /// </summary>
    /// <param name="value">The value string to parse</param>
    /// <param name="variables">Dictionary of available variables</param>
    /// <returns>The parsed value with variables substituted</returns>
    string ParseValueWithVariables(string? value, Dictionary<string, object>? variables = null);

    /// <summary>
    /// Checks if a value contains variable references in the format {@varName}
    /// </summary>
    /// <param name="value">The value string to check</param>
    /// <returns>True if the value contains variable references</returns>
    bool ContainsVariableReferences(string? value);

    /// <summary>
    /// Extracts all variable names from a value string
    /// </summary>
    /// <param name="value">The value string to parse</param>
    /// <returns>Collection of variable names found in the value</returns>
    IEnumerable<string> ExtractVariableNames(string? value);
}
