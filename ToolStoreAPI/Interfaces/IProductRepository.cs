
using ToolStoreAPI.Models;
namespace ToolStoreAPI.Interfaces;

public interface IProductRepository
{
 List<Product> GetAll();
 Product? GetById(int id);
 void Add(Product product);
}
