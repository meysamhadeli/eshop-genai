using System.Net;
using BuildingBlocks.Exception;

namespace Catalog.Products.Exceptions;

public class ProductNotFoundException: AppException
{
    public ProductNotFoundException() : base("Product not found!", HttpStatusCode.NotFound)
    {
    }
}