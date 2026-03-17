
using ToolStoreAPI.Models;
namespace ToolStoreAPI.Interfaces;

public interface IOrderRepository
{
 List<Order> GetAll();
 void Add(Order order);
}
