
namespace ToolStoreAPI.Models;

public class Order
{
    public int Id { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}
