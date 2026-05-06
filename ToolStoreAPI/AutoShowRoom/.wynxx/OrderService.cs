using OnlineShopping.Application.DTOs;
using OnlineShopping.Application.Interfaces;
using OnlineShopping.Domain.Entities;
using OnlineShopping.Infrastructure.Repositories;
 
namespace OnlineShopping.Application.Services;
 
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
 
    public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }
 
    public async Task<Order> CreateAsync(CreateOrderDto dto)
    {
        var order = new Order
        {
            CustomerEmail = dto.CustomerEmail
        };
 
        decimal total = 0;
 
        foreach (var item in dto.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId)
                ?? throw new Exception($"Product {item.ProductId} not found");
 
            if (product.Stock < item.Quantity)
                throw new Exception($"Insufficient stock for {product.Name}");
 
            product.Stock -= item.Quantity;
            await _productRepository.UpdateAsync(product);
 
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
 
            total += product.Price * item.Quantity;
        }
 
        order.TotalAmount = total;
 
        return await _orderRepository.AddAsync(order);
    }
 
    public Task<List<Order>> GetAllAsync() => _orderRepository.GetAllAsync();
 
    public Task<Order?> GetByIdAsync(int id) => _orderRepository.GetByIdAsync(id);
}