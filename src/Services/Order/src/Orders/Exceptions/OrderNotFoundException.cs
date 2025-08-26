using System.Net;
using BuildingBlocks.Exception;

namespace Order.Orders.Exceptions;

public class OrderNotFoundException : AppException
{
    public OrderNotFoundException() : base("Order not found!", HttpStatusCode.NotFound)
    {
    }
}