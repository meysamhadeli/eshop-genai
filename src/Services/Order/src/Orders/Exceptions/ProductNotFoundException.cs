using System.Net;
using BuildingBlocks.Exception;

namespace Order.Orders.Exceptions;

public class ProductNotFoundException: AppException
{
    public ProductNotFoundException() : base("Product not found!", HttpStatusCode.NotFound)
    {
    }
}