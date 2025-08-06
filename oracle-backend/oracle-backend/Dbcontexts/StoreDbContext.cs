using Microsoft.EntityFrameworkCore;
using oracle_backend.Models;

namespace oracle_backend.Dbcontexts
{
    // 与商店和区域相关的数据库上下文
    public class StoreDbContext : DbContext
    {
        // 构造函数
        public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options)
        {
        }

        // 导入与商店相关的表
        public DbSet<Store> STORE { get; set; }
        public DbSet<Area> AREA { get; set; }
        public DbSet<RetailArea> RETAIL_AREA { get; set; }
        public DbSet<RentStore> RENT_STORE { get; set; }
        public DbSet<RentBill> RENT_BILL { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置RetailArea和Area的外键关系
            modelBuilder.Entity<RetailArea>()
                .HasOne(r => r.AreaNavigation)
                .WithMany()
                .HasForeignKey(r => r.AREA_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置RentStore的外键关系
            modelBuilder.Entity<RentStore>()
                .HasOne(rs => rs.storeNavigation)
                .WithMany()
                .HasForeignKey(rs => rs.STORE_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RentStore>()
                .HasOne(rs => rs.retailAreaNavigation)
                .WithMany()
                .HasForeignKey(rs => rs.AREA_ID)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // 检查指定区域是否可用（空置）
        public async Task<bool> IsAreaAvailable(int areaId)
        {
            // 需要检查AREA表的ISEMPTY字段和RETAIL_AREA表的RENT_STATUS字段
            var query = from area in AREA
                       join retailArea in RETAIL_AREA on area.AREA_ID equals retailArea.AREA_ID
                       where area.AREA_ID == areaId
                       select new { area.ISEMPTY, retailArea.RENT_STATUS };
            
            var result = await query.FirstOrDefaultAsync();
            return result != null && result.ISEMPTY == 1 && result.RENT_STATUS == "空置";
        }

        // 检查租户是否已在综合体有店铺
        public async Task<bool> TenantExists(string tenantName, string contactInfo)
        {
            var count = await STORE.CountAsync(s => s.TENANT_NAME == tenantName || s.CONTACT_INFO == contactInfo);
            return count > 0;
        }

        // 获取下一个可用的店铺ID
        public async Task<int> GetNextStoreId()
        {
            var storeCount = await STORE.CountAsync();
            if (storeCount == 0)
            {
                return 1; // 如果没有任何店铺，从1开始
            }
            
            var maxId = await STORE.MaxAsync(s => s.STORE_ID);
            return maxId + 1;
        }

        // 更新区域状态为已租用
        public async Task UpdateAreaStatus(int areaId, bool isEmpty, string rentStatus)
        {
            // 更新AREA表的ISEMPTY字段
            var area = await AREA.FirstOrDefaultAsync(a => a.AREA_ID == areaId);
            if (area != null)
            {
                area.ISEMPTY = isEmpty ? 1 : 0; // Oracle中用1/0表示布尔值
            }
            
            // 更新RETAIL_AREA表的RENT_STATUS字段
            var retailArea = await RETAIL_AREA.FirstOrDefaultAsync(a => a.AREA_ID == areaId);
            if (retailArea != null)
            {
                retailArea.RENT_STATUS = rentStatus;
            }
        }

        // 根据店铺ID获取店铺信息
        public async Task<Store?> GetStoreById(int storeId)
        {
            return await STORE.FirstOrDefaultAsync(s => s.STORE_ID == storeId);
        }

        // 检查区域ID是否已存在
        public async Task<bool> AreaIdExists(int areaId)
        {
            return await AREA.AnyAsync(a => a.AREA_ID == areaId);
        }

        // 获取所有空置的零售区域
        public async Task<List<object>> GetAvailableAreas()
        {
            var query = from area in AREA
                       join retailArea in RETAIL_AREA on area.AREA_ID equals retailArea.AREA_ID
                       where area.ISEMPTY == 1 && retailArea.RENT_STATUS == "空置"
                       select new
                       {
                           areaId = area.AREA_ID,
                           areaSize = area.AREA_SIZE,
                           baseRent = retailArea.BASE_RENT,
                           rentStatus = retailArea.RENT_STATUS,
                           isEmpty = area.ISEMPTY  // 直接返回数值，让应用层处理布尔转换
                       };
            
            return await query.Cast<object>().ToListAsync();
        }

        #region 统计报表相关方法

        /// <summary>
        /// 获取基础统计数据
        /// </summary>
        public async Task<BasicStatistics> GetBasicStatistics()
        {
            var totalStores = await STORE.CountAsync();
            var activeStores = await STORE.CountAsync(s => s.STORE_STATUS == "正常营业");
            var totalAreas = await RETAIL_AREA.CountAsync();
            var vacantAreas = await AREA.CountAsync(a => a.ISEMPTY == 1);
            
            var occupancyRate = totalAreas > 0 ? Math.Round((double)(totalAreas - vacantAreas) / totalAreas * 100, 2) : 0;
            
            var averageRent = await RETAIL_AREA
                .Where(ra => ra.RENT_STATUS != "空置")
                .AverageAsync(ra => (double?)ra.BASE_RENT) ?? 0;

            return new BasicStatistics
            {
                TotalStores = totalStores,
                ActiveStores = activeStores,
                TotalAreas = totalAreas,
                VacantAreas = vacantAreas,
                OccupancyRate = occupancyRate,
                AverageRent = (decimal)averageRent
            };
        }

        /// <summary>
        /// 按店铺类型获取统计数据
        /// </summary>
        public async Task<List<StoreTypeStatistics>> GetStoreStatisticsByType()
        {
            var query = from store in STORE
                       join rentStore in RENT_STORE on store.STORE_ID equals rentStore.STORE_ID
                       join retailArea in RETAIL_AREA on rentStore.AREA_ID equals retailArea.AREA_ID
                       group new { store, retailArea } by store.STORE_TYPE into g
                       select new StoreTypeStatistics
                       {
                           StoreType = g.Key,
                           StoreCount = g.Count(),
                           ActiveStores = g.Count(x => x.store.STORE_STATUS == "正常营业"),
                           TotalRent = (decimal)g.Sum(x => x.retailArea.BASE_RENT),
                           AverageRent = (decimal)g.Average(x => x.retailArea.BASE_RENT)
                       };

            return await query.ToListAsync();
        }

        /// <summary>
        /// 按区域获取统计数据
        /// </summary>
        public async Task<List<AreaStatistics>> GetStoreStatisticsByArea()
        {
            var query = from area in AREA
                       join retailArea in RETAIL_AREA on area.AREA_ID equals retailArea.AREA_ID
                       join rentStore in RENT_STORE on retailArea.AREA_ID equals rentStore.AREA_ID into rs
                       from rentStore in rs.DefaultIfEmpty()
                       join store in STORE on rentStore.STORE_ID equals store.STORE_ID into s
                       from store in s.DefaultIfEmpty()
                       select new
                       {
                           AreaId = area.AREA_ID,
                           AreaSize = area.AREA_SIZE ?? 0,
                           BaseRent = (decimal)retailArea.BASE_RENT,
                           RentStatus = retailArea.RENT_STATUS,
                           IsEmptyFlag = area.ISEMPTY, // 使用数字标志，稍后转换
                           StoreName = store != null ? store.STORE_NAME : null,
                           TenantName = store != null ? store.TENANT_NAME : null,
                           StoreType = store != null ? store.STORE_TYPE : null,
                           RentStart = store != null ? (DateTime?)store.RENT_START : null,
                           RentEnd = store != null ? (DateTime?)store.RENT_END : null
                       };

            var results = await query.ToListAsync();
            
            // 在应用程序层转换布尔值
            return results.Select(x => new AreaStatistics
            {
                AreaId = x.AreaId,
                AreaSize = x.AreaSize,
                BaseRent = x.BaseRent,
                RentStatus = x.RentStatus,
                IsOccupied = x.IsEmptyFlag == 0, // 在这里进行布尔转换
                StoreName = x.StoreName,
                TenantName = x.TenantName,
                StoreType = x.StoreType,
                RentStart = x.RentStart,
                RentEnd = x.RentEnd
            }).ToList();
        }

        /// <summary>
        /// 按状态获取统计数据
        /// </summary>
        public async Task<List<StoreStatusStatistics>> GetStoreStatisticsByStatus()
        {
            var query = from store in STORE
                       join rentStore in RENT_STORE on store.STORE_ID equals rentStore.STORE_ID
                       join retailArea in RETAIL_AREA on rentStore.AREA_ID equals retailArea.AREA_ID
                       group new { store, retailArea } by store.STORE_STATUS into g
                       select new StoreStatusStatistics
                       {
                           StoreStatus = g.Key,
                           StoreCount = g.Count(),
                           AverageRent = (decimal)g.Average(x => x.retailArea.BASE_RENT),
                           TotalRent = (decimal)g.Sum(x => x.retailArea.BASE_RENT)
                       };

            return await query.ToListAsync();
        }

        /// <summary>
        /// 计算总收入
        /// </summary>
        public async Task<decimal> CalculateTotalRevenue()
        {
            var totalRevenue = await (from store in STORE
                                     join rentStore in RENT_STORE on store.STORE_ID equals rentStore.STORE_ID
                                     join retailArea in RETAIL_AREA on rentStore.AREA_ID equals retailArea.AREA_ID
                                     where store.STORE_STATUS != "已退租"
                                     select retailArea.BASE_RENT).SumAsync();

            return (decimal)totalRevenue;
        }

        #endregion

        #region 租金收取相关方法

        /// <summary>
        /// 生成月度租金单
        /// </summary>
        public async Task<List<RentBill>> GenerateMonthlyRentBills(string billPeriod)
        {
            var bills = new List<RentBill>();
            
            // 获取所有正在租用的店铺
            var activeStores = await (from store in STORE
                                    join rentStore in RENT_STORE on store.STORE_ID equals rentStore.STORE_ID
                                    join retailArea in RETAIL_AREA on rentStore.AREA_ID equals retailArea.AREA_ID
                                    where store.STORE_STATUS == "正常营业" && retailArea.RENT_STATUS == "已租用"
                                    select new
                                    {
                                        StoreId = store.STORE_ID,
                                        BaseRent = retailArea.BASE_RENT,
                                        RentStart = store.RENT_START,
                                        RentEnd = store.RENT_END
                                    }).ToListAsync();

            foreach (var store in activeStores)
            {
                // 检查是否已经生成过该期间的账单
                var existingBill = await RENT_BILL.FirstOrDefaultAsync(b => 
                    b.STORE_ID == store.StoreId && b.BILL_PERIOD == billPeriod);
                
                if (existingBill == null)
                {
                    var bill = new RentBill
                    {
                        STORE_ID = store.StoreId,
                        BILL_PERIOD = billPeriod,
                        BASE_RENT = (decimal)store.BaseRent,
                        RENT_MONTHS = 1, // 默认按月计算
                        TOTAL_AMOUNT = (decimal)store.BaseRent,
                        BILL_STATUS = "待缴纳",
                        GENERATE_TIME = DateTime.Now,
                        DUE_DATE = DateTime.Now.AddDays(30), // 30天缴费期限
                        REMARKS = $"账期：{billPeriod}"
                    };
                    
                    bills.Add(bill);
                    RENT_BILL.Add(bill);
                }
            }
            
            if (bills.Any())
            {
                await SaveChangesAsync();
            }
            
            return bills;
        }

        /// <summary>
        /// 获取租金单详情
        /// </summary>
        public async Task<List<RentBillDetailResponse>> GetRentBillsDetails(RentBillQueryRequest request)
        {
            var query = from bill in RENT_BILL
                       join store in STORE on bill.STORE_ID equals store.STORE_ID
                       select new { bill, store };

            // 应用筛选条件
            if (request.StoreId.HasValue)
            {
                query = query.Where(x => x.bill.STORE_ID == request.StoreId.Value);
            }

            if (!string.IsNullOrEmpty(request.BillPeriod))
            {
                query = query.Where(x => x.bill.BILL_PERIOD == request.BillPeriod);
            }

            if (!string.IsNullOrEmpty(request.BillStatus))
            {
                query = query.Where(x => x.bill.BILL_STATUS == request.BillStatus);
            }

            if (request.StartDate.HasValue)
            {
                query = query.Where(x => x.bill.GENERATE_TIME >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(x => x.bill.GENERATE_TIME <= request.EndDate.Value);
            }

            var results = await query.ToListAsync();
            
            return results.Select(x => new RentBillDetailResponse
            {
                BillId = x.bill.BILL_ID,
                StoreId = x.bill.STORE_ID,
                StoreName = x.store.STORE_NAME,
                TenantName = x.store.TENANT_NAME,
                BillPeriod = x.bill.BILL_PERIOD,
                BaseRent = x.bill.BASE_RENT,
                RentMonths = x.bill.RENT_MONTHS,
                TotalAmount = x.bill.TOTAL_AMOUNT,
                BillStatus = x.bill.BILL_STATUS,
                GenerateTime = x.bill.GENERATE_TIME,
                DueDate = x.bill.DUE_DATE,
                PaymentTime = x.bill.PAYMENT_TIME,
                PaymentMethod = x.bill.PAYMENT_METHOD,
                PaymentReference = x.bill.PAYMENT_REFERENCE,
                ConfirmedBy = x.bill.CONFIRMED_BY,
                ConfirmedTime = x.bill.CONFIRMED_TIME,
                Remarks = x.bill.REMARKS,
                DaysOverdue = x.bill.DUE_DATE < DateTime.Now && x.bill.BILL_STATUS != "已缴纳" 
                    ? (DateTime.Now - x.bill.DUE_DATE).Days : 0
            }).ToList();
        }

        /// <summary>
        /// 处理租金支付
        /// </summary>
        public async Task<bool> ProcessRentPayment(int billId, PayRentRequest request)
        {
            var bill = await RENT_BILL.FirstOrDefaultAsync(b => b.BILL_ID == billId);
            if (bill == null || bill.BILL_STATUS == "已缴纳")
            {
                return false;
            }

            bill.BILL_STATUS = "已缴纳";
            bill.PAYMENT_TIME = DateTime.Now;
            bill.PAYMENT_METHOD = request.PaymentMethod;
            bill.PAYMENT_REFERENCE = request.PaymentReference;
            bill.REMARKS = string.IsNullOrEmpty(bill.REMARKS) 
                ? request.Remarks 
                : $"{bill.REMARKS}; 支付备注: {request.Remarks}";

            await SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 财务确认支付
        /// </summary>
        public async Task<bool> ConfirmPayment(int billId, ConfirmPaymentRequest request)
        {
            var bill = await RENT_BILL.FirstOrDefaultAsync(b => b.BILL_ID == billId);
            if (bill == null)
            {
                return false;
            }

            bill.CONFIRMED_BY = request.ConfirmedBy;
            bill.CONFIRMED_TIME = DateTime.Now;
            bill.REMARKS = string.IsNullOrEmpty(bill.REMARKS) 
                ? request.Remarks 
                : $"{bill.REMARKS}; 确认备注: {request.Remarks}";

            await SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 更新逾期状态
        /// </summary>
        public async Task<int> UpdateOverdueStatus()
        {
            var overdueBills = await RENT_BILL
                .Where(b => b.BILL_STATUS == "待缴纳" && b.DUE_DATE < DateTime.Now)
                .ToListAsync();

            int updatedCount = 0;
            foreach (var bill in overdueBills)
            {
                var daysOverdue = (DateTime.Now - bill.DUE_DATE).Days;
                
                if (daysOverdue >= 30)
                {
                    bill.BILL_STATUS = "预警";
                    bill.REMARKS = string.IsNullOrEmpty(bill.REMARKS) 
                        ? $"逾期{daysOverdue}天，触发预警" 
                        : $"{bill.REMARKS}; 逾期{daysOverdue}天，触发预警";
                }
                else
                {
                    bill.BILL_STATUS = "逾期";
                    bill.REMARKS = string.IsNullOrEmpty(bill.REMARKS) 
                        ? $"逾期{daysOverdue}天" 
                        : $"{bill.REMARKS}; 逾期{daysOverdue}天";
                }
                updatedCount++;
            }

            if (updatedCount > 0)
            {
                await SaveChangesAsync();
            }

            return updatedCount;
        }

        /// <summary>
        /// 获取租金收取统计数据
        /// </summary>
        public async Task<RentCollectionStatistics> GetRentCollectionStatistics(string period)
        {
            var bills = await RENT_BILL
                .Where(b => b.BILL_PERIOD == period)
                .ToListAsync();

            if (!bills.Any())
            {
                return new RentCollectionStatistics
                {
                    Period = period,
                    TotalBills = 0,
                    PaidBills = 0,
                    OverdueBills = 0,
                    TotalAmount = 0,
                    PaidAmount = 0,
                    OverdueAmount = 0,
                    CollectionRate = 0
                };
            }

            var totalBills = bills.Count;
            var paidBills = bills.Count(b => b.BILL_STATUS == "已缴纳");
            var overdueBills = bills.Count(b => b.BILL_STATUS == "逾期" || b.BILL_STATUS == "预警");
            var totalAmount = bills.Sum(b => b.TOTAL_AMOUNT);
            var paidAmount = bills.Where(b => b.BILL_STATUS == "已缴纳").Sum(b => b.TOTAL_AMOUNT);
            var overdueAmount = bills.Where(b => b.BILL_STATUS == "逾期" || b.BILL_STATUS == "预警").Sum(b => b.TOTAL_AMOUNT);

            return new RentCollectionStatistics
            {
                Period = period,
                TotalBills = totalBills,
                PaidBills = paidBills,
                OverdueBills = overdueBills,
                TotalAmount = totalAmount,
                PaidAmount = paidAmount,
                OverdueAmount = overdueAmount,
                CollectionRate = totalAmount > 0 ? Math.Round((double)(paidAmount / totalAmount) * 100, 2) : 0
            };
        }

        #endregion
    }
}
