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
        // 注意：移除RENT_BILL表依赖，使用现有表结构模拟租金管理
        // public DbSet<RentBill> RENT_BILL { get; set; }

        // 内存中存储支付状态（生产环境中应使用数据库或缓存）
        private static readonly Dictionary<string, PaymentRecord> _paymentRecords = new Dictionary<string, PaymentRecord>();
        
        // 支付记录内部类
        private class PaymentRecord
        {
            public int StoreId { get; set; }
            public string BillPeriod { get; set; } = string.Empty;
            public string Status { get; set; } = "待缴纳";
            public DateTime? PaymentTime { get; set; }
            public string? PaymentMethod { get; set; }
            public string? PaymentReference { get; set; }
            public string? Remarks { get; set; }
            public bool IsConfirmed { get; set; }
            public string? ConfirmedBy { get; set; }
            public DateTime? ConfirmedTime { get; set; }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //// 配置RetailArea和Area的外键关系
            //modelBuilder.Entity<RetailArea>()
            //    .HasOne(r => r.AreaNavigation)
            //    .WithMany()
            //    .HasForeignKey(r => r.AREA_ID)
            //    .OnDelete(DeleteBehavior.Restrict);

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

        #region 租金收取功能 - 基于现有表结构模拟实现

        /// <summary>
        /// 生成月度租金单 - 基于现有店铺和区域信息虚拟生成
        /// </summary>
        public async Task<List<VirtualRentBill>> GenerateMonthlyRentBills(string billPeriod)
        {
            var bills = new List<VirtualRentBill>();

            // 从现有的店铺和区域关联中生成虚拟租金单
            var storeRentData = from store in STORE
                               join rentStore in RENT_STORE on store.STORE_ID equals rentStore.STORE_ID
                               join retailArea in RETAIL_AREA on rentStore.AREA_ID equals retailArea.AREA_ID
                               where store.STORE_STATUS == "正常营业"
                               select new
                               {
                                   store.STORE_ID,
                                   store.STORE_NAME,
                                   store.TENANT_NAME,
                                   retailArea.BASE_RENT
                               };

            var results = await storeRentData.ToListAsync();

            foreach (var item in results)
            {
                // 生成虚拟租金单
                var bill = new VirtualRentBill
                {
                    StoreId = item.STORE_ID,
                    StoreName = item.STORE_NAME,
                    TenantName = item.TENANT_NAME,
                    BillPeriod = billPeriod,
                    BaseRent = (decimal)item.BASE_RENT,
                    RentMonths = 1,
                    TotalAmount = (decimal)item.BASE_RENT,
                    BillStatus = "待缴纳",
                    GenerateTime = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(30),
                    Remarks = $"账期：{billPeriod}"
                };

                bills.Add(bill);
            }

            return bills;
        }

        /// <summary>
        /// 查询租金单详情 - 基于现有数据虚拟生成
        /// </summary>
        public async Task<List<VirtualRentBill>> GetRentBillsDetails(RentBillQueryRequest request)
        {
            var query = from store in STORE
                       join rentStore in RENT_STORE on store.STORE_ID equals rentStore.STORE_ID
                       join retailArea in RETAIL_AREA on rentStore.AREA_ID equals retailArea.AREA_ID
                       where store.STORE_STATUS == "正常营业"
                       select new
                       {
                           store.STORE_ID,
                           store.STORE_NAME,
                           store.TENANT_NAME,
                           retailArea.BASE_RENT
                       };

            // 根据查询条件筛选
            if (request.StoreId.HasValue)
            {
                query = query.Where(x => x.STORE_ID == request.StoreId.Value);
            }

            var results = await query.ToListAsync();
            var bills = new List<VirtualRentBill>();

            foreach (var item in results)
            {
                // 生成多个账期的虚拟账单用于演示
                var periods = new List<(string period, string defaultStatus, DateTime? defaultPayTime, string? defaultPayMethod)>
                {
                    ("202412", "待缴纳", null, null),
                    ("202411", "已缴纳", DateTime.Now.AddDays(-15), "银行转账"),
                    ("202410", "逾期", null, null),
                    ("202409", "预警", null, null)
                };

                foreach (var (period, defaultStatus, defaultPayTime, defaultPayMethod) in periods)
                {
                    // 如果指定了账期，只返回该账期的数据
                    if (!string.IsNullOrEmpty(request.BillPeriod) && period != request.BillPeriod)
                        continue;

                    // 检查是否有存储的支付记录
                    var key = $"{item.STORE_ID}_{period}";
                    var actualStatus = defaultStatus;
                    var actualPayTime = defaultPayTime;
                    var actualPayMethod = defaultPayMethod;
                    
                    if (_paymentRecords.TryGetValue(key, out var paymentRecord))
                    {
                        actualStatus = paymentRecord.Status;
                        actualPayTime = paymentRecord.PaymentTime;
                        actualPayMethod = paymentRecord.PaymentMethod;
                    }

                    // 如果指定了状态，只返回该状态的数据  
                    if (!string.IsNullOrEmpty(request.BillStatus) && actualStatus != request.BillStatus)
                        continue;

                    var dueDate = DateTime.ParseExact(period + "01", "yyyyMMdd", null).AddDays(30);
                    var daysOverdue = actualStatus == "逾期" || actualStatus == "预警" ? 
                        Math.Max(0, (DateTime.Now - dueDate).Days) : 0;

                    var bill = new VirtualRentBill
                    {
                        StoreId = item.STORE_ID,
                        StoreName = item.STORE_NAME,
                        TenantName = item.TENANT_NAME,
                        BillPeriod = period,
                        BaseRent = (decimal)item.BASE_RENT,
                        RentMonths = 1,
                        TotalAmount = (decimal)item.BASE_RENT,
                        BillStatus = actualStatus,
                        GenerateTime = DateTime.ParseExact(period + "01", "yyyyMMdd", null),
                        DueDate = dueDate,
                        PaymentTime = actualPayTime,
                        PaymentMethod = actualPayMethod,
                        ConfirmedBy = actualPayTime.HasValue ? "finance_admin" : null,
                        ConfirmedTime = actualPayTime?.AddHours(1),
                        Remarks = $"{period.Substring(0, 4)}年{period.Substring(4, 2)}月租金单" + 
                                 (actualStatus == "已缴纳" ? "，已按时缴纳" : 
                                  actualStatus == "逾期" ? "，逾期未缴" : 
                                  actualStatus == "预警" ? "，逾期超过30天，触发预警" : ""),
                        DaysOverdue = daysOverdue
                    };

                    bills.Add(bill);
                }
            }

            return bills.OrderByDescending(b => b.BillPeriod).ThenBy(b => b.StoreId).ToList();
        }

        /// <summary>
        /// 处理租金支付 - 模拟支付处理逻辑
        /// </summary>
        public async Task<bool> ProcessRentPayment(PayRentRequest request)
        {
            // 检查店铺是否存在
            var store = await STORE.FirstOrDefaultAsync(s => s.STORE_ID == request.StoreId);
            if (store == null)
                throw new InvalidOperationException($"店铺ID {request.StoreId} 不存在");

            // 生成支付记录的唯一键
            var key = $"{request.StoreId}_{request.BillPeriod}";
            
            // 存储支付记录到内存中
            _paymentRecords[key] = new PaymentRecord
            {
                StoreId = request.StoreId,
                BillPeriod = request.BillPeriod,
                Status = "已缴纳",
                PaymentTime = DateTime.Now,
                PaymentMethod = request.PaymentMethod,
                PaymentReference = request.PaymentReference,
                Remarks = request.Remarks,
                IsConfirmed = false
            };

            return true;
        }

        /// <summary>
        /// 确认支付 - 模拟财务确认逻辑
        /// </summary>
        public async Task<bool> ConfirmPayment(ConfirmPaymentRequest request)
        {
            // 检查店铺是否存在
            var store = await STORE.FirstOrDefaultAsync(s => s.STORE_ID == request.StoreId);
            if (store == null)
                throw new InvalidOperationException($"店铺ID {request.StoreId} 不存在");

            // 生成支付记录的唯一键
            var key = $"{request.StoreId}_{request.BillPeriod}";
            
            // 检查支付记录是否存在
            if (_paymentRecords.TryGetValue(key, out var record))
            {
                // 更新确认状态
                record.IsConfirmed = true;
                record.ConfirmedBy = request.ConfirmedBy;
                record.ConfirmedTime = DateTime.Now;
                if (!string.IsNullOrEmpty(request.Remarks))
                {
                    record.Remarks = (record.Remarks ?? "") + " | 财务确认: " + request.Remarks;
                }
            }
            else
            {
                // 如果没有支付记录，创建一个确认记录
                _paymentRecords[key] = new PaymentRecord
                {
                    StoreId = request.StoreId,
                    BillPeriod = request.BillPeriod,
                    Status = "已缴纳",
                    PaymentTime = DateTime.Now,
                    PaymentMethod = "现金",
                    IsConfirmed = true,
                    ConfirmedBy = request.ConfirmedBy,
                    ConfirmedTime = DateTime.Now,
                    Remarks = request.Remarks
                };
            }

            return true;
        }

        /// <summary>
        /// 更新逾期状态 - 基于虚拟逻辑模拟
        /// </summary>
        public async Task<int> UpdateOverdueStatus()
        {
            // 模拟更新逾期状态，返回受影响的记录数
            var activeStores = await STORE.CountAsync(s => s.STORE_STATUS == "正常营业");
            
            // 假设有一定比例的店铺可能逾期
            var overdueCount = activeStores / 5; // 假设20%可能逾期
            
            return overdueCount;
        }

        /// <summary>
        /// 获取租金收取统计数据 - 基于虚拟数据计算
        /// </summary>
        public async Task<RentCollectionStatistics> GetRentCollectionStatistics(string period)
        {
            // 获取所有正常营业的店铺
            var totalStores = await STORE.CountAsync(s => s.STORE_STATUS == "正常营业");
            var totalRent = await (from store in STORE
                                 join rentStore in RENT_STORE on store.STORE_ID equals rentStore.STORE_ID
                                 join retailArea in RETAIL_AREA on rentStore.AREA_ID equals retailArea.AREA_ID
                                 where store.STORE_STATUS == "正常营业"
                                 select retailArea.BASE_RENT).SumAsync();

            // 模拟统计数据
            var paidBills = (int)(totalStores * 0.7); // 假设70%已缴纳
            var overdueBills = (int)(totalStores * 0.2); // 假设20%逾期
            var paidAmount = (decimal)totalRent * 0.7m; // 对应已缴纳的金额
            var collectionRate = totalStores > 0 ? Math.Round((double)paidBills / totalStores * 100, 2) : 0;

            return new RentCollectionStatistics
            {
                Period = period,
                TotalBills = totalStores,
                PaidBills = paidBills,
                OverdueBills = overdueBills,
                TotalAmount = (decimal)totalRent,
                PaidAmount = paidAmount,
                OverdueAmount = (decimal)totalRent - paidAmount,
                CollectionRate = collectionRate
            };
        }

        /// <summary>
        /// 获取租金收缴明细数据
        /// </summary>
        public async Task<List<RentCollectionDetail>> GetRentCollectionDetails(string period)
        {
            var details = new List<RentCollectionDetail>();
            
            var storeData = await (from store in STORE
                                 join rentStore in RENT_STORE on store.STORE_ID equals rentStore.STORE_ID
                                 join retailArea in RETAIL_AREA on rentStore.AREA_ID equals retailArea.AREA_ID
                                 where store.STORE_STATUS == "正常营业"
                                 select new
                                 {
                                     StoreId = store.STORE_ID,
                                     StoreName = store.STORE_NAME,
                                     TenantName = store.TENANT_NAME,
                                     AreaId = retailArea.AREA_ID,
                                     BaseRent = retailArea.BASE_RENT,
                                     RentStart = store.RENT_START,
                                     RentEnd = store.RENT_END,
                                     ContactInfo = store.CONTACT_INFO
                                 }).ToListAsync();

            foreach (var store in storeData)
            {
                // 模拟收缴明细数据
                var random = new Random(store.StoreId + int.Parse(period));
                var status = GenerateCollectionStatus(random);
                
                details.Add(new RentCollectionDetail
                {
                    Period = period,
                    StoreId = store.StoreId,
                    StoreName = store.StoreName,
                    TenantName = store.TenantName,
                    AreaId = store.AreaId,
                    BaseRent = (decimal)store.BaseRent,
                    ActualAmount = (decimal)store.BaseRent,
                    Status = status.Status,
                    DueDate = new DateTime(int.Parse(period.Substring(0, 4)), int.Parse(period.Substring(4, 2)), 15),
                    PaymentDate = status.PaymentDate,
                    PaymentMethod = status.PaymentMethod ?? "",
                    DaysOverdue = status.DaysOverdue,
                    ContactInfo = store.ContactInfo,
                    Remarks = status.Remarks
                });
            }

            return details;
        }

        /// <summary>
        /// 获取历史收缴趋势数据
        /// </summary>
        public async Task<List<RentTrendData>> GetRentTrendAnalysis(string startPeriod, string endPeriod)
        {
            var trendData = new List<RentTrendData>();
            
            // 生成趋势数据
            int startYear = int.Parse(startPeriod.Substring(0, 4));
            int startMonth = int.Parse(startPeriod.Substring(4, 2));
            int endYear = int.Parse(endPeriod.Substring(0, 4));
            int endMonth = int.Parse(endPeriod.Substring(4, 2));
            
            var currentDate = new DateTime(startYear, startMonth, 1);
            var endDate = new DateTime(endYear, endMonth, 1);
            
            while (currentDate <= endDate)
            {
                var periodStr = currentDate.ToString("yyyyMM");
                var stats = await GetRentCollectionStatistics(periodStr);
                
                trendData.Add(new RentTrendData
                {
                    Period = periodStr,
                    Year = currentDate.Year,
                    Month = currentDate.Month,
                    TotalAmount = stats.TotalAmount,
                    CollectedAmount = stats.PaidAmount,
                    CollectionRate = stats.CollectionRate,
                    TotalBills = stats.TotalBills,
                    PaidBills = stats.PaidBills,
                    OverdueBills = stats.OverdueBills
                });
                
                currentDate = currentDate.AddMonths(1);
            }
            
            return trendData;
        }

        /// <summary>
        /// 获取月度趋势数据（单个月份的详细分析）
        /// </summary>
        public async Task<object> GetMonthlyTrend(string period)
        {
            var stats = await GetRentCollectionStatistics(period);
            var previousMonth = GetPreviousMonth(period);
            var previousStats = await GetRentCollectionStatistics(previousMonth);
            
            // 计算环比变化
            var amountChange = (decimal)(stats.TotalAmount - previousStats.TotalAmount);
            var rateChange = stats.CollectionRate - previousStats.CollectionRate;
            
            return new
            {
                currentPeriod = period,
                previousPeriod = previousMonth,
                currentStats = stats,
                changes = new
                {
                    amountChange = amountChange,
                    rateChange = rateChange,
                    amountChangePercent = previousStats.TotalAmount != 0 ? Math.Round((double)(amountChange / previousStats.TotalAmount) * 100, 2) : 0,
                    rateChangePercent = Math.Round(rateChange, 2)
                },
                trend = new
                {
                    amountTrend = amountChange >= 0 ? "上升" : "下降",
                    rateTrend = rateChange >= 0 ? "改善" : "恶化"
                }
            };
        }

        /// <summary>
        /// 生成收缴状态（模拟数据）
        /// </summary>
        private (string Status, DateTime? PaymentDate, string? PaymentMethod, int DaysOverdue, string Remarks) GenerateCollectionStatus(Random random)
        {
            var statusRandom = random.NextDouble();
            
            if (statusRandom < 0.65) // 65% 已缴纳
            {
                var paymentMethods = new[] { "微信支付", "支付宝", "银行转账", "现金", "POS刷卡" };
                return (
                    Status: "已缴纳",
                    PaymentDate: DateTime.Now.AddDays(-random.Next(1, 30)),
                    PaymentMethod: paymentMethods[random.Next(paymentMethods.Length)],
                    DaysOverdue: 0,
                    Remarks: "正常缴纳"
                );
            }
            else if (statusRandom < 0.8) // 15% 待缴纳
            {
                return (
                    Status: "待缴纳",
                    PaymentDate: null,
                    PaymentMethod: null,
                    DaysOverdue: 0,
                    Remarks: "在正常缴费期内"
                );
            }
            else if (statusRandom < 0.95) // 15% 逾期
            {
                var daysOverdue = random.Next(1, 30);
                return (
                    Status: "逾期",
                    PaymentDate: null,
                    PaymentMethod: null,
                    DaysOverdue: daysOverdue,
                    Remarks: $"已逾期{daysOverdue}天，需催缴"
                );
            }
            else // 5% 部分缴纳
            {
                return (
                    Status: "部分缴纳",
                    PaymentDate: DateTime.Now.AddDays(-random.Next(1, 15)),
                    PaymentMethod: "银行转账",
                    DaysOverdue: 0,
                    Remarks: "已缴纳部分金额，尚有余款"
                );
            }
        }

        /// <summary>
        /// 获取上个月份
        /// </summary>
        private string GetPreviousMonth(string period)
        {
            var year = int.Parse(period.Substring(0, 4));
            var month = int.Parse(period.Substring(4, 2));
            
            if (month == 1)
            {
                year--;
                month = 12;
            }
            else
            {
                month--;
            }
            
            return $"{year:D4}{month:D2}";
        }

        /// <summary>
        /// 获取按区域的租金统计数据
        /// </summary>
        public async Task<List<AreaStatistics>> GetRentStatisticsByArea()
        {
            var areaStats = await (from area in AREA
                                 join retailArea in RETAIL_AREA on area.AREA_ID equals retailArea.AREA_ID
                                 join rentStore in RENT_STORE on area.AREA_ID equals rentStore.AREA_ID into rs
                                 from rentStoreLeft in rs.DefaultIfEmpty()
                                 join store in STORE on rentStoreLeft.STORE_ID equals store.STORE_ID into s
                                 from storeLeft in s.DefaultIfEmpty()
                                 select new
                                 {
                                     AreaId = area.AREA_ID,
                                     AreaSize = area.AREA_SIZE,
                                     BaseRent = retailArea.BASE_RENT,
                                     RentStatus = retailArea.RENT_STATUS,
                                     IsEmpty = area.ISEMPTY,  // 先获取原始值
                                     StoreName = storeLeft != null ? storeLeft.STORE_NAME : null,
                                     TenantName = storeLeft != null ? storeLeft.TENANT_NAME : null,
                                     StoreType = storeLeft != null ? storeLeft.STORE_TYPE : null,
                                     RentStart = storeLeft != null ? storeLeft.RENT_START : (DateTime?)null,
                                     RentEnd = storeLeft != null ? storeLeft.RENT_END : (DateTime?)null
                                 }).ToListAsync();

            return areaStats.Select(a => new AreaStatistics
            {
                AreaId = a.AreaId,
                AreaSize = a.AreaSize ?? 0,  // 处理nullable int
                BaseRent = (decimal)a.BaseRent,  // 处理double到decimal转换
                RentStatus = a.RentStatus,
                IsOccupied = a.IsEmpty == 0,  // 在内存中进行布尔转换
                StoreName = a.StoreName,
                TenantName = a.TenantName,
                StoreType = a.StoreType,
                RentStart = a.RentStart,
                RentEnd = a.RentEnd
            }).ToList();
        }

        #endregion
    }
}