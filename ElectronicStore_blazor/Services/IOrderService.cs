using ElectronicStore_blazor.Models;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

/// <summary>
/// Service quản lý giao dịch và đơn hàng dành riêng cho End-User (Khách mua hàng)
/// </summary>
public interface IOrderService
{
    (bool Success, string Message, int OrderId) PlaceOrder(string fullName, string phone, string email, string address, string paymentMethod, string? notes, List<CartItem> items, string? userId = null);
    List<Order> GetCustomerOrders(string? userId, string? userPhone, string? searchPhone, string? statusFilter = null);
    Order? GetOrderById(int id, string? requesterUserId = null, bool requesterIsAdmin = false, int? guestOrderId = null, string? guestPhone = null);
    (bool Success, string Message) CancelOrder(int orderId, bool isClient = true, string? requesterUserId = null, string? requesterPhone = null);
    string NormalizeStatus(string? status);
}
