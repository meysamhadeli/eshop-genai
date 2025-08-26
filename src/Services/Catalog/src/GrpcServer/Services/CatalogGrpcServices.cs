using Catalog.Products.Features;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;

namespace Catalog.GrpcServer.Services;

public class CatalogGrpcServices : CatalogGrpcService.CatalogGrpcServiceBase
{
    private readonly IMediator _mediator;

    public CatalogGrpcServices(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<GetProductByIdResult> GetProductById(GetProductByIdRequest request, ServerCallContext context)
    {
        var productDto = await _mediator.Send(new GetProductById(new Guid(request.Id)));

        return new GetProductByIdResult
        {
            ProductDto = new ProductResponse
            {
                Id = productDto.Id.ToString(),
                Name = productDto.Name,
                Description = productDto.Description,
                Price = (double)productDto.Price,
                ImageUrl = productDto.ImageUrl,
                CreatedAt = Timestamp.FromDateTime(productDto.CreatedAt.ToUniversalTime()),
                LastModified = productDto.LastModified != null ? Timestamp.FromDateTime((DateTime)productDto.LastModified?.ToUniversalTime()!) : null
            }
        };
    }
}