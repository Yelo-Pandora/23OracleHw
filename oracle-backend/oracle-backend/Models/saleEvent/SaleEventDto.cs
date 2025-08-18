using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace oracle_backend.Models.Promotion
{
    public class SaleEventDto
    {
        public string PromotionName { get; set; }
        public decimal PromotionCost { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> ShopIds { get; set; }
        public Dictionary<string, object> PromotionRules { get; set; }
    }
}