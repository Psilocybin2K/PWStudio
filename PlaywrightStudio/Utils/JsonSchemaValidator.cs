using System.Text.Json;
using Json.Schema;

namespace PlaywrightStudio.Utils;

public class JsonSchemaValidator : ISchemaValidator
{
    private readonly Dictionary<string, JsonSchema> _schemaCache = new();

    public ValidationResult Validate<T>(string json, string schemaPath) where T : class
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return Validate<T>(document, schemaPath);
        }
        catch (JsonException ex)
        {
            return ValidationResult.Failure($"Invalid JSON: {ex.Message}");
        }
    }

    public ValidationResult Validate<T>(JsonDocument jsonDocument, string schemaPath) where T : class
    {
        try
        {
            var schema = GetOrLoadSchema(schemaPath);
            var validationResult = schema.Validate(jsonDocument.RootElement);
            
            return validationResult.IsValid 
                ? ValidationResult.Success() 
                : ValidationResult.Failure($"Schema validation failed: {validationResult}");
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Schema validation failed: {ex.Message}");
        }
    }

    private JsonSchema GetOrLoadSchema(string schemaPath)
    {
        if (_schemaCache.TryGetValue(schemaPath, out var cachedSchema))
        {
            return cachedSchema;
        }

        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Schema file not found: {schemaPath}");
        }

        var schemaJson = File.ReadAllText(schemaPath);
        var schema = JsonSchema.FromText(schemaJson);
        _schemaCache[schemaPath] = schema;
        
        return schema;
    }
}
