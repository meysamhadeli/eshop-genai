namespace BuildingBlocks.MassTransit;

public class PostgresOutboxOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string? Schema { get; set; }
    
    // Outbox configuration properties
    public TimeSpan? QueryDelay { get; set; }
    public TimeSpan? QueryTimeout { get; set; }
    public TimeSpan? DuplicateDetectionWindow { get; set; }
    public int? MessageDeliveryLimit { get; set; }
    public int? QueryMessageLimit { get; set; }
}