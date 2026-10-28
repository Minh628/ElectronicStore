using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký ElectronicStoreDbContext kết nối SQL Server (Database First chuẩn Identity)
builder.Services.AddDbContext<ElectronicStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình ASP.NET Core Identity chuẩn với ApplicationUser và IdentityRole
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Mật khẩu đơn giản phục vụ học tập & test đồ án
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

    // Tự động phân luồng: Cố vào trang Admin khi chưa đăng nhập -> chuyển về /Admin/Login
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect($"/Admin/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
        }
        else
        {
            context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
        }
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

// Cấu hình Session cho Giỏ hàng
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();

// Đăng ký dịch vụ Razor Pages với các Route alias chuẩn Blazor
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/ProductDetail", "product/{id?}");
    options.Conventions.AddPageRoute("/MyOrders", "my-orders");
    options.Conventions.AddPageRoute("/Account/Profile", "profile");
});
builder.Services.AddSingleton<IProductService, ProductService>();

var app = builder.Build();

// Tự động kiểm tra Unicode tiếng Việt và khởi tạo Identity Role/User
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ElectronicStoreDbContext>();
    DbSeeder.FixVietnameseEncoding(context);
    DbSeeder.SeedIdentityAsync(scope.ServiceProvider).GetAwaiter().GetResult();
}

// Cấu hình HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thứ tự Middleware quan trọng: Session -> Authentication -> Authorization
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
