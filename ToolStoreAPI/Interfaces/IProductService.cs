
using ToolStoreAPI.Models;
using ToolStoreAPI.DTOs;

namespace ToolStoreAPI.Interfaces;

public interface IProductService
{
 List<Product> GetAll();
 Product? GetById(int id);
 Product Create(ProductDto dto);
}
