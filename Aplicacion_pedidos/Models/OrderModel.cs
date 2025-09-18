using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aplicacion_pedidos.Models
{
    public class OrderModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Cliente")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public UserModel Cliente { get; set; }

        [Required]
        [Display(Name = "Fecha de Pedido")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = false)]
        public DateTime FechaPedido { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Estado")]
        public OrderStatus Estado { get; set; } = OrderStatus.Pendiente;

        [Required]
        [Display(Name = "Total")]
        [Column(TypeName = "decimal(10, 2)")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal Total { get; set; }
        
        // Relación con los items del pedido
        public virtual ICollection<OrderItemModel> Items { get; set; }
    }

    public enum OrderStatus
    {
        [Display(Name = "Pendiente")]
        Pendiente,
        
        [Display(Name = "En Proceso")]
        EnProceso,
        
        [Display(Name = "Enviado")]
        Enviado,
        
        [Display(Name = "Entregado")]
        Entregado,
        
        [Display(Name = "Cancelado")]
        Cancelado
    }
}