namespace BuildingBlocks.Caching;

public class HybridCacheOptions
{
    public bool UseDistributedLock { get; set; } = true;
    public string RedisConnectionString { get; set; } = string.Empty;
    public long? RedisExpireMinutes { get; set; }
    public long? InMemoryExpireMinutes { get; set; }
    public string InstanceName { get; set; } = string.Empty;
}