namespace PlaywrightStudio.Models;

/// <summary>
/// Represents a search result from semantic search
/// </summary>
public record SearchResult(
    string Id,
    string Type,
    string Name,
    string Description,
    string Url,
    float SimilarityScore,
    string MatchedUtterance,
    SearchResultContext Context
);

/// <summary>
/// Context information for search results
/// </summary>
public record SearchResultContext(
    string PageName,
    string? ElementName,
    string? TaskName,
    string? StepDescription
);

/// <summary>
/// Search request parameters
/// </summary>
public record SearchRequest(
    string Query,
    int MaxResults = 10,
    double MinSimilarityThreshold = 0.0f,
    SearchScope Scope = SearchScope.All
);

/// <summary>
/// Defines the scope of search
/// </summary>
public enum SearchScope
{
    All,
    Pages,
    Elements,
    Tasks,
    Steps
}

/// <summary>
/// Search response containing results and metadata
/// </summary>
public record SearchResponse(
    SearchRequest Request,
    SearchResult[] Results,
    int TotalResults,
    TimeSpan SearchDuration,
    string? ErrorMessage = null
);
