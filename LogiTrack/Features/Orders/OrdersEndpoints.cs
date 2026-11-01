using LogiTrack.Features.Orders.CreateOrder;
using LogiTrack.Features.Orders.DeleteOrder;
using LogiTrack.Features.Orders.GetOrder;

namespace LogiTrack.Features.Orders
{
    public static class OrdersEndpoints
    {
        public static void MapOrders(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/orders");
            
            // Map Orders endpoints
            group.MapCreateOrder();
            group.MapGetOrder();
            group.MapDeleteOrder();
        }
    }

    public static class OrdersEndpointsNames
    {
        public const string GetOrder = nameof(GetOrder);
    }
}
