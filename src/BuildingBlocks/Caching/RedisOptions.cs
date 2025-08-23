namespace BuildingBlocks.Caching;

public class RedisOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public int? ExpireSeconds { get; set; }
    public string InstanceName { get; set; } = string.Empty;
}