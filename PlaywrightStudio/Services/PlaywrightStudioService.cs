using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.Models;

namespace PlaywrightStudio.Services;

/// <summary>
/// Main service for PlaywrightStudio functionality
/// </summary>
public class PlaywrightStudioService
{
    private readonly IModelRepository _modelRepository;
    private readonly IPluginFactory _pluginFactory;
    private readonly IPlaywrightManagementService _playwrightManagementService;
    private readonly ILogger<PlaywrightStudioService> _logger;

    /// <summary>
    /// Initializes a new instance of the PlaywrightStudioService class
    /// </summary>
    /// <param name="modelRepository">Model repository instance</param>
    /// <param name="pluginFactory">Plugin factory instance</param>
    /// <param name="playwrightManagementService">Playwright management service</param>
    /// <param name="logger">Logger instance</param>
    public PlaywrightStudioService(
        IModelRepository modelRepository,
        IPluginFactory pluginFactory,
        IPlaywrightManagementService playwrightManagementService,
        ILogger<PlaywrightStudioService> logger)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _pluginFactory = pluginFactory ?? throw new ArgumentNullException(nameof(pluginFactory));
        _playwrightManagementService = playwrightManagementService ?? throw new ArgumentNullException(nameof(playwrightManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes PlaywrightStudio with the Playwright management service
    /// </summary>
    /// <returns>Result containing loaded models and created plugins</returns>
    public async Task<PlaywrightStudioResult> InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing PlaywrightStudio");

            // Ensure Playwright is initialized
            if (!_playwrightManagementService.IsBrowserOpen)
            {
                await _playwrightManagementService.InitializeAsync();
            }

            // Ensure we have at least one page
            if (!_playwrightManagementService.Pages.Any())
            {
                await _playwrightManagementService.CreatePageAsync();
            }

            var activePage = _playwrightManagementService.ActivePage ?? throw new InvalidOperationException("No active page available after initialization");

            // Load all models
            var models = await _modelRepository.GetAllModelsAsync();
            var modelList = models.ToList();

            _logger.LogInformation("Loaded {ModelCount} page object models", modelList.Count);

            if (!modelList.Any())
            {
                _logger.LogWarning("No page object models found!");
                return new PlaywrightStudioResult
                {
                    Models = modelList,
                    Plugins = new List<KernelPlugin>(),
                    Success = false,
                    Message = "No page object models found"
                };
            }

            // Create plugins for all models using the managed page
            var plugins = _pluginFactory.CreatePagePlugins(modelList, activePage).ToList();

            _logger.LogInformation("Created {PluginCount} plugins with {TotalFunctionCount} total functions",
                plugins.Count, plugins.Sum(p => p.Count()));

            return new PlaywrightStudioResult
            {
                Models = modelList,
                Plugins = plugins,
                Success = true,
                Message = $"Successfully initialized with {modelList.Count} models and {plugins.Count} plugins"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PlaywrightStudio");
            return new PlaywrightStudioResult
            {
                Models = new List<PageObjectModel>(),
                Plugins = new List<KernelPlugin>(),
                Success = false,
                Message = $"Initialization failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Initializes PlaywrightStudio with a specific page (legacy method for backward compatibility)
    /// </summary>
    /// <param name="page">The Playwright page instance</param>
    /// <returns>Result containing loaded models and created plugins</returns>
    public async Task<PlaywrightStudioResult> InitializeAsync(IPage page)
    {
        if (page == null)
        {
            throw new ArgumentNullException(nameof(page));
        }

        try
        {
            _logger.LogInformation("Initializing PlaywrightStudio with provided page");

            // Load all models
            var models = await _modelRepository.GetAllModelsAsync();
            var modelList = models.ToList();

            _logger.LogInformation("Loaded {ModelCount} page object models", modelList.Count);

            if (!modelList.Any())
            {
                _logger.LogWarning("No page object models found!");
                return new PlaywrightStudioResult
                {
                    Models = modelList,
                    Plugins = new List<KernelPlugin>(),
                    Success = false,
                    Message = "No page object models found"
                };
            }

            // Create plugins for all models
            var plugins = _pluginFactory.CreatePagePlugins(modelList, page).ToList();

            _logger.LogInformation("Created {PluginCount} plugins with {TotalFunctionCount} total functions",
                plugins.Count, plugins.Sum(p => p.Count()));

            return new PlaywrightStudioResult
            {
                Models = modelList,
                Plugins = plugins,
                Success = true,
                Message = $"Successfully initialized with {modelList.Count} models and {plugins.Count} plugins"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PlaywrightStudio");
            return new PlaywrightStudioResult
            {
                Models = new List<PageObjectModel>(),
                Plugins = new List<KernelPlugin>(),
                Success = false,
                Message = $"Initialization failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets a specific model by name
    /// </summary>
    /// <param name="name">The name of the model to retrieve</param>
    /// <returns>The model if found, null otherwise</returns>
    public async Task<PageObjectModel?> GetModelByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Model name is null or empty");
            return null;
        }

        try
        {
            return await _modelRepository.GetModelByNameAsync(name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model by name: {ModelName}", name);
            return null;
        }
    }

    /// <summary>
    /// Gets all available models
    /// </summary>
    /// <returns>Collection of all models</returns>
    public async Task<IEnumerable<PageObjectModel>> GetAllModelsAsync()
    {
        try
        {
            return await _modelRepository.GetAllModelsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all models");
            return Enumerable.Empty<PageObjectModel>();
        }
    }

    /// <summary>
    /// Creates a plugin for a specific model using the managed page
    /// </summary>
    /// <param name="modelName">The name of the model</param>
    /// <returns>The created plugin or null if model not found</returns>
    public async Task<KernelPlugin?> CreatePluginForModelAsync(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            _logger.LogWarning("Model name is null or empty");
            return null;
        }

        try
        {
            var model = await _modelRepository.GetModelByNameAsync(modelName);
            if (model == null)
            {
                _logger.LogWarning("Model not found: {ModelName}", modelName);
                return null;
            }

            var activePage = _playwrightManagementService.ActivePage;
            if (activePage == null)
            {
                _logger.LogError("No active page available for plugin creation");
                return null;
            }

            return _pluginFactory.CreatePagePlugin(model, activePage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create plugin for model: {ModelName}", modelName);
            return null;
        }
    }

    /// <summary>
    /// Creates a plugin for a specific model with a specific page (legacy method for backward compatibility)
    /// </summary>
    /// <param name="modelName">The name of the model</param>
    /// <param name="page">The Playwright page instance</param>
    /// <returns>The created plugin or null if model not found</returns>
    public async Task<KernelPlugin?> CreatePluginForModelAsync(string modelName, IPage page)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            _logger.LogWarning("Model name is null or empty");
            return null;
        }

        if (page == null)
        {
            throw new ArgumentNullException(nameof(page));
        }

        try
        {
            var model = await _modelRepository.GetModelByNameAsync(modelName);
            if (model == null)
            {
                _logger.LogWarning("Model not found: {ModelName}", modelName);
                return null;
            }

            return _pluginFactory.CreatePagePlugin(model, page);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create plugin for model: {ModelName}", modelName);
            return null;
        }
    }

    /// <summary>
    /// Gets the count of available models
    /// </summary>
    /// <returns>Number of available models</returns>
    public async Task<int> GetModelCountAsync()
    {
        try
        {
            return await _modelRepository.GetModelCountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model count");
            return 0;
        }
    }
}

/// <summary>
/// Result of PlaywrightStudio initialization
/// </summary>
public class PlaywrightStudioResult
{
    /// <summary>
    /// The loaded page object models
    /// </summary>
    public IReadOnlyList<PageObjectModel> Models { get; set; } = new List<PageObjectModel>();

    /// <summary>
    /// The created kernel plugins
    /// </summary>
    public IReadOnlyList<KernelPlugin> Plugins { get; set; } = new List<KernelPlugin>();

    /// <summary>
    /// Whether the initialization was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
