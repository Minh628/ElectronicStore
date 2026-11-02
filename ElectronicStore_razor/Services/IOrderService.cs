using ElectronicStore_razor.Models;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services;

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
/// Service quản lý giao dịch và đơn hàng (Order Domain) cho Razor Pages
/// </summary>
public interface IOrderService
{
    // End-User
    Task<(bool Success, string Message, int OrderId)> PlaceOrderAsync(
        string fullName, string phoneNumber, string? email, string shippingAddress,
        string paymentMethod, string? notes, List<CartItem> cart, string? currentUserId);

    Task<List<Order>> GetCustomerOrdersAsync(string? userId, string? userPhone, string? lookupPhone);
    Task<Order?> GetOrderForViewerAsync(int orderId, string? userId, string? userPhone, bool isAdmin, int? lastGuestOrderId, string? lookupPhone);
    Task<(bool Success, string Message)> CancelCustomerOrderAsync(int orderId, string? userId, string? userPhone, string? lookupPhone);

    // Admin
    Task<OrderAdminResult> GetAdminOrdersAsync(string? status, string? search);
    Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string newStatus);
    string NormalizeStatus(string? status);
}

