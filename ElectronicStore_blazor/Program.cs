using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_blazor.Components;
using ElectronicStore_blazor.Data;
using ElectronicStore_blazor.Models.Entities;
using ElectronicStore_blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký ElectronicStoreDbContext và DbContextFactory để xử lý an toàn đa luồng trong Blazor Server
builder.Services.AddDbContext<ElectronicStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContextFactory<ElectronicStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")),
    ServiceLifetime.Scoped);

// Cấu hình ASP.NET Core Identity với ApplicationUser và IdentityRole
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<ElectronicStoreDbContext>()
.AddDefaultTokenProviders();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true);

// Đăng ký dịch vụ ProductService kết nối SQL Server và CartService quản lý giỏ hàng
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<UserService>();

var app = builder.Build();

// Tự động kiểm tra Unicode và khởi tạo tài khoản/role Identity
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ElectronicStoreDbContext>>();
    using var context = factory.CreateDbContext();
    DbSeeder.FixVietnameseEncoding(context);
    DbSeeder.SeedIdentityAsync(scope.ServiceProvider).GetAwaiter().GetResult();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
