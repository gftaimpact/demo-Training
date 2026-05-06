
using ToolStoreAPI.Services;
using ToolStoreAPI.Repositories;
using ToolStoreAPI.Data;
using ToolStoreAPI.DTOs;
using ToolStoreAPI.Models;
using ToolStoreAPI.Interfaces;
using Xunit;

// ---------------------------------------------------------------------------
// Fakes
// ---------------------------------------------------------------------------

public class FakeProductRepository : IProductRepository
{
    private readonly List<Product> _store = new();
    private int _nextId = 1;

    public List<Product> GetAll() => _store;
    public Product? GetById(int id) => _store.FirstOrDefault(p => p.Id == id);
    public void Add(Product product) { product.Id = _nextId++; _store.Add(product); }
}

public class FakeOrderRepository : IOrderRepository
{
    private readonly List<Order> _store = new();
    private int _nextId = 1;

    public List<Order> GetAll() => _store;
    public void Add(Order order) { order.Id = _nextId++; _store.Add(order); }
}

// ---------------------------------------------------------------------------
// ProductService tests
// ---------------------------------------------------------------------------

public class ProductServiceTests
{
    private static (ProductService service, FakeProductRepository repo) Build()
    {
        var repo = new FakeProductRepository();
        return (new ProductService(repo), repo);
    }

    [Fact]
    public void GetAll_ReturnsEmptyList_WhenNoProductsAdded()
    {
        var (service, _) = Build();
        Assert.Empty(service.GetAll());
    }

    [Fact]
    public void Create_ReturnsProduct_WithCorrectValues()
    {
        var (service, _) = Build();
        var dto = new ProductDto { Name = "Hammer", Description = "Heavy", Price = 25m, StockQuantity = 10 };

        var product = service.Create(dto);

        Assert.NotNull(product);
        Assert.Equal("Hammer", product.Name);
        Assert.Equal(25m, product.Price);
        Assert.Equal(10, product.StockQuantity);
    }

    [Fact]
    public void Create_AssignsId_ToNewProduct()
    {
        var (service, _) = Build();
        var dto = new ProductDto { Name = "Wrench", Price = 15m, StockQuantity = 5 };

        var product = service.Create(dto);

        Assert.True(product.Id > 0);
    }

    [Fact]
    public void Create_Throws_WhenPriceIsZero()
    {
        var (service, _) = Build();
        var dto = new ProductDto { Name = "Free Tool", Price = 0m, StockQuantity = 1 };

        Assert.Throws<Exception>(() => service.Create(dto));
    }

    [Fact]
    public void Create_Throws_WhenPriceIsNegative()
    {
        var (service, _) = Build();
        var dto = new ProductDto { Name = "Bad Tool", Price = -5m, StockQuantity = 1 };

        Assert.Throws<Exception>(() => service.Create(dto));
    }

    [Fact]
    public void GetAll_ReturnsAllCreatedProducts()
    {
        var (service, _) = Build();
        service.Create(new ProductDto { Name = "A", Price = 1m, StockQuantity = 1 });
        service.Create(new ProductDto { Name = "B", Price = 2m, StockQuantity = 2 });

        Assert.Equal(2, service.GetAll().Count);
    }

    [Fact]
    public void GetById_ReturnsNull_WhenProductDoesNotExist()
    {
        var (service, _) = Build();
        Assert.Null(service.GetById(99));
    }

    [Fact]
    public void GetById_ReturnsCorrectProduct()
    {
        var (service, _) = Build();
        var created = service.Create(new ProductDto { Name = "Drill", Price = 120m, StockQuantity = 5 });

        var found = service.GetById(created.Id);

        Assert.NotNull(found);
        Assert.Equal("Drill", found.Name);
    }
}

// ---------------------------------------------------------------------------
// CartService tests
// ---------------------------------------------------------------------------

public class CartServiceTests
{
    private static (CartService service, AppDataContext ctx) Build()
    {
        var ctx = new AppDataContext();
        ctx.Cart.Clear();
        return (new CartService(ctx), ctx);
    }

    [Fact]
    public void GetCart_ReturnsEmptyList_Initially()
    {
        var (service, _) = Build();
        Assert.Empty(service.GetCart());
    }

    [Fact]
    public void AddToCart_AddsItem()
    {
        var (service, _) = Build();
        service.AddToCart(new CartItemDto { ProductId = 1, Quantity = 2 });

        var cart = service.GetCart();
        Assert.Single(cart);
        Assert.Equal(1, cart[0].ProductId);
        Assert.Equal(2, cart[0].Quantity);
    }

    [Fact]
    public void AddToCart_CanAddMultipleItems()
    {
        var (service, _) = Build();
        service.AddToCart(new CartItemDto { ProductId = 1, Quantity = 1 });
        service.AddToCart(new CartItemDto { ProductId = 2, Quantity = 3 });

        Assert.Equal(2, service.GetCart().Count);
    }

    [Fact]
    public void Clear_RemovesAllCartItems()
    {
        var (service, _) = Build();
        service.AddToCart(new CartItemDto { ProductId = 1, Quantity = 1 });
        service.AddToCart(new CartItemDto { ProductId = 2, Quantity = 2 });

        service.Clear();

        Assert.Empty(service.GetCart());
    }
}

// ---------------------------------------------------------------------------
// OrderService tests
// ---------------------------------------------------------------------------

public class OrderServiceTests
{
    private static (OrderService service, AppDataContext ctx, FakeOrderRepository repo) Build()
    {
        var ctx = new AppDataContext();
        ctx.Cart.Clear();
        var repo = new FakeOrderRepository();
        return (new OrderService(ctx, repo), ctx, repo);
    }

    [Fact]
    public void GetOrders_ReturnsEmptyList_Initially()
    {
        var (service, _, _) = Build();
        Assert.Empty(service.GetOrders());
    }

    [Fact]
    public void CreateOrder_Throws_WhenCartIsEmpty()
    {
        var (service, _, _) = Build();
        Assert.Throws<Exception>(() => service.CreateOrder());
    }

    [Fact]
    public void CreateOrder_ClearsCart_AfterCreation()
    {
        var (service, ctx, _) = Build();
        // Add a product that exists in the seed data (Id = 1, Price = 25)
        ctx.Cart.Add(new CartItem { ProductId = 1, Quantity = 1 });

        service.CreateOrder();

        Assert.Empty(ctx.Cart);
    }

    [Fact]
    public void CreateOrder_PersistsOrder_InRepository()
    {
        var (service, ctx, repo) = Build();
        ctx.Cart.Add(new CartItem { ProductId = 1, Quantity = 1 });

        service.CreateOrder();

        Assert.Single(repo.GetAll());
    }

    [Fact]
    public void CreateOrder_AssignsCreatedAt_ToOrder()
    {
        var (service, ctx, _) = Build();
        ctx.Cart.Add(new CartItem { ProductId = 1, Quantity = 1 });
        var before = DateTime.UtcNow.AddSeconds(-1);

        var order = service.CreateOrder();

        Assert.True(order.CreatedAt >= before);
    }

    [Fact]
    public void CreateOrder_ContainsBug_TotalUsesAdditionInsteadOfMultiplication()
    {
        // This test DOCUMENTS the known logical bug:
        // total += product.Price + item.Quantity  (wrong)
        // instead of: total += product.Price * item.Quantity  (correct)
        // Product Id=1: Price=25, Quantity=2 -> buggy total = 25 + 2 = 27, correct = 50
        var (service, ctx, _) = Build();
        ctx.Cart.Add(new CartItem { ProductId = 1, Quantity = 2 });

        var order = service.CreateOrder();

        // Assert the BUGGY behaviour so the test fails when the bug is fixed,
        // making it a clear signal to update both the service and this test.
        Assert.Equal(27m, order.TotalPrice);
    }

    [Fact]
    public void CreateOrder_DecreasesStockQuantity()
    {
        var (service, ctx, _) = Build();
        var product = ctx.Products.First(p => p.Id == 1);
        var originalStock = product.StockQuantity;
        ctx.Cart.Add(new CartItem { ProductId = 1, Quantity = 2 });

        service.CreateOrder();

        Assert.Equal(originalStock - 2, product.StockQuantity);
    }
}
