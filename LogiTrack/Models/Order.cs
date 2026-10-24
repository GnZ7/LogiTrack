namespace LogiTrack.Models;

public class Order
{        
    private readonly List<Item> _items = [];

    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DatePlaced { get; set; }        
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyList<Item> Items => _items.AsReadOnly();

    //private Order() { } lo comento para ver por qué hace falta?

    public static Order Create(string customerName, List<Item> items)
    {
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerName = customerName,
            DatePlaced = DateTime.UtcNow
        };

        order.AddItems(items);

        return order;
    }
    
    public void AddItems(List<Item> items)
    {
        _items.AddRange(items);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddItem(Item item)
    {
        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(Item item)
    {
        _items.Remove(item);
        UpdatedAt = DateTime.UtcNow;
    }
}