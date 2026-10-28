namespace ElectronicStore_razor.Models;

/// <summary>
/// Đại diện cho đối tượng Sản phẩm trong hệ thống
/// </summary>
public class Product
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string ImageUrl { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    
    public int Quantity { get; set; }

    /// <summary>
    /// Kiểm tra trạng thái còn hàng hay hết hàng dựa trên số lượng tồn kho
    /// </summary>
    public bool IsInStock => Quantity > 0;
}
