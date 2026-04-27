using Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InShopBLLayer.Abstractions
{
    public interface IAiAnalysisService
    {
        Task<ReviewSummaryDto?> GenerateReviewSummaryAsync(List<string> reviewsText);
    }
}
