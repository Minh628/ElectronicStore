using ElectronicStore_mvc.Models;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Services;

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
/// Service quản lý giao dịch và đơn hàng (Order Domain)
/// </summary>
public interface IOrderService
{
    // ================= DÀNH CHO KHÁCH HÀNG (END-USER) =================
    /// <summary>
    /// Thực hiện giao dịch đặt hàng an toàn (IsolationLevel.Serializable, kiểm tra danh mục/sản phẩm active, trừ kho, cập nhật hồ sơ)
    /// </summary>
    Task<(bool Success, string Message, int OrderId)> PlaceOrderAsync(
        string fullName, string phoneNumber, string? email, string shippingAddress,
        string paymentMethod, string? notes, List<CartItem> cart, string? currentUserId);

    /// <summary>
    /// Lấy danh sách đơn hàng của khách hàng (chống lộ thông tin IDOR giữa tài khoản thành viên và khách vãng lai)
    /// </summary>
    Task<List<Order>> GetCustomerOrdersAsync(string? userId, string? userPhone, string? lookupPhone);

    /// <summary>
    /// Lấy chi tiết đơn hàng cho người xem (kiểm tra quyền sở hữu bảo mật IDOR)
    /// </summary>
    Task<Order?> GetOrderForViewerAsync(int orderId, string? userId, string? userPhone, bool isAdmin, int? lastGuestOrderId, string? lookupPhone);

    /// <summary>
    /// Khách hàng yêu cầu hủy đơn hàng (chỉ cho phép khi Pending/Confirmed và tự động hoàn trả tồn kho)
    /// </summary>
    Task<(bool Success, string Message)> CancelCustomerOrderAsync(int orderId, string? userId, string? userPhone, string? lookupPhone);

    // ================= DÀNH CHO QUẢN TRỊ VIÊN (ADMIN) =================
    /// <summary>
    /// Lấy danh sách đơn hàng cho Admin kèm các chỉ số KPI doanh thu và số lượng theo trạng thái
    /// </summary>
    Task<OrderAdminResult> GetAdminOrdersAsync(string? status, string? search);

    /// <summary>
    /// Quản trị viên cập nhật trạng thái đơn hàng (tự động hoàn kho khi hủy và trừ lại kho khi khôi phục)
    /// </summary>
    Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string newStatus);

    /// <summary>
    /// Chuẩn hóa trạng thái đơn hàng giữa tiếng Việt và tiếng Anh
    /// </summary>
    string NormalizeStatus(string? status);
}

