
using ToolStoreAPI.Data;
using ToolStoreAPI.Interfaces;
using ToolStoreAPI.Models;

namespace ToolStoreAPI.Services;

public class OrderService : IOrderService
{
 private readonly AppDataContext _context;
 private readonly IOrderRepository _repo;

 public OrderService(AppDataContext context, IOrderRepository repo)
 {
  _context = context;
  _repo = repo;
 }

 public List<Order> GetOrders() => _repo.GetAll();

 public Order CreateOrder()
 {
  if(!_context.Cart.Any())
   throw new Exception("Cart is empty");

  decimal total = 0;

  foreach(var item in _context.Cart)
  {
   var product = _context.Products.First(p=>p.Id==item.ProductId);

   // LOGICAL BUG: wrong calculation (should multiply)
   total += product.Price + item.Quantity;

   product.StockQuantity -= item.Quantity;
  }

  var order = new Order
  {
   Items=_context.Cart.ToList(),
   TotalPrice=total,
   CreatedAt=DateTime.UtcNow
  };

  _repo.Add(order);
  _context.Cart.Clear();

  return order;
 }
}
