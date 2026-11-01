using LogiTrack.Data;
using LogiTrack.Features.Auth;
using LogiTrack.Features.Items.Shared;
using LogiTrack.Models;

namespace LogiTrack.Features.Items.CreateItem
{    
    public static class CreateItemEndpoint
    {
        // POST /api/items
        public static void MapCreateItem(this IEndpointRouteBuilder app)
        {
            app.MapPost("/", Handle)                
                .Produces<ItemResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem();
        }
        
        private static async Task<IResult> Handle(
            CreateItemRequest request,
            LogiTrackContext dbContext,
            ILogger<Program> logger
            )
        {
           
           var newItem = Item.Create(request.Name, request.Quantity, request.Location);

            dbContext.Items.Add(newItem);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Created new item ID: {ItemId}, Name: {ItemName}", newItem.ItemId, newItem.Name);

            return Results.CreatedAtRoute(
                ItemsEndpointsNames.GetItem,
                new { id = newItem.ItemId },
                new ItemResponse(newItem.ItemId, newItem.Name, newItem.Quantity, newItem.Location));
        }
    }
}
