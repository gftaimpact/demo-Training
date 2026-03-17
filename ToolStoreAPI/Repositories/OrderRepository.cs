
using ToolStoreAPI.Data;
using ToolStoreAPI.Interfaces;
using ToolStoreAPI.Models;

namespace ToolStoreAPI.Repositories;

public class OrderRepository : IOrderRepository
{
 private readonly AppDataContext _context;

 public OrderRepository(AppDataContext context)
 {
  _context = context;
 }

 public List<Order> GetAll() => _context.Orders;

 public void Add(Order order)
 {
  order.Id = _context.Orders.Count + 1;
  _context.Orders.Add(order);
 }
}
