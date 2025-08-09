using System;
using System.ComponentModel.DataAnnotations;

namespace oracle_backend.Models.venueEvent
{
    // 场地预约申请DTO
    public class VenueReservationDto
    {
        [Required]
        public int CollaborationId { get; set; }

        [Required]
        public int AreaId { get; set; }

        [Required]
        public DateTime RentStart { get; set; }

        [Required]
        public DateTime RentEnd { get; set; }

        [Required]
        [StringLength(200)]
        public string Purpose { get; set; }

        public double Funding { get; set; }
    }

    // 场地活动DTO
    public class VenueEventDto
    {
        [Required]
        [StringLength(100)]
        public string EventName { get; set; }

        public int? HeadCount { get; set; }

        public double Fee { get; set; }

        [Required]
        public int Capacity { get; set; }

        public int Expense { get; set; }

        [Required]
        public DateTime EventStart { get; set; }

        public DateTime? EventEnd { get; set; }

        [StringLength(20)]
        public string ActivityStatus { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }
    }

    // 场地活动更新DTO
    public class VenueEventUpdateDto
    {
        [StringLength(100)]
        public string EventName { get; set; }

        public int? HeadCount { get; set; }

        [StringLength(20)]
        public string ActivityStatus { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public string OperatorId { get; set; }
    }

    // 场地预约审批DTO
    public class VenueReservationApprovalDto
    {
        [Required]
        public int EventId { get; set; }

        [Required]
        public int AreaId { get; set; }

        [Required]
        public int CollaborationId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } // PENDING, APPROVED, REJECTED

        public string RejectReason { get; set; }
    }

    // 场地活动查询响应DTO
    public class VenueEventResponseDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; }
        public int? HeadCount { get; set; }
        public double Fee { get; set; }
        public int Capacity { get; set; }
        public int Expense { get; set; }
        public DateTime EventStart { get; set; }
        public DateTime? EventEnd { get; set; }

        // 预约详情
        public int AreaId { get; set; }
        public int CollaborationId { get; set; }
        public DateTime RentStart { get; set; }
        public DateTime RentEnd { get; set; }
        public string Status { get; set; }
        public double Funding { get; set; }

        // 关联信息
        public string CollaborationName { get; set; }
        public string AreaInfo { get; set; }
    }

    // 结算单生成请求DTO
    public class CreateBillingDto
    {
        [Required]
        public int EventId { get; set; }

        [Required]
        public DateTime ActualStartTime { get; set; }

        [Required]
        public DateTime ActualEndTime { get; set; }

        public List<AdditionalServiceItemDto> AdditionalServices { get; set; } = new List<AdditionalServiceItemDto>();

        [StringLength(500)]
        public string Remarks { get; set; }

        public string OperatorId { get; set; }
    }

    // 附加服务项目DTO
    public class AdditionalServiceItemDto
    {
        [Required]
        public int ServiceId { get; set; }

        [Required]
        public decimal Quantity { get; set; }
    }

    // 结算单确认DTO
    public class ConfirmBillingDto
    {
        [Required]
        public int BillingId { get; set; }

        [Required]
        [StringLength(30)]
        public string PaymentMethod { get; set; }

        public InvoiceInfoDto InvoiceInfo { get; set; }

        [StringLength(500)]
        public string Remarks { get; set; }

        [Required]
        public string ConfirmedBy { get; set; }
    }

    // 支付确认DTO
    public class PaymentConfirmationDto
    {
        [Required]
        public int BillingId { get; set; }

        [Required]
        public DateTime PaymentTime { get; set; }

        [StringLength(100)]
        public string TransactionId { get; set; }

        [StringLength(500)]
        public string PaymentRemarks { get; set; }

        public string ConfirmedBy { get; set; }
    }

    // 开票信息DTO
    public class InvoiceInfoDto
    {
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; }

        [Required]
        [StringLength(50)]
        public string TaxNumber { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(50)]
        public string Phone { get; set; }

        [StringLength(100)]
        public string BankName { get; set; }

        [StringLength(50)]
        public string BankAccount { get; set; }

        [StringLength(20)]
        public string InvoiceType { get; set; } = "NORMAL"; // NORMAL, SPECIAL
    }

    // 结算单响应DTO
    public class BillingResponseDto
    {
        public int BillingId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; }
        public int AreaId { get; set; }
        public int CollaborationId { get; set; }
        public string CollaborationName { get; set; }
        public DateTime ActualStartTime { get; set; }
        public DateTime ActualEndTime { get; set; }
        public decimal RentalHours { get; set; }
        public decimal BaseFee { get; set; }
        public decimal AdditionalFee { get; set; }
        public decimal TotalFee { get; set; }
        public string BillingStatus { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime? PaymentTime { get; set; }
        public InvoiceInfoDto InvoiceInfo { get; set; }
        public string Remarks { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? ConfirmedTime { get; set; }
        public string ConfirmedBy { get; set; }
        public List<BillingServiceDetailDto> ServiceDetails { get; set; } = new List<BillingServiceDetailDto>();
    }

    // 结算服务明细DTO
    public class BillingServiceDetailDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public string BillingUnit { get; set; }
    }

    // 统计报表生成请求DTO
    public class GenerateReportDto
    {
        [Required]
        [StringLength(20)]
        public string ReportType { get; set; } // DAILY, WEEKLY, MONTHLY, CUSTOM

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public int? AreaId { get; set; }

        public int? CollaborationId { get; set; }

        [StringLength(200)]
        public string ReportTitle { get; set; }

        public string GeneratedBy { get; set; }

        // 导出格式：PDF, EXCEL, JSON
        [StringLength(10)]
        public string ExportFormat { get; set; } = "JSON";
    }

    // 统计报表响应DTO
    public class VenueReportResponseDto
    {
        public int ReportId { get; set; }
        public string ReportType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ReportTitle { get; set; }
        public int? AreaId { get; set; }
        public int? CollaborationId { get; set; }

        // 基本统计指标
        public int TotalEvents { get; set; }
        public decimal TotalRentalHours { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageAttendance { get; set; }
        public decimal AverageOccupancyRate { get; set; }

        public DateTime GeneratedTime { get; set; }
        public string GeneratedBy { get; set; }
        public string Status { get; set; }
    }
}