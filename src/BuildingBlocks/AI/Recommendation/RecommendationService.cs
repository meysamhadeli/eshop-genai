using BuildingBlocks.AI.SemanticSearch.Models;
using BuildingBlocks.SemanticSearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.AI.SemanticSearch;

public interface IRecommendationService
{
    Task TrackUserActivityAsync(UserActivity activity, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetRecommendationsAsync<T>(
        string userId,
        int maxResults = 5,
        CancellationToken cancellationToken = default
    ) where T : class;
}

public class RecommendationService : IRecommendationService
{
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly ILogger<RecommendationService> _logger;
    private readonly AIOptions _options;
    private readonly bool _isEnabled;

    public RecommendationService(
        ISemanticSearchService semanticSearchService,
        IOptions<AIOptions> options,
        ILogger<RecommendationService> logger)
    {
        _semanticSearchService = semanticSearchService;
        _logger = logger;
        _options = options.Value;
        _isEnabled = _options.RecommendationEnabled;

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
            var activityEntity = new
            {
                Id = $"{activity.UserId}_{activity.ItemId}_{activity.Timestamp.Ticks}",
                activity.UserId,
                activity.ItemId,
                activity.ActivityType,
                activity.Timestamp,
                Weight = GetActivityWeight(activity.ActivityType)
            };

            await _semanticSearchService.IndexAsync(activityEntity, cancellationToken);

            _logger.LogDebug("Tracked user activity: {UserId} {ActivityType} {ItemId}",
                activity.UserId, activity.ActivityType, activity.ItemId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to track user activity");
        }
    }

    public async Task<IEnumerable<T>> GetRecommendationsAsync<T>(
        string userId,
        int maxResults = 5,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!_isEnabled)
        {
            _logger.LogDebug("Recommendation service is disabled, returning empty results");
            return Enumerable.Empty<T>();
        }

        var activityPriority = new[]
        {
            ActivityTypes.Purchase,    // 3.0 weight - most valuable
            ActivityTypes.AddToCart,   // 2.5 weight
            ActivityTypes.Bookmark,    // 2.0 weight  
            ActivityTypes.Like,        // 1.8 weight
            ActivityTypes.Rating,      // 1.7 weight
            ActivityTypes.Comment,     // 1.6 weight
            ActivityTypes.Click,       // 1.3 weight
            ActivityTypes.View         // 1.0 weight - least valuable
        };

        try
        {
            _logger.LogDebug("Getting recommendations for user {UserId}", userId);

            var recentActivity = await GetMostRelevantActivity(userId, activityPriority, cancellationToken);
            if (recentActivity == null)
            {
                _logger.LogDebug("No relevant activities found for user {UserId}", userId);
                return Enumerable.Empty<T>();
            }

            var itemVector = await _semanticSearchService.GetVectorAsync<T>(recentActivity.ItemId, cancellationToken);
            if (itemVector == null)
            {
                _logger.LogWarning("Item vector not found for {ItemId}", recentActivity.ItemId);
                return Enumerable.Empty<T>();
            }

            var boostedVector = ApplyWeightToVector(itemVector, GetActivityWeight(recentActivity.ActivityType));

            var recommendations = await _semanticSearchService.SearchVectorsAsync<T>(
                boostedVector,
                maxResults,
                0.6f,
                cancellationToken
            );

            _logger.LogInformation("Found {Count} recommendations for user {UserId}", recommendations.Count(), userId);
            return recommendations.ToList();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommendations for user {UserId}", userId);
            return Enumerable.Empty<T>();
        }
    }

    private async Task<UserActivity?> GetMostRelevantActivity(
        string userId,
        string[] activityTypes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Search for activities and filter in memory
            var allActivities = await _semanticSearchService.SemanticSearchAsync<object, UserActivity>(
                userId,
                maxResults: 20, // Get more to filter down
                cancellationToken: cancellationToken
            );

            // Filter in memory
            var filteredActivities = allActivities
                .Where(a => a.UserId == userId && activityTypes.Contains(a.ActivityType))
                .ToList();

            var relevantActivity = filteredActivities
                .OrderByDescending(a => GetActivityWeight(a.ActivityType))
                .ThenByDescending(a => a.Timestamp)
                .FirstOrDefault();

            if (relevantActivity != null)
            {
                _logger.LogDebug("Found relevant activity: {ActivityType} for {ItemId}",
                    relevantActivity.ActivityType, relevantActivity.ItemId);
            }

            return relevantActivity;
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user activities for {UserId}", userId);
            return null;
        }
    }

    private float[] ApplyWeightToVector(float[] vector, double weight)
    {
        return vector.Select(v => v * (float)weight).ToArray();
    }

    private double GetActivityWeight(string activityType)
    {
        return activityType switch
        {
            ActivityTypes.Purchase => 3.0,
            ActivityTypes.AddToCart => 2.5,
            ActivityTypes.Bookmark => 2.0,
            ActivityTypes.Like => 1.8,
            ActivityTypes.Rating => 1.7,
            ActivityTypes.Comment => 1.6,
            ActivityTypes.Click => 1.3,
            ActivityTypes.View => 1.0,
            _ => 1.0
        };
    }
}