using LogiTrack.Data;
using LogiTrack.Features.Items.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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

        private static async Task<IResult> Handle(
            LogiTrackContext context,
            IMemoryCache cache,
            ILogger<Program> logger,
            [AsParameters] GetItemsRequest request)
        {
            var cacheKey = $"items-page:{request.PageNumber}:{request.PageSize}";
            if (cache.TryGetValue<GetItemsResponse>(cacheKey, out var cachedResponse))
            {
                logger.LogInformation(
                    "Items cache hit for page {PageNumber} with page size {PageSize}.",
                    request.PageNumber,
                    request.PageSize);
                return Results.Ok(cachedResponse);
            }

            logger.LogInformation(
                "Items cache miss for page {PageNumber} with page size {PageSize}.",
                request.PageNumber,
                request.PageSize);

            var response = await cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);

                var skip = (request.PageNumber - 1) * request.PageSize;
                var itemsOnPage = await context.Items
                    .AsNoTracking()
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

                return new GetItemsResponse(itemsOnPage, totalPages);
            });

            return Results.Ok(response);
        }
    }
}
