using System.Net;
using BuildingBlocks.Exception;

namespace Basket.Baskets.Exceptions;

public class ProductNotFoundException: AppException
{
    public ProductNotFoundException() : base("Product not found!", HttpStatusCode.NotFound)
    {
    }
}