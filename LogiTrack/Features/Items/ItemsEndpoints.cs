using LogiTrack.Features.Items.CreateItem;
using LogiTrack.Features.Items.DeleteItem;
using LogiTrack.Features.Items.GetItem;
using LogiTrack.Features.Items.GetItems;
using LogiTrack.Features.Items.UpdateItem;
using LogiTrack.Features.Auth;

namespace LogiTrack.Features.Items
{
    public static class ItemsEndpoints
    {
        public static void MapItems(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/items")
                .RequireAuthorization(AuthPolicies.OperatorOrAdministrator);

            // Map Items endpoints
            group.MapCreateItem();
            group.MapGetItem();
            group.MapDeleteItem();
            group.MapGetItems();
            group.MapUpdateItem();
        }
    }

    public static class ItemsEndpointsNames
    {
        public const string GetItem = nameof(GetItem);
    }
}
