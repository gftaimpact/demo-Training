
using ToolStoreAPI.Data;
using ToolStoreAPI.Interfaces;
using ToolStoreAPI.Models;
using ToolStoreAPI.DTOs;

namespace ToolStoreAPI.Services;

public class CartService : ICartService
{
 private readonly AppDataContext _context;

 public CartService(AppDataContext context)
 {
  _context=context;
 }

 public List<CartItem> GetCart()=>_context.Cart;

 // Vulnerability 2: no validation of product existence
 public void AddToCart(CartItemDto item)
 {
  _context.Cart.Add(new CartItem{
   ProductId=item.ProductId,
   Quantity=item.Quantity
  });
 }

 public void Clear()=>_context.Cart.Clear();
}
