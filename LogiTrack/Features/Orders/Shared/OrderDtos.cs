using System.ComponentModel.DataAnnotations;

namespace LogiTrack.Features.Orders.Shared
{
    public class OrderDtos
    {
        public record CreateOrderRequest([Required][StringLength(120)] string CustomerName, [Required][MinLength(1)] IReadOnlyList<CreateOrderItemRequest> Items);
        public record UpdateOrderRequest([Required][StringLength(120)] string CustomerName, [Required][MinLength(1)] IReadOnlyList<CreateOrderItemRequest> Items);
        public record CreateOrderItemRequest(Guid ItemId, [Range(1, int.MaxValue)] int Quantity);
        public record OrderResponse(Guid OrderId, string CustomerName, DateTime DatePlaced, IEnumerable<ItemInOrder> Items);
        public record ItemInOrder(Guid ItemId, string Name, int Quantity, string Location);
    }
}