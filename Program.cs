using Bakery_Management_System.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register BakeryDbContext with SQL Server connection string from appsettings.json
builder.Services.AddDbContext<BakeryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("myconn")));

// Add Session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);  // Session timeout
    options.Cookie.HttpOnly = true;                  // Cookie can't be accessed via JS
    options.Cookie.IsEssential = true;               // Required for session to work
});

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<BakeryDbContext>();
        DbInitializer.Initialize(dbContext);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();      // Redirect HTTP to HTTPS
app.UseStaticFiles();           // Enable serving static files (CSS, JS, images)

app.UseRouting();

app.UseSession();               // Enable session BEFORE authorization
app.UseAuthorization();

// Default route: takes user to customer index page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Customer}/{action=Index}/{id?}");

app.Run();
