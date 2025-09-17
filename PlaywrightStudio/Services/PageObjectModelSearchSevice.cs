using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using PlaywrightStudio.Interfaces;
using PlaywrightStudio.Models;

namespace PlaywrightStudio.Services;

/// <summary>
/// Service for semantic search of page object models using embeddings and cosine similarity
/// </summary>
public class PageObjectModelSearchService
{
    private readonly IModelRepository _modelRepository;
    private readonly EmbeddingGeneratorService _embeddingService;
    private readonly ILogger<PageObjectModelSearchService> _logger;
    private readonly Dictionary<string, Embedding<float>> _utteranceEmbeddings = new();
    private readonly Dictionary<string, SearchableUtterance> _utteranceMetadata = new();

    // Lexical inverted index structures for BM25
    private readonly Dictionary<string, Dictionary<string, int>> _invertedIndex = new(); // term -> (docId -> tf)
    private readonly Dictionary<string, int> _docLengths = new(); // docId -> token count
    private double _avgDocLength = 1.0;
    private readonly Dictionary<string, string[]> _normalizedDocTokens = new(); // docId -> tokens

    private readonly object _indexLock = new();
    private readonly SearchOptions _options;

    public PageObjectModelSearchService(
        IModelRepository modelRepository,
        EmbeddingGeneratorService embeddingService,
        ILogger<PageObjectModelSearchService> logger,
        SearchOptions? options = null)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new SearchOptions();
    }

    /// <summary>
    /// Initializes the search service by loading and indexing all page object models
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing PageObjectModelSearchService");

            var models = await _modelRepository.GetAllModelsAsync();
            await IndexModelsAsync(models);

            _logger.LogInformation("Indexed {UtteranceCount} utterances from {ModelCount} models",
                _utteranceEmbeddings.Count, models.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing PageObjectModelSearchService");
            throw;
        }
    }

    /// <summary>
    /// Performs semantic search across page object model utterances
    /// </summary>
    /// <param name="request">Search request parameters</param>
    /// <returns>Search response with results</returns>
    public async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Performing semantic search for query: {Query}", request.Query);

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse(
                    request,
                    Array.Empty<SearchResult>(),
                    0,
                    stopwatch.Elapsed,
                    "Query cannot be empty"
                );
            }

            // Generate embedding and lexical query terms
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Query);
            var baseTokens = NormalizeAndTokenize(request.Query);
            var queryTerms = ExpandQueryTerms(baseTokens).ToArray();

            // Candidate pruning using lexical index
            var candidateDocs = new HashSet<string>();
            foreach (var term in queryTerms)
            {
                if (_invertedIndex.TryGetValue(term, out var postings))
                {
                    foreach (var docId in postings.Keys)
                    {
                        candidateDocs.Add(docId);
                    }
                }
            }

            if (candidateDocs.Count == 0)
            {
                // fallback: consider all
                candidateDocs.UnionWith(_utteranceEmbeddings.Keys);
            }
            else if (candidateDocs.Count > _options.MaxLexicalCandidates)
            {
                // simple trimming by summed term frequency
                var trimmed = candidateDocs
                    .Select(id => (id, score: queryTerms.Sum(t => _invertedIndex.TryGetValue(t, out var p) && p.TryGetValue(id, out var tf) ? tf : 0)))
                    .OrderByDescending(x => x.score)
                    .Take(_options.MaxLexicalCandidates)
                    .Select(x => x.id);
                candidateDocs = new HashSet<string>(trimmed);
            }

            var results = new List<(SearchableUtterance u, double score, float cos, double bm25)>();

            foreach (var docId in candidateDocs)
            {
                if (!_utteranceMetadata.TryGetValue(docId, out var utterance))
                {
                    continue;
                }

                if (!MatchesScope(utterance.Type, request.Scope))
                {
                    continue;
                }

                var cos = SimilarityCalculator.CalculateCosineSimilarity(queryEmbedding, _utteranceEmbeddings[docId]);
                var bm25 = Bm25Score(queryTerms, docId);

                // Preserve prior threshold behavior: if no lexical score, require cosine threshold
                if (bm25 == 0 && cos < request.MinSimilarityThreshold)
                {
                    continue;
                }

                // Type boost
                var typeBoost = utterance.Type switch
                {
                    "element" => _options.ElementBoost,
                    "task" => _options.TaskBoost,
                    "page" => _options.PageBoost,
                    "step" => _options.StepBoost,
                    _ => 1.0f
                };

                var hybrid = (cos * _options.EmbeddingWeight + (float)bm25 * _options.LexicalWeight) * typeBoost;
                results.Add((utterance, hybrid, cos, bm25));
            }

            var totalMatches = results.Count;

            var topResults = results
                .OrderByDescending(x => x.score)
                .Take(request.MaxResults)
                .Select(x => CreateSearchResult(x.u, (float)x.cos))
                .ToArray();

            stopwatch.Stop();

            _logger.LogDebug("Search completed in {Duration}ms, found {ResultCount} results",
                stopwatch.ElapsedMilliseconds, topResults.Length);

            return new SearchResponse(
                request,
                topResults,
                totalMatches,
                stopwatch.Elapsed
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error performing semantic search for query: {Query}", request.Query);

            return new SearchResponse(
                request,
                Array.Empty<SearchResult>(),
                0,
                stopwatch.Elapsed,
                ex.Message
            );
        }
    }

    /// <summary>
    /// Indexes all utterances from the provided page object models
    /// </summary>
    private async Task IndexModelsAsync(IEnumerable<PageObjectModel> models)
    {
        lock (_indexLock)
        {
            _utteranceEmbeddings.Clear();
            _utteranceMetadata.Clear();
            _invertedIndex.Clear();
            _docLengths.Clear();
            _normalizedDocTokens.Clear();
        }

        foreach (var model in models)
        {
            await IndexPageUtterancesAsync(model);
            await IndexElementUtterancesAsync(model);
            await IndexTaskUtterancesAsync(model);
            await IndexStepUtterancesAsync(model);
        }

        lock (_indexLock)
        {
            RecomputeAverages();
        }
    }

    /// <summary>
    /// Indexes page-level utterances
    /// </summary>
    private async Task IndexPageUtterancesAsync(PageObjectModel model)
    {
        if (model.Utterances != null)
        {
            foreach (var utterance in model.Utterances)
            {
                if (string.IsNullOrWhiteSpace(utterance) == false)
                {
                    var utteranceId = GenerateUtteranceId("page", model.Name, utterance);
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(utterance);

                    _utteranceEmbeddings[utteranceId] = embedding;
                    _utteranceMetadata[utteranceId] = new SearchableUtterance(
                        utteranceId,
                        "page",
                        utterance,
                        model.Name,
                        model.Description,
                        model.Url,
                        null, // element name
                        null, // task name
                        null  // step description
                        );
                    AddToLexicalIndex(utteranceId, utterance);
                }
            }
        }
    }

    /// <summary>
    /// Indexes element-level utterances
    /// </summary>
    private async Task IndexElementUtterancesAsync(PageObjectModel model)
    {
        foreach (var element in model.Elements)
        {
            if (element.Utterances != null)
            {
                foreach (var utterance in element.Utterances)
                {
                    if (string.IsNullOrWhiteSpace(utterance) == false)
                    {
                        var utteranceId = GenerateUtteranceId("element", model.Name, element.Name, utterance);
                        var embedding = await _embeddingService.GenerateEmbeddingAsync(utterance);

                        _utteranceEmbeddings[utteranceId] = embedding;
                        _utteranceMetadata[utteranceId] = new SearchableUtterance(
                            utteranceId,
                            "element",
                            utterance,
                            model.Name,
                            model.Description,
                            model.Url,
                            element.Name,
                            null, // task name
                            null  // step description
                            );
                        AddToLexicalIndex(utteranceId, utterance);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Indexes task-level utterances
    /// </summary>
    private async Task IndexTaskUtterancesAsync(PageObjectModel model)
    {
        foreach (var task in model.Tasks)
        {
            if (task.Utterances != null)
            {
                foreach (var utterance in task.Utterances)
                {
                    if (string.IsNullOrWhiteSpace(utterance) == false)
                    {
                        var utteranceId = GenerateUtteranceId("task", model.Name, task.Name, utterance);
                        var embedding = await _embeddingService.GenerateEmbeddingAsync(utterance);

                        _utteranceEmbeddings[utteranceId] = embedding;
                        _utteranceMetadata[utteranceId] = new SearchableUtterance(
                            utteranceId,
                            "task",
                            utterance,
                            model.Name,
                            model.Description,
                            model.Url,
                            null, // element name
                            task.Name,
                            null  // step description
                        );
                        AddToLexicalIndex(utteranceId, utterance);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Indexes step-level utterances
    /// </summary>
    private async Task IndexStepUtterancesAsync(PageObjectModel model)
    {
        foreach (var task in model.Tasks)
        {
            foreach (var step in task.Steps)
            {
                if (step.Utterances != null)
                {
                    foreach (var utterance in step.Utterances)
                    {
                        if (string.IsNullOrWhiteSpace(utterance) == false)
                        {
                            var utteranceId = GenerateUtteranceId("step", model.Name, task.Name, step.Description, utterance);
                            var embedding = await _embeddingService.GenerateEmbeddingAsync(utterance);

                            _utteranceEmbeddings[utteranceId] = embedding;
                            _utteranceMetadata[utteranceId] = new SearchableUtterance(
                                utteranceId,
                                "step",
                                utterance,
                                model.Name,
                                model.Description,
                                model.Url,
                                null, // element name
                                task.Name,
                                step.Description
                            );
                            AddToLexicalIndex(utteranceId, utterance);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Generates a unique ID for an utterance
    /// </summary>
    private static string GenerateUtteranceId(params string[] parts)
    {
        return string.Join("|", parts);
    }

    /// <summary>
    /// Checks if an utterance type matches the search scope
    /// </summary>
    private static bool MatchesScope(string utteranceType, SearchScope scope)
    {
        return scope switch
        {
            SearchScope.All => true,
            SearchScope.Pages => utteranceType == "page",
            SearchScope.Elements => utteranceType == "element",
            SearchScope.Tasks => utteranceType == "task",
            SearchScope.Steps => utteranceType == "step",
            _ => true
        };
    }

    private static readonly char[] SplitDelimiters = " \t\r\n,.;:!?()[]{}\"'/_-+".ToCharArray();

    private IEnumerable<string> ExpandQueryTerms(IEnumerable<string> tokens)
    {
        foreach (var t in tokens)
        {
            yield return t;
            if (_options.Synonyms.TryGetValue(t, out var syns))
            {
                foreach (var s in syns)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        yield return s.ToLowerInvariant();
                    }
                }
            }
        }
    }

    private string[] NormalizeAndTokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        text = text.ToLowerInvariant();
        var raw = text.Split(SplitDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return raw.Where(t => !_options.Stopwords.Contains(t)).ToArray();
    }

    private void AddToLexicalIndex(string docId, string text)
    {
        var tokens = NormalizeAndTokenize(text);
        _normalizedDocTokens[docId] = tokens;

        _docLengths[docId] = tokens.Length;
        foreach (var term in tokens)
        {
            if (!_invertedIndex.TryGetValue(term, out var postings))
            {
                postings = new Dictionary<string, int>();
                _invertedIndex[term] = postings;
            }
            postings.TryGetValue(docId, out var tf);
            postings[docId] = tf + 1;
        }
    }

    private void RecomputeAverages()
    {
        _avgDocLength = _docLengths.Count == 0 ? 1.0 : _docLengths.Values.Average();
    }

    private double Bm25Score(ReadOnlySpan<string> queryTerms, string docId)
    {
        const double k1 = 1.2;
        const double b = 0.75;

        if (!_docLengths.TryGetValue(docId, out var dl) || dl == 0) return 0;

        double score = 0.0;
        int N = _docLengths.Count;

        var qset = new HashSet<string>(queryTerms.ToArray());
        foreach (var term in qset)
        {
            if (!_invertedIndex.TryGetValue(term, out var postings) || !postings.TryGetValue(docId, out var tf))
            {
                continue;
            }

            int df = postings.Count;
            double idf = Math.Log((N - df + 0.5) / (df + 0.5) + 1.0);
            double denom = tf + k1 * (1.0 - b + b * dl / _avgDocLength);
            score += idf * (tf * (k1 + 1.0)) / denom;
        }

        return score;
    }

    /// <summary>
    /// Creates a search result from utterance metadata and similarity score
    /// </summary>
    private static SearchResult CreateSearchResult(SearchableUtterance utterance, float similarity)
    {
        return new SearchResult(
            utterance.Id,
            utterance.Type,
            utterance.PageName,
            utterance.PageDescription,
            utterance.Url,
            similarity,
            utterance.Utterance,
            new SearchResultContext(
                utterance.PageName,
                utterance.ElementName,
                utterance.TaskName,
                utterance.StepDescription
            )
        );
    }

    /// <summary>
    /// Gets statistics about the indexed utterances
    /// </summary>
    public SearchIndexStatistics GetIndexStatistics()
    {
        var stats = new SearchIndexStatistics();

        foreach (var utterance in _utteranceMetadata.Values)
        {
            stats.TotalUtterances++;

            switch (utterance.Type)
            {
                case "page":
                    stats.PageUtterances++;
                    break;
                case "element":
                    stats.ElementUtterances++;
                    break;
                case "task":
                    stats.TaskUtterances++;
                    break;
                case "step":
                    stats.StepUtterances++;
                    break;
            }
        }

        return stats;
    }

    /// <summary>
    /// Finds a page object model that matches the given URL
    /// </summary>
    /// <param name="url">The URL to match against</param>
    /// <returns>The matching page object model, or null if no match found</returns>
    public async Task<PageObjectModel?> FindModelByUrlAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("URL is null or empty, cannot find matching model");
            return null;
        }

        try
        {
            var models = await _modelRepository.GetAllModelsAsync();
            
            foreach (var model in models)
            {
                if (IsUrlMatch(url, model.Url))
                {
                    _logger.LogInformation("Found matching page object model '{ModelName}' for URL '{Url}'", 
                        model.Name, url);
                    return model;
                }
            }

            _logger.LogDebug("No matching page object model found for URL '{Url}'", url);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding matching model for URL '{Url}'", url);
            return null;
        }
    }

    /// <summary>
    /// Determines if a URL matches a model URL pattern
    /// </summary>
    /// <param name="url">The URL to test</param>
    /// <param name="modelUrl">The model URL pattern</param>
    /// <returns>True if the URL matches the pattern</returns>
    private static bool IsUrlMatch(string url, string modelUrl)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(modelUrl))
            return false;

        try
        {
            // Handle exact matches
            if (url.Equals(modelUrl, StringComparison.OrdinalIgnoreCase))
                return true;

            // Handle regex patterns in model URLs (prefixed with "regex:")
            if (modelUrl.StartsWith("regex:", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = modelUrl.Substring(6); // Remove "regex:" prefix
                return System.Text.RegularExpressions.Regex.IsMatch(url, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            // Handle wildcard patterns
            if (modelUrl.Contains("*"))
            {
                var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(modelUrl).Replace("\\*", ".*") + "$";
                return System.Text.RegularExpressions.Regex.IsMatch(url, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            // Handle base URL matches (e.g., model URL "https://mistral-nemo:7b" matches "https://mistral-nemo:7b/page")
            if (url.StartsWith(modelUrl, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

/// <summary>
/// Represents a searchable utterance with metadata
/// </summary>
internal record SearchableUtterance(
    string Id,
    string Type,
    string Utterance,
    string PageName,
    string PageDescription,
    string Url,
    string? ElementName,
    string? TaskName,
    string? StepDescription
);

/// <summary>
/// Statistics about the search index
/// </summary>
public class SearchIndexStatistics
{
    public int TotalUtterances { get; set; } = 0;
    public int PageUtterances { get; set; } = 0;
    public int ElementUtterances { get; set; } = 0;
    public int TaskUtterances { get; set; } = 0;
    public int StepUtterances { get; set; } = 0;
}

/// <summary>
/// Options to control search behavior and weighting
/// </summary>
public sealed class SearchOptions
{
    public float EmbeddingWeight { get; init; } = 0.6f;
    public float LexicalWeight { get; init; } = 0.4f;
    public float MinEmbeddingThreshold { get; init; } = 0.2f;

    public float PageBoost { get; init; } = 1.0f;
    public float ElementBoost { get; init; } = 1.05f;
    public float TaskBoost { get; init; } = 1.05f;
    public float StepBoost { get; init; } = 1.0f;

    public int MaxLexicalCandidates { get; init; } = 500;
    public IReadOnlySet<string> Stopwords { get; init; } = DefaultStopwords.English;
    public IReadOnlyDictionary<string, string[]> Synonyms { get; init; } = new Dictionary<string, string[]>();
}

internal static class DefaultStopwords
{
    public static readonly HashSet<string> English = new(new[]
    {
        "a","an","and","are","as","at","be","but","by","for","if","in","into","is","it","no","not","of","on","or","such","that","the","their","then","there","these","they","this","to","was","will","with","you","your","i","we","our","from","can","could","should","would"
    });
}
