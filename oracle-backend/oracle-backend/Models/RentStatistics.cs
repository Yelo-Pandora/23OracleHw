using System;
using System.ComponentModel.DataAnnotations;

namespace oracle_backend.Models
{
    /// <summary>
    /// 租金收缴明细数据模型
    /// </summary>
    public class RentCollectionDetail
    {
        public string Period { get; set; } = string.Empty;
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public int AreaId { get; set; }
        public decimal BaseRent { get; set; }
        public decimal ActualAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public int DaysOverdue { get; set; }
        public string ContactInfo { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// 租金收缴趋势数据模型
    /// </summary>
    public class RentTrendData
    {
        public string Period { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CollectedAmount { get; set; }
        public double CollectionRate { get; set; }
        public int TotalBills { get; set; }
        public int PaidBills { get; set; }
        public int OverdueBills { get; set; }
    }

    /// <summary>
    /// 租金统计报表请求DTO
    /// </summary>
    public class RentStatisticsRequest
    {
        [Required]
        public string Period { get; set; } = string.Empty;

        public string Dimension { get; set; } = "all";

        [Required]
        public string OperatorAccount { get; set; } = string.Empty;
    }

    /// <summary>
    /// 区域租金统计数据模型
    /// </summary>
    public class AreaRentStatistics
    {
        public int AreaId { get; set; }
        public decimal BaseRent { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? StoreName { get; set; }
        public string? TenantName { get; set; }
        public double CollectionRate { get; set; }
        public decimal CollectedAmount { get; set; }
        public int DaysOverdue { get; set; }
        public string RiskLevel { get; set; } = "低";
    }

    /// <summary>
    /// 欠款明细数据模型
    /// </summary>
    public class OutstandingDetail
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public decimal OutstandingAmount { get; set; }
        public int DaysOverdue { get; set; }
        public DateTime DueDate { get; set; }
        public string ContactInfo { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "低";
        public string? LastContactDate { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// 租金收缴率统计数据模型
    /// </summary>
    public class CollectionRateStatistics
    {
        public string Period { get; set; } = string.Empty;
        public double OnTimeRate { get; set; } // 按时缴纳率
        public double LatePaymentRate { get; set; } // 延期缴纳率
        public double OverdueRate { get; set; } // 逾期率
        public double DefaultRate { get; set; } // 违约率
        public int TotalStores { get; set; }
        public int OnTimeStores { get; set; }
        public int LatePaymentStores { get; set; }
        public int OverdueStores { get; set; }
        public int DefaultStores { get; set; }
    }

    /// <summary>
    /// 历史收缴趋势分析结果
    /// </summary>
    public class TrendAnalysisResult
    {
        public string StartPeriod { get; set; } = string.Empty;
        public string EndPeriod { get; set; } = string.Empty;
        public List<RentTrendData> TrendData { get; set; } = new List<RentTrendData>();
        public double AverageCollectionRate { get; set; }
        public decimal AverageMonthlyRevenue { get; set; }
        public string Trend { get; set; } = string.Empty; // 上升/下降/稳定
        public List<string> KeyInsights { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    /// <summary>
    /// 商户租金风险评估模型
    /// </summary>
    public class MerchantRiskAssessment
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "低"; // 低/中/高
        public double RiskScore { get; set; }
        public List<string> RiskFactors { get; set; } = new List<string>();
        public int ConsecutiveOverdueMonths { get; set; }
        public decimal TotalOutstanding { get; set; }
        public double PaymentReliabilityScore { get; set; }
        public string? RecommendedAction { get; set; }
    }
}
