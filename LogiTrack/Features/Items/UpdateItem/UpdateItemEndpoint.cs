using LogiTrack.Data;
using LogiTrack.Features.Items.Shared;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Features.Items.UpdateItem;

public static class UpdateItemEndpoint
{
    public static void MapUpdateItem(this IEndpointRouteBuilder app)
    {
        app.MapPut("/{id:guid}", Handle)
            .WithSummary("Update an inventory item")
            .Produces<ItemResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateItemRequest request,
        LogiTrackContext dbContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .FirstOrDefaultAsync(item => item.ItemId == id, cancellationToken);

        if (item is null)
        {
            logger.LogWarning("Item with ID: {ItemId} not found for update.", id);
            return Results.NotFound();
        }

        item.Name = request.Name;
        item.Quantity = request.Quantity;
        item.Location = request.Location;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated item ID: {ItemId}, Name: {ItemName}", item.ItemId, item.Name);

        return Results.Ok(new ItemResponse(item.ItemId, item.Name, item.Quantity, item.Location));
    }
}
