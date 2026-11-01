using LogiTrack.Data;
using LogiTrack.Features.Auth;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Features.Items.DeleteItem
{
    public static class DeleteItemEndpoint
    {
        public static void MapDeleteItem(this IEndpointRouteBuilder app)
        {
            app.MapDelete("/{id}", Handle)
                .RequireAuthorization(AuthPolicies.Administrator)
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }

        private static async Task<IResult> Handle(
            Guid id,
            LogiTrackContext dbContext,
            ILogger<Program> logger,
            CancellationToken cancellationToken
            )
        {
            var item = await dbContext.Items.FindAsync(id);

            if (item is null)
            {
                logger.LogWarning("Item with ID: {ItemId} not found for deletion.", id);
                return Results.NotFound();
            }

            var isReferencedByOrder = await dbContext.OrderItems
                .AnyAsync(orderItem => orderItem.ItemId == id, cancellationToken);

            if (isReferencedByOrder)
            {
                logger.LogWarning("Item with ID: {ItemId} cannot be deleted because it is referenced by an order.", id);
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Item is referenced by an order",
                    detail: "Delete the order before deleting this inventory item.");
            }
            
            dbContext.Items.Remove(item);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Deleted item ID: {ItemId}, Name: {ItemName}", item.ItemId, item.Name);
            
            return Results.NoContent();
        }
    }
}
