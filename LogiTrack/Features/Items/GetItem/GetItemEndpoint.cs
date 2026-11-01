using LogiTrack.Data;
using LogiTrack.Features.Items.Shared;
using LogiTrack.Models;

namespace LogiTrack.Features.Items.GetItem
{
    public static class GetItemEndpoint
    {
        public static void MapGetItem(this IEndpointRouteBuilder app)
        {
            app.MapGet("/{id:guid}", Handle)
                .WithName(ItemsEndpointsNames.GetItem)
                .Produces<ItemResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);
        }

        private static async Task<IResult> Handle(
            Guid id,
            LogiTrackContext dbContext,
            ILogger<Program> logger
            )
        {
            var item = await dbContext.Items.FindAsync(id);
            
            if (item is null)
            {
                logger.LogWarning("Item with ID: {ItemId} not found.", id);
                return Results.NotFound();
            }

            logger.LogInformation("Retrieved item ID: {ItemId}, Name: {ItemName}", item.ItemId, item.Name);

            var response = new ItemResponse(item.ItemId, item.Name, item.Quantity, item.Location);

            return Results.Ok(response);
        }
    }
}
