using System.Security.Claims;
using BuildingBlocks.Core.Event;
using BuildingBlocks.Web;
using MassTransit;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Core;

public sealed class EventDispatcher(
    IPublishEndpoint publishEndpoint,
    IHttpContextAccessor httpContextAccessor
) : IEventDispatcher
{
    public async Task SendAsync<T>(IReadOnlyList<T> events, Type type = null, CancellationToken cancellationToken = default)
    where T : IEvent
    {
        if (events.Count == 0) return;

        foreach (var @event in events)
        {
            await publishEndpoint.Publish(@event, context => SetHeaders(context), cancellationToken);
        }
    }

    public async Task SendAsync<T>(T @event, Type type = null, CancellationToken cancellationToken = default) 
    where T : IEvent =>
        await SendAsync(new[] { @event }, type, cancellationToken);

    
    private void SetHeaders(SendContext context)
    {
        context.Headers.Set("CorrelationId", httpContextAccessor?.HttpContext?.GetCorrelationId());
        context.Headers.Set("UserId", httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier));
        context.Headers.Set("UserName", httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.Name));
    }
}