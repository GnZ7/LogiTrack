namespace LogiTrack.Models;

public class OrderItem
{
    public Guid OrderId { get; set; }
    public Guid ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Location { get; set; } = string.Empty;
    public Order? Order { get; set; }
    public Item Item { get; set; } = null!;

    private OrderItem() { }

    public static OrderItem Create(Guid itemId, string name, int quantity, string location)
    {
        return new OrderItem
        {
            ItemId = itemId,
            Name = name,
            Quantity = quantity,
            Location = location
        };
    }
}
