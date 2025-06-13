using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class UserSessionDto
    {
        [Required(ErrorMessage = "Передача IP обязательна")]
        public string UserIpaddress { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
