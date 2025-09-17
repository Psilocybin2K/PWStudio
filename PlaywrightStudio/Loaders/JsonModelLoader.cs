using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlaywrightStudio.Configuration;
using PlaywrightStudio.Exceptions;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.Models;
using PlaywrightStudio.Services;
using PlaywrightStudio.Utils;

namespace PlaywrightStudio.Loaders;

/// <summary>
/// Implementation of IModelLoader for JSON files
/// </summary>
public class JsonModelLoader : IModelLoader
{
    private readonly ISchemaValidator _schemaValidator;
    private readonly ILogger<JsonModelLoader> _logger;
    private readonly PlaywrightStudioOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the JsonModelLoader class
    /// </summary>
    /// <param name="schemaValidator">Schema validator instance</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="options">Configuration options</param>
    public JsonModelLoader(
        EmbeddingGeneratorService embeddingGeneratorService,
        ISchemaValidator schemaValidator,
        ILogger<JsonModelLoader> logger,
        IOptions<PlaywrightStudioOptions> options)
    {
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async Task<PageObjectModel?> LoadModelAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("File path is null or empty");
            return null;
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File does not exist: {FilePath}", filePath);
            return null;
        }

        try
        {
            _logger.LogDebug("Loading model from file: {FilePath}", filePath);

            var json = await File.ReadAllTextAsync(filePath);
            
            // Skip schema files
            if (filePath.EndsWith(".schema.json"))
            {
                _logger.LogDebug("Skipping schema file: {FilePath}", filePath);
                return null;
            }

            // Validate against schema if not disabled
            if (!_options.SkipSchemaValidation)
            {
                var validationResult = _schemaValidator.Validate<PageObjectModel>(json, _options.DefaultSchemaPath);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Schema validation failed for {FilePath}: {Errors}", 
                        filePath, string.Join(", ", validationResult.Errors));
                    
                    if (!_options.IncludeDebugInfo)
                    {
                        return null;
                    }
                    
                    throw new ModelValidationException(filePath, validationResult.Errors);
                }
            }

            var model = JsonSerializer.Deserialize<PageObjectModel>(json, _jsonOptions);
            
            if (model == null)
            {
                _logger.LogWarning("Failed to deserialize model from file: {FilePath}", filePath);
                return null;
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.LogWarning("Model has no name in file: {FilePath}", filePath);
                return null;
            }

            _logger.LogDebug("Successfully loaded model '{ModelName}' from {FilePath}", model.Name, filePath);
            return model;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error in file {FilePath}", filePath);
            throw new ModelValidationException(filePath, new[] { $"Invalid JSON: {ex.Message}" }, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading model from file {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PageObjectModel>> LoadModelsFromDirectoryAsync(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            _logger.LogWarning("Directory path is null or empty");
            return Enumerable.Empty<PageObjectModel>();
        }

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
            return Enumerable.Empty<PageObjectModel>();
        }

        var models = new List<PageObjectModel>();
        var files = _options.ModelFilePatterns
            .SelectMany(pattern => Directory.GetFiles(directoryPath, pattern))
            .Distinct()
            .ToArray();

        _logger.LogDebug("Found {FileCount} files matching patterns in {DirectoryPath}", 
            files.Length, directoryPath);

        foreach (var file in files)
        {
            try
            {
                var model = await LoadModelAsync(file);
                if (model != null)
                {
                    models.Add(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model from file {FilePath}", file);
                // Continue with other files
            }
        }

        _logger.LogInformation("Loaded {ModelCount} models from {DirectoryPath}", 
            models.Count, directoryPath);

        return models;
    }

    /// <inheritdoc />
    public async Task<bool> IsValidModelFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            
            // Skip schema files
            if (filePath.EndsWith(".schema.json"))
            {
                return false;
            }

            // Basic JSON validation
            JsonDocument.Parse(json);
            
            // Schema validation if not disabled
            if (!_options.SkipSchemaValidation)
            {
                var validationResult = _schemaValidator.Validate<PageObjectModel>(json, _options.DefaultSchemaPath);
                return validationResult.IsValid;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
