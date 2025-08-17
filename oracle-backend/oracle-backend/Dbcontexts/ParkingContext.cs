using Microsoft.EntityFrameworkCore;
using oracle_backend.Models;

namespace oracle_backend.Dbcontexts
{
    // 停车场相关的数据库上下文
    public class ParkingContext : DbContext
    {
        /*
         * 方法结构说明：
         * 
         * 1. 停车场信息管理
         *    - ParkingLotExists(int areaId) - 检查停车场是否存在
         *    - GetParkingLotById(int areaId) - 获取停车场信息
         *    - UpdateParkingLotInfo(...) - 更新停车场信息
         *    - GetParkingLotStatus(int areaId) - 获取停车场状态
         *    - GetParkingLotSpaceCount(int areaId) - 获取车位数
         * 
         * 2. 车位管理
         *    - GetParkingSpaces(int? areaId = null) - 获取车位（支持可选区域过滤）
         *    - GetParkingSpacesByArea(int areaId) - 获取指定区域车位（向后兼容）
         *    - GetParkingStatusStatistics(int areaId) - 获取车位状态统计
         *    - GetParkingSpaceStatuses(int areaId) - 获取车位状态详情
         * 
         * 3. 车辆管理
         *    - VehicleEntry(...) - 车辆入场
         *    - VehicleExit(...) - 车辆出场
         *    - GetCurrentVehicles(int? areaId = null) - 获取在停车辆（支持可选区域过滤）
         *    - GetCurrentVehiclesByParkingLot(int areaId) - 获取指定停车场车辆（向后兼容）
         *    - GetVehicleStatusByLicensePlate(...) - 根据车牌查询车辆状态
         * 
         * 4. 支付管理
         *    - GetPaymentRecords(string status = "all") - 获取支付记录（支持状态过滤）
         *    - GetPaidParkingRecords() - 获取已支付记录（向后兼容）
         *    - GetUnpaidParkingRecords() - 获取未支付记录
         *    - ProcessParkingPayment(...) - 处理支付
         * 
         * 5. 统计报表
         *    - GetParkingStatisticsReport(...) - 生成统计报表
         *    - GetParkingOperationStatistics(...) - 获取运营统计
         */
        // 构造函数
        public ParkingContext(DbContextOptions<ParkingContext> options) : base(options)
        {
        }

        // 导入与停车场相关的表
        public DbSet<ParkingLot> PARKING_LOT { get; set; }
        public DbSet<ParkingSpace> PARKING_SPACE { get; set; }
        public DbSet<ParkingSpaceDistribution> PARKING_SPACE_DISTRIBUTION { get; set; }
        public DbSet<Car> CAR { get; set; }
        public DbSet<Park> PARK { get; set; }
        public DbSet<Area> AREA { get; set; }


        // 内存中存储支付记录（生产环境中应使用数据库或缓存）
        private static readonly Dictionary<string, Models.ParkingPaymentRecord> _paymentRecords = new Dictionary<string, Models.ParkingPaymentRecord>();
        
        // 公共访问器
        public Dictionary<string, Models.ParkingPaymentRecord> PaymentRecords => _paymentRecords;
        
        // 内存中存储停车场状态（纯内存，重启后恢复默认）
        private static readonly Dictionary<int, string> _parkingLotStatuses = new Dictionary<int, string>();
        
        // 内存中存储停车场车位数（纯内存，重启后恢复默认）
        private static readonly Dictionary<int, int> _parkingLotSpaceCounts = new Dictionary<int, int>();
        
        // 初始化默认状态
        static ParkingContext()
        {
            // 初始化测试停车场的默认状态
            _parkingLotStatuses[1001] = "正常运营";
            _parkingLotStatuses[1002] = "正常运营";
            _parkingLotStatuses[1003] = "正常运营";
        }
        


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置ParkingLot和Area的外键关系
            modelBuilder.Entity<ParkingLot>()
                .HasOne<Area>()
                .WithMany()
                .HasForeignKey(pl => pl.AREA_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置ParkingSpaceDistribution的外键关系
            modelBuilder.Entity<ParkingSpaceDistribution>()
                .HasOne(psd => psd.parkingLotNavigation)
                .WithMany()
                .HasForeignKey(psd => psd.AREA_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ParkingSpaceDistribution>()
                .HasOne(psd => psd.parkingSpaceNavigation)
                .WithMany()
                .HasForeignKey(psd => psd.PARKING_SPACE_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置Park的外键关系 - 使用自定义映射解决类型不匹配
            modelBuilder.Entity<Park>()
                .HasOne(p => p.carNavigation)
                .WithMany()
                .HasPrincipalKey(c => new { c.LICENCE_PLATE_NUMBER, c.PARK_START })
                .HasForeignKey(p => new { p.LICENSE_PLATE_NUMBER, p.PARK_START })
                .OnDelete(DeleteBehavior.Restrict);

            // 忽略类型不匹配的导航属性，避免外键关系错误
            modelBuilder.Entity<Park>()
                .Ignore(p => p.parkingSpaceNavigation);

            // 配置Oracle数据库的布尔值转换 (Oracle中用0/1表示false/true)
            modelBuilder.Entity<ParkingSpace>()
                .Property(ps => ps.OCCUPIED)
                .HasConversion(
                    v => v ? 1 : 0,     // bool -> int (保存到数据库)
                    v => v == 1         // int -> bool (从数据库读取)
                )
                .HasColumnType("NUMBER(1)"); // 明确指定列类型为NUMBER(1)
        }

        #region 停车场信息管理功能

        /// <summary>
        /// 检查停车场区域是否存在
        /// </summary>
        public async Task<bool> ParkingLotExists(int areaId)
        {
            var count = await PARKING_LOT.CountAsync(pl => pl.AREA_ID == areaId);
            return count > 0;
        }

        /// <summary>
        /// 获取停车场信息
        /// </summary>
        public async Task<ParkingLot?> GetParkingLotById(int areaId)
        {
            return await PARKING_LOT.FirstOrDefaultAsync(pl => pl.AREA_ID == areaId);
        }

        /// <summary>
        /// 更新停车场信息
        /// </summary>
        public async Task<bool> UpdateParkingLotInfo(int areaId, int parkingFee, string status, int? parkingSpaceCount = null)
        {
            var parkingLot = await PARKING_LOT.FirstOrDefaultAsync(pl => pl.AREA_ID == areaId);
            if (parkingLot == null)
                return false;

            // 更新停车费
            parkingLot.PARKING_FEE = parkingFee;
            
            // 更新停车场状态（纯内存存储）
            var oldStatus = _parkingLotStatuses.TryGetValue(areaId, out var old) ? old : "未设置";
            _parkingLotStatuses[areaId] = status;
            
            Console.WriteLine($"[DEBUG] 停车场状态更新: 区域{areaId}, 旧状态: {oldStatus}, 新状态: {status}");
            Console.WriteLine($"[DEBUG] 当前内存中所有状态: {string.Join(", ", _parkingLotStatuses.Select(kvp => $"区域{kvp.Key}={kvp.Value}"))}");
            
            // 更新车位数（如果提供了的话）
            if (parkingSpaceCount.HasValue)
            {
                _parkingLotSpaceCounts[areaId] = parkingSpaceCount.Value;
            }
            
            await SaveChangesAsync();
            return true;
        }
        
        /// <summary>
        /// 获取停车场状态
        /// </summary>
        public string GetParkingLotStatus(int areaId)
        {
            // 从内存获取，如果没有则返回默认值
            var status = _parkingLotStatuses.TryGetValue(areaId, out var statusValue) ? statusValue : "正常运营";
            Console.WriteLine($"[DEBUG] 获取停车场状态: 区域{areaId}, 状态: {status}");
            Console.WriteLine($"[DEBUG] 当前内存中所有状态: {string.Join(", ", _parkingLotStatuses.Select(kvp => $"区域{kvp.Key}={kvp.Value}"))}");
            return status;
        }
        
        /// <summary>
        /// 获取停车场车位数（优先从内存获取，否则计算实际车位数）
        /// </summary>
        public async Task<int> GetParkingLotSpaceCount(int areaId)
        {
            // 优先从内存获取
            if (_parkingLotSpaceCounts.TryGetValue(areaId, out var spaceCount))
            {
                return spaceCount;
            }
            
            // 否则从数据库计算实际车位数
            var parkingSpaces = await GetParkingSpacesByArea(areaId);
            return parkingSpaces.Count;
        }

        /// <summary>
        /// 获取停车场区域的所有车位（支持查询指定区域或所有区域）
        /// </summary>
        public async Task<List<ParkingSpace>> GetParkingSpaces(int? areaId = null)
        {
            if (areaId.HasValue)
            {
                // 查询指定区域
                var query = from psd in PARKING_SPACE_DISTRIBUTION
                           join ps in PARKING_SPACE on psd.PARKING_SPACE_ID equals ps.PARKING_SPACE_ID
                           where psd.AREA_ID == areaId.Value
                           select ps;
                return await query.ToListAsync();
            }
            else
            {
                // 查询所有区域
                var query = from psd in PARKING_SPACE_DISTRIBUTION
                           join ps in PARKING_SPACE on psd.PARKING_SPACE_ID equals ps.PARKING_SPACE_ID
                           select ps;
                return await query.ToListAsync();
            }
        }

        /// <summary>
        /// 获取停车场区域的所有车位（保持向后兼容）
        /// </summary>
        public async Task<List<ParkingSpace>> GetParkingSpacesByArea(int areaId)
        {
            return await GetParkingSpaces(areaId);
        }

        #endregion

        #region 车位状态查询功能

        /// <summary>
        /// 获取停车场车位状态统计
        /// </summary>
        public async Task<Models.ParkingStatusStatistics> GetParkingStatusStatistics(int areaId)
        {
            try
            {
                Console.WriteLine($"[DEBUG] GetParkingStatusStatistics 开始执行，areaId: {areaId}");
                
                // 使用与GetParkingSpaceStatuses相同的逻辑，通过查询PARK和CAR表来判断车位占用状态
                var allSpaces = await (from psd in PARKING_SPACE_DISTRIBUTION
                                     join ps in PARKING_SPACE on psd.PARKING_SPACE_ID equals ps.PARKING_SPACE_ID
                                     where psd.AREA_ID == areaId
                                     select new 
                                     {
                                         ParkingSpaceId = ps.PARKING_SPACE_ID,
                                         ParkingSpace = ps
                                     }).ToListAsync();

                Console.WriteLine($"[DEBUG] 查询到 {allSpaces.Count} 个车位");

                var totalSpaces = allSpaces.Count;
                var occupiedSpaces = 0;

                // 统计实际被占用的车位数
                foreach (var space in allSpaces)
                {
                    // 查找当前正在停车的车辆（PARK_END为NULL表示还在停车）
                    var currentPark = await PARK
                        .Where(p => p.PARKING_SPACE_ID == space.ParkingSpaceId)
                        .Join(CAR, 
                              p => new { LicensePlate = p.LICENSE_PLATE_NUMBER, ParkStart = p.PARK_START },
                              c => new { LicensePlate = c.LICENCE_PLATE_NUMBER, ParkStart = c.PARK_START },
                              (p, c) => new { Park = p, Car = c })
                        .Where(pc => !pc.Car.PARK_END.HasValue)
                        .FirstOrDefaultAsync();

                    if (currentPark != null)
                    {
                        occupiedSpaces++;
                        Console.WriteLine($"[DEBUG] 车位 {space.ParkingSpaceId} 被占用，车牌: {currentPark.Park.LICENSE_PLATE_NUMBER}");
                    }
                }

                var availableSpaces = totalSpaces - occupiedSpaces;
                var occupancyRate = totalSpaces > 0 ? Math.Round((double)occupiedSpaces / totalSpaces * 100, 2) : 0;

                Console.WriteLine($"[DEBUG] 统计结果: 总车位={totalSpaces}, 已占用={occupiedSpaces}, 可用={availableSpaces}, 利用率={occupancyRate}%");

                return new Models.ParkingStatusStatistics
                {
                    AreaId = areaId,
                    TotalSpaces = totalSpaces,
                    OccupiedSpaces = occupiedSpaces,
                    AvailableSpaces = availableSpaces,
                    OccupancyRate = occupancyRate
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetParkingStatusStatistics 发生异常: {ex.Message}");
                Console.WriteLine($"[ERROR] 异常堆栈: {ex.StackTrace}");
                
                // 如果查询失败，返回默认值
                return new Models.ParkingStatusStatistics
                {
                    AreaId = areaId,
                    TotalSpaces = 0,
                    OccupiedSpaces = 0,
                    AvailableSpaces = 0,
                    OccupancyRate = 0
                };
            }
        }

        /// <summary>
        /// 获取所有车位状态详情
        /// </summary>
        public async Task<List<Models.ParkingSpaceStatus>> GetParkingSpaceStatuses(int areaId)
        {
            Console.WriteLine($"[DEBUG] GetParkingSpaceStatuses 开始执行，areaId: {areaId}");
            
            try
            {
                // 简化查询，避免复杂JOIN可能触发的布尔值问题
                var allSpaces = await (from psd in PARKING_SPACE_DISTRIBUTION
                                     join ps in PARKING_SPACE on psd.PARKING_SPACE_ID equals ps.PARKING_SPACE_ID
                                     where psd.AREA_ID == areaId
                                     select new 
                                     {
                                         ParkingSpaceId = ps.PARKING_SPACE_ID,
                                         ParkingSpace = ps
                                     }).ToListAsync();

                Console.WriteLine($"[DEBUG] 查询到 {allSpaces.Count} 个车位");

            // 在内存中处理车辆信息
            var results = new List<dynamic>();
            foreach (var space in allSpaces)
            {
                // 查找当前正在停车的车辆（不限制日期，只要PARK_END为NULL就表示还在停车）
                var currentPark = await PARK
                    .Where(p => p.PARKING_SPACE_ID == space.ParkingSpaceId)
                    .Join(CAR, 
                          p => new { LicensePlate = p.LICENSE_PLATE_NUMBER, ParkStart = p.PARK_START },
                          c => new { LicensePlate = c.LICENCE_PLATE_NUMBER, ParkStart = c.PARK_START },
                          (p, c) => new { Park = p, Car = c })
                    .Where(pc => !pc.Car.PARK_END.HasValue)
                    .OrderByDescending(pc => pc.Park.PARK_START)
                    .FirstOrDefaultAsync();

                results.Add(new 
                {
                    ParkingSpaceId = space.ParkingSpaceId,
                    ParkingSpace = space.ParkingSpace,
                    LicensePlateNumber = currentPark?.Park.LICENSE_PLATE_NUMBER,
                    ParkStart = currentPark?.Park.PARK_START
                });
            }

            // 在内存中处理布尔值转换，避免直接访问OCCUPIED字段
            var finalResults = results.Select(r => new Models.ParkingSpaceStatus
            {
                ParkingSpaceId = r.ParkingSpaceId,
                IsOccupied = r.LicensePlateNumber != null, // 有车牌号就表示占用
                LicensePlateNumber = r.LicensePlateNumber,
                ParkStart = r.ParkStart,
                UpdateTime = DateTime.Now
            }).ToList();

            Console.WriteLine($"[DEBUG] 最终返回 {finalResults.Count} 个车位状态");
            return finalResults;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetParkingSpaceStatuses 发生异常: {ex.Message}");
                Console.WriteLine($"[ERROR] 异常堆栈: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 检查车位是否可用
        /// </summary>
        public async Task<bool> IsParkingSpaceAvailable(int parkingSpaceId)
        {
            var parkingSpace = await PARKING_SPACE.FirstOrDefaultAsync(ps => ps.PARKING_SPACE_ID == parkingSpaceId);
            if (parkingSpace == null) return false;
            return !parkingSpace.OCCUPIED;  // 在内存中进行布尔运算，不在SQL中
        }

        #endregion

        #region 车辆出入计费功能

        /// <summary>
        /// 车辆入场 - 简化版本，分步执行
        /// </summary>
        public async Task<(bool Success, string Message)> VehicleEntry(string licensePlateNumber, int parkingSpaceId)
        {
            try
            {
                // 1. 检查车位是否已被其他车辆占用
                var spaceOccupied = await PARK
                    .Where(p => p.PARKING_SPACE_ID == parkingSpaceId)
                    .Join(CAR, 
                          p => new { LicensePlate = p.LICENSE_PLATE_NUMBER, ParkStart = p.PARK_START },
                          c => new { LicensePlate = c.LICENCE_PLATE_NUMBER, ParkStart = c.PARK_START },
                          (p, c) => new { Park = p, Car = c })
                    .Where(pc => !pc.Car.PARK_END.HasValue)
                    .FirstOrDefaultAsync();
                    
                if (spaceOccupied != null)
                    return (false, $"车位{parkingSpaceId}已被车辆{spaceOccupied.Park.LICENSE_PLATE_NUMBER}占用");

                // 2. 检查车辆是否还在停车场（只有当前还在停车的车辆才不能重复入场）
                var currentParking = await PARK
                    .Where(p => p.LICENSE_PLATE_NUMBER == licensePlateNumber)
                    .Join(CAR, 
                          p => new { LicensePlate = p.LICENSE_PLATE_NUMBER, ParkStart = p.PARK_START },
                          c => new { LicensePlate = c.LICENCE_PLATE_NUMBER, ParkStart = c.PARK_START },
                          (p, c) => new { Park = p, Car = c })
                    .Where(pc => !pc.Car.PARK_END.HasValue)
                    .FirstOrDefaultAsync();
                    
                if (currentParking != null)
                    return (false, "该车辆当前还在停车场内，不可重复入场");

                // 3. 使用中国本地时间戳
                var currentTime = DateTime.Now;

                // 4. 先插入车辆记录
                var car = new Car
                {
                    LICENCE_PLATE_NUMBER = licensePlateNumber,
                    PARK_START = currentTime,
                    PARK_END = null
                };
                CAR.Add(car);

                // 5. 插入停车记录
                var park = new Park
                {
                    LICENSE_PLATE_NUMBER = licensePlateNumber,
                    PARKING_SPACE_ID = parkingSpaceId,
                    PARK_START = currentTime
                };
                PARK.Add(park);

                // 6. 保存到数据库
                await SaveChangesAsync();

                // 7. 最后更新车位状态（使用原生SQL）
                await Database.ExecuteSqlRawAsync(
                    "UPDATE PARKING_SPACE SET OCCUPIED = 1 WHERE PARKING_SPACE_ID = {0}", 
                    parkingSpaceId);

                // 8. 记录日志
                Console.WriteLine($"[停车记录创建成功] 车牌号: {licensePlateNumber}, 车位ID: {parkingSpaceId}");
                Console.WriteLine($"  入场时间: {currentTime:yyyy-MM-dd HH:mm:ss} (中国本地时间)");
                Console.WriteLine($"  时区信息: {TimeZoneInfo.Local.DisplayName}");

                return (true, "车辆入场成功");
            }
            catch (Exception ex)
            {
                return (false, $"入场失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 车辆出场 - 使用纯SQL重写
        /// </summary>
        public async Task<Models.VehicleExitResult?> VehicleExit(string licensePlateNumber)
        {
            try
            {
                // 1. 查找当前停车记录（查找最近的未结束停车记录）
                var park = await PARK
                    .Where(p => p.LICENSE_PLATE_NUMBER == licensePlateNumber)
                    .OrderByDescending(p => p.PARK_START)
                    .FirstOrDefaultAsync();

                if (park == null)
                    return null;

                // 查找对应的CAR记录（必须是PARK_END为NULL的，即还在停车的）
                var car = await CAR.FirstOrDefaultAsync(c => 
                    c.LICENCE_PLATE_NUMBER == licensePlateNumber && 
                    c.PARK_START == park.PARK_START && 
                    !c.PARK_END.HasValue);

                if (car == null)
                    return null;

                // 2. 获取停车场信息
                var parkingSpaceId = park.PARKING_SPACE_ID;
                var parkingLot = await (from psd in PARKING_SPACE_DISTRIBUTION
                                      join pl in PARKING_LOT on psd.AREA_ID equals pl.AREA_ID
                                      where psd.PARKING_SPACE_ID == parkingSpaceId
                                      select pl).FirstOrDefaultAsync();

                if (parkingLot == null)
                    return null;

                // 3. 计算停车费用（使用中国本地时间）
                var parkEnd = DateTime.Now;
                var parkingDuration = parkEnd - park.PARK_START;
                var hours = Math.Ceiling(parkingDuration.TotalHours);
                var totalFee = (decimal)(hours * parkingLot.PARKING_FEE);

                // 4. 更新车辆记录
                car.PARK_END = parkEnd;

                // 5. 使用原生SQL释放车位，避免布尔值问题
                await Database.ExecuteSqlRawAsync(
                    "UPDATE PARKING_SPACE SET OCCUPIED = 0 WHERE PARKING_SPACE_ID = {0}", 
                    parkingSpaceId);

                await SaveChangesAsync();

                // 记录出场日志
                Console.WriteLine($"[车辆出场成功] 车牌号: {licensePlateNumber}, 车位ID: {parkingSpaceId}");
                Console.WriteLine($"  入场时间: {park.PARK_START:yyyy-MM-dd HH:mm:ss} (数据库存储时间)");
                Console.WriteLine($"  出场时间: {parkEnd:yyyy-MM-dd HH:mm:ss} (中国本地时间)");
                Console.WriteLine($"  停车时长: {parkingDuration}, 费用: {totalFee}元");
                Console.WriteLine($"  时区信息: {TimeZoneInfo.Local.DisplayName}");

                                        return new Models.VehicleExitResult
                        {
                            LicensePlateNumber = licensePlateNumber,
                            ParkingSpaceId = parkingSpaceId,
                            ParkStart = park.PARK_START,
                            ParkEnd = parkEnd,
                            ParkingDuration = parkingDuration,
                            HourlyRate = (decimal)parkingLot.PARKING_FEE,
                            TotalFee = totalFee,
                            PaymentStatus = "未支付"
                        };
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取所有未支付的停车记录
        /// </summary>
        public async Task<List<Models.VehicleExitResult>> GetUnpaidParkingRecords()
        {
            try
            {
                var unpaidRecords = new List<Models.VehicleExitResult>();
                
                // 查找所有已出场但未支付的记录
                var exitedCars = await CAR
                    .Where(c => c.PARK_END.HasValue)
                    .Join(PARK,
                          c => new { LicensePlate = c.LICENCE_PLATE_NUMBER, ParkStart = c.PARK_START },
                          p => new { LicensePlate = p.LICENSE_PLATE_NUMBER, ParkStart = p.PARK_START },
                          (c, p) => new { Car = c, Park = p })
                    .OrderByDescending(cp => cp.Car.PARK_END)
                    .ToListAsync();

                foreach (var record in exitedCars)
                {
                    // 检查是否已支付
                    var paymentKey = $"{record.Car.LICENCE_PLATE_NUMBER}_{record.Park.PARKING_SPACE_ID}_{record.Car.PARK_START:yyyyMMddHHmmss}";
                    Console.WriteLine($"[DEBUG] 未支付查询 - 检查key: {paymentKey}");
                    Console.WriteLine($"[DEBUG] 是否存在于支付记录: {_paymentRecords.ContainsKey(paymentKey)}");
                    
                    if (_paymentRecords.ContainsKey(paymentKey))
                        continue; // 已支付，跳过

                    // 获取停车场信息
                    var parkingLot = await (from psd in PARKING_SPACE_DISTRIBUTION
                                          join pl in PARKING_LOT on psd.AREA_ID equals pl.AREA_ID
                                          where psd.PARKING_SPACE_ID == record.Park.PARKING_SPACE_ID
                                          select pl).FirstOrDefaultAsync();

                    if (parkingLot != null && record.Car.PARK_END.HasValue)
                    {
                        var parkingDuration = record.Car.PARK_END.Value - record.Car.PARK_START;
                        var hours = Math.Ceiling(parkingDuration.TotalHours);
                        var totalFee = (decimal)(hours * parkingLot.PARKING_FEE);

                        unpaidRecords.Add(new Models.VehicleExitResult
                        {
                            LicensePlateNumber = record.Car.LICENCE_PLATE_NUMBER,
                            ParkingSpaceId = record.Park.PARKING_SPACE_ID,
                            ParkStart = record.Car.PARK_START,
                            ParkEnd = record.Car.PARK_END.Value,
                            ParkingDuration = parkingDuration,
                            HourlyRate = (decimal)parkingLot.PARKING_FEE,
                            TotalFee = totalFee,
                            PaymentStatus = "未支付"
                        });
                    }
                }

                return unpaidRecords;
            }
            catch (Exception)
            {
                return new List<Models.VehicleExitResult>();
            }
        }

        /// <summary>
        /// 获取支付记录（支持状态过滤）
        /// </summary>
        public async Task<List<Models.VehicleExitResult>> GetPaymentRecords(string status = "all")
        {
            try
            {
                var records = new List<Models.VehicleExitResult>();
                
                // 查找所有已出场的记录
                var exitedCars = await CAR
                    .Where(c => c.PARK_END.HasValue)
                    .Join(PARK,
                          c => new { LicensePlate = c.LICENCE_PLATE_NUMBER, ParkStart = c.PARK_START },
                          p => new { LicensePlate = p.LICENSE_PLATE_NUMBER, ParkStart = p.PARK_START },
                          (c, p) => new { Car = c, Park = p })
                    .OrderByDescending(cp => cp.Car.PARK_END)
                    .ToListAsync();

                foreach (var record in exitedCars)
                {
                    // 检查是否已支付
                    var paymentKey = $"{record.Car.LICENCE_PLATE_NUMBER}_{record.Park.PARKING_SPACE_ID}_{record.Car.PARK_START:yyyyMMddHHmmss}";
                    var isPaid = _paymentRecords.ContainsKey(paymentKey);
                    
                    // 根据状态过滤
                    bool shouldInclude = status.ToLower() switch
                    {
                        "paid" => isPaid,
                        "unpaid" => !isPaid,
                        "all" => true,
                        _ => true
                    };

                    if (!shouldInclude) continue;

                    // 获取停车场信息
                    var parkingLot = await (from psd in PARKING_SPACE_DISTRIBUTION
                                          join pl in PARKING_LOT on psd.AREA_ID equals pl.AREA_ID
                                          where psd.PARKING_SPACE_ID == record.Park.PARKING_SPACE_ID
                                          select pl).FirstOrDefaultAsync();

                    if (parkingLot != null && record.Car.PARK_END.HasValue)
                    {
                        var parkingDuration = record.Car.PARK_END.Value - record.Car.PARK_START;
                        var hours = Math.Ceiling(parkingDuration.TotalHours);
                        var totalFee = (decimal)(hours * parkingLot.PARKING_FEE);

                        records.Add(new Models.VehicleExitResult
                        {
                            LicensePlateNumber = record.Car.LICENCE_PLATE_NUMBER,
                            ParkingSpaceId = record.Park.PARKING_SPACE_ID,
                            ParkStart = record.Car.PARK_START,
                            ParkEnd = record.Car.PARK_END.Value,
                            ParkingDuration = parkingDuration,
                            HourlyRate = (decimal)parkingLot.PARKING_FEE,
                            TotalFee = totalFee,
                            PaymentStatus = isPaid ? "已支付" : "未支付"
                        });
                    }
                }

                return records;
            }
            catch (Exception)
            {
                return new List<Models.VehicleExitResult>();
            }
        }

        /// <summary>
        /// 获取所有已支付的停车记录（保持向后兼容）
        /// </summary>
        public async Task<List<Models.VehicleExitResult>> GetPaidParkingRecords()
        {
            return await GetPaymentRecords("paid");
        }

        /// <summary>
        /// 处理停车费支付
        /// </summary>
        public Task<bool> ProcessParkingPayment(Models.ParkingPaymentRequest request)
        {
            // 将UTC时间转换为本地时间，确保与数据库中的时间格式一致
            var localParkStart = request.ParkStart.ToLocalTime();
            var key = $"{request.LicensePlateNumber}_{request.ParkingSpaceId}_{localParkStart:yyyyMMddHHmmss}";
            
            // 调试信息
            Console.WriteLine($"[DEBUG] 支付处理 - 原始时间: {request.ParkStart}");
            Console.WriteLine($"[DEBUG] 支付处理 - 本地时间: {localParkStart}");
            Console.WriteLine($"[DEBUG] 支付处理 - 生成的key: {key}");
            Console.WriteLine($"[DEBUG] 当前支付记录数量: {_paymentRecords.Count}");
            Console.WriteLine($"[DEBUG] 所有已存在的keys: {string.Join(", ", _paymentRecords.Keys)}");
            
            if (_paymentRecords.ContainsKey(key))
            {
                var record = _paymentRecords[key];
                record.PaymentStatus = "已支付";
                record.PaymentMethod = request.PaymentMethod;
                record.PaymentTime = DateTime.Now;
                record.PaymentReference = request.PaymentReference;
                record.Remarks = request.Remarks;
                return Task.FromResult(true);
            }

            // 创建新的支付记录
            _paymentRecords[key] = new Models.ParkingPaymentRecord
            {
                LicensePlateNumber = request.LicensePlateNumber,
                ParkingSpaceId = request.ParkingSpaceId,
                ParkStart = localParkStart,
                ParkEnd = request.ParkEnd?.ToLocalTime() ?? DateTime.Now,
                TotalFee = request.TotalFee,
                PaymentStatus = "已支付",
                PaymentMethod = request.PaymentMethod,
                PaymentTime = DateTime.Now,
                PaymentReference = request.PaymentReference,
                Remarks = request.Remarks
            };

            return Task.FromResult(true);
        }

        #endregion

        #region 车辆状态查询功能

        /// <summary>
        /// 查询停车场内所有车辆
        /// </summary>
        public async Task<List<Models.ParkedVehicleInfo>> GetAllParkedVehicles(int areaId)
        {
            var query = from psd in PARKING_SPACE_DISTRIBUTION
                       join ps in PARKING_SPACE on psd.PARKING_SPACE_ID equals ps.PARKING_SPACE_ID
                       join p in PARK on ps.PARKING_SPACE_ID equals p.PARKING_SPACE_ID
                       where psd.AREA_ID == areaId && p.PARK_START.Date == DateTime.Today
                       select new Models.ParkedVehicleInfo
                       {
                           LicensePlateNumber = p.LICENSE_PLATE_NUMBER,
                           ParkingSpaceId = ps.PARKING_SPACE_ID,
                           AreaId = psd.AREA_ID,
                           ParkStart = p.PARK_START,
                           CurrentDuration = DateTime.Now - p.PARK_START,
                           ParkingLotName = "", // 暂时设为空字符串，后续可以关联查询
                           AreaName = "", // 暂时设为空字符串，后续可以关联查询
                           EstimatedFee = CalculateEstimatedFee(p.PARK_START, areaId)
                       };

            return await query.ToListAsync();
        }

        /// <summary>
        /// 根据车牌号查询车辆信息
        /// </summary>
        public async Task<Models.ParkedVehicleInfo?> GetVehicleByLicensePlate(string licensePlateNumber)
        {
            var query = from p in PARK
                       join ps in PARKING_SPACE on p.PARKING_SPACE_ID equals ps.PARKING_SPACE_ID
                       join psd in PARKING_SPACE_DISTRIBUTION on ps.PARKING_SPACE_ID equals psd.PARKING_SPACE_ID
                       where p.LICENSE_PLATE_NUMBER == licensePlateNumber && p.PARK_START.Date == DateTime.Today
                       select new Models.ParkedVehicleInfo
                       {
                           LicensePlateNumber = p.LICENSE_PLATE_NUMBER,
                           ParkingSpaceId = ps.PARKING_SPACE_ID,
                           AreaId = psd.AREA_ID,
                           ParkStart = p.PARK_START,
                           CurrentDuration = DateTime.Now - p.PARK_START,
                           ParkingLotName = "", // 暂时设为空字符串，后续可以关联查询
                           AreaName = "", // 暂时设为空字符串，后续可以关联查询
                           EstimatedFee = CalculateEstimatedFee(p.PARK_START, psd.AREA_ID)
                       };

            return await query.FirstOrDefaultAsync();
        }

        #endregion

        #region 统计报表功能

        /// <summary>
        /// 获取停车场运营统计
        /// </summary>
        public async Task<Models.ParkingOperationStatistics> GetParkingOperationStatistics(DateTime startDate, DateTime endDate, int areaId)
        {
            var query = from p in PARK
                       join ps in PARKING_SPACE on p.PARKING_SPACE_ID equals ps.PARKING_SPACE_ID
                       join psd in PARKING_SPACE_DISTRIBUTION on ps.PARKING_SPACE_ID equals psd.PARKING_SPACE_ID
                       join pl in PARKING_LOT on psd.AREA_ID equals pl.AREA_ID
                       where psd.AREA_ID == areaId && p.PARK_START >= startDate && p.PARK_START <= endDate
                       select new
                       {
                           p.LICENSE_PLATE_NUMBER,
                           p.PARK_START,
                           p.PARKING_SPACE_ID,
                           pl.PARKING_FEE
                       };

            var records = await query.ToListAsync();

            var totalVehicles = records.Count;
            var totalRevenue = records.Sum(r => CalculateParkingFee(r.PARK_START, DateTime.Now, r.PARKING_FEE));
            var avgParkingDuration = records.Any() ? 
                TimeSpan.FromTicks((long)records.Average(r => (DateTime.Now - r.PARK_START).Ticks)) : 
                TimeSpan.Zero;

            return new Models.ParkingOperationStatistics
            {
                AreaId = areaId,
                StartDate = startDate,
                EndDate = endDate,
                TotalVehicles = totalVehicles,
                TotalRevenue = totalRevenue,
                AverageParkingHours = avgParkingDuration.TotalHours,
                PeakHour = 0, // 暂时设为0，后续可以优化
                PeakHourVehicleCount = 0 // 暂时设为0，后续可以优化
            };
        }

        /// <summary>
        /// 获取高峰时段分析
        /// </summary>
        private async Task<List<Models.PeakHourData>> GetPeakHourAnalysis(DateTime startDate, DateTime endDate, int areaId)
        {
            var query = from p in PARK
                       join ps in PARKING_SPACE on p.PARKING_SPACE_ID equals ps.PARKING_SPACE_ID
                       join psd in PARKING_SPACE_DISTRIBUTION on ps.PARKING_SPACE_ID equals psd.PARKING_SPACE_ID
                       where psd.AREA_ID == areaId && p.PARK_START >= startDate && p.PARK_START <= endDate
                       select new { p.PARK_START };

            var records = await query.ToListAsync();

            var hourlyData = records.GroupBy(r => r.PARK_START.Hour)
                                  .Select(g => new Models.PeakHourData
                                  {
                                      Hour = g.Key,
                                      VehicleCount = g.Count(),
                                      Revenue = 0, // 暂时设为0，后续可以基于实际数据计算
                                      UtilizationRate = CalculateHourlyUtilizationRate(g.Key, areaId)
                                  })
                                  .OrderBy(x => x.Hour)
                                  .ToList();

            return hourlyData;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算预估费用
        /// </summary>
        private decimal CalculateEstimatedFee(DateTime parkStart, int areaId)
        {
            var duration = DateTime.Now - parkStart;
            var hours = Math.Ceiling(duration.TotalHours);
            // 这里需要获取停车费率，暂时使用默认值
            var hourlyRate = 10m; // 默认每小时10元
            return (decimal)hours * hourlyRate;
        }

        /// <summary>
        /// 计算停车费用
        /// </summary>
        private decimal CalculateParkingFee(DateTime parkStart, DateTime parkEnd, int hourlyRate)
        {
            var duration = parkEnd - parkStart;
            var hours = Math.Ceiling(duration.TotalHours);
            return (decimal)hours * hourlyRate;
        }

        /// <summary>
        /// 计算小时利用率
        /// </summary>
        private double CalculateHourlyUtilizationRate(int hour, int areaId)
        {
            // 模拟计算，实际应该基于真实数据
            var random = new Random(hour + areaId);
            return random.NextDouble() * 100;
        }

        #endregion

        #region 2.8.4 停车场内车辆状态查询

        /// <summary>
        /// 获取所有停车场列表
        /// </summary>
        public async Task<List<dynamic>> GetParkingLotList()
        {
            try
            {
                var parkingLots = await (from pl in PARKING_LOT
                                       join a in AREA on pl.AREA_ID equals a.AREA_ID
                                       select new
                                       {
                                           AreaId = pl.AREA_ID,
                                           ParkingLotName = $"停车场{pl.AREA_ID}",
                                           ParkingFee = pl.PARKING_FEE,
                                           AreaSize = a.AREA_SIZE
                                       }).ToListAsync();

                return parkingLots.Cast<dynamic>().ToList();
            }
            catch (Exception)
            {
                return new List<dynamic>();
            }
        }

        /// <summary>
        /// 获取指定停车场的在停车辆信息（保持向后兼容）
        /// </summary>
        public async Task<List<Models.VehicleStatusResult>> GetCurrentVehiclesByParkingLot(int areaId)
        {
            return await GetCurrentVehicles(areaId);
        }

        /// <summary>
        /// 获取在停车辆信息（支持查询指定停车场或所有停车场）
        /// </summary>
        public async Task<List<Models.VehicleStatusResult>> GetCurrentVehicles(int? areaId = null)
        {
            try
            {
                var currentVehicles = new List<Models.VehicleStatusResult>();

                // 调试信息
                Console.WriteLine($"[DEBUG] 查询在停车辆，区域ID: {areaId?.ToString() ?? "所有"}");
                
                // 先检查CAR表中的在停车辆
                var carsInParking = await CAR.Where(c => !c.PARK_END.HasValue).ToListAsync();
                Console.WriteLine($"[DEBUG] CAR表中在停车辆总数: {carsInParking.Count}");
                foreach(var car in carsInParking.Take(5)) // 只显示前5个
                {
                    Console.WriteLine($"[DEBUG] 在停车辆: {car.LICENCE_PLATE_NUMBER}, 入场时间: {car.PARK_START}");
                }

                // 查找所有在停车辆（已入场但未出场）
                var parkedCars = await CAR
                    .Where(c => !c.PARK_END.HasValue) // 未出场
                    .Join(PARK,
                          c => new { LicensePlate = c.LICENCE_PLATE_NUMBER, c.PARK_START },
                          p => new { LicensePlate = p.LICENSE_PLATE_NUMBER, PARK_START = p.PARK_START },
                          (c, p) => new { Car = c, Park = p })
                    .OrderBy(cp => cp.Car.PARK_START)
                    .ToListAsync();

                foreach (var record in parkedCars)
                {
                    // 获取停车场和区域信息
                    var parkingInfo = await (from psd in PARKING_SPACE_DISTRIBUTION
                                            join pl in PARKING_LOT on psd.AREA_ID equals pl.AREA_ID
                                            join area in AREA on pl.AREA_ID equals area.AREA_ID
                                            where psd.PARKING_SPACE_ID == record.Park.PARKING_SPACE_ID
                                            select new { ParkingLot = pl, Area = area }).FirstOrDefaultAsync();

                    if (parkingInfo != null)
                    {
                        // 如果指定了区域ID，则过滤
                        if (areaId.HasValue && parkingInfo.Area.AREA_ID != areaId.Value)
                        {
                            continue;
                        }

                        var currentDuration = DateTime.Now - record.Car.PARK_START;

                        currentVehicles.Add(new Models.VehicleStatusResult
                        {
                            LicensePlateNumber = record.Car.LICENCE_PLATE_NUMBER,
                            ParkingSpaceId = record.Park.PARKING_SPACE_ID,
                            AreaId = parkingInfo.Area.AREA_ID,
                            ParkStart = record.Car.PARK_START,
                            CurrentDuration = currentDuration,
                            ParkingLotName = $"停车场{parkingInfo.Area.AREA_ID}",
                            AreaName = $"区域{parkingInfo.Area.AREA_ID}",
                            Status = "在停中",
                            IsCurrentlyParked = true
                        });
                    }
                }

                return currentVehicles;
            }
            catch (Exception)
            {
                return new List<Models.VehicleStatusResult>();
            }
        }

        /// <summary>
        /// 根据车牌号查询车辆状态
        /// </summary>
        public async Task<Models.VehicleStatusResult?> GetVehicleStatusByLicensePlate(string licensePlateNumber)
        {
            try
            {
                // 查找指定车牌号的在停车辆
                var parkedCar = await CAR
                    .Where(c => c.LICENCE_PLATE_NUMBER == licensePlateNumber && !c.PARK_END.HasValue)
                    .Join(PARK,
                          c => new { LicensePlate = c.LICENCE_PLATE_NUMBER, c.PARK_START },
                          p => new { LicensePlate = p.LICENSE_PLATE_NUMBER, PARK_START = p.PARK_START },
                          (c, p) => new { Car = c, Park = p })
                    .FirstOrDefaultAsync();

                if (parkedCar == null)
                    return null; // 未找到在停记录

                // 获取停车场和区域信息
                var parkingInfo = await (from psd in PARKING_SPACE_DISTRIBUTION
                                        join pl in PARKING_LOT on psd.AREA_ID equals pl.AREA_ID
                                        join area in AREA on pl.AREA_ID equals area.AREA_ID
                                        where psd.PARKING_SPACE_ID == parkedCar.Park.PARKING_SPACE_ID
                                        select new { ParkingLot = pl, Area = area }).FirstOrDefaultAsync();

                if (parkingInfo == null)
                    return null;

                var currentDuration = DateTime.Now - parkedCar.Car.PARK_START;

                return new Models.VehicleStatusResult
                {
                    LicensePlateNumber = parkedCar.Car.LICENCE_PLATE_NUMBER,
                    ParkingSpaceId = parkedCar.Park.PARKING_SPACE_ID,
                    AreaId = parkingInfo.Area.AREA_ID,
                    ParkStart = parkedCar.Car.PARK_START,
                    CurrentDuration = currentDuration,
                    ParkingLotName = $"停车场{parkingInfo.Area.AREA_ID}",
                    AreaName = $"区域{parkingInfo.Area.AREA_ID}",
                    Status = "在停中",
                    IsCurrentlyParked = true
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region 支付记录查询方法

        /// <summary>
        /// 获取指定时间范围内的支付记录
        /// </summary>
        public Task<List<Models.ParkingPaymentRecord>> GetPaymentRecordsInTimeRange(DateTime start, DateTime end)
        {
            var records = _paymentRecords.Values
                .Where(r => r.ParkStart >= start && r.ParkStart <= end)
                .ToList();
            return Task.FromResult(records);
        }

        /// <summary>
        /// 获取指定时间范围和区域的支付记录
        /// </summary>
        public Task<List<Models.ParkingPaymentRecord>> GetPaymentRecordsInTimeRange(DateTime start, DateTime end, int? areaId)
        {
            var records = _paymentRecords.Values
                .Where(r => r.ParkStart >= start && r.ParkStart <= end)
                .ToList();

            // 如果指定了区域，需要根据车位ID筛选
            if (areaId.HasValue)
            {
                // 这里需要根据车位ID和区域ID的关联关系来筛选
                // 由于内存中的支付记录没有车位ID信息，我们需要从数据库查询
                // 暂时返回所有记录，让调用方处理筛选逻辑
                Console.WriteLine($"[DEBUG] 区域筛选暂未实现，返回所有记录: {records.Count}条");
            }

            return Task.FromResult(records);
        }



        #endregion

        #region 2.8.5 停车场信息统计报表功能

        /// <summary>
        /// 获取停车场统计报表数据
        /// </summary>
        public async Task<Controllers.ParkingController.ParkingStatisticsReportResponseDto> GetParkingStatisticsReport(
            DateTime startDate, DateTime endDate, int? areaId)
        {
            try
            {
                Console.WriteLine($"[DEBUG] 开始生成统计报表: {startDate} - {endDate}, 区域: {areaId}");
                
                // 先检查数据库表结构
                Console.WriteLine("[DEBUG] 检查数据库表结构...");
                try
                {
                    var carColumns = await Database.SqlQueryRaw<string>("SELECT COLUMN_NAME FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'CAR' ORDER BY COLUMN_ID").ToListAsync();
                    Console.WriteLine($"[DEBUG] CAR表列名: {string.Join(", ", carColumns)}");
                    
                    var parkColumns = await Database.SqlQueryRaw<string>("SELECT COLUMN_NAME FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'PARK' ORDER BY COLUMN_ID").ToListAsync();
                    Console.WriteLine($"[DEBUG] PARK表列名: {string.Join(", ", parkColumns)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] 获取表结构失败: {ex.Message}");
                }

                // 使用原生SQL查询，避免EF Core的布尔值转换问题
                var sql = @"
                    SELECT 
                        c.LICENCE_PLATE_NUMBER as LICENCE_PLATE_NUMBER,
                        c.PARK_START,
                        c.PARK_END,
                        p.PARKING_SPACE_ID
                    FROM CAR c
                                          INNER JOIN PARK p ON c.LICENCE_PLATE_NUMBER = p.LICENSE_PLATE_NUMBER 
                                     AND c.PARK_START = p.PARK_START
                    WHERE c.PARK_END IS NOT NULL 
                      AND c.PARK_START >= {0} 
                      AND c.PARK_END <= {1}";

                var parameters = new List<object> { startDate, endDate };

                // 如果指定了区域，添加区域筛选
                if (areaId.HasValue)
                {
                    sql += @" AND EXISTS (
                        SELECT 1 FROM PARKING_SPACE_DISTRIBUTION psd 
                        WHERE psd.PARKING_SPACE_ID = CAST(p.PARKING_SPACE_ID AS NUMBER)
                          AND psd.AREA_ID = {" + parameters.Count + @"}
                    )";
                    parameters.Add(areaId.Value);
                }

                sql += " ORDER BY c.PARK_START";

                Console.WriteLine($"[DEBUG] 执行SQL: {sql}");
                Console.WriteLine($"[DEBUG] 参数: startDate={startDate}, endDate={endDate}, areaId={areaId}");
                
                // 先测试简单的表查询
                Console.WriteLine("[DEBUG] 测试简单查询...");
                try
                {
                    var carCount = await Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM CAR").FirstOrDefaultAsync();
                    Console.WriteLine($"[DEBUG] CAR表记录数: {carCount}");
                    
                    var parkCount = await Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM PARK").FirstOrDefaultAsync();
                    Console.WriteLine($"[DEBUG] PARK表记录数: {parkCount}");
                    
                    // 测试单个表的列查询
                    var carSample = await Database.SqlQueryRaw<object>("SELECT * FROM CAR WHERE ROWNUM <= 1").FirstOrDefaultAsync();
                    Console.WriteLine($"[DEBUG] CAR表样本数据: {carSample}");
                    
                    // 检查CAR表的列名
                    var carColumns = await Database.SqlQueryRaw<string>("SELECT COLUMN_NAME FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'CAR' ORDER BY COLUMN_ID").ToListAsync();
                    Console.WriteLine($"[DEBUG] CAR表列名: {string.Join(", ", carColumns)}");
                    
                    // 检查PARK表的列名
                    var parkColumns = await Database.SqlQueryRaw<string>("SELECT COLUMN_NAME FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'PARK' ORDER BY COLUMN_ID").ToListAsync();
                    Console.WriteLine($"[DEBUG] PARK表列名: {string.Join(", ", parkColumns)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] 简单查询测试失败: {ex.Message}");
                    Console.WriteLine($"[DEBUG] 错误详情: {ex}");
                }

                // 先测试简单查询
                var totalCarsInDb = await Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM CAR").FirstOrDefaultAsync();
                Console.WriteLine($"[DEBUG] 数据库中CAR表总记录数: {totalCarsInDb}");

                var testCarsInDb = await Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM CAR WHERE LICENCE_PLATE_NUMBER LIKE 'STAT%'").FirstOrDefaultAsync();
                Console.WriteLine($"[DEBUG] 数据库中测试记录数: {testCarsInDb}");

                // 执行原生SQL查询
                Console.WriteLine("[DEBUG] 准备执行复杂SQL查询...");
                List<ParkingRecord> records;
                try
                {
                    records = await Database.SqlQueryRaw<ParkingRecord>(sql, parameters.ToArray()).ToListAsync();
                    Console.WriteLine($"[DEBUG] 查询到 {records.Count} 条停车记录");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] 复杂SQL查询失败: {ex.Message}");
                    Console.WriteLine($"[DEBUG] 错误详情: {ex}");
                    Console.WriteLine($"[DEBUG] SQL语句: {sql}");
                    Console.WriteLine($"[DEBUG] 参数: {string.Join(", ", parameters)}");
                    
                    // 尝试更简单的查询
                    Console.WriteLine("[DEBUG] 尝试更简单的查询...");
                    try
                    {
                        records = await Database.SqlQueryRaw<ParkingRecord>(
                            "SELECT LICENCE_PLATE_NUMBER, PARK_START, PARK_END, PARKING_SPACE_ID FROM PARK WHERE ROWNUM <= 5"
                        ).ToListAsync();
                        Console.WriteLine($"[DEBUG] 简单查询成功，获取到 {records.Count} 条记录");
                        Console.WriteLine($"[DEBUG] 使用简单查询结果继续处理");
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"[DEBUG] 简单查询也失败了: {ex2.Message}");
                        
                        // 最后尝试使用EF Core查询
                        Console.WriteLine("[DEBUG] 尝试使用EF Core查询...");
                        try
                        {
                                                    var efRecords = await (from c in CAR
                                              join p in PARK on new { LicensePlate = c.LICENCE_PLATE_NUMBER, ParkStart = c.PARK_START } equals new { LicensePlate = p.LICENSE_PLATE_NUMBER, ParkStart = p.PARK_START }
                                              where c.PARK_END.HasValue
                                                && c.PARK_START >= startDate
                                                && c.PARK_END <= endDate
                                              select new ParkingRecord
                                              {
                                                  LICENSE_PLATE_NUMBER = c.LICENCE_PLATE_NUMBER,
                                                  PARK_START = c.PARK_START,
                                                  PARK_END = c.PARK_END,
                                                  PARKING_SPACE_ID = p.PARKING_SPACE_ID
                                              }).ToListAsync();
                            
                            records = efRecords;
                            Console.WriteLine($"[DEBUG] EF Core查询成功，获取到 {records.Count} 条记录");
                        }
                        catch (Exception ex3)
                        {
                            Console.WriteLine($"[DEBUG] EF Core查询也失败了: {ex3.Message}");
                            throw new Exception($"统计报表生成失败: {ex.Message}. 所有查询方法都失败: SQL={ex2.Message}, EF={ex3.Message}");
                        }
                    }
                }

                // 计算基础统计数据
                var totalParkingCount = records.Count;
                var totalRevenue = 0m;
                var totalParkingHours = 0.0;

                // 计算收入和停车时长
                foreach (var record in records)
                {
                    if (record.PARK_END.HasValue)
                    {
                        var duration = record.PARK_END.Value - record.PARK_START;
                        var hours = Math.Ceiling(duration.TotalHours);
                        totalParkingHours += duration.TotalHours;

                        // 使用固定费率计算（简化处理）
                        totalRevenue += (decimal)(hours * 5); // 5元/小时
                    }
                }

                var averageParkingHours = totalParkingCount > 0 ? totalParkingHours / totalParkingCount : 0;

                // 计算每小时收入统计
                var hourlyTraffic = new List<Controllers.ParkingController.HourlyTrafficDto>();
                for (int hour = 0; hour < 24; hour++)
                {
                    // 统计该时间段内出场的车辆及其费用
                    var hourlyExits = records
                        .Where(r => r.PARK_END.HasValue && r.PARK_END.Value.Hour == hour)
                        .ToList();
                    
                    var exitCount = hourlyExits.Count;
                    var revenue = hourlyExits.Sum(r => {
                        if (r.PARK_END.HasValue)
                        {
                            var duration = r.PARK_END.Value - r.PARK_START;
                            var hours = Math.Ceiling(duration.TotalHours);
                            return (decimal)(hours * 5); // 5元/小时
                        }
                        return 0m;
                    });

                    hourlyTraffic.Add(new Controllers.ParkingController.HourlyTrafficDto
                    {
                        Hour = hour,
                        Revenue = revenue,
                        ExitCount = exitCount
                    });
                }

                // 计算每日统计
                var dailyStats = records
                    .GroupBy(r => r.PARK_START.Date)
                    .Select(g => new Controllers.ParkingController.DailyStatisticsDto
                    {
                        Date = g.Key,
                        ParkingCount = g.Count(),
                        Revenue = g.Sum(r => {
                            if (r.PARK_END.HasValue)
                            {
                                var duration = r.PARK_END.Value - r.PARK_START;
                                var hours = Math.Ceiling(duration.TotalHours);
                                return (decimal)(hours * 5); // 5元/小时
                            }
                            return 0m;
                        }),
                        AverageParkingHours = g.Average(r => {
                            if (r.PARK_END.HasValue)
                            {
                                return (r.PARK_END.Value - r.PARK_START).TotalHours;
                            }
                            return 0;
                        })
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                // 获取区域名称
                var areaName = "所有停车场";
                if (areaId.HasValue)
                {
                    areaName = $"停车场{areaId.Value}";
                }

                Console.WriteLine($"[DEBUG] 统计完成: 总记录{totalParkingCount}条, 总收入{totalRevenue}元");

                return new Controllers.ParkingController.ParkingStatisticsReportResponseDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    AreaId = areaId,
                    AreaName = areaName,
                    TotalParkingCount = totalParkingCount,
                    TotalRevenue = totalRevenue,
                    AverageParkingHours = averageParkingHours,
                    HourlyTraffic = hourlyTraffic,
                    DailyStatistics = dailyStats,
                    GeneratedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 生成统计报表时发生错误: {ex.Message}");
                Console.WriteLine($"[ERROR] 堆栈跟踪: {ex.StackTrace}");
                return new Controllers.ParkingController.ParkingStatisticsReportResponseDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    AreaId = areaId,
                    AreaName = areaId.HasValue ? $"停车场{areaId.Value}" : "所有停车场",
                    TotalParkingCount = 0,
                    TotalRevenue = 0,
                    AverageParkingHours = 0,
                    HourlyTraffic = new List<Controllers.ParkingController.HourlyTrafficDto>(),
                    DailyStatistics = new List<Controllers.ParkingController.DailyStatisticsDto>(),
                    GeneratedAt = DateTime.Now
                };
            }
        }

        // 用于原生SQL查询的临时类
        public class ParkingRecord
        {
            public string LICENSE_PLATE_NUMBER { get; set; } = string.Empty;
            public DateTime PARK_START { get; set; }
            public DateTime? PARK_END { get; set; }
            public int PARKING_SPACE_ID { get; set; }
        }

        #endregion

    }
}
