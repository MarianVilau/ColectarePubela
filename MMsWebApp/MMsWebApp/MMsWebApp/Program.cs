using Microsoft.EntityFrameworkCore;
using MMsWebApp.Data;
using MMsWebApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Listen on all network interfaces
builder.WebHost.UseUrls("http://0.0.0.0:5138");

builder.Services.AddRazorPages();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowESP", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Adăugați după builder.Build() și înainte de app.Run()
if (app.Environment.IsDevelopment())
{
    await SeedInitialData(app.Services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://unpkg.com/ https://*.googleapis.com; " +
        "style-src 'self' 'unsafe-inline' https://unpkg.com/ https://cdn.jsdelivr.net/ https://fonts.googleapis.com https://fonts.gstatic.com; " +
        "img-src 'self' data: https://*.tile.openstreetmap.org https://unpkg.com/ https://*.googleapis.com https://*.gstatic.com https://*.leafletjs.com; " +
        "font-src 'self' data: https://cdn.jsdelivr.net/ https://fonts.gstatic.com https://fonts.googleapis.com; " +
        "connect-src 'self' https://*.tile.openstreetmap.org/ https://*.googleapis.com https://*.leafletjs.com; " +
        "frame-src 'self'; " +
        "media-src 'self'; " +
        "worker-src 'self' blob:; " +
        "child-src 'self' blob:; " +
        "object-src 'none'");

    await next();
});

app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Adăugați această metodă la sfârșitul fișierului
static async Task SeedInitialData(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Verificăm dacă avem deja date
    if (!context.Pubele.Any())
    {
        // Adăugăm câteva pubele de test
        var pubele = new List<Pubela>
        {
            new Pubela { Id = "4307062F", Tip = "Plastic" },
            new Pubela { Id = "4307063A", Tip = "Hartie" },
            new Pubela { Id = "4307064B", Tip = "Sticla" }
        };
        context.Pubele.AddRange(pubele);
        await context.SaveChangesAsync();
    }

    if (!context.Cetateni.Any())
    {
        // Adăugăm câțiva cetățeni de test
        var cetateni = new List<Cetatean>
        {
            new Cetatean
            {
                Nume = "Popescu",
                Prenume = "Ion",
                Email = "ion.popescu@example.com",
                CNP = "1234567890123"
            },
            new Cetatean
            {
                Nume = "Ionescu",
                Prenume = "Maria",
                Email = "maria.ionescu@example.com",
                CNP = "2234567890123"
            }
        };
        context.Cetateni.AddRange(cetateni);
        await context.SaveChangesAsync();
    }
}