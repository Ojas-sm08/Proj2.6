using HospitalManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
// using Rotativa.AspNetCore; // Removed Rotativa using directive

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<HospitalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true; // Make the session cookie inaccessible to client-side script
    options.Cookie.IsEssential = true; // Make the session cookie essential
});

// Register IHttpContextAccessor for session access in controllers/views if needed
builder.Services.AddHttpContextAccessor();

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

// Enable session before authorization
app.UseSession(); // Must be here, after UseRouting() and before UseAuthorization()

app.UseAuthorization();

// Removed: RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Start at Login page

app.Run();
