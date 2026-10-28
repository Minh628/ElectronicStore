namespace ElectronicStore_mvc.Models;

/// <summary>
/// Đại diện cho một sản phẩm trong giỏ hàng (lưu trữ trong Session)
/// </summary>
public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public int WarrantyMonths { get; set; }
    public int StockQuantity { get; set; }

    /// <summary>
    /// Thành tiền tạm tính = Đơn giá * Số lượng
    /// </summary>
    public decimal TotalPrice => UnitPrice * Quantity;
}
