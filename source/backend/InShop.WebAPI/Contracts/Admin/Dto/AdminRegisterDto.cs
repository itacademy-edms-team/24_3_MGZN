using System.ComponentModel.DataAnnotations;

namespace Contracts.Admin.Dto
{
    /// <summary>Регистрация первого администратора (корпоративный email = логин).</summary>
    public class AdminRegisterDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public string Password { get; set; } = null!;
    }
}
