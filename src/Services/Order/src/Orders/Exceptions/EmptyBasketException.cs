using System.Net;
using BuildingBlocks.Exception;

namespace Order.Orders.Exceptions;
public class EmptyBasketException : AppException
{
    public EmptyBasketException() : base("Basket already is empty!", HttpStatusCode.Conflict)
    {
    }
}