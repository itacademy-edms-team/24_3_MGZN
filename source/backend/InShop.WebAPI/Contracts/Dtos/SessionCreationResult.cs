using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public class SessionCreationResult
    {
        public int SessionId { get; set; }
        public string Message { get; set; }
    }
}
