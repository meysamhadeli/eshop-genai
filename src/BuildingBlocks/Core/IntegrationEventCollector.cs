using BuildingBlocks.Core.Event;

namespace BuildingBlocks.Core;

public interface IIntegrationEventCollector
{
    void AddEvent(IIntegrationEvent @event);
    IReadOnlyList<IIntegrationEvent> GetEvents();
    void ClearEvents();
}

public class IntegrationEventCollector : IIntegrationEventCollector
{
    private readonly List<IIntegrationEvent> _events = new();
    
    public void AddEvent(IIntegrationEvent @event) => _events.Add(@event);
    
    public IReadOnlyList<IIntegrationEvent> GetEvents() => _events.ToList();
    
    public void ClearEvents() => _events.Clear();
}