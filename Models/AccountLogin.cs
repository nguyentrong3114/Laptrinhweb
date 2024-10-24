using System.ComponentModel.DataAnnotations;
namespace shopflowerproject.Models
{
    public class AccountLogin
    {
        [Required(ErrorMessage = "Không được trống trường này")]
        public string? Username { get; set; }
        [Required(ErrorMessage = "Không được trống trường này")]
        public string? Password { get; set; }

        public bool RememberMe { get; set;}
    }
}