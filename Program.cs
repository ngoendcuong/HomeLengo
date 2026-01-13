// Program.cs
using HomeLengo.Hubs;
using HomeLengo.Models;
using HomeLengo.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<HomeLengoContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// Session configuration
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// --- ĐĂNG KÝ SERVICE TẠI ĐÂY ---
// 1. SignalR
builder.Services.AddSignalR();

// 2. HttpClient (để gọi API Gemini)
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();


// 3. Đăng ký các Service ta vừa viết
builder.Services.AddScoped<IProductService, ProductService>();
//builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<VNPayService>();
builder.Services.AddScoped<ServicePackageService>();
builder.Services.AddScoped<PackageExpirationService>();

// 4. Đăng ký Background Service để kiểm tra gói hết hạn
builder.Services.AddHostedService<PackageExpirationBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

// --- ĐỊNH TUYẾN HUB ---
app.MapHub<ChatHub>("/chatHub");

// --- ROUTE CHO ADMIN 1: Admin Area ---
app.MapControllerRoute(
    name: "admin_area",
    pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Admin" }
);

// --- ROUTE CHO ADMIN 2: RealEstateAdmin Area ---
app.MapControllerRoute(
    name: "realestate_admin_area",
    pattern: "RealEstateAdmin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "RealEstateAdmin" }
);

// --- ROUTE MẶC ĐỊNH CHO AREAS KHÁC ---
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);
app.MapControllerRoute(
    name: "contactus",
    pattern: "pages/ContactUs",
    defaults: new { controller = "Contact", action = "Index" }
);

// --- ROUTE MẶC ĐỊNH ---
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();