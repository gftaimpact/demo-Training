
using ToolStoreAPI.Data;
using ToolStoreAPI.Interfaces;
using ToolStoreAPI.Models;

namespace ToolStoreAPI.Repositories;

public class ProductRepository : IProductRepository
{
 private readonly AppDataContext _context;

 public ProductRepository(AppDataContext context)
 {
  _context = context;
 }

 public List<Product> GetAll() => _context.Products;

 public Product? GetById(int id) => _context.Products.FirstOrDefault(p => p.Id == id);

 public void Add(Product product)
 {
  product.Id = _context.Products.Max(p => p.Id) + 1;
  _context.Products.Add(product);
 }
}
