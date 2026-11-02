using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services.Admin;

public class OrderAdminResult
{
    public List<Order> Orders { get; set; } = new();
    public int TotalOrdersCount { get; set; }
    public int PendingCount { get; set; }
    public int ShippingCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

/// <summary>
/// Service quản lý đơn hàng dành riêng cho phân hệ Quản trị (Admin)
/// </summary>
public interface IAdminOrderService
{
    Task<OrderAdminResult> GetAdminOrdersAsync(string? status, string? search);
    Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string newStatus);
    string NormalizeStatus(string? status);
}
