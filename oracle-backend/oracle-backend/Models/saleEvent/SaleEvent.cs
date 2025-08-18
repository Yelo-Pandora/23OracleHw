using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace oracle_backend.Models
{
    [Table("SALE_EVENT")]
    public class SaleEvent
    {
        [Key]
        [Column("EVENT_ID")]
        [StringLength(20)]
        public string EventId { get; set; } = $"PROMO-{DateTime.Now:yyyyMMddHHmmss}";

        [Required]
        [Column("EVENT_NAME")]
        [StringLength(100)]
        public string EventName { get; set; }

        [Required]
        [Column("COST")]
        public double Cost { get; set; }

        [Required]
        [Column("START_TIME")]
        public DateTime StartTime { get; set; }

        [Required]
        [Column("END_TIME")]
        public DateTime EndTime { get; set; }

        [Column("DESCRIPTION")]
        public string Description { get; set; }

        [Required]
        [Column("SHOP_IDS")]
        public string ShopIdsJson { get; set; }  // JSON序列化存储

        [Required]
        [Column("PROMOTION_RULES")]
        public string PromotionRulesJson { get; set; }  // JSON序列化存储

        [Column("STATUS")]
        [StringLength(20)]
        public string Status { get; set; } = "PENDING"; // PENDING, ACTIVE, STOPPED, COMPLETED

        // 非映射属性，用于业务逻辑
        [NotMapped]
        public List<string> ShopIds
        {
            get => JsonSerializer.Deserialize<List<string>>(ShopIdsJson);
            set => ShopIdsJson = JsonSerializer.Serialize(value);
        }

        [NotMapped]
        public Dictionary<string, object> PromotionRules
        {
            get => JsonSerializer.Deserialize<Dictionary<string, object>>(PromotionRulesJson);
            set => PromotionRulesJson = JsonSerializer.Serialize(value);
        }
    }

    public class SaleEventDto
    {
        [Required]
        public string EventName { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public double Cost { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public string Description { get; set; }

        [Required]
        public List<string> ShopIds { get; set; }

        [Required]
        public Dictionary<string, object> PromotionRules { get; set; }
    }

    public class SaleEventReport
    {
        public string EventId { get; set; }
        public string EventName { get; set; }
        public int ShopCount { get; set; }
        public double SalesIncrement { get; set; }
        public double Cost { get; set; }
        public double ROI { get; set; }
        public double CouponRedemptionRate { get; set; }
    }
}