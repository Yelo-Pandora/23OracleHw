using System;
using System.ComponentModel.DataAnnotations;

namespace oracle_backend.Models
{
    /// <summary>
    /// 租金收取统计数据模型 - 基于现有表结构的虚拟租金单
    /// 使用STORE、RETAIL_AREA、RENT_STORE表组合实现租金管理
    /// </summary>
    public class RentCollectionStatistics
    {
        public string Period { get; set; } = string.Empty;
        public int TotalBills { get; set; }
        public int PaidBills { get; set; }
        public int OverdueBills { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OverdueAmount { get; set; }
        public double CollectionRate { get; set; }
    }

    /// <summary>
    /// 虚拟租金单数据模型 - 基于现有表结构计算生成
    /// </summary>
    public class VirtualRentBill
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string BillPeriod { get; set; } = string.Empty;
        public decimal BaseRent { get; set; }
        public int RentMonths { get; set; } = 1;
        public decimal TotalAmount { get; set; }
        public string BillStatus { get; set; } = "待缴纳";
        public DateTime GenerateTime { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaymentTime { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
        public string? ConfirmedBy { get; set; }
        public DateTime? ConfirmedTime { get; set; }
        public string? Remarks { get; set; }
        public int DaysOverdue { get; set; }
    }

    /// <summary>
    /// 租金缴纳请求DTO
    /// </summary>
    public class PayRentRequest
    {
        [Required]
        public int StoreId { get; set; }

        [Required]
        public string BillPeriod { get; set; } = string.Empty;

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public string? PaymentReference { get; set; }

        public string? Remarks { get; set; }
    }

    /// <summary>
    /// 租金确认请求DTO
    /// </summary>
    public class ConfirmPaymentRequest
    {
        [Required]
        public int StoreId { get; set; }

        [Required]
        public string BillPeriod { get; set; } = string.Empty;

        [Required]
        public string ConfirmedBy { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }

    /// <summary>
    /// 租金单查询DTO
    /// </summary>
    public class RentBillQueryRequest
    {
        public int? StoreId { get; set; }
        public string? BillPeriod { get; set; }
        public string? BillStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string OperatorAccount { get; set; } = string.Empty;
    }

    /// <summary>
    /// 店铺租金状态扩展模型 - 用于存储租金相关状态
    /// 利用STORE表的现有字段或扩展逻辑
    /// </summary>
    public class StoreRentStatus
    {
        public int StoreId { get; set; }
        public string LastPaymentPeriod { get; set; } = string.Empty;
        public DateTime? LastPaymentTime { get; set; }
        public string? LastPaymentMethod { get; set; }
        public string RentStatus { get; set; } = "正常"; // 正常、逾期、预警
        public int OverdueDays { get; set; } = 0;
        public string? Remarks { get; set; }
    }
}
