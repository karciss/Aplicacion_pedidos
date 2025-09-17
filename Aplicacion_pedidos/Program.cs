using Microsoft.EntityFrameworkCore;
using Aplicacion_pedidos.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure database context
builder.Services.AddDbContext<PedidosDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services for Identity if needed
// builder.Services.AddDefaultIdentity<IdentityUser>()
//    .AddEntityFrameworkStores<PedidosDBContext>();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// To create and apply migrations, run these commands in the terminal:
// dotnet ef migrations add InitialCreate
// dotnet ef database update

// Or to automatically apply migrations during startup (development only):
// using (var scope = app.Services.CreateScope())
// {
//     var dbContext = scope.ServiceProvider.GetRequiredService<PedidosDBContext>();
//     dbContext.Database.Migrate();
// }

app.Run();
