using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.BLModels
{
    public class EmailVerificationCode
    {
        public string Email { get; set; }
        public string Code { get; set; }
        public DateTime ExpiryTime { get; set; }
        public bool IsUsed { get; set; }

        public EmailVerificationCode(string email, string code, TimeSpan validityPeriod)
        {
            Email = email;
            Code = code;
            ExpiryTime = DateTime.UtcNow.Add(validityPeriod);
            IsUsed = false;
        }
        public bool IsExpired => DateTime.UtcNow > ExpiryTime;
        public bool IsValid => !IsExpired && !IsUsed;
    }
}
