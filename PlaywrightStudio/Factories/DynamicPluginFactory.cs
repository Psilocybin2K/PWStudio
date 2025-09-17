using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Microsoft.SemanticKernel;
using PlaywrightStudio.Configuration;
using PlaywrightStudio.Exceptions;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.Models;

namespace PlaywrightStudio.Factories;

/// <summary>
/// Factory for creating KernelPlugin instances from PageObjectModel using dynamic assembly generation
/// </summary>
public class DynamicPluginFactory : IPluginFactory
{
    private readonly IVariableParser _variableParser;
    private readonly ILogger<DynamicPluginFactory> _logger;
    private readonly PlaywrightStudioOptions _options;

    /// <summary>
    /// Initializes a new instance of the DynamicPluginFactory class
    /// </summary>
    /// <param name="variableParser">Variable parser instance</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="options">Configuration options</param>
    public DynamicPluginFactory(
        IVariableParser variableParser,
        ILogger<DynamicPluginFactory> logger,
        IOptions<PlaywrightStudioOptions> options)
    {
        _variableParser = variableParser ?? throw new ArgumentNullException(nameof(variableParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public KernelPlugin CreatePagePlugin(PageObjectModel model, IPage page)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (page == null)
        {
            throw new ArgumentNullException(nameof(page));
        }

        try
        {
            _logger.LogDebug("Creating plugin for model: {ModelName}", model.Name);

            var kernel = new Kernel();
            var sanitizedModelName = SanitizeName(model.Name);
            var assemblyName = new AssemblyName($"PlaywrightStudio.DynamicTasks_{sanitizedModelName}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            var typeBuilder = moduleBuilder.DefineType(
                $"TaskFunctions_{sanitizedModelName}",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract
            );

            // Create a static field to hold the page instance
            var pageField = typeBuilder.DefineField("_page", typeof(IPage), FieldAttributes.Public | FieldAttributes.Static);

            var functionCount = 0;
            var functionNames = new List<string>();
            var functionSummaries = new List<string>();

            _logger.LogDebug("Generating {TaskCount} functions for model '{ModelName}'", 
                model.Tasks?.Count() ?? 0, model.Name);

            foreach (var task in model.Tasks ?? Array.Empty<PageTask>())
            {
                try
                {
                    _logger.LogDebug("Generating function for task: {TaskName}", task.Name);
                    
                    var parameterTypes = (task.Parameters ?? Array.Empty<PageElementParameter>())
                        .Select(p => MapParameterType(p.Type))
                        .ToArray();

                    var methodBuilder = typeBuilder.DefineMethod(
                        task.Name,
                        MethodAttributes.Public | MethodAttributes.Static,
                        typeof(Task),
                        parameterTypes
                    );

                    // Assign parameter names to match the PageTask definition
                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        var paramName = task.Parameters![i].Name;
                        methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, paramName);
                        _logger.LogDebug("  Parameter {Index}: {ParamName} ({ParamType})", 
                            i + 1, paramName, task.Parameters[i].Type);
                    }

                    // Generate method body based on task steps
                    var il = methodBuilder.GetILGenerator();
                    
                    // For now, just return Task.CompletedTask
                    // TODO: Implement actual Playwright operations based on task steps
                    var completedTaskGetter = typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetMethod!;
                    il.Emit(OpCodes.Call, completedTaskGetter);
                    il.Emit(OpCodes.Ret);

                    functionCount++;
                    functionNames.Add(task.Name);

                    // Build a summary for this function
                    var summary = $"Function '{task.Name}'";
                    if (!string.IsNullOrWhiteSpace(task.Description))
                        summary += $": {task.Description}";
                    if (task.Parameters != null && task.Parameters.Count() > 0)
                    {
                        summary += $" | Parameters: {string.Join(", ", task.Parameters.Select(p => $"{p.Name}:{p.Type}"))}";
                    }
                    functionSummaries.Add(summary);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate function for task: {TaskName}", task.Name);
                    throw new PluginCreationException(model.Name, $"Failed to generate function '{task.Name}'", ex);
                }
            }

            var generatedType = typeBuilder.CreateType() ?? throw new PluginCreationException(model.Name, "Failed to create dynamic type");

            // Set the page field value
            generatedType.GetField("_page", BindingFlags.Public | BindingFlags.Static)!.SetValue(null, page);

            var functionsList = new List<KernelFunction>();

            foreach (var task in model.Tasks ?? Array.Empty<PageTask>())
            {
                try
                {
                    var methodInfo = generatedType.GetMethod(task.Name, BindingFlags.Public | BindingFlags.Static);
                    if (methodInfo == null)
                    {
                        _logger.LogWarning("Method '{TaskName}' not found on generated type", task.Name);
                        continue;
                    }

                    var typeArgs = new List<Type>();

                    if (task.Parameters != null)
                    {
                        foreach (var p in task.Parameters)
                        {
                            typeArgs.Add(MapParameterType(p.Type));
                        }
                        typeArgs.Add(typeof(Task));
                    }

                    var delegateType = Expression.GetFuncType(typeArgs.ToArray());
                    var del = methodInfo.CreateDelegate(delegateType);

                    var fn = kernel.CreateFunctionFromMethod(del);
                    functionsList.Add(fn);

                    _logger.LogDebug("Registered function: {TaskName}", task.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to register function: {TaskName}", task.Name);
                    throw new PluginCreationException(model.Name, $"Failed to register function '{task.Name}'", ex);
                }
            }

            var plugin = KernelPluginFactory.CreateFromFunctions(sanitizedModelName, functionsList);

            _logger.LogInformation("Successfully created plugin '{PluginName}' with {FunctionCount} functions", 
                sanitizedModelName, functionCount);

            if (_options.IncludeDebugInfo)
            {
                _logger.LogDebug("Plugin creation summary for model '{ModelName}':", model.Name);
                _logger.LogDebug("  - Total functions generated: {FunctionCount}", functionCount);
                _logger.LogDebug("  - Function names: {FunctionNames}", string.Join(", ", functionNames));
                _logger.LogDebug("  - Function details:");
                foreach (var summary in functionSummaries)
                {
                    _logger.LogDebug("      * {Summary}", summary);
                }
            }

            return plugin;
        }
        catch (Exception ex) when (!(ex is PluginCreationException))
        {
            _logger.LogError(ex, "Unexpected error creating plugin for model: {ModelName}", model.Name);
            throw new PluginCreationException(model.Name, "Unexpected error during plugin creation", ex);
        }
    }

    /// <inheritdoc />
    public IEnumerable<KernelPlugin> CreatePagePlugins(IEnumerable<PageObjectModel> models, IPage page)
    {
        if (models == null)
        {
            throw new ArgumentNullException(nameof(models));
        }

        if (page == null)
        {
            throw new ArgumentNullException(nameof(page));
        }

        var plugins = new List<KernelPlugin>();

        foreach (var model in models)
        {
            try
            {
                var plugin = CreatePagePlugin(model, page);
                plugins.Add(plugin);
            }
            catch (PluginCreationException ex)
            {
                _logger.LogError(ex, "Failed to create plugin for model: {ModelName}", model.Name);
                // Continue with other models
            }
        }

        _logger.LogInformation("Created {PluginCount} plugins from {ModelCount} models", 
            plugins.Count, models.Count());

        return plugins;
    }

    private static Type MapParameterType(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) 
            return typeof(string);
        
        return typeName.Trim().ToLowerInvariant() switch
        {
            "string" => typeof(string),
            "int" => typeof(int),
            "bool" => typeof(bool),
            "double" => typeof(double),
            _ => typeof(string)
        };
    }

    private static string SanitizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) 
            return "Unnamed";
        
        var chars = name.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? "Unnamed" : new string(chars);
    }
}
