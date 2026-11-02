using LogiTrack.Features.Orders.CreateOrder;
using LogiTrack.Features.Orders.DeleteOrder;
using LogiTrack.Features.Orders.GetOrder;
using LogiTrack.Features.Orders.UpdateOrder;
using LogiTrack.Features.Auth;

namespace LogiTrack.Features.Orders
{
    public static class OrdersEndpoints
    {
        public static void MapOrders(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/orders")
                .RequireAuthorization(AuthPolicies.OperatorOrAdministrator);
            
            // Map Orders endpoints
            group.MapCreateOrder();
            group.MapGetOrder();
            group.MapUpdateOrder();
            group.MapDeleteOrder();
        }
    }

    public static class OrdersEndpointsNames
    {
        public const string GetOrder = nameof(GetOrder);
    }
}
