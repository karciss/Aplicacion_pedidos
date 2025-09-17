using System.ComponentModel.DataAnnotations;

namespace Aplicacion_pedidos.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo electr�nico es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electr�nico v�lido")]
        [Display(Name = "Correo Electr�nico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contrase�a es obligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Contrase�a")]
        public string Password { get; set; }

        [Display(Name = "Recordar cuenta")]
        public bool RememberMe { get; set; }
    }
}