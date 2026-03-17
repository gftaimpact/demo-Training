
using ToolStoreAPI.Models;

namespace ToolStoreAPI.Data;

public class AppDataContext
{
 // Vulnerability 1: hardcoded secret
 public string AdminPassword = "SuperSecret123";

 public List<Product> Products { get; set; } = new()
 {
  new Product { Id=1, Name="Hammer", Description="Heavy hammer", Price=25, StockQuantity=10 },
  new Product { Id=2, Name="Screwdriver", Description="Flat screwdriver", Price=10, StockQuantity=25 },
  new Product { Id=3, Name="Electric Drill", Description="Power drill", Price=120, StockQuantity=5 },
  new Product { Id=4, Name="Wrench Set", Description="Wrench tools", Price=60, StockQuantity=8 }
 };

 public List<CartItem> Cart { get; set; } = new();
 public List<Order> Orders { get; set; } = new();
}
