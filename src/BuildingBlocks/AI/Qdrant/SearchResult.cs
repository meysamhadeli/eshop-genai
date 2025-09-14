namespace BuildingBlocks.AI.Qdrant;

public class SearchResult<T>
{
    public IEnumerable<T> Results { get; set; } = Enumerable.Empty<T>();
    public string? Explanation { get; set; }
    public string OriginalQuery { get; set; } = string.Empty;
    public bool HasExactMatches { get; set; }
    public int TotalCounts { get; set; }
    public double AverageSimilarity { get; set; }
    public DateTime Timestamp { get; set; }
}