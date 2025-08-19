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
    public interface ISaleEventService
    {
        Task AddStoreToEventAsync(int eventId, int storeId);
        Task RemoveStoreFromEventAsync(int eventId, int storeId);
        Task<List<Store>> GetStoresByEventAsync(int eventId);
        Task<List<SaleEvent>> GetEventsByStoreAsync(int storeId);
    }
    public class SaleEventService : ISaleEventService
    {
        private readonly SaleEventDbContext _context;
        private readonly ILogger<SaleEventService> _logger;
        public SaleEventService(SaleEventDbContext context)
        {
            _context = context;
        }

        public async Task<List<SaleEvent>> GetAllSaleEventsAsync()
        {
            try
            {
                _logger.LogInformation("开始获取所有促销活动");

                // 直接使用 Set<SaleEvent>()，不需要 Include
                var result = await _context.Set<SaleEvent>().ToListAsync();

                _logger.LogInformation($"成功获取 {result.Count} 条促销活动记录");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取促销活动列表时出错: {ex.Message}");
                throw;
            }
        }

        public SaleEventService(SaleEventDbContext context, ILogger<SaleEventService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SaleEvent> CreateSaleEventAsync(SaleEventDto dto)
        {
            if (dto.EventStart > dto.EventEnd)
                throw new ArgumentException("结束时间必须晚于开始时间");

            // 获取当前最大ID
            int? maxId = await _context.SaleEvents
                .MaxAsync(e => (int?)e.EVENT_ID);

            int newId = (maxId ?? 0) + 1;

            var saleEvent = new SaleEvent
            {
                EVENT_ID = newId, // 手动设置ID
                EVENT_NAME = dto.EventName,
                Cost = dto.Cost,
                EVENT_START = dto.EventStart,
                EVENT_END = dto.EventEnd,
                Description = dto.Description,
            };

            _context.SaleEvents.Add(saleEvent);
            await _context.SaveChangesAsync();

            return saleEvent;
        }

        public async Task<SaleEvent> UpdateSaleEventAsync(int id, SaleEventDto dto)
        {
            var saleEvent = await _context.SaleEvents.FindAsync(id);
            if (saleEvent == null)
                throw new KeyNotFoundException("促销活动不存在");

            // 更新字段
            if (!string.IsNullOrEmpty(dto.EventName))
                saleEvent.EVENT_NAME = dto.EventName;

            if (dto.Cost > 0)
                saleEvent.Cost = dto.Cost;

            if (!string.IsNullOrEmpty(dto.Description))
                saleEvent.Description = dto.Description;

            if (dto.EventStart != default)
                saleEvent.EVENT_START = dto.EventStart;

            if (dto.EventEnd != default)
                saleEvent.EVENT_END = dto.EventEnd;

            await _context.SaveChangesAsync();
            return saleEvent;
        }

        public async Task<bool> DeleteSaleEventAsync(int id)
        {
            var saleEvent = await _context.SaleEvents.FindAsync(id);
            if (saleEvent == null) return false;

            _context.SaleEvents.Remove(saleEvent);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SaleEvent> GetSaleEventAsync(int id)
        {
            return await _context.SaleEvents.FindAsync(id);
        }

        public async Task<SaleEventReport> GenerateSaleEventReportAsync(int eventId)
        {
            var saleEvent = await _context.SaleEvents.FindAsync(eventId);
            if (saleEvent == null)
                throw new KeyNotFoundException("促销活动不存在");

            // 销售
            var reportData = await FetchSalesDataFromExternalSystem(saleEvent);

            return new SaleEventReport
            {
                EventId = saleEvent.EVENT_ID,
                EventName = saleEvent.EVENT_NAME,
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

        public async Task AddStoreToEventAsync(int eventId, int storeId)
        {
            // 检查是否已存在关联
            var existing = await _context.PartStores
                .FirstOrDefaultAsync(ps => ps.EVENT_ID == eventId && ps.STORE_ID == storeId);

            if (existing != null)
                throw new Exception("商铺已参与该活动");

            var partStore = new PartStore
            {
                EVENT_ID = eventId,
                STORE_ID = storeId
            };

            _context.PartStores.Add(partStore);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveStoreFromEventAsync(int eventId, int storeId)
        {
            var partStore = await _context.PartStores
                .FirstOrDefaultAsync(ps => ps.EVENT_ID == eventId && ps.STORE_ID == storeId);

            if (partStore == null)
                throw new Exception("未找到关联记录");

            _context.PartStores.Remove(partStore);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Store>> GetStoresByEventAsync(int eventId)
        {
            return await _context.PartStores
                .Where(ps => ps.EVENT_ID == eventId)
                .Join(_context.Stores,
                    ps => ps.STORE_ID,
                    s => s.STORE_ID,
                    (ps, s) => s)
                .ToListAsync();
        }

        public async Task<List<SaleEvent>> GetEventsByStoreAsync(int storeId)
        {
            return await _context.PartStores
                .Where(ps => ps.STORE_ID == storeId)
                .Join(_context.SaleEvents,
                    ps => ps.EVENT_ID,
                    se => se.EVENT_ID,
                    (ps, se) => se)
                .ToListAsync();
        }
    }
}