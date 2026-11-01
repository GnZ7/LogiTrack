using LogiTrack.Data;
using LogiTrack.Features.Items.Shared;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Features.Items.GetItems
{
    public static class GetItemsEndpoint
    {
        // GET /api/items
        public static void MapGetItems(this IEndpointRouteBuilder app)
        {
            app.MapGet("/", Handle)
                .Produces<GetItemsResponse>(StatusCodes.Status200OK);
        }

        private static async Task<IResult> Handle(LogiTrackContext context, [AsParameters] GetItemsRequest request)
        {
            var skip = (request.PageNumber - 1) * request.PageSize;

            var itemsOnPage = await context.Items
                .OrderBy(i => i.Name)
                .Skip(skip)
                .Take(request.PageSize)
                .Select(i => new ItemResponse(
                    i.ItemId,
                    i.Name,
                    i.Quantity,
                    i.Location))
                .ToListAsync();

            var totalItems = await context.Items.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);

            return Results.Ok(new GetItemsResponse(
                itemsOnPage,                
                totalPages));
        }
    }
}
