using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace oracle_backend.Models.Promotion
{
    public class PromotionReport
    {
        public string PromotionId { get; set; }
        public string PromotionName { get; set; }
        public int ShopCount { get; set; }
        public decimal SalesIncrement { get; set; }
        public decimal PromotionCost { get; set; }
        public decimal ROI { get; set; }
        public decimal CouponRedemptionRate { get; set; }
    }
}