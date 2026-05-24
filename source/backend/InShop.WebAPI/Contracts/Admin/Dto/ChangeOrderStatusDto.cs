using System.ComponentModel.DataAnnotations;

namespace Contracts.Admin.Dto
{
    public class ChangeOrderStatusDto
    {
        [Required]
        [MaxLength(50)]
        public string NewStatus { get; set; } = null!;
    }
}
