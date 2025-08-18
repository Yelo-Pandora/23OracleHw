using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using Microsoft.Extensions.Logging;

namespace oracle_backend.Services
{
    public class SaleEventService
    {
        private readonly SaleEventDbContext _context;
        private readonly ILogger<SaleEventService> _logger;

        public SaleEventService(SaleEventDbContext context, ILogger<SaleEventService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SaleEvent> CreateSaleEventAsync(SaleEventDto dto)
        {
            // 验证时间
            if (dto.StartTime >= dto.EndTime)
                throw new ArgumentException("结束时间必须晚于开始时间");

            var saleEvent = new SaleEvent
            {
                EventName = dto.EventName,
                Cost = dto.Cost,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Description = dto.Description,
                ShopIds = dto.ShopIds,
                PromotionRules = dto.PromotionRules
            };

            _context.SaleEvents.Add(saleEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"创建促销活动: {saleEvent.EventId}");
            return saleEvent;
        }

        public async Task<SaleEvent> UpdateSaleEventAsync(string id, SaleEventDto dto)
        {
            var saleEvent = await _context.SaleEvents.FindAsync(id);
            if (saleEvent == null)
                throw new KeyNotFoundException("促销活动不存在");

            // 防止修改已开始的活动
            if (saleEvent.Status == "ACTIVE" &&
                (dto.ShopIds != null || dto.PromotionRules != null))
            {
                throw new InvalidOperationException("活动进行中，禁止修改规则或店铺列表");
            }

            // 更新字段
            if (!string.IsNullOrEmpty(dto.EventName))
                saleEvent.EventName = dto.EventName;

            if (dto.Cost > 0)
                saleEvent.Cost = dto.Cost;

            if (!string.IsNullOrEmpty(dto.Description))
                saleEvent.Description = dto.Description;

            if (dto.ShopIds != null)
                saleEvent.ShopIds = dto.ShopIds;

            if (dto.PromotionRules != null)
                saleEvent.PromotionRules = dto.PromotionRules;

            if (dto.StartTime != default)
                saleEvent.StartTime = dto.StartTime;

            if (dto.EndTime != default)
                saleEvent.EndTime = dto.EndTime;

            await _context.SaveChangesAsync();
            return saleEvent;
        }

        public async Task<bool> DeleteSaleEventAsync(string id)
        {
            var saleEvent = await _context.SaleEvents.FindAsync(id);
            if (saleEvent == null) return false;

            // 活动进行中禁止删除
            if (saleEvent.Status == "ACTIVE")
                throw new InvalidOperationException("活动进行中，禁止删除");

            _context.SaleEvents.Remove(saleEvent);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SaleEvent> GetSaleEventAsync(string id)
        {
            return await _context.SaleEvents.FindAsync(id);
        }

        public async Task<List<SaleEvent>> GetAllSaleEventsAsync()
        {
            return await _context.SaleEvents.ToListAsync();
        }

        public async Task<SaleEventReport> GenerateSaleEventReportAsync(string eventId)
        {
            var saleEvent = await _context.SaleEvents.FindAsync(eventId);
            if (saleEvent == null)
                throw new KeyNotFoundException("促销活动不存在");

            // 销售
            var reportData = await FetchSalesDataFromExternalSystem(saleEvent);

            return new SaleEventReport
            {
                EventId = saleEvent.EventId,
                EventName = saleEvent.EventName,
                ShopCount = saleEvent.ShopIds.Count,
                SalesIncrement = reportData.SalesIncrement,
                Cost = saleEvent.Cost,
                ROI = CalculateROI(reportData.SalesIncrement, saleEvent.Cost),
                CouponRedemptionRate = reportData.CouponRedemptionRate
            };
        }

        private double CalculateROI(double salesIncrement, double cost)
        {
            return cost == 0 ? 0 : (salesIncrement - cost) / cost;
        }

        private async Task<(double SalesIncrement, double CouponRedemptionRate)>
            FetchSalesDataFromExternalSystem(SaleEvent saleEvent)
        {
            // 模拟调用
            await Task.Delay(100);

            // 销售
            return (
                SalesIncrement: new Random().Next(1000, 10000),
                CouponRedemptionRate: new Random().Next(50, 95) / 100.0
            );
        }
    }
}