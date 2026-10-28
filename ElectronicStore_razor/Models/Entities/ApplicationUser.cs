using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace ElectronicStore_razor.Models.Entities;

/// <summary>
/// Người dùng hệ thống kế thừa IdentityUser chuẩn của ASP.NET Core
/// Tích hợp đầy đủ thông tin cá nhân thay thế bảng Customers cũ
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
