using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace oracle_backend.Models
{
    [Table("SALE_EVENT")]
    public class SaleEvent : Event
    {
        [Required]
        [Column("COST")]
        public double Cost { get; set; }

        [Column("DESCRIPTION")]
        public string Description { get; set; }
    }

    public class SaleEventDto
    {
        [Required]
        public string EventName { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public double Cost { get; set; }

        [Required]
        public DateTime EventStart { get; set; }

        [Required]
        public DateTime EventEnd { get; set; }

        public string Description { get; set; }

    }

    public class SaleEventReport
    {
        public int EventId { get; set; }
        public string EventName { get; set; }
        public int ShopCount { get; set; }
        public double SalesIncrement { get; set; }
        public double Cost { get; set; }
        public double ROI { get; set; }
        public double CouponRedemptionRate { get; set; }
    }
}