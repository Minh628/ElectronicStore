using ElectronicStore_razor.Models;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services;

/// <summary>
/// Service quản lý đơn hàng dành riêng cho End-User (Khách mua hàng)
/// </summary>
public interface IOrderService
{
    Task<(bool Success, string Message, int OrderId)> PlaceOrderAsync(
        string fullName, string phoneNumber, string? email, string shippingAddress,
        string paymentMethod, string? notes, List<CartItem> cart, string? currentUserId);

    Task<List<Order>> GetCustomerOrdersAsync(string? userId, string? userPhone, string? lookupPhone);
    Task<(List<Order> AllOrders, List<Order> FilteredOrders)> GetCustomerOrdersOverviewAsync(string? userId, string? userPhone, string? lookupPhone, string? statusFilter = null);
    Task<Order?> GetOrderForViewerAsync(int orderId, string? userId, string? userPhone, bool isAdmin, int? lastGuestOrderId, string? lookupPhone);
    Task<(bool Success, string Message)> CancelCustomerOrderAsync(int orderId, string? userId, string? userPhone, string? lookupPhone);
    string NormalizeStatus(string? status);
}
