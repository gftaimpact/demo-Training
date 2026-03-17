
using ToolStoreAPI.Models;
namespace ToolStoreAPI.Interfaces;

public interface IOrderService
{
 List<Order> GetOrders();
 Order CreateOrder();
}
