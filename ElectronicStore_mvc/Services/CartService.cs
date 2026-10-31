using System.Text.Json;
using ElectronicStore_mvc.Models;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Services;

/// <summary>
/// Service quản lý Giỏ hàng của người dùng thông qua Session
/// </summary>
public class CartService
{
    private const string CartSessionKey = "ElectroStore_Mvc_Cart";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession Session => _httpContextAccessor.HttpContext?.Session 
        ?? throw new InvalidOperationException("Session không khả dụng!");

    /// <summary>
    /// Lấy danh sách các sản phẩm hiện có trong giỏ hàng từ Session
    /// </summary>
    public List<CartItem> GetCart()
    {
        var json = Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return new List<CartItem>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }
        catch (JsonException)
        {
            return new List<CartItem>();
        }
    }

    /// <summary>
    /// Lưu danh sách giỏ hàng trở lại Session
    /// </summary>
    private void SaveCart(List<CartItem> cart)
    {
        var json = JsonSerializer.Serialize(cart);
        Session.SetString(CartSessionKey, json);
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng với số lượng chỉ định
    /// </summary>
    public (bool Success, string Message) AddToCart(Product product, int quantity = 1)
    {
        // Chặn thêm sản phẩm với số lượng không hợp lệ (nhỏ hơn hoặc bằng 0)
        if (quantity <= 0)
            return (false, "Số lượng sản phẩm thêm vào giỏ phải lớn hơn 0!");

        if (product == null || !product.IsActive || (product.Category != null && !product.Category.IsActive))
            return (false, "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh!");

        if (product.Quantity <= 0)
            return (false, "Sản phẩm này hiện đã hết hàng!");

        var cart = GetCart();
        var existing = cart.FirstOrDefault(c => c.ProductId == product.Id);

        if (existing != null)
        {
            // Kiểm tra tồn kho không vượt quá số lượng còn lại trong DB
            if (existing.Quantity + quantity > product.Quantity)
            {
                return (false, $"Kho chỉ còn {product.Quantity} sản phẩm, bạn đã có {existing.Quantity} trong giỏ!");
            }
            existing.Quantity += quantity;
            existing.StockQuantity = product.Quantity;
            existing.UnitPrice = product.Price;
            existing.ProductName = product.Name;
            existing.ImageUrl = product.ImageUrl;
        }
        else
        {
            if (quantity > product.Quantity)
            {
                return (false, $"Kho chỉ còn {product.Quantity} sản phẩm!");
            }

            cart.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Brand = product.Brand,
                UnitPrice = product.Price,
                Quantity = quantity,
                ImageUrl = product.ImageUrl,
                WarrantyMonths = product.WarrantyMonths,
                StockQuantity = product.Quantity
            });
        }

        SaveCart(cart);
        return (true, $"Đã thêm {product.Name} vào giỏ hàng thành công!");
    }

    /// <summary>
    /// Cập nhật số lượng của một mặt hàng trong giỏ
    /// </summary>
    public void UpdateQuantity(int productId, int quantity, int maxStock)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.ProductId == productId);
        if (item != null)
        {
            if (quantity <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                // Giới hạn số lượng không vượt quá số lượng kho
                item.Quantity = Math.Min(quantity, maxStock);
            }
            SaveCart(cart);
        }
    }

    /// <summary>
    /// Xóa hoàn toàn một mặt hàng khỏi giỏ
    /// </summary>
    public void RemoveFromCart(int productId)
    {
        var cart = GetCart();
        cart.RemoveAll(c => c.ProductId == productId);
        SaveCart(cart);
    }

    /// <summary>
    /// Xóa toàn bộ giỏ hàng
    /// </summary>
    public void ClearCart()
    {
        Session.Remove(CartSessionKey);
    }

    /// <summary>
    /// Tính tổng số lượng tất cả sản phẩm trong giỏ
    /// </summary>
    public int GetCount()
    {
        return GetCart().Sum(c => c.Quantity);
    }

    /// <summary>
    /// Tính tổng số tiền của cả giỏ hàng
    /// </summary>
    public decimal GetTotal()
    {
        return GetCart().Sum(c => c.TotalPrice);
    }

    /// <summary>
    /// Đồng bộ tồn kho, đơn giá và trạng thái kinh doanh thực tế từ Database cho các sản phẩm trong giỏ
    /// </summary>
    public void SyncWithProducts(IEnumerable<Product> products)
    {
        var cart = GetCart();
        if (cart.Count == 0) return;

        var productMap = products.ToDictionary(p => p.Id);
        bool changed = false;

        for (int i = cart.Count - 1; i >= 0; i--)
        {
            var item = cart[i];
            if (!productMap.TryGetValue(item.ProductId, out var p) || !p.IsActive || (p.Category != null && !p.Category.IsActive) || p.Quantity <= 0)
            {
                cart.RemoveAt(i);
                changed = true;
                continue;
            }

            if (item.StockQuantity != p.Quantity || item.UnitPrice != p.Price || item.ProductName != p.Name || item.ImageUrl != p.ImageUrl)
            {
                item.StockQuantity = p.Quantity;
                item.UnitPrice = p.Price;
                item.ProductName = p.Name;
                item.Brand = p.Brand;
                item.ImageUrl = p.ImageUrl;
                item.WarrantyMonths = p.WarrantyMonths;
                changed = true;
            }

            if (item.Quantity > p.Quantity)
            {
                item.Quantity = p.Quantity;
                changed = true;
            }
        }

        if (changed)
        {
            SaveCart(cart);
        }
    }
}
