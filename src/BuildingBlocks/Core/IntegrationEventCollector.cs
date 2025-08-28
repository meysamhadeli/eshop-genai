using System.Collections.Immutable;
using BuildingBlocks.Core.Event;

namespace BuildingBlocks.Core;

public interface IIntegrationEventCollector
{
    void AddIntegrationEvent(IIntegrationEvent @event);
    IReadOnlyList<IIntegrationEvent> GetIntegrationEvents();
    void ClearIntegrationEvents();
}

public class IntegrationEventCollector : IIntegrationEventCollector
{
    private readonly List<IIntegrationEvent> _events = new();

    public void AddIntegrationEvent(IIntegrationEvent @event) => _events.Add(@event);

    public IReadOnlyList<IIntegrationEvent> GetIntegrationEvents()
    {
        var integrationEvents = _events.ToImmutableList();

        ClearIntegrationEvents();

        return integrationEvents;
    }

    public void ClearIntegrationEvents() => _events.Clear();
}