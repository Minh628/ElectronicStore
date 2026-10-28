using System;
using System.Collections.Generic;

namespace ElectronicStore_mvc.Models.Entities;

public partial class Product
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Brand { get; set; } = null!;

    public string? Specifications { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public int WarrantyMonths { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
