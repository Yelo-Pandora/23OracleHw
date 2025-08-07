using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace oracle_backend.Models.Promotion
{
    public class Promotion
    {
        [Key]
        [StringLength(20)]
        public string PromotionId { get; set; } = $"PROMO-{DateTime.Now:yyyyMMddHHmmss}";

        [Required]
        [StringLength(100)]
        public string PromotionName { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PromotionCost { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        public string PromotionRules { get; set; } // JSON序列化存储

        [Required]
        public string ShopIds { get; set; } // JSON序列化存储

        [StringLength(20)]
        public string Status { get; set; } = "PENDING"; // PENDING, ACTIVE, STOPPED, COMPLETED
    }
}