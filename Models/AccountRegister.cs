using System.ComponentModel.DataAnnotations;
namespace shopflowerproject.Models
{
    public class AccountRegister
    {
        public string? Id { get; set; }

        [Required]
        public string? FullName { get; set; }
        [Required]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "Nhập Nhiều Hơn 6 kí tự")]
        public string? Username { get; set; }
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
        [Required]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "Nhập Nhiều Hơn 6 kí tự")]
        public string? Password { get; set; }
        [Required]
        [Compare("Password", ErrorMessage = "Không Khớp với Password")]
        public string? ConfirmPassword { get; set; }

    }
}