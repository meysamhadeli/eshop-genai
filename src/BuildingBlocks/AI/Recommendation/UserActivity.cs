using System.Text.Json.Serialization;

namespace BuildingBlocks.AI.Recommendation;

public class UserActivity
{
    public string UserId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string ActivityType { get; set; } = ActivityTypes.View;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public ActivityContext? Context { get; set; }
    public double Weight { get; set; } = 1.0;
    
    [JsonIgnore]
    public string SearchableText { get; set; } = string.Empty;
}

public class ActivityContext
{
    public string? SessionId { get; set; }
    public string? DeviceType { get; set; }
    public string? Location { get; set; }
    public string? Referrer { get; set; }
    public double? Duration { get; set; }
    public string? SearchQuery { get; set; }
    public string? Category { get; set; }
    public List<string>? Tags { get; set; } = new();
}

public static class ActivityTypes
{
    public const string View = "view";
    public const string Click = "click";
    public const string Purchase = "purchase";
    public const string Like = "like";
    public const string Rating = "rating";
    public const string Review = "review";
    public const string Search = "search";
}