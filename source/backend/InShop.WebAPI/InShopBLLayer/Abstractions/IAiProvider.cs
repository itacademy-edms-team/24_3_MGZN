using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Abstractions
{
    public interface IAiProvider
    {
        Task<string> GenerateAsync(
            string systemPrompt,
            string userPrompt,
            float temperature = 0.2f,
            int maxTokens = 1000);
    }
}
