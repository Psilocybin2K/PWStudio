# PageObjectModelSearchService

A semantic search service for page object models using embeddings and cosine similarity.

## Features

- **Semantic Search**: Uses embeddings to find semantically similar utterances across page object models
- **Multi-level Indexing**: Indexes utterances from pages, elements, tasks, and steps
- **Scoped Search**: Filter results by type (pages, elements, tasks, steps, or all)
- **Similarity Scoring**: Returns results with cosine similarity scores
- **Caching**: Leverages the existing embedding cache for performance
- **Statistics**: Provides index statistics for monitoring

## Usage

### Basic Setup

The service is automatically registered in the DI container. You can inject it into your services:

```csharp
public class MyService
{
    private readonly PageObjectModelSearchService _searchService;
    
    public MyService(PageObjectModelSearchService searchService)
    {
        _searchService = searchService;
    }
}
```

### Initialize the Service

Before searching, you must initialize the service to index all page object models:

```csharp
await _searchService.InitializeAsync();
```

### Perform Searches

```csharp
// Basic search
var request = new SearchRequest(
    Query: "login",
    MaxResults: 10,
    MinSimilarityThreshold: 0.1f,
    Scope: SearchScope.All
);

var response = await _searchService.SearchAsync(request);

// Process results
foreach (var result in response.Results)
{
    Console.WriteLine($"[{result.Type}] {result.Name} - Similarity: {result.SimilarityScore:F3}");
    Console.WriteLine($"Matched: '{result.MatchedUtterance}'");
}
```

### Scoped Searches

```csharp
// Search only pages
var pageRequest = new SearchRequest("login", Scope: SearchScope.Pages);

// Search only elements
var elementRequest = new SearchRequest("input", Scope: SearchScope.Elements);

// Search only tasks
var taskRequest = new SearchRequest("submit", Scope: SearchScope.Tasks);

// Search only steps
var stepRequest = new SearchRequest("click", Scope: SearchScope.Steps);
```

### Get Index Statistics

```csharp
var stats = _searchService.GetIndexStatistics();
Console.WriteLine($"Total utterances: {stats.TotalUtterances}");
Console.WriteLine($"Pages: {stats.PageUtterances}, Elements: {stats.ElementUtterances}");
Console.WriteLine($"Tasks: {stats.TaskUtterances}, Steps: {stats.StepUtterances}");
```

## Search Request Parameters

- **Query**: The search text (required)
- **MaxResults**: Maximum number of results to return (default: 10)
- **MinSimilarityThreshold**: Minimum similarity score (0.0 to 1.0, default: 0.0)
- **Scope**: Search scope filter (default: SearchScope.All)

## Search Response

- **Request**: The original search request
- **Results**: Array of search results
- **TotalResults**: Total number of results found (before MaxResults limit)
- **SearchDuration**: Time taken to perform the search
- **ErrorMessage**: Error message if search failed

## Search Result Properties

- **Id**: Unique identifier for the utterance
- **Type**: Type of result (page, element, task, step)
- **Name**: Name of the page
- **Description**: Description of the page
- **Url**: URL of the page
- **SimilarityScore**: Cosine similarity score (0.0 to 1.0)
- **MatchedUtterance**: The utterance that matched the query
- **Context**: Additional context information (element, task, step details)

## Example

See `Examples/SearchServiceExample.cs` for a complete working example.

## Performance Considerations

- The service uses the existing embedding cache, so repeated searches are fast
- Initialization time depends on the number of utterances in your page object models
- Search performance is O(n) where n is the number of indexed utterances
- Consider using appropriate similarity thresholds to filter out irrelevant results

## Dependencies

- `EmbeddingGeneratorService`: For generating embeddings
- `IModelRepository`: For loading page object models
- `EmbeddingCacheService`: For caching embeddings (automatic)
