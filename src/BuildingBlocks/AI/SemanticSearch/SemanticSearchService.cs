using System.Linq.Expressions;
using System.Text.Json;
using BuildingBlocks.AI.Qdrant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace BuildingBlocks.AI.SemanticSearch;

public interface ISemanticSearchService
{
    Task<SearchResult<TResult>> SemanticSearchAsync<TResult>(
        string query,
        Expression<Func<TResult, bool>>? filter = null,
        int maxResults = 10,
        double similarityThreshold = 0.7,
        bool includeExplanation = false,
        CancellationToken cancellationToken = default
    )
    where TResult : class;

    Task<IEnumerable<T>> SearchVectorsAsync<T>(
        float[] vector,
        int maxResults = 10,
        double similarityThreshold = 0.7,
        CancellationToken cancellationToken = default) where T : class;
}

public class SemanticSearchService : ISemanticSearchService
{
    private readonly AIOptions _options;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<SemanticSearchService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly bool _isEnabled;

    public SemanticSearchService(
        IOptions<AIOptions> options,
        ITextEmbeddingGenerationService embeddingService,
        IChatCompletionService chatService,
        ILogger<SemanticSearchService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _options = options.Value;
        _embeddingService = embeddingService;
        _chatService = chatService;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _isEnabled = _options.SemanticSearchEnabled;

        if (_isEnabled)
        {
            _logger.LogInformation("Semantic search service initialized with provider: {Provider}", _options.Provider);
        }
        else
        {
            _logger.LogInformation("Semantic search service is disabled");
        }
    }

    public async Task<SearchResult<TResult>> SemanticSearchAsync<TResult>(
        string query,
        Expression<Func<TResult, bool>>? filter = null,
        int maxResults = 10,
        double similarityThreshold = 0.7,
        bool includeExplanation = false,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
        if (!_isEnabled) return new SearchResult<TResult>();

        try
        {
            var repo = GetRepository<TResult>();
            if (!await repo.CollectionExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Collection for {TypeName} does not exist", typeof(TResult).Name);
                return new SearchResult<TResult>();
            }

            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);

            var scoredPoints = await repo.SearchAsync(
                queryEmbedding.ToArray(),
                maxResults,
                similarityThreshold,
                cancellationToken);

            var averageSimilarity = scoredPoints.Any() ? scoredPoints.Average(p => p.Score) : 0.0;

            var entities = scoredPoints
                .Select(repo.ExtractEntity)
                .Where(e => e != null)
                .Select(e => e!)
                .ToList();

            if (filter != null)
            {
                var compiledFilter = filter.Compile();
                entities = entities.Where(compiledFilter).ToList();
            }

            var explanation = includeExplanation || _options.SearchExplanationEnabled
                ? await GenerateExplanationAsync(query, entities, averageSimilarity, cancellationToken)
                : null;

            return new SearchResult<TResult>
            {
                Results = entities,
                Explanation = explanation,
                OriginalQuery = query,
                HasExactMatches = entities.Any(),
                TotalCounts = entities.Count,
                AverageSimilarity = averageSimilarity,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to perform semantic search for query: {Query}", query);
            return new SearchResult<TResult>();
        }
    }

    public async Task<IEnumerable<T>> SearchVectorsAsync<T>(
        float[] vector,
        int maxResults = 10,
        double similarityThreshold = 0.7,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!_isEnabled) return Enumerable.Empty<T>();
        try
        {
            var repo = GetRepository<T>();
            var scoredPoints = await repo.SearchAsync(vector, maxResults, similarityThreshold, cancellationToken);
            return scoredPoints
                .Select(repo.ExtractEntity)
                .Where(e => e != null)!;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to perform vector search");
            return Enumerable.Empty<T>();
        }
    }


    private async Task<string?> GenerateExplanationAsync<TResult>(
        string query,
        List<TResult> results,
        double averageSimilarity,
        CancellationToken cancellationToken) where TResult : class
    {
        try
        {
            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage($"""
                You are a helpful search assistant that provides explanations for search results.
                Your task is to explain the search results to the user in a friendly, helpful manner.

                Guidelines:
                1. If there are no exact matches, acknowledge this and suggest similar alternatives
                2. If there are results, explain how they relate to the user's query
                3. Be concise but helpful (2-3 sentences)
                4. Use natural, conversational language
                5. If suggesting alternatives, mention they are similar but not exact matches
                6. Consider the average similarity score: {averageSimilarity:F2}

                Current query: {query}
                Number of results found: {results.Count}
                """);

            var resultsJson = JsonSerializer.Serialize(results.Take(3), new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            chatHistory.AddUserMessage($"""
                Please provide a helpful explanation for these search results.

                User Query: "{query}"

                Search Results (first 3 items):
                {resultsJson}

                Average similarity score: {averageSimilarity:F2}

                Please provide a concise, helpful explanation that:
                1. Acknowledges whether exact matches were found
                2. Explains the relationship between results and query
                3. Suggests alternatives if no exact matches or low similarity
                4. Is friendly and encouraging
                5. References the similarity score if relevant
                """);

            var response = await _chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            return response.Content;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to generate search explanation");
            return null;
        }
    }

    private IQdrantRepository<T> GetRepository<T>() where T : class
    {
        using var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IQdrantRepository<T>>();
    }
}