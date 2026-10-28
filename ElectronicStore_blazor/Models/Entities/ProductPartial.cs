namespace ElectronicStore_blazor.Models.Entities;

public partial class Product
{
    /// <summary>
    /// Kiểm tra trạng thái còn hàng dựa trên số lượng tồn kho thực tế trong database
    /// </summary>
    public bool IsInStock => Quantity > 0;
}
