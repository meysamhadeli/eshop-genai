using System.Linq.Expressions;
using System.Text.Json;
using BuildingBlocks.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace BuildingBlocks.SemanticSearch;

public interface ISemanticSearchService
{
    // Core search functionality
    Task<IEnumerable<TResult>> SemanticSearchAsync<T, TResult>(
        string query,
        Expression<Func<T, bool>>? filter = null,
        int maxResults = 10,
        double similarityThreshold = 0.7,
        CancellationToken cancellationToken = default)
    where T : class
    where TResult : class;

    // Vector-based search (for recommendations)
    Task<IEnumerable<T>> SearchVectorsAsync<T>(
        float[] vector, 
        int maxResults = 10,
        double similarityThreshold = 0.7,
        CancellationToken cancellationToken = default) where T : class;

    // CRUD operations
    Task IndexAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    Task UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    Task DeleteAsync<T>(object id, CancellationToken cancellationToken = default) where T : class;
    
    // Vector operations
    Task<float[]?> GetVectorAsync<T>(object id, CancellationToken cancellationToken = default) where T : class;

    // Collection management
    Task EnsureCollectionExists<T>(CancellationToken cancellationToken = default) where T : class;
    Task<bool> CollectionExistsAsync<T>(CancellationToken cancellationToken = default) where T : class;
    Task DeleteCollectionAsync<T>(CancellationToken cancellationToken = default) where T : class;
    Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken = default);
}


public class SemanticSearchService : ISemanticSearchService
{
    private readonly AIOptions _options;
    private readonly QdrantClient _qdrantClient;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly ILogger<SemanticSearchService> _logger;
    private readonly bool _isEnabled;

    public SemanticSearchService(
        IOptions<AIOptions> options,
        QdrantClient qdrantClient,
        ITextEmbeddingGenerationService embeddingService,
        ILogger<SemanticSearchService> logger)
    {
        _options = options.Value;
        _qdrantClient = qdrantClient;
        _embeddingService = embeddingService;
        _logger = logger;
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
            if (!await CollectionExistsAsync<T>(cancellationToken))
            {
                _logger.LogWarning("Collection {CollectionName} does not exist", collectionName);
                return Enumerable.Empty<TResult>();
            }

            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);
            
            var searchResult = await _qdrantClient.SearchAsync(
                collectionName: collectionName,
                vector: queryEmbedding.ToArray(),
                limit: (ulong)maxResults,
                scoreThreshold: (float)similarityThreshold,
                cancellationToken: cancellationToken);

            return ExtractEntities<TResult>(searchResult);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to perform semantic search for query: {Query}", query);
            return Enumerable.Empty<TResult>();
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
            var collectionName = GetCollectionName<T>();
            if (!await CollectionExistsAsync<T>(cancellationToken))
            {
                _logger.LogWarning("Collection {CollectionName} does not exist", collectionName);
                return Enumerable.Empty<T>();
            }

            var searchResult = await _qdrantClient.SearchAsync(
                collectionName: collectionName,
                vector: vector,
                limit: (ulong)maxResults,
                scoreThreshold: (float)similarityThreshold,
                cancellationToken: cancellationToken);

            return ExtractEntities<T>(searchResult);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to perform vector search");
            return Enumerable.Empty<T>();
        }
    }

    public async Task IndexAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        if (!_isEnabled) return;

        try
        {
            var id = GetEntityId(entity);
            await EnsureCollectionExists<T>(cancellationToken);
            var collectionName = GetCollectionName<T>();
            var text = GenerateSearchText(entity);

            var embedding = await _embeddingService.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
            
            var point = new PointStruct
                        {
                            Id = new PointId { Uuid = id },
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
            
            _logger.LogDebug("Auto-indexed entity {EntityId} in collection {CollectionName}", id, collectionName);
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
            var id = GetEntityId(entity);
            await DeleteAsync<T>(id, cancellationToken);
            await IndexAsync(entity, cancellationToken);
            
            _logger.LogDebug("Updated entity {EntityId} in vector store", id);
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
                id: new PointId { Uuid = id.ToString() },
                cancellationToken: cancellationToken);

            _logger.LogDebug("Deleted entity {EntityId} from collection {CollectionName}", id, collectionName);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to delete entity {EntityId}", id);
        }
    }

    public async Task<float[]?> GetVectorAsync<T>(object id, CancellationToken cancellationToken = default) where T : class
    {
        if (!_isEnabled) return null;

        try
        {
            var collectionName = GetCollectionName<T>();
            var results = await _qdrantClient.RetrieveAsync(
                collectionName: collectionName,
                ids: new[] { new PointId { Uuid = id.ToString() } },
                withVectors: true,
                cancellationToken: cancellationToken
            );

            var point = results.FirstOrDefault();
            return point != null ? ExtractVectorFromPoint(point) : null;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to get vector for entity {EntityId}", id);
            return null;
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

    private IEnumerable<T> ExtractEntities<T>(IEnumerable<ScoredPoint> points) where T : class
    {
        return points
            .Select(point =>
                    {
                        try
                        {
                            if (point.Payload.TryGetValue("entity", out var entityJson))
                            {
                                return JsonSerializer.Deserialize<T>(entityJson.StringValue);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to deserialize entity from vector store");
                        }
                        return null;
                    })
            .Where(entity => entity != null)
            .Select(entity => entity!);
    }

    private float[]? ExtractVectorFromPoint(RetrievedPoint point)
    {
        if (point.Vectors == null) 
        {
            _logger.LogWarning("Point has no vectors");
            return null;
        }

        try
        {
            var vectorsProperty = point.Vectors.GetType().GetProperty("Vectors");
            if (vectorsProperty != null)
            {
                var vectorsValue = vectorsProperty.GetValue(point.Vectors);
                if (vectorsValue is System.Collections.IDictionary vectorsDict && vectorsDict.Count > 0)
                {
                    foreach (System.Collections.DictionaryEntry entry in vectorsDict)
                    {
                        var vectorObj = entry.Value;
                        var vectorProp = vectorObj?.GetType().GetProperty("Vector");
                        if (vectorProp != null)
                        {
                            var vectorData = vectorProp.GetValue(vectorObj);
                            if (vectorData is Google.Protobuf.Collections.RepeatedField<float> repeatedField)
                            {
                                return repeatedField.ToArray();
                            }
                        }
                    }
                }
            }

            _logger.LogWarning("Could not extract vector from RetrievedPoint structure");
            return null;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to extract vector using reflection");
            return null;
        }
    }

    private string GetCollectionName<T>() where T : class
    {
        var typeName = typeof(T).Name.ToLowerInvariant();
        
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
        var idProperty = typeof(T).GetProperties()
            .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                                p.Name.Equals(typeof(T).Name + "Id", StringComparison.OrdinalIgnoreCase));

        return idProperty?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
    }

    private string GenerateSearchText<T>(T entity) where T : class
    {
        var properties = typeof(T).GetProperties()
            .Where(p => p.PropertyType == typeof(string) &&
                       !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.GetValue(entity)?.ToString())
            .Where(value => !string.IsNullOrEmpty(value));

        return string.Join(" ", properties);
    }
}