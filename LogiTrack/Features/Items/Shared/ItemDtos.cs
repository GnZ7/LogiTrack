using System.ComponentModel.DataAnnotations;

namespace LogiTrack.Features.Items.Shared
{
    public record ItemResponse(Guid ItemId, string Name, int Quantity, string Location);

    
    public record CreateItemRequest([Required][StringLength(120)] string Name, [Range(0, int.MaxValue)] int Quantity, [Required][StringLength(200)] string Location);
   
    public record GetItemsResponse(IEnumerable<ItemResponse> Items, int TotalPages);
    public record GetItemsRequest([Range(1, int.MaxValue)] int PageNumber = 1, [Range(1, int.MaxValue)] int PageSize = 10); 
}