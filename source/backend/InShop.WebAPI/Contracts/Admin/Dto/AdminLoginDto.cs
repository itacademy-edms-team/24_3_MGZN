using System.ComponentModel.DataAnnotations;

namespace Contracts.Admin.Dto
{
    public class AdminLoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}
