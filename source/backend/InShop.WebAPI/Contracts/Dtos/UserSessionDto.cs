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
        public string? UserIpaddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
