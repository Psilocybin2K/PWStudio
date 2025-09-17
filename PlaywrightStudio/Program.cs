using System.Diagnostics;
using System.Text.Json;
using HandlebarsDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PlaywrightStudio.Commands;
using PlaywrightStudio.Configuration;
using PlaywrightStudio.Events;
using PlaywrightStudio.Extensions;
using PlaywrightStudio.Handlers.EventHandlers;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.Models;
using PlaywrightStudio.Plugins;
using PlaywrightStudio.Services;

namespace PlaywrightStudio
{
    internal class Program
    {
        public static async Task<string?> PerformIntentRecognitionByPageAsync(Kernel kernel, string message, ILogger logger)
        {
            try
            {
                // Load the page object model JSON
                var pageObjectModelJson = await File.ReadAllTextAsync("PageObjectModels/login.json");
                var pageObjectModel = JsonSerializer.Deserialize<JsonElement>(pageObjectModelJson);

                // Load the Handlebars template
                var templateContent = await File.ReadAllTextAsync("PromptTemplates/IntentRecognitionByPage.md");

                // Compile the Handlebars template
                var template = Handlebars.Compile(templateContent);

                // Create the context object for the template
                var templateContext = new
                {
                    name = pageObjectModel.GetProperty("name").GetString(),
                    url = pageObjectModel.GetProperty("url").GetString(),
                    description = pageObjectModel.GetProperty("description").GetString(),
                    elements = pageObjectModel.GetProperty("elements").EnumerateArray().Select(element => new
                    {
                        name = element.GetProperty("name").GetString(),
                        cssPath = element.GetProperty("cssPath").GetString()
                    }).ToArray(),
                    tasks = pageObjectModel.GetProperty("tasks").EnumerateArray().Select(task => new
                    {
                        name = task.GetProperty("name").GetString(),
                        description = task.GetProperty("description").GetString(),
                        parameters = task.TryGetProperty("parameters", out var parameters)
                            ? parameters.EnumerateArray().Select(param => new
                            {
                                name = param.GetProperty("name").GetString(),
                                type = param.GetProperty("type").GetString()
                            }).ToArray()
                            : null,
                        steps = task.TryGetProperty("steps", out var steps)
                            ? steps.EnumerateArray().Select(step => new
                            {
                                description = step.GetProperty("description").GetString(),
                                action = step.GetProperty("action").GetString(),
                                element = step.GetProperty("element").GetString(),
                                value = step.TryGetProperty("value", out var value) ? value.GetString() : null
                            }).ToArray()
                            : null
                    }).ToArray()
                };

                // Render the template with the context
                var compiledPrompt = $$$"""
                    {{{template(templateContext)}}}
                    """;

                logger.LogInformation("Compiled prompt length: {Length} characters", compiledPrompt.Length);
                logger.LogInformation("Page: {PageName} with {ElementCount} elements and {TaskCount} tasks",
                    templateContext.name, templateContext.elements.Length, templateContext.tasks.Length);

                // Use the compiled prompt with Semantic Kernel
                var promptExecutionSettings = new OpenAIPromptExecutionSettings()
                {
                    ServiceId = "aoai"
                };

                var sw = Stopwatch.StartNew();
                var result = await kernel.InvokePromptAsync(compiledPrompt, new KernelArguments(promptExecutionSettings)
                {
                    ["message"] = message
                });
                sw.Stop();

                var response = result.GetValue<string>();
                logger.LogInformation("Intent recognition completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
                logger.LogInformation("Response: {Response}", response);

                // Now validate the response
                var validationPrompt = await File.ReadAllTextAsync("PromptTemplates/IntentRecognitionByPageValidation.md");

                var validationSw = Stopwatch.StartNew();
                var validationResult = await kernel.InvokePromptAsync(validationPrompt, new KernelArguments(promptExecutionSettings)
                {
                    ["relevance_analysis"] = response
                });
                validationSw.Stop();

                var validationResponse = validationResult.GetValue<string>();
                logger.LogInformation("Validation completed in {ElapsedMs}ms", validationSw.ElapsedMilliseconds);
                logger.LogInformation("Validation Response: {ValidationResponse}", validationResponse);

                // Deserialize the validation response
                try
                {
                    var cleanedResponse = validationResponse?.Replace("```json", "").Replace("```", "") ?? "";
                    var jsonValidation = JsonSerializer.Deserialize<IntentRecognitionByPageValidationResponse>(
                        cleanedResponse,
                        new JsonSerializerOptions()
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (jsonValidation != null)
                    {
                        logger.LogInformation("Validation Status: {Status}", jsonValidation.AnalysisStatus);
                        logger.LogInformation("Final Verdict: {Verdict}", jsonValidation.Recommendations.FinalVerdict);
                        logger.LogInformation("Confidence: {Confidence}", jsonValidation.Recommendations.Confidence);
                        logger.LogInformation("Element Validations: {Count}", jsonValidation.ElementValidations.Length);
                        logger.LogInformation("Task Validations: {Count}", jsonValidation.TaskValidations.Length);

                        if (jsonValidation.Recommendations.CriticalIssues.Length > 0)
                        {
                            logger.LogWarning("Critical Issues Found: {Issues}", string.Join(", ", jsonValidation.Recommendations.CriticalIssues));
                        }

                        if (jsonValidation.Recommendations.Improvements.Length > 0)
                        {
                            logger.LogInformation("Improvements Suggested: {Improvements}", string.Join(", ", jsonValidation.Recommendations.Improvements));
                        }
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "Failed to deserialize validation response JSON");
                }

                return $"{response}\n---\n{validationResponse}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during page-level intent recognition with validation");
                throw;
            }
        }

        public static async Task<string?> PerformIntentRecognitionBySiteWithValidationAsync(Kernel kernel, ChatHistoryAgentThread thread, ChatCompletionAgent chatCompletionAgent, string message, ILogger logger)
        {
            try
            {
                // Load all page object models from the directory
                var pageObjectModelsDirectory = "PageObjectModels";
                var pageObjectModelFiles = Directory.GetFiles(pageObjectModelsDirectory, "*.json");

                logger.LogInformation("Found {FileCount} page object model files", pageObjectModelFiles.Length);

                var pages = new List<object>();

                foreach (var file in pageObjectModelFiles)
                {
                    if (file.EndsWith(".schema.json"))
                    {
                        continue;
                    }

                    try
                    {
                        logger.LogInformation("Processing file: {FileName}", Path.GetFileName(file));
                        var pageObjectModelJson = await File.ReadAllTextAsync(file);
                        var pageObjectModel = JsonSerializer.Deserialize<JsonElement>(pageObjectModelJson);

                        // Throw if required properties are missing
                        if (!pageObjectModel.TryGetProperty("name", out var nameProp) || nameProp.ValueKind != JsonValueKind.String)
                            throw new InvalidDataException($"Missing or invalid 'name' property in {file}");
                        if (!pageObjectModel.TryGetProperty("url", out var urlProp) || urlProp.ValueKind != JsonValueKind.String)
                            throw new InvalidDataException($"Missing or invalid 'url' property in {file}");
                        if (!pageObjectModel.TryGetProperty("description", out var descProp) || descProp.ValueKind != JsonValueKind.String)
                            throw new InvalidDataException($"Missing or invalid 'description' property in {file}");

                        object[]? elementsArr = null;
                        if (pageObjectModel.TryGetProperty("elements", out var elementsProp))
                        {
                            if (elementsProp.ValueKind != JsonValueKind.Array)
                                throw new InvalidDataException($"'elements' property is not an array in {file}");

                            elementsArr = elementsProp.EnumerateArray().Select(element =>
                            {
                                if (!element.TryGetProperty("name", out var elementNameProp) || elementNameProp.ValueKind != JsonValueKind.String)
                                    throw new InvalidDataException($"Element missing 'name' property in {file}");
                                if (!element.TryGetProperty("cssPath", out var cssPathProp) || cssPathProp.ValueKind != JsonValueKind.String)
                                    throw new InvalidDataException($"Element missing 'cssPath' property in {file}");

                                return new
                                {
                                    name = elementNameProp.GetString(),
                                    cssPath = cssPathProp.GetString()
                                };
                            }).ToArray();
                        }

                        object[]? tasksArr = null;
                        if (pageObjectModel.TryGetProperty("tasks", out var tasksProp))
                        {
                            if (tasksProp.ValueKind != JsonValueKind.Array)
                                throw new InvalidDataException($"'tasks' property is not an array in {file}");

                            tasksArr = tasksProp.EnumerateArray().Select(task =>
                            {
                                if (!task.TryGetProperty("name", out var taskNameProp) || taskNameProp.ValueKind != JsonValueKind.String)
                                    throw new InvalidDataException($"Task missing 'name' property in {file}");
                                if (!task.TryGetProperty("description", out var taskDescProp) || taskDescProp.ValueKind != JsonValueKind.String)
                                    throw new InvalidDataException($"Task missing 'description' property in {file}");

                                object[]? parametersArr = null;
                                if (task.TryGetProperty("parameters", out var parametersProp))
                                {
                                    if (parametersProp.ValueKind != JsonValueKind.Array)
                                        throw new InvalidDataException($"'parameters' property is not an array in {file}");

                                    parametersArr = parametersProp.EnumerateArray().Select(param =>
                                    {
                                        if (!param.TryGetProperty("name", out var paramNameProp) || paramNameProp.ValueKind != JsonValueKind.String)
                                            throw new InvalidDataException($"Parameter missing 'name' property in {file}");
                                        if (!param.TryGetProperty("type", out var paramTypeProp) || paramTypeProp.ValueKind != JsonValueKind.String)
                                            throw new InvalidDataException($"Parameter missing 'type' property in {file}");

                                        return new
                                        {
                                            name = paramNameProp.GetString(),
                                            type = paramTypeProp.GetString()
                                        };
                                    }).ToArray();
                                }

                                return new
                                {
                                    name = taskNameProp.GetString(),
                                    description = taskDescProp.GetString(),
                                    parameters = parametersArr
                                };
                            }).ToArray();
                        }

                        var page = new
                        {
                            name = nameProp.GetString(),
                            url = urlProp.GetString(),
                            description = descProp.GetString(),
                            elements = elementsArr,
                            tasks = tasksArr
                        };

                        pages.Add(page);
                        logger.LogInformation("Successfully processed page: {PageName}", page.name);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing file {FileName}", Path.GetFileName(file));
                        // Continue processing other files even if one fails
                    }
                }

                if (pages.Count == 0)
                {
                    logger.LogWarning("No valid page object models found in {Directory}", pageObjectModelsDirectory);

                    return null;
                }

                logger.LogInformation("Successfully loaded {PageCount} page object models", pages.Count);

                // Load the Handlebars template
                var templateContent = await File.ReadAllTextAsync("PromptTemplates/IntentRecognitionBySite.md");

                // Compile the Handlebars template
                var template = Handlebars.Compile(templateContent);

                // Create the context object for the template
                var templateContext = new
                {
                    pages = pages.ToArray()
                };

                // Render the template with the context
                var compiledPrompt = $$$"""
                    {{{template(templateContext)}}}
                    """;

                logger.LogInformation("Compiled site-level prompt length: {Length} characters", compiledPrompt.Length);
                logger.LogInformation("Site contains {PageCount} pages", pages.Count);

                // Use the compiled prompt with Semantic Kernel
                var promptExecutionSettings = new OpenAIPromptExecutionSettings()
                {
                    ServiceId = "aoai"
                };

                Console.WriteLine(compiledPrompt);
                Console.WriteLine(message);

                var sw = Stopwatch.StartNew();
                var agentResponse = chatCompletionAgent.InvokeAsync(message, thread, new AgentInvokeOptions()
                {
                    AdditionalInstructions = compiledPrompt,
                    KernelArguments = new KernelArguments(promptExecutionSettings)
                    {
                        {"message", message}
                    }
                }).ToBlockingEnumerable().First();
                sw.Stop();

                var response = agentResponse.Message.Content;
                logger.LogInformation("Site-level intent recognition completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
                logger.LogInformation("Response: {Response}", response);

                // Now validate the response
                var validationPrompt = await File.ReadAllTextAsync("PromptTemplates/IntentRecognitionBySiteValidation.md");

                var validationSw = Stopwatch.StartNew();
                var validationResult = await kernel.InvokePromptAsync(validationPrompt, new KernelArguments(promptExecutionSettings)
                {
                    ["site_analysis"] = response
                });
                validationSw.Stop();

                var validationResponse = validationResult.GetValue<string>();
                logger.LogInformation("Site-level validation completed in {ElapsedMs}ms", validationSw.ElapsedMilliseconds);
                logger.LogInformation("Validation Response: {ValidationResponse}", validationResponse);

                // Deserialize the validation response
                try
                {
                    var cleanedResponse = validationResponse?.Replace("```json", "").Replace("```", "") ?? "";
                    var jsonValidation = JsonSerializer.Deserialize<IntentRecognitionBySiteValidationResponse>(
                        cleanedResponse,
                        new JsonSerializerOptions()
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (jsonValidation != null)
                    {
                        logger.LogInformation("Validation Status: {Status}", jsonValidation.AnalysisStatus);
                        logger.LogInformation("Final Verdict: {Verdict}", jsonValidation.Recommendations.FinalVerdict);
                        logger.LogInformation("Confidence: {Confidence}", jsonValidation.Recommendations.Confidence);
                        logger.LogInformation("Page Validations: {Count}", jsonValidation.PageValidations.Length);
                        logger.LogInformation("Navigation Flow: {Flow}", jsonValidation.CrossPageAssessment.NavigationFlowCorrectness);

                        if (jsonValidation.Recommendations.CriticalIssues.Length > 0)
                        {
                            logger.LogWarning("Critical Issues Found: {Issues}", string.Join(", ", jsonValidation.Recommendations.CriticalIssues));
                        }

                        if (jsonValidation.Recommendations.Improvements.Length > 0)
                        {
                            logger.LogInformation("Improvements Suggested: {Improvements}", string.Join(", ", jsonValidation.Recommendations.Improvements));
                        }

                        if (jsonValidation.CrossPageAssessment.MissingPageConnections.Length > 0)
                        {
                            logger.LogWarning("Missing Page Connections: {Connections}", string.Join(", ", jsonValidation.CrossPageAssessment.MissingPageConnections));
                        }
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "Failed to deserialize site-level validation response JSON");
                }

                return $"{response}\n---\n{validationResponse}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during site-level intent recognition with validation");
                throw;
            }
        }

        public static async Task<string?> PerformIntentRecognitionAsync(Kernel kernel, string message, ILogger logger)
        {

            var intentRecognitionPrompt = File.ReadAllText("PromptTemplates/IntentRecognition.md");
            var intentRecognitionValidationPrompt = File.ReadAllText("PromptTemplates/IntentRecognitionValidation.md");

            var promptexecutionSettings = new OpenAIPromptExecutionSettings()
            {
                ServiceId = "aoai"
            };

            var sw = Stopwatch.StartNew();
            var test = await kernel.InvokePromptAsync(intentRecognitionPrompt, new KernelArguments(promptexecutionSettings)
            {
                ["message"] = "Double click the file item"
            });

            var testValidation = await kernel.InvokePromptAsync(intentRecognitionValidationPrompt, new KernelArguments(promptexecutionSettings)
            {
                ["message"] = "Double click the file item with CSS Path .file-item",
                ["execution_plan"] = test.GetValue<string>()
            });

            var text = test.GetValue<string>();
            var textValidation = testValidation.GetValue<string>() ?? throw new Exception("Validation failed");

            var jsonValidation = JsonSerializer.Deserialize<IntentRecognitionValidationResponse>(textValidation.Replace("```json", "").Replace("```", ""), new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });

            sw.Stop();

            logger.LogInformation("Ollama time: {OllamaTime} milliseconds", sw.ElapsedMilliseconds);

            return $"{text}\n---\n{textValidation}";
        }

        private static async Task<SearchResponse> PerformSearchExample(
            PageObjectModelSearchService searchService,
            string query,
            string description,
            int topK = 10,
            double minSimilarityThreshold = 0.1,
            double withinPercentile = 0.2 // 0.0 means no filtering, 0.1 means within 10% of max, etc.
        )
        {
            Console.WriteLine($"\n=== {description} ===");
            Console.WriteLine($"Query: '{query}'");

            var request = new SearchRequest(
                Query: query,
                MaxResults: topK,
                MinSimilarityThreshold: minSimilarityThreshold,
                Scope: SearchScope.All
            );

            var response = await searchService.SearchAsync(request);

            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                throw new Exception($"Search error: {response.ErrorMessage}");
            }

            Console.WriteLine($"Found {response.Results.Length} results in {response.SearchDuration.TotalMilliseconds:F1}ms");

            if (response.Results.Length == 0)
            {
                Console.WriteLine("No results found.");
            }

            // Filter results by withinPercentile if specified
            var results = response.Results;
            if (withinPercentile > 0 && results.Length > 0)
            {
                var maxScore = results.Max(r => r.SimilarityScore);
                var threshold = maxScore * (1.0 - withinPercentile);
                results = results.Where(r => r.SimilarityScore >= threshold).ToArray();
                Console.WriteLine($"Filtered to {results.Length} results within {withinPercentile:P0} of max similarity ({maxScore:F3})");

                response = new SearchResponse(
                    request,
                    results,
                    results.Length,
                    response.SearchDuration,
                    response.ErrorMessage
                );
            }

            foreach (var result in results)
            {
                Console.WriteLine($"  [{result.Type.ToUpper()}] {result.Name}");
                Console.WriteLine($"    Similarity: {result.SimilarityScore:F3}");
                Console.WriteLine($"    Matched: '{result.MatchedUtterance}'");
                Console.WriteLine($"    Context: {FormatSearchContext(result.Context)}");
                Console.WriteLine($"    URL: {result.Url}");
            }

            return response;
        }

        private static async Task PerformScopedSearchExample(PageObjectModelSearchService searchService, string query, SearchScope scope, string description)
        {
            Console.WriteLine($"\n=== {description} ===");
            Console.WriteLine($"Query: '{query}' (Scope: {scope})");

            var request = new SearchRequest(
                Query: query,
                MaxResults: 3,
                MinSimilarityThreshold: 0.1f,
                Scope: scope
            );

            var response = await searchService.SearchAsync(request);

            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                Console.WriteLine($"Search error: {response.ErrorMessage}");
                return;
            }

            Console.WriteLine($"Found {response.Results.Length} {scope.ToString().ToLower()} results");

            foreach (var result in response.Results)
            {
                Console.WriteLine($"  [{result.Type.ToUpper()}] {result.Name} - Similarity: {result.SimilarityScore:F3}");
                Console.WriteLine($"    Matched: '{result.MatchedUtterance}'");
            }
        }

        private static string FormatSearchContext(SearchResultContext context)
        {
            var parts = new List<string> { context.PageName };

            if (!string.IsNullOrEmpty(context.ElementName))
                parts.Add($"Element: {context.ElementName}");

            if (!string.IsNullOrEmpty(context.TaskName))
                parts.Add($"Task: {context.TaskName}");

            if (!string.IsNullOrEmpty(context.StepDescription))
                parts.Add($"Step: {context.StepDescription}");

            return string.Join(" | ", parts);
        }

        public static async Task Main(string[] args)
        {
            // Check if we should run tests
            if (args.Length > 0 && args[0].ToLowerInvariant() == "test")
            {
                if (args.Length > 1)
                {
                    await TestRunner.RunSpecificTestAsync(args[1]);
                }
                else
                {
                    await TestRunner.RunNavigateCommandTestsAsync();
                }
                return;
            }

            // Build configuration
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // Create service collection
            var services = new ServiceCollection();

            // Configure PlaywrightStudio services from configuration
            services.AddPlaywrightStudio(configuration);

            // Configure Ollama services from configuration
            services.AddOllamaServices(configuration);

            services.AddAzureOpenAIChatCompletion("gpt-4.1-nano", Environment.GetEnvironmentVariable("AOAI_ENDPOINT"), Environment.GetEnvironmentVariable("AOAI_API_KEY"), serviceId: "aoai");

            services.AddSingleton(serviceProvider =>
            {
                var k = new Kernel(serviceProvider);

                k.Plugins.AddFromType<WebDriverPlugin>(serviceProvider: serviceProvider);

                return k;
            });

            services.AddSingleton<EmbeddingGeneratorService>();

            // Add console logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddConfiguration(configuration.GetSection("Logging"));
            });

            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Register event handlers
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            var pageNavigationHandler = serviceProvider.GetRequiredService<PageNavigationEventHandler>();
            eventBus.Subscribe<PageNavigatedEvent>(pageNavigationHandler.HandleAsync);

            // Get services
            var playwrightService = serviceProvider.GetRequiredService<PlaywrightStudioService>();
            var playwrightManagementService = serviceProvider.GetRequiredService<IPlaywrightManagementService>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var browserOptions = serviceProvider.GetRequiredService<IOptions<BrowserOptions>>().Value;
            var playwrightOptions = serviceProvider.GetRequiredService<IOptions<PlaywrightStudioOptions>>().Value;
            var ollamaOptions = serviceProvider.GetRequiredService<IOptions<OllamaOptions>>().Value;
            var kernel = serviceProvider.GetRequiredService<Kernel>();

            var chatCompletionAgent = new ChatCompletionAgent()
            {
                Kernel = kernel.Clone(),
                Name = "WebAppAgent",
                Instructions = """
                Analyze the user's message: {{$message}}
                """
            };

            var chatThread = new ChatHistoryAgentThread();

            // Get the command bus for sending commands
            var commandBus = serviceProvider.GetRequiredService<ICommandBus>();

            // Example 1: Initialize browser
            logger.LogInformation("Example 1: Initializing browser...");
            await commandBus.SendAsync(new InitializeBrowserCommand());
            logger.LogInformation("Browser initialized: {IsOpen}", playwrightManagementService.IsBrowserOpen);

            // Example 1: Initialize browser
            logger.LogInformation("Example 1: Initializing browser...");
            await commandBus.SendAsync(new InitializeBrowserCommand());
            logger.LogInformation("Browser initialized: {IsOpen}", playwrightManagementService.IsBrowserOpen);

            // Example 2: Create a page
            logger.LogInformation("Example 2: Creating a page...");
            await commandBus.SendAsync(new CreatePageCommand("https://saucedemo.com"));
            logger.LogInformation("Page created. Active page URL: {Url}", playwrightManagementService.ActivePage?.Url);

            var sw = Stopwatch.StartNew();
            // await PerformIntentRecognitionAsync(kernel, "Double click the file item", logger);

            // // Test the new page-level intent recognition
            // await PerformIntentRecognitionByPageAsync(kernel, "Login with username 'testuser' and password 'testpass'", logger);

            // Test the site-level intent recognition with validation
            var input = "Login as username `standard_user` and password `secret_sauce`";
            var siteIntentRecognitionResponse = await PerformIntentRecognitionBySiteWithValidationAsync(
                kernel, chatThread, chatCompletionAgent, input, logger) ?? throw new Exception("Site-level intent recognition failed");

            sw.Stop();

            logger.LogInformation("Site-level intent recognition completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);

            // Test the page object model search functionality
            var pageObjectModelSearchService = serviceProvider.GetRequiredService<PageObjectModelSearchService>();
            await pageObjectModelSearchService.InitializeAsync();

            var searchResponse = await PerformSearchExample(pageObjectModelSearchService, "login", "Login application", withinPercentile: 0.2);

            // Compile the Handlebars template for SearchResults.md to a string
            var searchResultsTemplatePath = Path.Combine("PromptTemplates", "SearchResults.md");
            var searchResultsTemplateContent = await File.ReadAllTextAsync(searchResultsTemplatePath);
            var handlebars = HandlebarsDotNet.Handlebars.Create();
            var searchResultsTemplateCompiler = handlebars.Compile(searchResultsTemplateContent);
            var searchResultsTemplate = searchResultsTemplateCompiler(searchResponse);

            string prompt = $$$"""
                Based on the provided input, only invoke the tools that are required to complete the task.

                # Input

                {{{siteIntentRecognitionResponse.Split("---")[0]}}}

                ## Context

                Current Page: {{{playwrightManagementService.ActivePage?.Url}}}

                **Search Results**: The following search results may aid in understanding the user's intent:

                {{{searchResultsTemplate}}}
                """;

            var webDriverToolsPrompt = await File.ReadAllTextAsync("PromptTemplates/WebDriverPlugins.md");

            var swTools = Stopwatch.StartNew();
            var promptExecutionSettings = new OpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                ServiceId = "aoai"
            };
            var kernelArguments = new KernelArguments(promptExecutionSettings);

            var webDriverAgent = new ChatCompletionAgent()
            {
                Kernel = kernel.Clone(),
                Name = "WebDriverAgent",
                Instructions = """
                    Analyze the users message.
                    """
            };

            var webDriverThread = new ChatHistoryAgentThread();

            var fnPrompt = webDriverAgent.InvokeAsync(prompt, webDriverThread, new AgentInvokeOptions()
            {
                KernelArguments = kernelArguments,
                AdditionalInstructions = webDriverToolsPrompt
            }).ToBlockingEnumerable().First();

            swTools.Stop();

            logger.LogInformation("WebDriver tools prompt completed in {ElapsedMs}ms", swTools.ElapsedMilliseconds);

            var fnPromptResponse = fnPrompt.Message.Content;

            Debugger.Break();

            // Example 3: Navigate to different URLs
            logger.LogInformation("Example 3: Navigating to different URLs...");
            await commandBus.SendAsync(new NavigateToUrlCommand("https://httpbin.org"));
            logger.LogInformation("Navigated to: {Url}", playwrightManagementService.ActivePage?.Url);

            await commandBus.SendAsync(new NavigateToUrlCommand("https://jsonplaceholder.typicode.com"));
            logger.LogInformation("Navigated to: {Url}", playwrightManagementService.ActivePage?.Url);

            // Example 4: Navigate with specific page ID
            var pageId = $"page-{playwrightManagementService.ActivePage?.GetHashCode()}";
            logger.LogInformation("Example 4: Navigating with specific page ID: {PageId}", pageId);
            await commandBus.SendAsync(new NavigateToUrlCommand("https://httpbin.org/json", pageId));
            logger.LogInformation("Navigated to: {Url}", playwrightManagementService.ActivePage?.Url);

            // Example 5: Error handling demonstration
            logger.LogInformation("Example 5: Demonstrating error handling...");
            try
            {
                await commandBus.SendAsync(new NavigateToUrlCommand("invalid-url"));
            }
            catch (Exception ex)
            {
                logger.LogInformation("Expected error caught: {Error}", ex.Message);
            }

            // Example 7: Close browser
            logger.LogInformation("Example 7: Closing browser...");
            await commandBus.SendAsync(new CloseBrowserCommand());
            logger.LogInformation("Browser closed: {IsOpen}", playwrightManagementService.IsBrowserOpen);

            logger.LogInformation("All Playwright command examples completed successfully!");

            // Example 8: WebDriver Plugin Usage
            logger.LogInformation("Example 8: WebDriver Plugin Usage...");

            // Initialize browser and create page for plugin usage
            await commandBus.SendAsync(new InitializeBrowserCommand());
            await commandBus.SendAsync(new CreatePageCommand("https://httpbin.org/forms/post"));

            // Close browser
            await commandBus.SendAsync(new CloseBrowserCommand());

            // Debug configuration values
            logger.LogDebug("Configuration values:");
            logger.LogDebug("  ModelsDirectory: {ModelsDirectory}", playwrightOptions.ModelsDirectory);
            logger.LogDebug("  DefaultSchemaPath: {DefaultSchemaPath}", playwrightOptions.DefaultSchemaPath);
            logger.LogDebug("  EnableCaching: {EnableCaching}", playwrightOptions.EnableCaching);
            logger.LogDebug("  CacheExpiration: {CacheExpiration}", playwrightOptions.CacheExpiration);
            logger.LogDebug("  IncludeDebugInfo: {IncludeDebugInfo}", playwrightOptions.IncludeDebugInfo);
            logger.LogDebug("  Ollama BaseAddress: {OllamaBaseAddress}", ollamaOptions.BaseAddress);
            logger.LogDebug("  Ollama Timeout: {OllamaTimeout} minutes", ollamaOptions.TimeoutMinutes);
            logger.LogDebug("  Ollama DefaultModel: {OllamaDefaultModel}", ollamaOptions.DefaultModelName);

            try
            {
                logger.LogInformation("Starting PlaywrightStudio application");
                logger.LogInformation("Environment: {Environment}", environment);
                logger.LogInformation("Configuration loaded from appsettings.json and appsettings.{Environment}.json", environment);

                // Initialize PlaywrightStudio using the new architecture
                var result = await playwrightService.InitializeAsync();

                if (result.Success)
                {
                    logger.LogInformation("PlaywrightStudio initialized successfully: {Message}", result.Message);
                    Console.WriteLine($"Loaded {result.Models.Count} page object models");
                    Console.WriteLine($"Created {result.Plugins.Count} plugins with {result.Plugins.Sum(p => p.Count())} total functions");
                    Console.WriteLine($"Browser open: {playwrightManagementService.IsBrowserOpen}");
                    Console.WriteLine($"Pages count: {playwrightManagementService.Pages.Count}");
                    Console.WriteLine($"Active page URL: {playwrightManagementService.ActivePage?.Url}");

                    // Display plugin information
                    foreach (var plugin in result.Plugins)
                    {
                        Console.WriteLine($"Plugin '{plugin.Name}' has {plugin.Count()} functions");
                    }
                }
                else
                {
                    logger.LogError("PlaywrightStudio initialization failed: {Message}", result.Message);
                    Console.WriteLine($"Initialization failed: {result.Message}");
                }

                // Keep the application running for debugging
                Debugger.Break();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Application failed to start");
                Console.WriteLine($"Application failed: {ex.Message}");
            }
            finally
            {
                // Cleanup
                try
                {
                    await playwrightManagementService.DisposeAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during PlaywrightManagementService disposal");
                }

                serviceProvider.Dispose();
            }
        }

    }
}