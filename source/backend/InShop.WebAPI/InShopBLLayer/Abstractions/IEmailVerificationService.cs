using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Abstractions
{
    public interface IEmailVerificationService
    {
        Task<string> GenerateAndSendCodeAsync(string email);
        bool ValidateCode(string email, string code);
    }
}
