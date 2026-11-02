using ElectronicStore_mvc.Models;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Services;

/// <summary>
/// Service chuyên trách đơn hàng phục vụ khách hàng (End-User)
/// </summary>
public interface IOrderService
{
    Task<(bool Success, string Message, int OrderId)> PlaceOrderAsync(
        string fullName, string phoneNumber, string? email, string shippingAddress,
        string paymentMethod, string? notes, List<CartItem> cart, string? currentUserId);

    Task<List<Order>> GetCustomerOrdersAsync(string? userId, string? userPhone, string? lookupPhone);
    Task<Order?> GetOrderForViewerAsync(int orderId, string? userId, string? userPhone, bool isAdmin, int? lastGuestOrderId, string? lookupPhone);
    Task<(bool Success, string Message)> CancelCustomerOrderAsync(int orderId, string? userId, string? userPhone, string? lookupPhone);
    string NormalizeStatus(string? status);
}
