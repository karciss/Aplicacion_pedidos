using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aplicacion_pedidos.Models
{
    public class ProductModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los {1} caracteres")]
        [Display(Name = "Nombre del producto")]
        public string Nombre { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(300, ErrorMessage = "La descripción no puede exceder los {1} caracteres")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 99999.99, ErrorMessage = "El precio debe ser positivo (mayor a 0) y no puede superar {2}")]
        [Column(TypeName = "decimal(10, 2)")]
        [Display(Name = "Precio")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio")]
        [Range(0, 1000, ErrorMessage = "El stock no puede ser negativo y no debe superar {2} unidades")]
        [Display(Name = "Stock")]
        public int Stock { get; set; }

        [Display(Name = "Categoría")]
        [StringLength(50)]
        public string Categoria { get; set; }

        [Display(Name = "Disponible")]
        public bool Disponible { get; set; } = true;
        
        
        public bool HayStockDisponible(int cantidad)
        {
            return Disponible && Stock >= cantidad;
        }
    }
}