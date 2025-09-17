using Microsoft.EntityFrameworkCore;
using Aplicacion_pedidos.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Aplicacion_pedidos.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure database context
builder.Services.AddDbContext<PedidosDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.Name = "PedidosAuth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(3);
        options.SlidingExpiration = true;
    });

// Register other services
builder.Services.AddScoped<PedidosDBContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

    // Seed the database with example users
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PedidosDBContext>();
    
    // Apply migrations
    dbContext.Database.Migrate();
    
    // Seed users if none exist
    if (!dbContext.Users.Any())
    {
        // Add admin user
        dbContext.Users.Add(new UserModel
        {
            Nombre = "Administrador",
            Email = "admin@example.com",
            Password = "admin123",  // In production, use a strong password and hash it
            Rol = UserModel.ROLE_ADMIN
        });
        
        // Add employee user
        dbContext.Users.Add(new UserModel
        {
            Nombre = "Empleado Ejemplo",
            Email = "empleado@example.com",
            Password = "empleado123",  // In production, use a strong password and hash it
            Rol = UserModel.ROLE_EMPLEADO
        });
        
        // Add client user
        dbContext.Users.Add(new UserModel
        {
            Nombre = "Cliente Ejemplo",
            Email = "cliente@example.com",
            Password = "cliente123",  // In production, use a strong password and hash it
            Rol = UserModel.ROLE_CLIENTE
        });
        
        dbContext.SaveChanges();
        
        Console.WriteLine("Database seeded with example users.");
    }
}

app.Run();
