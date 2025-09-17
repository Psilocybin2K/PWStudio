using System.Text.RegularExpressions;
using PlaywrightStudio.Interfaces;

namespace PlaywrightStudio.Parsers;

/// <summary>
/// Implementation of IVariableParser using regular expressions
/// </summary>
public class RegexVariableParser : IVariableParser
{
    private readonly Regex _variablePattern;

    /// <summary>
    /// Initializes a new instance of the RegexVariableParser class
    /// </summary>
    public RegexVariableParser()
    {
        _variablePattern = new Regex(@"\{@(\w+)\}", RegexOptions.Compiled);
    }

    /// <summary>
    /// Initializes a new instance of the RegexVariableParser class with custom pattern
    /// </summary>
    /// <param name="pattern">Custom regex pattern for variable references</param>
    public RegexVariableParser(string pattern)
    {
        _variablePattern = new Regex(pattern, RegexOptions.Compiled);
    }

    /// <inheritdoc />
    public string ParseValueWithVariables(string? value, Dictionary<string, object>? variables = null)
    {
        if (string.IsNullOrWhiteSpace(value)) 
            return string.Empty;

        return _variablePattern.Replace(value, match =>
        {
            var variableName = match.Groups[1].Value;

            if (variables != null && variables.TryGetValue(variableName, out var variableValue))
            {
                return variableValue?.ToString() ?? string.Empty;
            }

            // If variable not found, return the original pattern
            return match.Value;
        });
    }

    /// <inheritdoc />
    public bool ContainsVariableReferences(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) 
            return false;
        
        return _variablePattern.IsMatch(value);
    }

    /// <inheritdoc />
    public IEnumerable<string> ExtractVariableNames(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) 
            return Enumerable.Empty<string>();

        return _variablePattern.Matches(value)
            .Cast<Match>()
            .Select(match => match.Groups[1].Value)
            .Distinct();
    }
}
