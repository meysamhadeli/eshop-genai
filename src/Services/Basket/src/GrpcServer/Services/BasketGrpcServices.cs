using Basket.Baskets.Features;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;

namespace Basket.GrpcServer.Services;

public class BasketGrpcServices : BasketGrpcService.BasketGrpcServiceBase
{
    private readonly IMediator _mediator;

    public BasketGrpcServices(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<GetBasketResponse> GetBasket(GetBasketRequest request, ServerCallContext context)
    {
        var basketDto = await _mediator.Send(new GetBasket(request.UserId));

        return new GetBasketResponse
        {
            UserId = basketDto.UserId,
            CreatedAt = Timestamp.FromDateTime(basketDto.CreatedAt.ToUniversalTime()),
            ExpirationTime = basketDto.ExpirationTime.HasValue ? Timestamp.FromDateTime(basketDto.ExpirationTime.Value.ToUniversalTime()) : null,
            LastModified = basketDto.LastModified.HasValue ? Timestamp.FromDateTime(basketDto.LastModified.Value.ToUniversalTime()) : null,
            Items = { basketDto.Items.Select(x => new BasketItem { ProductId = x.ProductId.ToString(), Quantity = x.Quantity, }) }
        };
    }

    public override async Task<UpdateBasketResponse> UpdateBasket(UpdateBasketRequest request, ServerCallContext context)
    {
        var basketDto = await _mediator.Send(new UpdateItem(request.UserId, new Guid(request.Item.ProductId), request.Item.Quantity));

        return new UpdateBasketResponse
        {
            Basket = new Basket
            {
                Id = basketDto.Id.ToString(),
                UserId = basketDto.UserId,
                CreatedAt = Timestamp.FromDateTime(basketDto.CreatedAt.ToUniversalTime()),
                ExpirationTime = basketDto.ExpirationTime.HasValue ? Timestamp.FromDateTime(basketDto.ExpirationTime.Value.ToUniversalTime()) : null,
                LastModified = basketDto.LastModified.HasValue ? Timestamp.FromDateTime(basketDto.LastModified.Value.ToUniversalTime()) : null,
                Items = { basketDto.Items.Select(x => new BasketItem { ProductId = x.ProductId.ToString(), Quantity = x.Quantity }) }
            }
        };
    }

    public override async Task<ClearBasketResponse> ClearBasket(ClearBasketRequest request, ServerCallContext context)
    {
        var isCleared = await _mediator.Send(new ClearBasket(request.UserId));

        return new ClearBasketResponse() { Success = isCleared };
    }
}