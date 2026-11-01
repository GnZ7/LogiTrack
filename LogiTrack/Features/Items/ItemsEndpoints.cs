using LogiTrack.Features.Items.CreateItem;
using LogiTrack.Features.Items.DeleteItem;
using LogiTrack.Features.Items.GetItem;
using LogiTrack.Features.Items.GetItems;

namespace LogiTrack.Features.Items
{
    public static class ItemsEndpoints
    {
        public static void MapItems(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/items");

            // Map Items endpoints
            group.MapCreateItem();
            group.MapGetItem();
            group.MapDeleteItem();
            group.MapGetItems();
        }
    }

    public static class ItemsEndpointsNames
    {
        public const string GetItem = nameof(GetItem);
    }
}
