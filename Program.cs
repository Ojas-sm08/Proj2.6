using HospitalManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// -------------------------
// Add services to the container
// -------------------------

builder.Services.AddControllersWithViews();

// ✅ Register HospitalDbContext with SQL Server
builder.Services.AddDbContext<HospitalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Add session support
builder.Services.AddSession();

// ✅ Register IHttpContextAccessor for session access in views
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// -------------------------
// Configure HTTP pipeline
// -------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ Enable session before authorization
app.UseSession();

app.UseAuthorization();

// ✅ Set default route (starts at login)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
