using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models.Promotion;

namespace oracle_backend.Services
{
    public class PromotionService
    {
        private readonly PromotionDbContext _context;
        private readonly ILogger<PromotionService> _logger;

        public PromotionService(PromotionDbContext context, ILogger<PromotionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Promotion> CreatePromotionAsync(PromotionDto dto)
        {
            // 验证时间
            if (dto.StartTime >= dto.EndTime)
                throw new ArgumentException("End time must be later than start time");

            var promotion = new Promotion
            {
                PromotionName = dto.PromotionName,
                PromotionCost = dto.PromotionCost,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                PromotionRules = JsonSerializer.Serialize(dto.PromotionRules),
                ShopIds = JsonSerializer.Serialize(dto.ShopIds),
                Status = "PENDING"
            };

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created promotion: {promotion.PromotionId}");
            return promotion;
        }

        public async Task<Promotion> UpdatePromotionAsync(string id, PromotionDto dto)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
                throw new KeyNotFoundException("Promotion not found");

            // 防止修改已开始的活动
            if (promotion.Status == "ACTIVE" &&
                (dto.ShopIds != null || dto.PromotionRules != null))
            {
                throw new InvalidOperationException("Cannot modify rules or shops for active promotion");
            }

            // 更新字段
            if (!string.IsNullOrEmpty(dto.PromotionName))
                promotion.PromotionName = dto.PromotionName;

            if (dto.PromotionCost > 0)
                promotion.PromotionCost = dto.PromotionCost;

            if (dto.ShopIds != null)
                promotion.ShopIds = JsonSerializer.Serialize(dto.ShopIds);

            if (dto.PromotionRules != null)
                promotion.PromotionRules = JsonSerializer.Serialize(dto.PromotionRules);

            if (dto.StartTime != default)
                promotion.StartTime = dto.StartTime;

            if (dto.EndTime != default)
                promotion.EndTime = dto.EndTime;

            await _context.SaveChangesAsync();
            return promotion;
        }

        public async Task<bool> DeletePromotionAsync(string id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return false;

            // 活动进行中禁止删除
            if (promotion.Status == "ACTIVE")
                throw new InvalidOperationException("Cannot delete active promotion");

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Promotion> GetPromotionAsync(string id)
        {
            return await _context.Promotions.FindAsync(id);
        }

        public async Task<List<Promotion>> GetAllPromotionsAsync()
        {
            return await _context.Promotions.ToListAsync();
        }

        public async Task<PromotionReport> GenerateReportAsync(string promotionId)
        {
            var promotion = await _context.Promotions.FindAsync(promotionId);
            if (promotion == null)
                throw new KeyNotFoundException("Promotion not found");

            // 模拟销售
            var reportData = await FetchSalesDataFromExternalSystem(promotion);

            return new PromotionReport
            {
                PromotionId = promotion.PromotionId,
                PromotionName = promotion.PromotionName,
                ShopCount = JsonSerializer.Deserialize<List<string>>(promotion.ShopIds).Count,
                SalesIncrement = reportData.SalesIncrement,
                PromotionCost = promotion.PromotionCost,
                ROI = CalculateROI(reportData.SalesIncrement, promotion.PromotionCost),
                CouponRedemptionRate = reportData.CouponRedemptionRate
            };
        }

        private decimal CalculateROI(decimal salesIncrement, decimal cost)
        {
            return cost == 0 ? 0 : (salesIncrement - cost) / cost;
        }

        private async Task<(decimal SalesIncrement, decimal CouponRedemptionRate)>
            FetchSalesDataFromExternalSystem(Promotion promotion)
        {
            // 模拟外部API调用
            await Task.Delay(100);

            // 模拟销售
            return (
                SalesIncrement: new Random().Next(1000, 10000),
                CouponRedemptionRate: new Random().Next(50, 95) / 100m
            );
        }
    }
}