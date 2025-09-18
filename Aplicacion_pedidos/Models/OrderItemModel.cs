using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aplicacion_pedidos.Models
{
    public class OrderItemModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Pedido")]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public OrderModel Order { get; set; }

        [Required]
        [Display(Name = "Producto")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public ProductModel Producto { get; set; }

        [Required]
        [Display(Name = "Cantidad")]
        [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre {1} y {2}")]
        public int Cantidad { get; set; }

        [Required]
        [Display(Name = "Subtotal")]
        [Column(TypeName = "decimal(10, 2)")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal Subtotal { get; set; }
    }
}