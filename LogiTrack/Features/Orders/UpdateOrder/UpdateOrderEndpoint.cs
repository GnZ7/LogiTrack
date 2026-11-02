using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static LogiTrack.Features.Orders.Shared.OrderDtos;

namespace LogiTrack.Features.Orders.UpdateOrder;

public static class UpdateOrderEndpoint
{
    public static void MapUpdateOrder(this IEndpointRouteBuilder app)
    {
        app.MapPut("/{id:guid}", Handle)
            .WithSummary("Update an order and its reserved inventory")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateOrderRequest request,
        LogiTrackContext dbContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (request.Items.Any(item => item.ItemId == Guid.Empty || item.Quantity <= 0))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Items)] = ["Each item must have a valid ItemId and a quantity greater than zero."]
            });
        }

        var duplicateItemIds = request.Items
            .GroupBy(item => item.ItemId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateItemIds.Length > 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Items)] = [$"Each inventory item can appear only once. Duplicate IDs: {string.Join(", ", duplicateItemIds)}."]
            });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var order = await dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.OrderId == id, cancellationToken);

        if (order is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogWarning("Order with ID: {OrderId} not found for update.", id);
            return Results.NotFound();
        }

        var requestedQuantities = request.Items.ToDictionary(item => item.ItemId, item => item.Quantity);
        var requestedItemIds = requestedQuantities.Keys.ToArray();
        var inventoryItems = await dbContext.Items
            .AsNoTracking()
            .Where(item => requestedItemIds.Contains(item.ItemId))
            .ToDictionaryAsync(item => item.ItemId, cancellationToken);

        var missingItemIds = requestedItemIds
            .Where(itemId => !inventoryItems.ContainsKey(itemId))
            .ToArray();

        if (missingItemIds.Length > 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Items)] = [$"Inventory items were not found: {string.Join(", ", missingItemIds)}."]
            });
        }

        foreach (var existingItem in order.Items)
        {
            var affectedRows = await dbContext.Items
                .Where(item =>
                    item.ItemId == existingItem.ItemId
                    && item.Quantity <= int.MaxValue - existingItem.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        item => item.Quantity,
                        item => item.Quantity + existingItem.Quantity),
                    cancellationToken);

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogWarning(
                    "Could not safely restore inventory for order {OrderId}, item {ItemId}.",
                    id,
                    existingItem.ItemId);
                return Results.Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Inventory quantity overflow",
                    Detail = $"The reserved quantity for item {existingItem.ItemId} cannot be restored safely."
                });
            }
        }

        foreach (var requestedItem in request.Items)
        {
            var affectedRows = await dbContext.Items
                .Where(item => item.ItemId == requestedItem.ItemId && item.Quantity >= requestedItem.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        item => item.Quantity,
                        item => item.Quantity - requestedItem.Quantity),
                    cancellationToken);

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogWarning(
                    "Order {OrderId} could not be updated because item {ItemId} has insufficient inventory.",
                    id,
                    requestedItem.ItemId);
                return Results.Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Insufficient inventory",
                    Detail = $"There is not enough quantity available for item {requestedItem.ItemId}."
                });
            }
        }

        var updatedItems = request.Items
            .Select(requestedItem =>
            {
                var inventoryItem = inventoryItems[requestedItem.ItemId];
                return OrderItem.Create(
                    inventoryItem.ItemId,
                    inventoryItem.Name,
                    requestedItem.Quantity,
                    inventoryItem.Location);
            })
            .ToList();

        order.Update(request.CustomerName, updatedItems);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Updated order ID {OrderId} for customer {CustomerName}.",
            order.OrderId,
            order.CustomerName);

        var response = new OrderResponse(
            order.OrderId,
            order.CustomerName,
            order.DatePlaced,
            order.Items.Select(item => new ItemInOrder(item.ItemId, item.Name, item.Quantity, item.Location)));

        return Results.Ok(response);
    }
}
