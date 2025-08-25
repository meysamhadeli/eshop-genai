using System.Linq.Expressions;
using System.Text.Json;
using BuildingBlocks.AI.SemanticSearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace BuildingBlocks.SemanticSearch;

public interface ISemanticSearchService
{
    Task<IEnumerable<TResult>> SemanticSearchAsync<T, TResult>(
        string query,
        Expression<Func<T, bool>>? filter = null,
        int maxResults = 10,
        double similarityThreshold = 0.7,
        CancellationToken cancellationToken = default)
    where T : class
    where TResult : class;

    Task IndexAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    Task UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    Task DeleteAsync<T>(object id, CancellationToken cancellationToken = default) where T : class;
    
    Task EnsureCollectionExists<T>(CancellationToken cancellationToken = default) where T : class;
    Task<bool> CollectionExistsAsync<T>(CancellationToken cancellationToken = default) where T : class;
    Task DeleteCollectionAsync<T>(CancellationToken cancellationToken = default) where T : class;
    Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken = default);
}

public class SemanticSearchService : ISemanticSearchService
{
    private readonly SemanticSearchOptions _options;
    private readonly QdrantClient _qdrantClient;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly ILogger<SemanticSearchService> _logger;
    private readonly bool _isEnabled;

    public SemanticSearchService(
        IOptions<SemanticSearchOptions> options,
        QdrantClient qdrantClient,
        ITextEmbeddingGenerationService embeddingService,
        ILogger<SemanticSearchService> logger)
    {
        _options = options.Value;
        _qdrantClient = qdrantClient;
        _embeddingService = embeddingService;
        _logger = logger;
        _isEnabled = _options.Enabled;

        if (_isEnabled)
        {
            _logger.LogInformation("Semantic search service initialized with provider: {Provider}", _options.Provider);
        }
        else
        {
            _logger.LogInformation("Semantic search service is disabled");
        }
    }

    public async Task<IEnumerable<TResult>> SemanticSearchAsync<T, TResult>(
        string query,
        Expression<Func<T, bool>>? filter = null,
        int maxResults = 10,
        double similarityThreshold = 0.7,
        CancellationToken cancellationToken = default)
        where T : class
        where TResult : class
    {
        if (!_isEnabled) return Enumerable.Empty<TResult>();

        try
        {
            var collectionName = GetCollectionName<T>();
            
            // Check if collection exists before searching
            var collectionExists = await _qdrantClient.CollectionExistsAsync(collectionName, cancellationToken);
            if (!collectionExists)
            {
                _logger.LogWarning("Collection {CollectionName} does not exist, returning empty results", collectionName);
                return Enumerable.Empty<TResult>();
            }

            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);
            
            var searchResult = await _qdrantClient.SearchAsync(
                collectionName: collectionName,
                vector: queryEmbedding.ToArray(),
                limit: (ulong?)_options?.MaxResults ?? (ulong)maxResults,
                scoreThreshold: (float?)_options?.SimilarityThreshold ?? (float?)similarityThreshold,
                cancellationToken: cancellationToken);

            var entities = new List<TResult>();

            foreach (var point in searchResult)
            {
                try
                {
                    if (point.Payload.TryGetValue("entity", out var entityJson))
                    {
                        var entity = JsonSerializer.Deserialize<TResult>(entityJson.StringValue);
                        if (entity != null)
                        {
                            entities.Add(entity);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize entity from vector store");
                }
            }

            _logger.LogInformation("Semantic search found {Count} entities for query: {Query}", entities.Count, query);
            return entities;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to perform semantic search for query: {Query}", query);
            return Enumerable.Empty<TResult>();
        }
    }

    public async Task IndexAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        if (!_isEnabled) return;

        try
        {
            // Ensure collection exists before indexing
            await EnsureCollectionExists<T>(cancellationToken);
            
            var collectionName = GetCollectionName<T>();
            var entityId = GetEntityId(entity);
            var text = GenerateSearchText(entity);
            
            var embedding = await _embeddingService.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
            
            var point = new PointStruct
            {
                Id = new PointId{Uuid = entityId},
                Vectors = embedding.ToArray(),
                Payload = 
                {
                    ["text"] = text,
                    ["entity"] = JsonSerializer.Serialize(entity),
                    ["type"] = typeof(T).Name,
                    ["timestamp"] = DateTime.UtcNow.ToString("O")
                }
            };

            await _qdrantClient.UpsertAsync(collectionName, new[] { point }, cancellationToken: cancellationToken);
            
            _logger.LogDebug("Entity {EntityId} indexed successfully in collection {Collection}", entityId, collectionName);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to index entity {Entity}", entity);
        }
    }

    public async Task UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        if (!_isEnabled) return;

        try
        {
            var entityId = GetEntityId(entity);
            await DeleteAsync<T>(entityId, cancellationToken);
            await IndexAsync(entity, cancellationToken);
            _logger.LogDebug("Entity {EntityId} updated in vector store", entityId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to update entity {Entity}", entity);
        }
    }

    public async Task DeleteAsync<T>(object id, CancellationToken cancellationToken = default) where T : class
    {
        if (!_isEnabled) return;

        try
        {
            var collectionName = GetCollectionName<T>();
            await _qdrantClient.DeleteAsync(
                collectionName: collectionName,
                id: new PointId{Uuid = id.ToString()},
                cancellationToken: cancellationToken);
            
            _logger.LogDebug("Entity {EntityId} deleted from collection {Collection}", id, collectionName);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to delete entity {EntityId} from collection", id);
        }
    }

    public async Task EnsureCollectionExists<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (!_isEnabled) return;

        var collectionName = GetCollectionName<T>();
        
        try
        {
            var collectionExists = await _qdrantClient.CollectionExistsAsync(collectionName, cancellationToken);
            
            if (!collectionExists)
            {
                await _qdrantClient.CreateCollectionAsync(
                    collectionName: collectionName,
                    vectorsConfig: new VectorParams
                    {
                        Size = (uint)_options.VectorSize,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: cancellationToken);
                
                _logger.LogInformation("Created collection {CollectionName} with vector size {VectorSize}", 
                    collectionName, _options.VectorSize);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to create collection {CollectionName}", collectionName);
            throw;
        }
    }

    public async Task<bool> CollectionExistsAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (!_isEnabled) return false;

        var collectionName = GetCollectionName<T>();
        return await _qdrantClient.CollectionExistsAsync(collectionName, cancellationToken);
    }

    public async Task DeleteCollectionAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (!_isEnabled) return;

        var collectionName = GetCollectionName<T>();
        await _qdrantClient.DeleteCollectionAsync(collectionName, cancellationToken: cancellationToken);
        _logger.LogInformation("Deleted collection {CollectionName}", collectionName);
    }

    public async Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken = default)
    {
        if (!_isEnabled) return new List<string>();

        var collections = await _qdrantClient.ListCollectionsAsync(cancellationToken);
        return collections.ToList();
    }
    
    private string GetCollectionName<T>() where T : class
    {
        var typeName = typeof(T).Name.ToLowerInvariant();
        
        // Remove common suffixes
        var suffixesToRemove = new[] { "dto", "model", "entity", "record", "viewmodel" };
        foreach (var suffix in suffixesToRemove)
        {
            if (typeName.EndsWith(suffix) && typeName.Length > suffix.Length)
            {
                typeName = typeName.Substring(0, typeName.Length - suffix.Length);
                break;
            }
        }
        
        return $"{_options.DefaultCollectionPrefix}{typeName}";
    }

    private string GetEntityId<T>(T entity) where T : class
    {
        // Try to get ID property using reflection
        var idProperty = typeof(T).GetProperties()
            .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                                p.Name.Equals(typeof(T).Name + "Id", StringComparison.OrdinalIgnoreCase));

        return idProperty?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
    }

    private string GenerateSearchText<T>(T entity) where T : class
    {
        // Create searchable text from entity properties
        var properties = typeof(T).GetProperties()
            .Where(p => p.PropertyType == typeof(string) && 
                       !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.GetValue(entity)?.ToString())
            .Where(value => !string.IsNullOrEmpty(value));

        return string.Join(" ", properties);
    }
}