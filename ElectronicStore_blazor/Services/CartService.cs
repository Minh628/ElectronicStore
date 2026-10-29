using ElectronicStore_blazor.Models;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

/// <summary>
/// Service quản lý giỏ hàng trong phiên làm việc của người dùng Blazor (Scoped)
/// </summary>
public class CartService
{
    public List<CartItem> Items { get; private set; } = new();
    public int? LastGuestOrderId { get; set; }

    public event Action? OnChange;

    public (bool Success, string Message) AddToCart(Product product, int quantity = 1)
    {
        // Chặn thêm sản phẩm với số lượng không hợp lệ (nhỏ hơn hoặc bằng 0)
        if (quantity <= 0)
            return (false, "Số lượng sản phẩm thêm vào giỏ phải lớn hơn 0!");

        if (product == null)
            return (false, "Sản phẩm không tồn tại!");

        if (product.Quantity <= 0)
            return (false, "Sản phẩm này hiện đã hết hàng!");

        var existing = Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing != null)
        {
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

            Items.Add(new CartItem
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
        NotifyStateChanged();
        return (true, $"Đã thêm {product.Name} vào giỏ hàng thành công!");
    }

    public void UpdateQuantity(int productId, int delta)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            item.Quantity += delta;
            if (item.Quantity <= 0)
            {
                Items.Remove(item);
            }
            else if (item.Quantity > item.StockQuantity)
            {
                item.Quantity = item.StockQuantity;
            }
            NotifyStateChanged();
        }
    }

    public bool UpdateQuantityExact(int productId, int quantity, int maxStock)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return false;

        if (quantity <= 0)
        {
            Items.Remove(item);
        }
        else
        {
            item.Quantity = Math.Min(quantity, maxStock);
        }
        NotifyStateChanged();
        return true;
    }

    public void Remove(int productId)
    {
        Items.RemoveAll(i => i.ProductId == productId);
        NotifyStateChanged();
    }

    public void Clear()
    {
        Items.Clear();
        NotifyStateChanged();
    }

    public int GetCount() => Items.Sum(i => i.Quantity);

    public decimal GetTotal() => Items.Sum(i => i.TotalPrice);

    private void NotifyStateChanged() => OnChange?.Invoke();
}
