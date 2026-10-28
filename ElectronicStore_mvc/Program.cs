using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models.Entities;
using ElectronicStore_mvc.Services;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký ElectronicStoreDbContext kết nối SQL Server (Database First chuẩn Identity)
builder.Services.AddDbContext<ElectronicStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình ASP.NET Core Identity chuẩn với ApplicationUser và IdentityRole
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Cấu hình mật khẩu đơn giản, phù hợp môi trường đồ án học tập
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<ElectronicStoreDbContext>()
.AddDefaultTokenProviders();

// Cấu hình Cookie Authentication phân luồng đăng nhập
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // Tự động phân luồng: Cố vào trang Admin khi chưa đăng nhập -> chuyển về /admin/login
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect($"/admin/login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
        }
        else
        {
            context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
        }
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

// Cấu hình HttpContext và Session cho giỏ hàng
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký CartService
builder.Services.AddScoped<CartService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Khởi tạo Role, User và sửa font tiếng Việt khi khởi động server
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ElectronicStoreDbContext>();
    DbSeeder.SeedSampleDataAsync(context).GetAwaiter().GetResult();
    DbSeeder.FixVietnameseEncoding(context);
    DbSeeder.SeedIdentityAsync(scope.ServiceProvider).GetAwaiter().GetResult();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Thứ tự Middleware quan trọng
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// 1. Cấu hình Route cho Area (Admin)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

// 2. Cấu hình các Route ngắn thân thiện đồng bộ 100% với Blazor
app.MapControllerRoute(
    name: "product_detail",
    pattern: "product/{id?}",
    defaults: new { controller = "Home", action = "ProductDetail" });

app.MapControllerRoute(
    name: "cart_short",
    pattern: "cart",
    defaults: new { controller = "Cart", action = "Index" });

app.MapControllerRoute(
    name: "checkout_short",
    pattern: "checkout",
    defaults: new { controller = "Checkout", action = "Index" });

app.MapControllerRoute(
    name: "ordersuccess_short",
    pattern: "ordersuccess",
    defaults: new { controller = "Checkout", action = "OrderSuccess" });

app.MapControllerRoute(
    name: "my_orders_short",
    pattern: "my-orders",
    defaults: new { controller = "Orders", action = "MyOrders" });

app.MapControllerRoute(
    name: "profile_short",
    pattern: "profile",
    defaults: new { controller = "Account", action = "Profile" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
