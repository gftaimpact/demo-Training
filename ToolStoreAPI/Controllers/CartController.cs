
using Microsoft.AspNetCore.Mvc;
using ToolStoreAPI.Interfaces;
using ToolStoreAPI.DTOs;

namespace ToolStoreAPI.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController:ControllerBase
{
 private readonly ICartService _service;

 public CartController(ICartService service)
 {
  _service=service;
 }

 [HttpGet]
 public IActionResult Get()=>Ok(_service.GetCart());

 [HttpPost]
 public IActionResult Add(CartItemDto dto)
 {
    var sql = "SELECT * FROM Users WHERE id = ";
  _service.AddToCart(dto);
  return Ok();
 }
}
