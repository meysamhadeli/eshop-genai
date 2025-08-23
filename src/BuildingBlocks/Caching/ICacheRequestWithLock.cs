namespace BuildingBlocks.Caching;

public interface ICacheRequestWithLock : ICacheRequest
{
    bool UseDistributedLock { get; }
    TimeSpan? LockTimeout { get; }
}