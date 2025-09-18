using Microsoft.EntityFrameworkCore;
using Aplicacion_pedidos.Models;

namespace Aplicacion_pedidos.Data
{
    public class PedidosDBContext : DbContext
    {
        public PedidosDBContext(DbContextOptions<PedidosDBContext> options) : base(options)
        {
        }

        public DbSet<UserModel> Users { get; set; }
        public DbSet<ProductModel> Products { get; set; }
        public DbSet<OrderModel> Orders { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure the UserModel entity
            modelBuilder.Entity<UserModel>(entity =>
            {
                // Table name
                entity.ToTable("Users");
                
                // Primary key
                entity.HasKey(e => e.Id);
                
                // Properties
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Rol).IsRequired().HasMaxLength(50);
                
                // Indexes
                entity.HasIndex(e => e.Email).IsUnique();
            });
            
            // Configure the ProductModel entity
            modelBuilder.Entity<ProductModel>(entity =>
            {
                // Table name
                entity.ToTable("Products");
                
                // Primary key
                entity.HasKey(e => e.Id);
                
                // Properties
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Descripcion).HasMaxLength(500);
                entity.Property(e => e.Precio).IsRequired().HasColumnType("decimal(10, 2)");
                entity.Property(e => e.Stock).IsRequired();
                entity.Property(e => e.Disponible).HasDefaultValue(true);
            });
            
            // Configure the OrderModel entity
            modelBuilder.Entity<OrderModel>(entity =>
            {
                // Table name
                entity.ToTable("Orders");
                
                // Primary key
                entity.HasKey(e => e.Id);
                
                // Properties
                entity.Property(e => e.FechaPedido).IsRequired();
                entity.Property(e => e.Estado).IsRequired();
                entity.Property(e => e.Total).IsRequired().HasColumnType("decimal(10, 2)");
                
                // Relationships
                entity.HasOne(e => e.Cliente)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
