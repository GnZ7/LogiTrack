using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static LogiTrack.Features.Orders.Shared.OrderDtos;

namespace LogiTrack.Features.Orders.CreateOrder
{
    public static class CreateOrderEndpoint
    {
        public static void MapCreateOrder(this IEndpointRouteBuilder app)
        {
            app.MapPost("/", Handle)
                .Produces<OrderResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status409Conflict);
        }

        private static async Task<IResult> Handle(
            LogiTrackContext context,
            CreateOrderRequest request,
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

            var requestedQuantities = request.Items.ToDictionary(item => item.ItemId, item => item.Quantity);
            var itemIds = requestedQuantities.Keys.ToArray();
            var inventoryItems = await context.Items
                .AsNoTracking()
                .Where(item => itemIds.Contains(item.ItemId))
                .ToDictionaryAsync(item => item.ItemId, cancellationToken);

            var missingItemIds = itemIds
                .Where(itemId => !inventoryItems.ContainsKey(itemId))
                .ToArray();

            if (missingItemIds.Length > 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Items)] = [$"Inventory items were not found: {string.Join(", ", missingItemIds)}."]
                });
            }

            var orderItems = request.Items
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

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            foreach (var requestedItem in request.Items)
            {
                var affectedRows = await context.Items
                    .Where(item => item.ItemId == requestedItem.ItemId && item.Quantity >= requestedItem.Quantity)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            item => item.Quantity,
                            item => item.Quantity - requestedItem.Quantity),
                        cancellationToken);

                if (affectedRows == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Results.Conflict(new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Insufficient inventory",
                        Detail = $"There is not enough quantity available for item {requestedItem.ItemId}."
                    });
                }
            }

            var newOrder = Order.Create(request.CustomerName, orderItems);

            context.Orders.Add(newOrder);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Created new order with ID {OrderId} for customer {CustomerName}", newOrder.OrderId, newOrder.CustomerName);

            var response = new OrderResponse(
                newOrder.OrderId,
                newOrder.CustomerName,
                newOrder.DatePlaced,
                newOrder.Items.Select(i => new ItemInOrder(i.ItemId, i.Name, i.Quantity, i.Location))
            );

            return Results.CreatedAtRoute(
                OrdersEndpointsNames.GetOrder,
                new { id = newOrder.OrderId },
                response);
        }
    }
}