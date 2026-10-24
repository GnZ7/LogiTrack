namespace LogiTrack.Models;

public class Item
{        
    public Guid ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Location { get; set; } = string.Empty;

    public Guid? OrderId { get; set; } // Foreign key to Order
    public Order? Order { get; set; } // Navigation property to Order

    private Item() { }

    public static Item Create(string name, int quantity, string location)
    {
        return new Item
        {
            ItemId = Guid.NewGuid(),
            Name = name,
            Quantity = quantity,
            Location = location
        };
    }

    public string DisplayInfo()
    {
        return $"Item: {Name} | Quantity: {Quantity}, Location: {Location}";
    }
}    