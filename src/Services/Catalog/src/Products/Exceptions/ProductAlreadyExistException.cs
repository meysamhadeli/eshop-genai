using System.Net;
using BuildingBlocks.Exception;

namespace Catalog.Products.Exceptions;

public class ProductAlreadyExistException : AppException
{
    public ProductAlreadyExistException() : base("Product already exist!", HttpStatusCode.Conflict)
    {
    }
}