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

        [Required(ErrorMessage = "Debe seleccionar un cliente")]
        [Display(Name = "Cliente")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un cliente válido")]
        public int UserId { get; set; }

        //Clave Foranea, hace referencia a cliente (user)
        [ForeignKey("UserId")]
        public UserModel Cliente { get; set; }

        [Required(ErrorMessage = "La fecha del pedido es obligatoria")]
        [Display(Name = "Fecha de Pedido")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = false)]
        public DateTime FechaPedido { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El estado del pedido es obligatorio")]
        [Display(Name = "Estado")]
        public OrderStatus Estado { get; set; } = OrderStatus.Pendiente;

        [Required(ErrorMessage = "El total es obligatorio")]
        [Display(Name = "Total")]
        [Column(TypeName = "decimal(10, 2)")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        [Range(0, 999999.99, ErrorMessage = "El total debe ser un valor positivo")]
        public decimal Total { get; set; } = 0;
        
        // Relación con los items del pedido
        public virtual ICollection<OrderItemModel> Items { get; set; } = new List<OrderItemModel>();
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