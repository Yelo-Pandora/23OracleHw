using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oracle_backend.Models
{
    /// <summary>
    /// 租金单表 - 记录商户租金收取信息
    /// </summary>
    [Table("RENT_BILL")]
    public class RentBill
    {
        [Key]
        public int BILL_ID { get; set; }

        /// <summary>
        /// 店铺ID
        /// </summary>
        public int STORE_ID { get; set; }

        /// <summary>
        /// 账期年月 (格式: YYYYMM)
        /// </summary>
        public string BILL_PERIOD { get; set; } = string.Empty;

        /// <summary>
        /// 基础租金
        /// </summary>
        public decimal BASE_RENT { get; set; }

        /// <summary>
        /// 租用月数
        /// </summary>
        public int RENT_MONTHS { get; set; }

        /// <summary>
        /// 总租金金额
        /// </summary>
        public decimal TOTAL_AMOUNT { get; set; }

        /// <summary>
        /// 账单状态：待缴纳、已缴纳、逾期、预警
        /// </summary>
        public string BILL_STATUS { get; set; } = "待缴纳";

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GENERATE_TIME { get; set; }

        /// <summary>
        /// 缴费截止时间
        /// </summary>
        public DateTime DUE_DATE { get; set; }

        /// <summary>
        /// 支付时间
        /// </summary>
        public DateTime? PAYMENT_TIME { get; set; }

        /// <summary>
        /// 支付方式：现金、银行转账、支付宝、微信
        /// </summary>
        public string? PAYMENT_METHOD { get; set; }

        /// <summary>
        /// 支付流水号
        /// </summary>
        public string? PAYMENT_REFERENCE { get; set; }

        /// <summary>
        /// 确认人员账号
        /// </summary>
        public string? CONFIRMED_BY { get; set; }

        /// <summary>
        /// 确认时间
        /// </summary>
        public DateTime? CONFIRMED_TIME { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string? REMARKS { get; set; }

        // 导航属性
        [ForeignKey("STORE_ID")]
        public Store? StoreNavigation { get; set; }
    }

    /// <summary>
    /// 租金收取统计数据模型
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
    /// 租金缴纳请求DTO
    /// </summary>
    public class PayRentRequest
    {
        [Required]
        public int BillId { get; set; }

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
        public int BillId { get; set; }

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
    /// 租金单详情响应DTO
    /// </summary>
    public class RentBillDetailResponse
    {
        public int BillId { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string BillPeriod { get; set; } = string.Empty;
        public decimal BaseRent { get; set; }
        public int RentMonths { get; set; }
        public decimal TotalAmount { get; set; }
        public string BillStatus { get; set; } = string.Empty;
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
}
