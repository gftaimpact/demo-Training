
using ToolStoreAPI.Models;
using ToolStoreAPI.DTOs;

namespace ToolStoreAPI.Interfaces;

public interface ICartService
{
 List<CartItem> GetCart();
 void AddToCart(CartItemDto item);
 void Clear();
}
