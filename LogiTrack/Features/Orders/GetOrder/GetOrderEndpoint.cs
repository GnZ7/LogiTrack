using LogiTrack.Data;
using LogiTrack.Features.Orders.Shared;
using Microsoft.EntityFrameworkCore;
using static LogiTrack.Features.Orders.Shared.OrderDtos;

namespace LogiTrack.Features.Orders.GetOrder
{
    public static class GetOrderEndpoint
    {
        public static void MapGetOrder(this IEndpointRouteBuilder app)
        {
            app.MapGet("/{id:guid}", Handle)
                .WithName(OrdersEndpointsNames.GetOrder)
                .Produces<OrderResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);
        }

        private static async Task<IResult> Handle(
            Guid id,
            LogiTrackContext dbContext,
            ILogger<Program> logger,
            CancellationToken cancellationToken)
        {
            var order = await dbContext.Orders
                .AsNoTracking()
                .Include(order => order.Items)
                .FirstOrDefaultAsync(order => order.OrderId == id, cancellationToken);

            if (order is null)
            {
                logger.LogWarning("Order with ID: {OrderId} not found.", id);
                return Results.NotFound();
            }

            logger.LogInformation("Retrieved order ID: {OrderId} for customer {CustomerName}", order.OrderId, order.CustomerName);

            var response = new OrderResponse(
                order.OrderId,
                order.CustomerName,
                order.DatePlaced,
                order.Items.Select(item => new ItemInOrder(item.ItemId, item.Name, item.Quantity, item.Location)));

            return Results.Ok(response);
        }
    }
}
