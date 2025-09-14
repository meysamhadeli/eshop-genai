using System.Globalization;
using System.Text.Json;
using BuildingBlocks.AI.Qdrant;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace BuildingBlocks.AI.Recommendation;

public interface IRecommendationService
{
    Task TrackUserActivityAsync(UserActivity activity, CancellationToken cancellationToken = default);
    Task<SearchResult<T>> GetRecommendationsAsync<T>(
        string userId,
        int maxResults = 5,
        double similarityThreshold = 0.6,
        bool includeExplanation = false,
        CancellationToken cancellationToken = default) where T : class;
}

public class RecommendationService : IRecommendationService
{
    private readonly IQdrantRepository<UserActivity> _activityRepository;
    private readonly IChatCompletionService _chatService;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly ILogger<RecommendationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AIOptions _options;
    private readonly bool _isEnabled;

    public RecommendationService(
        IQdrantRepository<UserActivity> activityRepository,
        IChatCompletionService chatService,
        ITextEmbeddingGenerationService embeddingService,
        IOptions<AIOptions> options,
        ILogger<RecommendationService> logger,
        IServiceProvider serviceProvider)
    {
        _activityRepository = activityRepository;
        _chatService = chatService;
        _embeddingService = embeddingService;
        _logger = logger;
        _options = options.Value;
        _isEnabled = _options.RecommendationEnabled;
        _serviceProvider = serviceProvider;

        if (_isEnabled)
        {
            _logger.LogInformation("Recommendation service initialized");
        }
        else
        {
            _logger.LogInformation("Recommendation service is disabled");
        }
    }

    public async Task TrackUserActivityAsync(UserActivity activity, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled) return;

        try
        {
            activity.SearchableText = GenerateActivitySearchText(activity);
            activity.Weight = GetActivityWeight(activity.ActivityType);
            activity.Timestamp = DateTime.UtcNow;

            await _activityRepository.EnsureCollectionExistsAsync(cancellationToken);
            await _activityRepository.IndexAsync(activity, cancellationToken);

            _logger.LogDebug("Tracked user activity: {UserId} {ActivityType} {ItemId}", 
                activity.UserId, activity.ActivityType, activity.ItemId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to track user activity for user {UserId}", activity.UserId);
        }
    }

    public async Task<SearchResult<T>> GetRecommendationsAsync<T>(
        string userId,
        int maxResults = 5,
        double similarityThreshold = 0.6,
        bool includeExplanation = false,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!_isEnabled)
        {
            return new SearchResult<T>
            {
                Explanation = includeExplanation ? "Recommendation service is currently disabled." : null
            };
        }

        try
        {
            _logger.LogDebug("Getting {TypeName} recommendations for user {UserId}", typeof(T).Name, userId);

            var userActivities = await GetUserActivities(userId, cancellationToken);
            if (!userActivities.Any())
            {
                _logger.LogDebug("No activities found for user {UserId}", userId);
                return await GetFallbackRecommendations<T>(maxResults, similarityThreshold, includeExplanation, cancellationToken);
            }

            var targetRepository = GetRepository<T>();
            if (targetRepository == null)
            {
                _logger.LogWarning("Repository not found for type: {TypeName}", typeof(T).Name);
                return new SearchResult<T>();
            }

            var recommendations = await GetRecommendationsFromActivities<T>(
                userActivities, targetRepository, maxResults, similarityThreshold, cancellationToken);
            
            if (!recommendations.Any())
            {
                _logger.LogDebug("No recommendations found for user {UserId}", userId);
                return await GetFallbackRecommendations<T>(maxResults, similarityThreshold, includeExplanation, cancellationToken);
            }

            var explanation = includeExplanation || _options.RecommendationExplanationEnabled
                ? await GenerateRecommendationExplanationAsync(userId, recommendations, userActivities.First(), cancellationToken)
                : null;

            _logger.LogInformation("Found {Count} recommendations for user {UserId}", recommendations.Count, userId);

            return new SearchResult<T>
            {
                Results = recommendations,
                Explanation = explanation,
                OriginalQuery = $"Recommendations for user {userId} based on {userActivities.Count} activities",
                HasExactMatches = recommendations.Any(),
                TotalCounts = recommendations.Count,
                AverageSimilarity = CalculateAverageSimilarity(recommendations),
                Timestamp = DateTime.UtcNow
            };
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommendations for user {UserId}", userId);
            return new SearchResult<T>
            {
                Explanation = includeExplanation
                    ? "Sorry, we encountered an issue while generating your recommendations. Please try again later."
                    : null
            };
        }
    }

    private async Task<List<UserActivity>> GetUserActivities(string userId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Searching for activities for user {UserId}", userId);
            
            var scoredPoints = await _activityRepository.SearchByTextAsync(
                userId, 
                50,
                0.3,
                cancellationToken);

            var activities = scoredPoints
                .Select(_activityRepository.ExtractEntity)
                .Where(a => a != null && a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .ToList();

            _logger.LogDebug("Found {Count} activities for user {UserId}", activities.Count, userId);
            
            foreach (var activity in activities)
            {
                _logger.LogDebug("Activity: User={UserId}, Item={ItemId}, Type={ActivityType}", 
                    activity.UserId, activity.ItemId, activity.ActivityType);
            }
            
            return activities;
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user activities for {UserId}", userId);
            return new List<UserActivity>();
        }
    }

    private async Task<List<T>> GetRecommendationsFromActivities<T>(
        List<UserActivity> userActivities,
        IQdrantRepository<T> targetRepository,
        int maxResults,
        double similarityThreshold,
        CancellationToken cancellationToken) where T : class
    {
        var allRecommendations = new List<T>();
        var seenItemIds = new HashSet<string>();

        foreach (var activity in userActivities.OrderByDescending(a => GetActivityWeight(a.ActivityType)))
        {
            try
            {
                _logger.LogDebug("Processing activity for item {ItemId} by user {UserId}", activity.ItemId, activity.UserId);

                var sourceItem = await targetRepository.GetByIdAsync(activity.ItemId, cancellationToken);
                if (sourceItem == null)
                {
                    _logger.LogWarning("Item {ItemId} not found in target repository", activity.ItemId);
                    continue;
                }

                var itemVector = await GenerateItemVectorAsync(sourceItem, cancellationToken);
                if (itemVector == null) continue;

                var boostedVector = ApplyWeightToVector(itemVector, activity.Weight);

                // Search for similar ITEMS in the target repository
                var scoredPoints = await targetRepository.SearchAsync(
                    boostedVector,
                    maxResults * 2,
                    similarityThreshold,
                    cancellationToken);

                var similarItems = scoredPoints
                    .Select(targetRepository.ExtractEntity)
                    .Where(item => item != null && !IsSameItem(item, activity.ItemId))
                    .ToList();

                _logger.LogDebug("Found {Count} similar items for activity {ItemId}", similarItems.Count, activity.ItemId);

                // Add new recommendations
                foreach (var item in similarItems)
                {
                    var itemId = GetItemId(item);
                    if (!seenItemIds.Contains(itemId))
                    {
                        allRecommendations.Add(item);
                        seenItemIds.Add(itemId);
                    }
                }

                if (allRecommendations.Count >= maxResults * 2)
                    break;
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get recommendations from activity for item {ItemId}", activity.ItemId);
            }
        }

        return allRecommendations.Take(maxResults).ToList();
    }

    private async Task<float[]?> GenerateItemVectorAsync<T>(T item, CancellationToken cancellationToken) where T : class
    {
        try
        {
            // Generate embedding from the item's content, not the activity
            var itemText = GenerateItemSearchText(item);
            var embedding = await _embeddingService.GenerateEmbeddingAsync(itemText, cancellationToken: cancellationToken);
            return embedding.ToArray();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to generate vector for item {ItemId}", GetItemId(item));
            return null;
        }
    }

    private string GenerateItemSearchText<T>(T item) where T : class
    {
        try
        {
            // Generate meaningful text from the item properties
            var properties = typeof(T).GetProperties()
                .Where(p => (p.PropertyType == typeof(string) || 
                             p.PropertyType == typeof(int) ||
                             p.PropertyType == typeof(decimal)) &&
                            p.CanRead &&
                            !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                .Select(p => 
                        {
                            var value = p.GetValue(item);
                            return value != null ? $"{p.Name.ToLower(CultureInfo.CurrentCulture)}:{value}" : null;
                        })
                .Where(v => !string.IsNullOrEmpty(v));

            var result = string.Join(" ", properties);
        
            // Fallback to JSON if no meaningful text generated
            return !string.IsNullOrWhiteSpace(result) ? result : JsonSerializer.Serialize(item);
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate search text for item, falling back to JSON");
            return JsonSerializer.Serialize(item);
        }
    }

    private async Task<SearchResult<T>> GetFallbackRecommendations<T>(
        int maxResults,
        double similarityThreshold,
        bool includeExplanation,
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            var targetRepository = GetRepository<T>();
            if (targetRepository == null)
            {
                _logger.LogWarning("Repository not found for type: {TypeName}", typeof(T).Name);
                return new SearchResult<T>();
            }

            // Get popular items
            var scoredPoints = await targetRepository.SearchByTextAsync(
                "popular", 
                maxResults, 
                similarityThreshold, 
                cancellationToken);

            var items = scoredPoints
                .Select(targetRepository.ExtractEntity)
                .Where(item => item != null)
                .ToList();

            return new SearchResult<T>
            {
                Results = items,
                Explanation = includeExplanation
                    ? $"Here are some popular {typeof(T).Name.Pluralize()} you might like!"
                    : null,
                HasExactMatches = items.Any(),
                TotalCounts = items.Count,
                AverageSimilarity = CalculateAverageSimilarity(items)
            };
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get fallback recommendations for type {TypeName}", typeof(T).Name);
            return new SearchResult<T>();
        }
    }

    private string GenerateActivitySearchText(UserActivity activity)
    {
        var textParts = new List<string> 
            {
                $"user:{activity.UserId}", 
                $"item:{activity.ItemId}", 
                $"action:{activity.ActivityType}", 
                $"weight:{activity.Weight:F1}", 
                $"time:{activity.Timestamp:yyyy-MM-dd HH:mm}"
            };

        if (!string.IsNullOrEmpty(activity.Context?.SearchQuery))
            textParts.Add($"query:{activity.Context.SearchQuery}");

        if (!string.IsNullOrEmpty(activity.Context?.Category))
            textParts.Add($"category:{activity.Context.Category}");

        if (activity.Context?.Tags?.Any() == true)
            textParts.Add($"tags:{string.Join(",", activity.Context.Tags)}");

        if (!string.IsNullOrEmpty(activity.Context?.DeviceType))
            textParts.Add($"device:{activity.Context.DeviceType}");

        if (activity.Context?.Duration.HasValue == true)
            textParts.Add($"duration:{activity.Context.Duration.Value}s");

        return string.Join(" ", textParts);
    }

    private async Task<string?> GenerateRecommendationExplanationAsync<T>(
        string userId,
        IEnumerable<T> recommendations,
        UserActivity sourceActivity,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var chatHistory = new ChatHistory();
            var recommendationList = recommendations.Take(3).ToList();

            if (!recommendationList.Any())
                return "Based on your activity, we found some items you might like!";

            chatHistory.AddSystemMessage("""
                You are a helpful recommendation assistant. Provide a friendly, 1-2 sentence explanation.
                Be positive and encouraging. Mention the connection to user activity if relevant.
                """);

            chatHistory.AddUserMessage($"""
                User {userId} recently {sourceActivity.ActivityType} an item.
                We found {recommendationList.Count} recommendations based on their activity.
                Provide a brief, friendly explanation.
                """);

            var response = await _chatService.GetChatMessageContentAsync(
                chatHistory, cancellationToken: cancellationToken);

            return response.Content;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to generate recommendation explanation");
            return "We found some great items based on your activity!";
        }
    }

    private IQdrantRepository<T>? GetRepository<T>() where T : class
    {
        try
        {
            return _serviceProvider.GetService<IQdrantRepository<T>>();
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get repository for type: {TypeName}", typeof(T).Name);
            return null;
        }
    }

    private string GetItemId<T>(T item) where T : class
    {
        var idProperty = typeof(T).GetProperties()
            .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                                 p.Name.Equals(typeof(T).Name + "Id", StringComparison.OrdinalIgnoreCase));

        return idProperty?.GetValue(item)?.ToString() ?? "unknown";
    }

    private bool IsSameItem<T>(T item, string sourceItemId) where T : class
    {
        var itemId = GetItemId(item);
        return string.Equals(itemId, sourceItemId, StringComparison.OrdinalIgnoreCase);
    }

    private double CalculateAverageSimilarity<T>(List<T> items) where T : class
    {
        return items.Any() ? 0.8 : 0.0;
    }

    private float[] ApplyWeightToVector(float[] vector, double weight)
    {
        return vector.Select(v => v * (float)weight).ToArray();
    }

    private double GetActivityWeight(string activityType)
    {
        return activityType.ToLower(CultureInfo.CurrentCulture) switch
        {
            ActivityTypes.Purchase => 3.0,
            ActivityTypes.Rating => 2.5,
            ActivityTypes.Review => 2.5,
            ActivityTypes.Like => 2.0,
            ActivityTypes.Click => 1.5,
            ActivityTypes.View => 1.0,
            ActivityTypes.Search => 1.2,
            _ => 1.0
        };
    }
}