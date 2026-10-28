using System.Text.Json;
using ElectronicStore_razor.Models;
using ElectronicStore_razor.Models.Entities;
using Product = ElectronicStore_razor.Models.Entities.Product;

namespace ElectronicStore_razor.Services;

/// <summary>
/// Service quản lý Giỏ hàng của người dùng thông qua Session
/// </summary>
public class CartService
{
    private const string CartSessionKey = "ElectroStore_Cart";
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
        catch
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

        if (product == null)
            return (false, "Sản phẩm không tồn tại!");

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
    /// Cập nhật số lượng sản phẩm trong giỏ
    /// </summary>
    public bool UpdateQuantity(int productId, int quantity, int maxStock)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.ProductId == productId);
        if (item == null) return false;

        if (quantity <= 0)
        {
            cart.Remove(item);
        }
        else
        {
            item.Quantity = Math.Min(quantity, maxStock);
        }

        SaveCart(cart);
        return true;
    }

    /// <summary>
    /// Xóa một sản phẩm khỏi giỏ hàng
    /// </summary>
    public void RemoveFromCart(int productId)
    {
        var cart = GetCart();
        cart.RemoveAll(c => c.ProductId == productId);
        SaveCart(cart);
    }

    /// <summary>
    /// Xóa sạch giỏ hàng (sau khi đặt hàng thành công)
    /// </summary>
    public void ClearCart()
    {
        Session.Remove(CartSessionKey);
    }

    /// <summary>
    /// Tổng số lượng món hàng trong giỏ
    /// </summary>
    public int GetCount() => GetCart().Sum(c => c.Quantity);

    /// <summary>
    /// Tổng số tiền thanh toán của cả giỏ hàng
    /// </summary>
    public decimal GetTotal() => GetCart().Sum(c => c.TotalPrice);
}
