using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services.Admin;

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
    OrderAdminResult GetAdminOrders(string? statusFilter, string? search);
    Order? GetOrderById(int id);
    (bool Success, string Message) UpdateOrderStatus(int orderId, string newStatus);
    string NormalizeStatus(string? status);
}
