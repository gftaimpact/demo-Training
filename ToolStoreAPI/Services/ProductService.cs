
using ToolStoreAPI.Interfaces;
using ToolStoreAPI.Models;
using ToolStoreAPI.DTOs;

namespace ToolStoreAPI.Services;

public class ProductService : IProductService
{
 private readonly IProductRepository _repo;

 public ProductService(IProductRepository repo)
 {
  _repo = repo;
 }

 public List<Product> GetAll()=>_repo.GetAll();

 public Product? GetById(int id)=>_repo.GetById(id);

 public Product Create(ProductDto dto)
 {
  if(dto.Price<=0)
   throw new Exception("Price must be greater than zero");

  var product = new Product{
   Name=dto.Name,
   Description=dto.Description,
   Price=dto.Price,
   StockQuantity=dto.StockQuantity
  };

  _repo.Add(product);
  return product;
 }
}
