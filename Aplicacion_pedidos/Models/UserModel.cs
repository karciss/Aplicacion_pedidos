using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aplicacion_pedidos.Models
{
    public class UserModel
    {
        // Role constants
        public const string ROLE_ADMIN = "Admin";
        public const string ROLE_CLIENTE = "Cliente";
        public const string ROLE_EMPLEADO = "Empleado";

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los {1} caracteres")]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Email no válido")]
        [StringLength(150, ErrorMessage = "El email no puede exceder los {1} caracteres")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre {2} y {1} caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Rol")]
        public string Rol { get; set; }

        // Navigation properties can be added here if needed
    }
}
