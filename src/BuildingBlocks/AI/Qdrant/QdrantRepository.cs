using System.Text.Json;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace BuildingBlocks.AI.Qdrant;

public interface IQdrantRepository<T> where T : class
{
    Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default);
    Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default);
    Task DeleteCollectionAsync(CancellationToken cancellationToken = default);
    Task IndexAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(object id, CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task<float[]?> GetVectorAsync(object id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ScoredPoint>> SearchAsync(
        float[] vector,
        int maxResults = 10,
        double similarityThreshold = 0.7,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ScoredPoint>> SearchByTextAsync(
        string queryText,
        int maxResults = 10,
        double similarityThreshold = 0.7,
        CancellationToken cancellationToken = default);

    Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken = default);
    public T? ExtractEntity(ScoredPoint point);
}

public class QdrantRepository<T> : IQdrantRepository<T> where T : class
{
    private readonly QdrantClient _qdrantClient;
    private readonly ILogger<QdrantRepository<T>> _logger;
    private readonly string _collectionName;
    private readonly ITextEmbeddingGenerationService? _embeddingService;
    private readonly AIOptions _options;

    public QdrantRepository(
        QdrantClient qdrantClient,
        ILogger<QdrantRepository<T>> logger,
        IOptions<AIOptions> options,
        ITextEmbeddingGenerationService? embeddingService = null)
    {
        _qdrantClient = qdrantClient;
        _logger = logger;
        _options = options.Value;
        _embeddingService = embeddingService;
        _collectionName = GetCollectionName();
    }

    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _qdrantClient.CollectionExistsAsync(_collectionName, cancellationToken);
            if (!exists)
            {
                await _qdrantClient.CreateCollectionAsync(
                    collectionName: _collectionName,
                    vectorsConfig: new VectorParams
                    {
                        Size = (uint)_options.VectorSize,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Created collection {CollectionName} with vector size {VectorSize}", _collectionName, (uint)_options.VectorSize);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to create collection {CollectionName}", _collectionName);
            throw;
        }
    }

    public async Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        return await _qdrantClient.CollectionExistsAsync(_collectionName, cancellationToken);
    }

    public async Task DeleteCollectionAsync(CancellationToken cancellationToken = default)
    {
        await _qdrantClient.DeleteCollectionAsync(_collectionName, cancellationToken: cancellationToken);
        _logger.LogInformation("Deleted collection {CollectionName}", _collectionName);
    }


    public async Task IndexAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (_embeddingService == null)
            throw new InvalidOperationException("Embedding service is not configured.");

        await EnsureCollectionExistsAsync(cancellationToken);

        var text = GenerateSearchText(entity);
        var vector = await _embeddingService.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        var id = GetEntityId(entity);

        var point = new PointStruct
                    {
                        Id = new PointId { Uuid = id },
                        Vectors = vector.ToArray(),
                        Payload =
                        {
                            ["entity"] = JsonSerializer.Serialize(entity),
                            ["type"] = typeof(T).Name,
                            ["timestamp"] = DateTime.UtcNow.ToString("O")
                        }
                    };

        await _qdrantClient.UpsertAsync(_collectionName, new[] { point }, cancellationToken: cancellationToken);
        _logger.LogDebug("Indexed entity {EntityId}", id);
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(GetEntityId(entity), cancellationToken);
        await IndexAsync(entity, cancellationToken);

        _logger.LogDebug("Updated entity {EntityId} by text in collection {CollectionName}", GetEntityId(entity), _collectionName);
    }

    public async Task DeleteAsync(object id, CancellationToken cancellationToken = default)
    {
        await _qdrantClient.DeleteAsync(
            collectionName: _collectionName,
            id: new PointId { Uuid = id.ToString() },
            cancellationToken: cancellationToken);

        _logger.LogDebug("Deleted entity {EntityId} from collection {CollectionName}", id, _collectionName);
    }

    public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _qdrantClient.RetrieveAsync(
                collectionName: _collectionName,
                ids: new[] { new PointId { Uuid = id.ToString() } },
                withVectors: false,
                cancellationToken: cancellationToken);

            var point = results.FirstOrDefault();
            if (point == null) return null;

            return ExtractEntity(point); // ✅ Uses RetrievedPoint overload
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity by id: {Id}", id);
            return null;
        }
    }

    public async Task<float[]?> GetVectorAsync(object id, CancellationToken cancellationToken = default)
    {
        var results = await _qdrantClient.RetrieveAsync(
            collectionName: _collectionName,
            ids: new[] { new PointId { Uuid = id.ToString() } },
            withVectors: true,
            cancellationToken: cancellationToken);

        var point = results.FirstOrDefault();
        return point != null ? ExtractVectorFromPoint(point) : null;
    }

    public async Task<IEnumerable<ScoredPoint>> SearchAsync(
        float[] vector,
        int maxResults = 10,
        double similarityThreshold = 0.7,
        CancellationToken cancellationToken = default)
    {
        return await _qdrantClient.SearchAsync(
                   collectionName: _collectionName,
                   vector: vector,
                   limit: (ulong)maxResults,
                   scoreThreshold: (float)similarityThreshold,
                   cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ScoredPoint>> SearchByTextAsync(
        string queryText,
        int maxResults = 10,
        double similarityThreshold = 0.7,
        CancellationToken cancellationToken = default)
    {
        if (_embeddingService == null)
            throw new InvalidOperationException("Embedding service not configured.");

        var vector = await _embeddingService.GenerateEmbeddingAsync(queryText, cancellationToken: cancellationToken);
        return await SearchAsync(vector.ToArray(), maxResults, similarityThreshold, cancellationToken);
    }

    public async Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken = default)
    {
        var collections = await _qdrantClient.ListCollectionsAsync(cancellationToken);
        return collections.ToList();
    }

    public T? ExtractEntity(ScoredPoint point)
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
    }

    public T? ExtractEntity(RetrievedPoint point)
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
    }

    private string GetCollectionName() => typeof(T).Name.Underscore();

    private string GetEntityId(T entity)
    {
        var idProperty = typeof(T).GetProperties()
            .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                                 p.Name.Equals(typeof(T).Name + "Id", StringComparison.OrdinalIgnoreCase));

        return idProperty?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
    }
    
    private string GenerateSearchText(T entity)
    {
        try
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.PropertyType == typeof(string) || p.PropertyType == typeof(int) &&
                            !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) &&
                            p.CanRead)
                .Select(p => p.GetValue(entity)?.ToString())
                .Where(value => !string.IsNullOrWhiteSpace(value));

            var result = string.Join(" ", properties);
        
            return !string.IsNullOrWhiteSpace(result) ? result : JsonSerializer.Serialize(entity);
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate search text for type {Type}, falling back to JSON", typeof(T).Name);
            return JsonSerializer.Serialize(entity);
        }
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
}