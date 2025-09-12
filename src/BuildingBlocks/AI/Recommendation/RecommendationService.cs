using System.Text.Json;
using BuildingBlocks.AI.Qdrant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;

namespace BuildingBlocks.AI.Recommendation;

public interface IRecommendationService
{
    Task TrackUserActivityAsync(UserActivity activity, CancellationToken cancellationToken = default);
    
    Task<SearchResult<T>> GetRecommendationsAsync<T>(
        string userId,
        int maxResults = 5,
        bool includeExplanation = false,
        CancellationToken cancellationToken = default
    ) where T : class;
}

public class RecommendationService : IRecommendationService
{
    private readonly IQdrantRepository<UserActivity> _activityRepository;
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<RecommendationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AIOptions _options;
    private readonly bool _isEnabled;

    public RecommendationService(
        IQdrantRepository<UserActivity> activityRepository,
        IChatCompletionService chatService,
        IOptions<AIOptions> options,
        ILogger<RecommendationService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _activityRepository = activityRepository;
        _chatService = chatService;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _isEnabled = _options.RecommendationEnabled;

        if (_isEnabled)
        {
            _logger.LogInformation("Recommendation service initialized with direct Qdrant access");
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
            activity.Id = $"{activity.UserId}_{activity.ItemId}_{activity.Timestamp.Ticks}";
            activity.Weight = GetActivityWeight(activity.ActivityType);

            await _activityRepository.IndexAsync(activity, cancellationToken);

            _logger.LogDebug("Tracked user activity: {UserId} {ActivityType} {ItemId}", activity.UserId, activity.ActivityType, activity.ItemId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to track user activity for user {UserId}", activity.UserId);
        }
    }

    public async Task<SearchResult<T>> GetRecommendationsAsync<T>(
        string userId,
        int maxResults = 5,
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

        var activityPriority = new[]
        {
            ActivityTypes.Purchase,
            ActivityTypes.AddToCart,
            ActivityTypes.Bookmark,
            ActivityTypes.Like,
            ActivityTypes.Rating,
            ActivityTypes.Comment,
            ActivityTypes.Click,
            ActivityTypes.View
        };

        try
        {
            _logger.LogDebug("Getting recommendations for user {UserId}", userId);

            var recentActivity = await GetMostRelevantActivity(userId, activityPriority, cancellationToken);
            if (recentActivity == null)
            {
                _logger.LogDebug("No relevant activities found for user {UserId}", userId);
                return new SearchResult<T>
                {
                    Explanation = includeExplanation
                        ? "We couldn't find enough activity history to provide personalized recommendations. Try exploring more items!"
                        : null
                };
            }

            var itemRepository = GetItemRepository<T>();

            var itemVector = await itemRepository.GetVectorAsync(recentActivity.ItemId, cancellationToken);
            if (itemVector == null)
            {
                _logger.LogWarning("Item vector not found for {ItemId}", recentActivity.ItemId);
                return new SearchResult<T>
                {
                    Explanation = includeExplanation
                        ? "We're having trouble finding recommendations based on your recent activity."
                        : null
                };
            }

            var boostedVector = ApplyWeightToVector(itemVector, GetActivityWeight(recentActivity.ActivityType));

            var scoredPoints = await itemRepository.SearchAsync(
                boostedVector,
                maxResults,
                0.6,
                cancellationToken);

            var averageSimilarity = scoredPoints.Any() ? scoredPoints.Average(p => p.Score) : 0.0;

            var recommendations = scoredPoints
                .Select(itemRepository.ExtractEntity)
                .Where(e => e != null)
                .Take(maxResults)
                .ToList();

            var explanation = includeExplanation || _options.RecommendationExplanationEnabled
                ? await GenerateRecommendationExplanationAsync(userId, recommendations, recentActivity, cancellationToken)
                : null;

            _logger.LogInformation("Found {Count} recommendations for user {UserId}", recommendations.Count, userId);

            return new SearchResult<T>
            {
                Results = recommendations,
                Explanation = explanation,
                OriginalQuery = $"Recommendations for user {userId} based on {recentActivity.ActivityType} activity",
                HasExactMatches = recommendations.Any(),
                TotalCounts = recommendations.Count,
                AverageSimilarity = averageSimilarity, // ✅ Real score from Qdrant!
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

    private async Task<UserActivity?> GetMostRelevantActivity(
        string userId,
        string[] activityTypes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var scoredPoints = await _activityRepository.SearchByTextAsync(
                 userId,
                20,
                0.0, // Low threshold to cast wide net
                cancellationToken);

            var activities = scoredPoints
                .Select(_activityRepository.ExtractEntity)
                .Where(a => a != null && a.UserId == userId && activityTypes.Contains(a.ActivityType))
                .ToList();

            var relevantActivity = activities
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

    private async Task<string?> GenerateRecommendationExplanationAsync<T>(
        string userId,
        IEnumerable<T> recommendations,
        UserActivity sourceActivity,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage("""
                You are a helpful recommendation assistant that explains why certain items are being recommended.
                Your task is to provide a friendly, natural explanation for the recommendations.

                Guidelines:
                1. Explain the connection to the user's recent activity
                2. Be concise but helpful (1-2 sentences)
                3. Use natural, conversational language
                4. Mention the type of activity that triggered the recommendations
                5. Sound excited and encouraging
                """);

            var recommendationsJson = JsonSerializer.Serialize(
                recommendations.Take(3),
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            chatHistory.AddUserMessage($"""
                Please provide a helpful explanation for these recommendations.

                User ID: {userId}
                Source Activity: {sourceActivity.ActivityType} on item {sourceActivity.ItemId}

                Recommended Items (first 3):
                {recommendationsJson}

                Please provide a concise, friendly explanation that:
                1. Mentions the user's recent activity
                2. Explains why these items are being recommended
                3. Is encouraging and positive
                4. Keeps it brief (1-2 sentences)
                """);

            var response = await _chatService.GetChatMessageContentAsync(
                chatHistory, cancellationToken: cancellationToken);

            return response.Content;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to generate recommendation explanation");
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

    private IQdrantRepository<T> GetItemRepository<T>() where T : class
    {
        using var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IQdrantRepository<T>>();
    }
}