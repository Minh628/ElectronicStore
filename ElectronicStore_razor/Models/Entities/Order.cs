using System;
using System.Collections.Generic;

namespace ElectronicStore_razor.Models.Entities;

public partial class Order
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string OrderStatus { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string ReceiverName { get; set; } = null!;

    public string ReceiverPhone { get; set; } = null!;

    public string ShippingAddress { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual ApplicationUser? User { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
