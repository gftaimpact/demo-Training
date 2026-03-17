
using Microsoft.AspNetCore.Mvc;
using ToolStoreAPI.Interfaces;

namespace ToolStoreAPI.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController:ControllerBase
{
 private readonly IOrderService _service;

 public OrdersController(IOrderService service)
 {
  _service=service;
 }

 [HttpGet]
 public IActionResult Get()=>Ok(_service.GetOrders());

 [HttpPost]
 public IActionResult Create()=>Ok(_service.CreateOrder());
}
