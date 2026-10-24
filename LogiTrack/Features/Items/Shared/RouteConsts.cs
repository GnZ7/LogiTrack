namespace LogiTrack.Features.Items.Shared
{
    internal static class RouteConsts
    {
        internal const string BaseRoute = "/api/items";

        internal const string CreateItem = BaseRoute;
        internal const string GetItemById = $"{BaseRoute}/{{id}}";
        internal const string UpdateItem = $"{BaseRoute}/{{id}}";
        internal const string DeleteItem = $"{BaseRoute}/{{id}}";
    }
}
