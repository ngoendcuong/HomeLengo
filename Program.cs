using HomeLengo.Hubs;
using HomeLengo.Models;
//using HomeLengo.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<HomeLengoContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

// --- ĐĂNG KÝ SERVICE TẠI ĐÂY ---

// 1. SignalR
builder.Services.AddSignalR();

// 2. HttpClient (để gọi API Gemini)
builder.Services.AddHttpClient();

// 3. Đăng ký các Service ta vừa viết
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();

// 4. Controller & View (Mặc định)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseStaticFiles();
app.UseAuthorization();

app.MapStaticAssets();

// --- ĐỊNH TUYẾN HUB ---
app.MapHub<ChatHub>("/chatHub"); // Đường dẫn này để JS kết nối

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
