
using Microsoft.EntityFrameworkCore;
using Hospital.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ✅ Add SQL Server Database Connection (CHANGED HERE)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer("Data Source=HIRA\\SQLEXPRESS;Initial Catalog=HOSPITALDB;Integrated Security=True;Trust Server Certificate = True"));

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
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();