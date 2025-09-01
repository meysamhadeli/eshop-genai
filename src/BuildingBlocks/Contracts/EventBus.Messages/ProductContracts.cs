using BuildingBlocks.Core.Event;

namespace BuildingBlocks.Contracts.EventBus.Messages;

public record ProductAddedIntegrationEvent(Guid Id, string Name, string Description, decimal Price, string ImageUrl, bool IsDeleted) : IIntegrationEvent;
