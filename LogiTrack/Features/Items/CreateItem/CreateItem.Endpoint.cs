using FluentValidation;
using LogiTrack.Common;
using LogiTrack.Features.Items.Shared;
using Microsoft.AspNetCore.Mvc;

namespace LogiTrack.Features.Items.CreateItem
{
    public class CreateItemApiEndpoint : IApiEndpoint
    {
        public void MapEndpoint(WebApplication app)
        {
            app.MapPost(RouteConsts.CreateItem, Handle);
        }

        private static async Task<IResult> Handle(
            [FromBody] CreateItemRequest request,
            IValidator<CreateItemRequest> validator,
            
            CancellationToken cancellationToken)
        {
           
            return Results.Ok();
        }
    }
}
