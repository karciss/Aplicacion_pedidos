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
            
            
        }
    }
}
