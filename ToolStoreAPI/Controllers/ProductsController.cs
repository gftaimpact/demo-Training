
using Microsoft.AspNetCore.Mvc;
using ToolStoreAPI.Interfaces;
using ToolStoreAPI.DTOs;

namespace ToolStoreAPI.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController:ControllerBase
{
 private readonly IProductService _service;

 public ProductsController(IProductService service)
 {
  _service=service;
 }

 [HttpGet]
 public IActionResult GetAll()=>Ok(_service.GetAll());

 [HttpPost]
 public IActionResult Create(ProductDto dto)
 {
  var product=_service.Create(dto);
  return Created($"/api/products/{product.Id}",product);
 }
}
