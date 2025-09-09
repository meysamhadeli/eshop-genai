using System.Text.Json.Serialization;

namespace BuildingBlocks.AI.SemanticSearch.Models;

public class UserActivity
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("activityType")]
    public string ActivityType { get; set; } = "view";

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    [JsonPropertyName("context")]
    public ActivityContext? Context { get; set; }

    [JsonPropertyName("weight")]
    public double Weight { get; set; } = 1.0;
}

public class ActivityContext
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("deviceType")]
    public string? DeviceType { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("referrer")]
    public string? Referrer { get; set; }

    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    [JsonPropertyName("searchQuery")]
    public string? SearchQuery { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}

public static class ActivityTypes
{
    public const string View = "view";
    public const string Click = "click";
    public const string Purchase = "purchase";
    public const string AddToCart = "add_to_cart";
    public const string Like = "like";
    public const string Bookmark = "bookmark";
    public const string Rating = "rating";
    public const string Comment = "comment";
}