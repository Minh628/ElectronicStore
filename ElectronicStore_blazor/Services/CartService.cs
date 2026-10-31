using System.Text.Json;
using Microsoft.JSInterop;
using ElectronicStore_blazor.Models;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

/// <summary>
/// Service quản lý giỏ hàng trong phiên làm việc của người dùng Blazor (Scoped)
/// Đồng bộ tự động với localStorage để giữ giỏ hàng khi người dùng F5 (Reload trang).
/// </summary>
public class CartService
{
    private const string CartStorageKey = "electro_cart_v1";
    private const string LastOrderStorageKey = "electro_last_order_id";

    private IJSRuntime? _js;
    private bool _isRestored = false;
    private int? _lastGuestOrderId;

    public List<CartItem> Items { get; private set; } = new();

    public int? LastGuestOrderId
    {
        get => _lastGuestOrderId;
        set
        {
            _lastGuestOrderId = value;
            if (_js != null)
            {
                _ = SaveLastOrderIdAsync(_js, value);
            }
        }
    }

    public event Action? OnChange;

    /// <summary>
    /// Khôi phục giỏ hàng và mã đơn hàng khách vãng lai gần nhất từ localStorage sau khi F5
    /// </summary>
    public async Task RestoreCartAsync(IJSRuntime js)
    {
        _js = js;
        if (_isRestored) return;
        _isRestored = true;

        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", CartStorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var savedItems = JsonSerializer.Deserialize<List<CartItem>>(json);
                if (savedItems != null && savedItems.Count > 0)
                {
                    // Gộp các sản phẩm đã lưu nếu giỏ hàng hiện tại đang trống hoặc mới thêm
                    foreach (var saved in savedItems)
                    {
                        if (!Items.Any(i => i.ProductId == saved.ProductId))
                        {
                            Items.Add(saved);
                        }
                    }
                    OnChange?.Invoke();
                }
            }

            if (_lastGuestOrderId == null)
            {
                var savedOrderId = await js.InvokeAsync<string?>("localStorage.getItem", LastOrderStorageKey);
                if (int.TryParse(savedOrderId, out int parsedId) && parsedId > 0)
                {
                    _lastGuestOrderId = parsedId;
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi JSInterop nếu đang ở bước prerender tĩnh
        }
    }

    private async Task SaveCartToStorageAsync()
    {
        if (_js == null) return;
        try
        {
            var json = JsonSerializer.Serialize(Items);
            await _js.InvokeVoidAsync("localStorage.setItem", CartStorageKey, json);
        }
        catch
        {
            // Bỏ qua lỗi khi ngắt kết nối mạch SignalR
        }
    }

    private async Task SaveLastOrderIdAsync(IJSRuntime js, int? orderId)
    {
        try
        {
            if (orderId.HasValue && orderId.Value > 0)
            {
                await js.InvokeVoidAsync("localStorage.setItem", LastOrderStorageKey, orderId.Value.ToString());
            }
            else
            {
                await js.InvokeVoidAsync("localStorage.removeItem", LastOrderStorageKey);
            }
        }
        catch { }
    }

    public (bool Success, string Message) AddToCart(Product product, int quantity = 1)
    {
        // Chặn thêm sản phẩm với số lượng không hợp lệ (nhỏ hơn hoặc bằng 0)
        if (quantity <= 0)
            return (false, "Số lượng sản phẩm thêm vào giỏ phải lớn hơn 0!");

        if (product == null || !product.IsActive || (product.Category != null && !product.Category.IsActive))
            return (false, "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh!");

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

    /// <summary>
    /// Đồng bộ tồn kho, đơn giá và trạng thái kinh doanh thực tế từ Database cho các sản phẩm trong giỏ
    /// </summary>
    public void SyncWithProducts(IEnumerable<Product> activeProducts)
    {
        if (Items.Count == 0) return;

        var productMap = activeProducts.ToDictionary(p => p.Id);
        bool changed = false;

        for (int i = Items.Count - 1; i >= 0; i--)
        {
            var item = Items[i];
            if (!productMap.TryGetValue(item.ProductId, out var p) || !p.IsActive || p.Quantity <= 0)
            {
                Items.RemoveAt(i);
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
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
        _ = SaveCartToStorageAsync();
    }
}
