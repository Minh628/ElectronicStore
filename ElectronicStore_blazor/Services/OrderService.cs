using Microsoft.EntityFrameworkCore;
using System.Data;
using ElectronicStore_blazor.Data;
using ElectronicStore_blazor.Models;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

/// <summary>
/// Triển khai nghiệp vụ giao dịch và đơn hàng trong Blazor Server
/// </summary>
public class OrderService : IOrderService
{
    private readonly IDbContextFactory<ElectronicStoreDbContext> _contextFactory;

    public OrderService(IDbContextFactory<ElectronicStoreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public List<Order> GetOrders(string? status = null)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            switch (status)
            {
                case "Pending":
                    query = query.Where(o => o.OrderStatus == "Pending" || o.OrderStatus == "Chờ xử lý");
                    break;
                case "Confirmed":
                    query = query.Where(o => o.OrderStatus == "Confirmed" || o.OrderStatus == "Đã duyệt");
                    break;
                case "Shipping":
                    query = query.Where(o => o.OrderStatus == "Shipping" || o.OrderStatus == "Đang giao" || o.OrderStatus == "Đang giao hàng");
                    break;
                case "Completed":
                    query = query.Where(o => o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn thành" || o.OrderStatus == "Đã giao" || o.OrderStatus == "Hoàn tất");
                    break;
                case "Cancelled":
                    query = query.Where(o => o.OrderStatus == "Cancelled" || o.OrderStatus == "Đã hủy");
                    break;
                default:
                    query = query.Where(o => o.OrderStatus == status);
                    break;
            }
        }

        return query.OrderByDescending(o => o.OrderDate).ToList();
    }

    public Order? GetOrderById(int id, string? requesterUserId = null, bool requesterIsAdmin = false, int? guestOrderId = null, string? guestPhone = null)
    {
        using var context = _contextFactory.CreateDbContext();
        var order = context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefault(o => o.Id == id);

        if (order == null) return null;
        if (requesterIsAdmin) return order;

        var effectivePhone = !string.IsNullOrWhiteSpace(guestPhone) ? guestPhone.Trim() : null;
        bool ownsOrder = (!string.IsNullOrEmpty(requesterUserId) && order.UserId == requesterUserId) ||
                         (guestOrderId.HasValue && guestOrderId.Value == id) ||
                         (string.IsNullOrEmpty(order.UserId) && !string.IsNullOrWhiteSpace(effectivePhone) && order.ReceiverPhone == effectivePhone);

        return ownsOrder ? order : null;
    }

    public bool UpdateOrderStatus(int orderId, string status)
    {
        using var context = _contextFactory.CreateDbContext();
        var order = context.Orders.Include(o => o.OrderDetails).FirstOrDefault(o => o.Id == orderId);
        if (order != null)
        {
            var oldStatus = order.OrderStatus;
            var isOldCancelled = oldStatus == "Cancelled" || oldStatus == "Đã hủy";
            var isNewCancelled = status == "Cancelled" || status == "Đã hủy";

            if (!isOldCancelled && isNewCancelled)
            {
                foreach (var detail in order.OrderDetails)
                {
                    var product = context.Products.Find(detail.ProductId);
                    if (product != null)
                    {
                        product.Quantity += detail.Quantity;
                    }
                }
            }
            else if (isOldCancelled && !isNewCancelled)
            {
                foreach (var detail in order.OrderDetails)
                {
                    var product = context.Products.Find(detail.ProductId);
                    if (product != null)
                    {
                        product.Quantity = Math.Max(0, product.Quantity - detail.Quantity);
                    }
                }
            }

            order.OrderStatus = status;
            context.SaveChanges();
            return true;
        }
        return false;
    }

    public (bool Success, string Message) CancelOrder(int orderId, bool isClient = true, string? requesterUserId = null, string? requesterPhone = null)
    {
        using var context = _contextFactory.CreateDbContext();
        var order = context.Orders.Include(o => o.OrderDetails).FirstOrDefault(o => o.Id == orderId);
        if (order == null)
        {
            return (false, "Không tìm thấy đơn hàng cần hủy!");
        }

        if (isClient)
        {
            if (!string.IsNullOrEmpty(requesterUserId))
            {
                bool isOwner = order.UserId == requesterUserId ||
                               (string.IsNullOrEmpty(order.UserId) && !string.IsNullOrWhiteSpace(requesterPhone) && order.ReceiverPhone == requesterPhone.Trim());
                if (!isOwner)
                {
                    return (false, "Bạn không có quyền thao tác trên đơn hàng này!");
                }
            }
            else if (!string.IsNullOrWhiteSpace(requesterPhone))
            {
                if (order.ReceiverPhone != requesterPhone.Trim())
                {
                    return (false, "Số điện thoại tra cứu không khớp với đơn hàng!");
                }

                if (!string.IsNullOrEmpty(order.UserId))
                {
                    return (false, "Đơn hàng này thuộc tài khoản thành viên. Vui lòng đăng nhập tài khoản để hủy đơn!");
                }
            }
            else
            {
                return (false, "Yêu cầu không hợp lệ!");
            }

            var currentStatus = order.OrderStatus;
            if (currentStatus != "Pending" && currentStatus != "Chờ xử lý" && currentStatus != "Confirmed" && currentStatus != "Đã duyệt")
            {
                return (false, "Đơn hàng đang giao hoặc đã hoàn tất/đã hủy, không thể hủy.");
            }
        }

        foreach (var detail in order.OrderDetails)
        {
            var product = context.Products.Find(detail.ProductId);
            if (product != null)
            {
                product.Quantity += detail.Quantity;
            }
        }

        order.OrderStatus = "Cancelled";
        context.SaveChanges();
        return (true, $"Đã hủy thành công đơn hàng #ES{order.Id:D7}. Số lượng sản phẩm đã được tự động hoàn lại vào kho.");
    }

    public (bool Success, string Message, int OrderId) PlaceOrder(
        string fullName, string phone, string email, string address, string paymentMethod, string? notes, List<CartItem> items, string? userId = null)
    {
        if (paymentMethod is not ("COD" or "BankTransfer" or "VNPAY" or "MOMO"))
        {
            return (false, "Phương thức thanh toán không hợp lệ.", 0);
        }

        if (items == null || !items.Any())
        {
            return (false, "Giỏ hàng của bạn đang trống!", 0);
        }

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(address) || address.Trim() == "Chưa cập nhật địa chỉ")
        {
            return (false, "Vui lòng nhập đầy đủ Họ tên, Số điện thoại và Địa chỉ giao hàng!", 0);
        }

        using var context = _contextFactory.CreateDbContext();
        using var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);

        foreach (var item in items)
        {
            var prod = context.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == item.ProductId);
            if (prod == null || !prod.IsActive || (prod.Category != null && !prod.Category.IsActive))
            {
                transaction.Rollback();
                return (false, $"Sản phẩm '{item.ProductName}' hiện không còn kinh doanh!", 0);
            }

            if (prod.Quantity < item.Quantity)
            {
                transaction.Rollback();
                return (false, $"Sản phẩm '{prod.Name}' chỉ còn {prod.Quantity} chiếc trong kho (bạn đặt {item.Quantity} chiếc)!", 0);
            }

            item.ProductName = prod.Name;
            item.UnitPrice = prod.Price;
        }

        if (!string.IsNullOrEmpty(userId))
        {
            var user = context.Users.Find(userId);
            if (user != null)
            {
                bool userUpdated = false;
                if (string.IsNullOrEmpty(user.FullName))
                {
                    user.FullName = fullName.Trim();
                    userUpdated = true;
                }
                if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    user.PhoneNumber = phone.Trim();
                    userUpdated = true;
                }
                if (string.IsNullOrEmpty(user.Address) || user.Address == "Chưa cập nhật địa chỉ")
                {
                    user.Address = address.Trim();
                    userUpdated = true;
                }
                if (userUpdated)
                {
                    context.Users.Update(user);
                }
            }
        }

        var order = new Order
        {
            UserId = userId,
            ReceiverName = fullName.Trim(),
            ReceiverPhone = phone.Trim(),
            ShippingAddress = address.Trim(),
            PaymentMethod = paymentMethod,
            Notes = notes?.Trim(),
            OrderDate = DateTime.Now,
            OrderStatus = "Pending",
            TotalAmount = items.Sum(i => i.TotalPrice)
        };

        context.Orders.Add(order);
        context.SaveChanges();

        foreach (var item in items)
        {
            var detail = new OrderDetail
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            };
            context.OrderDetails.Add(detail);

            var product = context.Products.Find(item.ProductId);
            if (product != null)
            {
                product.Quantity = Math.Max(0, product.Quantity - item.Quantity);
            }
        }
        context.SaveChanges();
        transaction.Commit();

        return (true, "Đặt hàng thành công!", order.Id);
    }
}

