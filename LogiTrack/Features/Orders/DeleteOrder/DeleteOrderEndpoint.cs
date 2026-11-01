using LogiTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Features.Orders.DeleteOrder
{
    public static class DeleteOrderEndpoint
    {
        public static void MapDeleteOrder(this IEndpointRouteBuilder app)
        {
            app.MapDelete("/{id:guid}", Handle)
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound);
        }

        private static async Task<IResult> Handle(
            Guid id,
            LogiTrackContext dbContext,
            ILogger<Program> logger,
            CancellationToken cancellationToken)
        {
            var order = await dbContext.Orders
                .FirstOrDefaultAsync(order => order.OrderId == id, cancellationToken);

            if (order is null)
            {
                logger.LogWarning("Order with ID: {OrderId} not found for deletion.", id);
                return Results.NotFound();
            }

            dbContext.Orders.Remove(order);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Deleted order ID: {OrderId} for customer {CustomerName}", order.OrderId, order.CustomerName);

            return Results.NoContent();
        }
    }
}
