using System;
using System.Collections.Generic;
using oracle_backend.Models;

namespace oracle_backend.Services
{
    public class DiscountRuleService
    {
        public double ApplyDiscount(double originalPrice, Dictionary<string, object> rule)
        {
            if (!rule.ContainsKey("type"))
                throw new ArgumentException("无效的折扣规则");

            return rule["type"].ToString() switch
            {
                "FULL_REDUCTION" => ApplyFullReduction(originalPrice, rule),
                "PERCENT_DISCOUNT" => ApplyPercentDiscount(originalPrice, rule),
                _ => originalPrice
            };
        }

        private double ApplyFullReduction(double price, Dictionary<string, object> rule)
        {
            if (!rule.ContainsKey("threshold") || !rule.ContainsKey("discountValue"))
                throw new ArgumentException("无效的满减规则");

            double threshold = Convert.ToDouble(rule["threshold"]);
            double discountValue = Convert.ToDouble(rule["discountValue"]);

            return price >= threshold ? price - discountValue : price;
        }

        private double ApplyPercentDiscount(double price, Dictionary<string, object> rule)
        {
            if (!rule.ContainsKey("discountValue"))
                throw new ArgumentException("无效的折扣规则");

            double discountValue = Convert.ToDouble(rule["discountValue"]);

            if (discountValue <= 0 || discountValue > 1)
                throw new ArgumentException("折扣值必须在0-1之间");

            return price * discountValue;
        }
    }
}