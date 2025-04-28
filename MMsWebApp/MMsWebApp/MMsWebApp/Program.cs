using Microsoft.EntityFrameworkCore;
using MMsWebApp.Data;
using MMsWebApp.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Configurarea Kestrel să asculte pe toate interfețele, doar pe HTTP (fără HTTPS)
builder.WebHost.UseUrls("http://0.0.0.0:5138");

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Comentăm UseHsts pentru a nu forța HTTPS
    // app.UseHsts();
}

// Adăugăm header-ul CSP
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self' https:; " +
        "img-src 'self' https: data: blob:; " +
        "style-src 'self' https: 'unsafe-inline'; " +
        "script-src 'self' https: 'unsafe-inline' 'unsafe-eval'; " +
        "font-src 'self' https: data:; " +
        "connect-src 'self' https:;");
    
    await next();
});

// Comentăm redirectarea la HTTPS
// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<RouteHub>("/routeHub");

app.Run();