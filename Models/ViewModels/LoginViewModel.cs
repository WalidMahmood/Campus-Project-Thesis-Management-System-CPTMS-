using System.ComponentModel.DataAnnotations;

namespace CPTMS.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email address or Username is required.")]
        [Display(Name = "Email or Institutional ID")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember this device")]
        public bool RememberMe { get; set; } = false;

        public string? ReturnUrl { get; set; }
    }
}
