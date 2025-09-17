using System.Text.Json;

namespace PlaywrightStudio.Utils;

public interface ISchemaValidator
{
    ValidationResult Validate<T>(string json, string schemaPath) where T : class;
    ValidationResult Validate<T>(JsonDocument jsonDocument, string schemaPath) where T : class;
}

public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static ValidationResult Success() => new(true, Array.Empty<string>());
    public static ValidationResult Failure(params string[] errors) => new(false, errors);
}
