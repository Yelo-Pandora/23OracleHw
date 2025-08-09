using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oracle_backend.Models.venueEvent
{
    // 场地活动结算单
    [Table("VENUE_EVENT_BILLING")]
    public class VenueEventBilling
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BILLING_ID { get; set; }

        // 关联的活动ID
        [Required]
        public int EVENT_ID { get; set; }

        // 关联的区域ID
        [Required]
        public int AREA_ID { get; set; }

        // 关联的合作方ID
        [Required]
        public int COLLABORATION_ID { get; set; }

        // 实际租用开始时间
        [Required]
        public DateTime ACTUAL_START_TIME { get; set; }

        // 实际租用结束时间
        [Required]
        public DateTime ACTUAL_END_TIME { get; set; }

        // 租用时长（小时）
        [Required]
        public decimal RENTAL_HOURS { get; set; }

        // 场地基础费用
        [Required]
        public decimal BASE_FEE { get; set; }

        // 附加服务费用
        public decimal ADDITIONAL_FEE { get; set; } = 0;

        // 总费用
        [Required]
        public decimal TOTAL_FEE { get; set; }

        // 结算状态：PENDING, CONFIRMED, PAID, CANCELLED
        [Required]
        [StringLength(20)]
        public string BILLING_STATUS { get; set; } = "PENDING";

        // 支付方式：CASH, BANK_TRANSFER, ONLINE_PAYMENT, CREDIT_CARD
        [StringLength(30)]
        public string PAYMENT_METHOD { get; set; }

        // 支付时间
        public DateTime? PAYMENT_TIME { get; set; }

        // 开票信息（JSON格式）
        public string INVOICE_INFO { get; set; }

        // 备注
        [StringLength(500)]
        public string REMARKS { get; set; }

        // 创建时间
        [Required]
        public DateTime CREATED_TIME { get; set; } = DateTime.Now;

        // 确认时间
        public DateTime? CONFIRMED_TIME { get; set; }

        // 确认人员ID
        [StringLength(50)]
        public string CONFIRMED_BY { get; set; }

        // 外键关联
        [ForeignKey("EVENT_ID")]
        public VenueEvent VenueEventNavigation { get; set; }

        [ForeignKey("AREA_ID")]
        public EventArea EventAreaNavigation { get; set; }

        [ForeignKey("COLLABORATION_ID")]
        public Collaboration CollaborationNavigation { get; set; }
    }

    // 场地费用配置
    [Table("VENUE_FEE_CONFIG")]
    public class VenueFeeConfig
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CONFIG_ID { get; set; }

        // 区域ID
        [Required]
        public int AREA_ID { get; set; }

        // 基础场地费（每小时）
        [Required]
        public decimal HOURLY_RATE { get; set; }

        // 最低收费时长（小时）
        public decimal MIN_HOURS { get; set; } = 1;

        // 超时费率（每小时）
        public decimal OVERTIME_RATE { get; set; } = 0;

        // 节假日费率倍数
        public decimal HOLIDAY_MULTIPLIER { get; set; } = 1;

        // 周末费率倍数
        public decimal WEEKEND_MULTIPLIER { get; set; } = 1;

        // 生效时间
        [Required]
        public DateTime EFFECTIVE_DATE { get; set; }

        // 失效时间
        public DateTime? EXPIRY_DATE { get; set; }

        // 是否启用
        [Required]
        public bool IS_ACTIVE { get; set; } = true;

        // 外键关联
        [ForeignKey("AREA_ID")]
        public EventArea EventAreaNavigation { get; set; }
    }

    // 附加服务项目
    [Table("ADDITIONAL_SERVICE")]
    public class AdditionalService
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SERVICE_ID { get; set; }

        // 服务名称
        [Required]
        [StringLength(100)]
        public string SERVICE_NAME { get; set; }

        // 服务费用
        [Required]
        public decimal SERVICE_FEE { get; set; }

        // 计费单位：FIXED, HOURLY, PER_PERSON
        [Required]
        [StringLength(20)]
        public string BILLING_UNIT { get; set; }

        // 服务描述
        [StringLength(500)]
        public string DESCRIPTION { get; set; }

        // 是否启用
        [Required]
        public bool IS_ACTIVE { get; set; } = true;
    }

    // 结算单附加服务明细
    [Table("BILLING_SERVICE_DETAIL")]
    public class BillingServiceDetail
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DETAIL_ID { get; set; }

        // 关联的结算单ID
        [Required]
        public int BILLING_ID { get; set; }

        // 关联的服务ID
        [Required]
        public int SERVICE_ID { get; set; }

        // 服务数量
        [Required]
        public decimal QUANTITY { get; set; }

        // 单价
        [Required]
        public decimal UNIT_PRICE { get; set; }

        // 小计
        [Required]
        public decimal SUBTOTAL { get; set; }

        // 外键关联
        [ForeignKey("BILLING_ID")]
        public VenueEventBilling BillingNavigation { get; set; }

        [ForeignKey("SERVICE_ID")]
        public AdditionalService ServiceNavigation { get; set; }
    }
}