using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace oracle_backend.Models.Promotion
{
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

        [Required]
        public string Description { get; set; }
    }
}