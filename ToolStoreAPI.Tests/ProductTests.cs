
using ToolStoreAPI.Services;
using ToolStoreAPI.Repositories;
using ToolStoreAPI.Data;
using ToolStoreAPI.DTOs;
using Xunit;

public class ProductTests
{
 [Fact]
 public void CreateProduct_ShouldWork()
 {
  var ctx=new AppDataContext();
  var repo=new ProductRepository(ctx);
  var service=new ProductService(repo);

  var dto=new ProductDto{
   Name="Test",
   Description="Test",
   Price=10,
   StockQuantity=1
  };

  var product=service.Create(dto);

  Assert.NotNull(product);
 }
}
