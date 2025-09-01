using BuildingBlocks.Core.Event;

namespace BuildingBlocks.Contracts.EventBus.Messages;

public record ClearBasketItemIntegrationEvent(string UserId, bool IsCleared) : IIntegrationEvent;

public record UpdatedBasketItemsIntegrationEvent(Guid Id, string UserId, ICollection<BasketItemsIntegrationEvent> Items, DateTime? ExpirationTime, bool IsDeleted) : IIntegrationEvent;

public record BasketItemsIntegrationEvent
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
