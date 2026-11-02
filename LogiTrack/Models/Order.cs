namespace LogiTrack.Models;

public class Order
{        
    private readonly List<OrderItem> _items = [];

    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DatePlaced { get; set; }        
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    //private Order() { } lo comento para ver por qué hace falta?

    public static Order Create(string customerName, List<OrderItem> items)
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
    
    public void AddItems(List<OrderItem> items)
    {
        _items.AddRange(items);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddItem(OrderItem item)
    {
        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string customerName, List<OrderItem> items)
    {
        CustomerName = customerName;
        var itemsById = items.ToDictionary(item => item.ItemId);

        foreach (var existingItem in _items.ToArray())
        {
            if (itemsById.Remove(existingItem.ItemId, out var updatedItem))
            {
                existingItem.Name = updatedItem.Name;
                existingItem.Quantity = updatedItem.Quantity;
                existingItem.Location = updatedItem.Location;
            }
            else
            {
                _items.Remove(existingItem);
            }
        }

        _items.AddRange(itemsById.Values);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(OrderItem item)
    {
        _items.Remove(item);
        UpdatedAt = DateTime.UtcNow;
    }
}