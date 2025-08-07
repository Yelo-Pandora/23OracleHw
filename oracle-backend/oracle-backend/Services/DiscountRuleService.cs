using System;
using System.Collections.Generic;
using System.Text.Json;
using oracle_backend.Models.Promotion;

namespace oracle_backend.Services
{
    public class DiscountRuleService
    {
        public decimal ApplyDiscount(decimal originalPrice, string ruleJson)
        {
            var rule = JsonSerializer.Deserialize<Dictionary<string, object>>(ruleJson);

            if (!rule.ContainsKey("type"))
                throw new ArgumentException("Invalid discount rule");

            return rule["type"].ToString() switch
            {
                "FULL_REDUCTION" => ApplyFullReduction(originalPrice, rule),
                "PERCENT_DISCOUNT" => ApplyPercentDiscount(originalPrice, rule),
                _ => originalPrice
            };
        }

        private decimal ApplyFullReduction(decimal price, Dictionary<string, object> rule)
        {
            if (!rule.ContainsKey("threshold") || !rule.ContainsKey("discountValue"))
                throw new ArgumentException("Invd full reduction rule");
            decimal threshold = Convert.ToDecimal(rule["threshold"]);
            decimal discountValue = Convert.ToDecimal(rule["discountValue"]);

            return price >= threshold ? price - discountValue : price;
        }

        private decimal ApplyPercentDiscount(decimal price, Dictionary<string, object> rule)
        {
            if (!rule.ContainsKey("discountValue"))
                throw new ArgumentException("Invalid percent discount rule");

            decimal discountValue = Convert.ToDecimal(rule["discountValue"]);

            if (discountValue <= 0 || discountValue > 1)
                throw new ArgumentException("Discount value must be between 0 and 1");

            return price * discountValue;
        }
    }
}