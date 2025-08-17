using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using System.ComponentModel.DataAnnotations;

namespace oracle_backend.Controllers
{
    [Route("api/Parking")]
    [ApiController]
    public class ParkingController : ControllerBase
    {
        /*
         * 优化后的API结构说明：
         * 
         * 1. 停车场信息管理
         *    - GET /api/Parking/GetParkingLotInfo/{areaId} - 获取停车场信息
         *    - PATCH /api/Parking/UpdateParkingLotInfo/{areaId} - 更新停车场信息（合并了原来的UpdateStatus）
         *    - PUT /api/Parking/ForceMaintenanceStatus - 强制设置维护状态
         * 
         * 2. 车位状态查询
         *    - GET /api/Parking/summary - 获取停车场概览
         *    - GET /api/Parking/spaces - 获取车位状态（支持areaId查询参数）
         *    - GET /api/Parking/GetSingleParkingSpaceStatus/{parkingSpaceId} - 获取单个车位状态
         * 
         * 3. 车辆出入计费
         *    - POST /api/Parking/Entry - 车辆入场
         *    - POST /api/Parking/Exit - 车辆出场
         *    - POST /api/Parking/Pay - 停车费支付
         * 
         * 4. 车辆状态查询
         *    - GET /api/Parking/CurrentVehicles - 获取在停车辆（支持areaId查询参数）
         *    - GET /api/Parking/VehicleStatus/{licensePlate} - 根据车牌查询车辆状态
         *    - GET /api/Parking/ParkingLots - 获取停车场列表
         * 
         * 5. 支付记录查询
         *    - GET /api/Parking/PaymentRecords - 获取支付记录（支持status查询参数：paid/unpaid/all）
         * 
         * 6. 统计报表
         *    - POST /api/Parking/StatisticsReport - 生成统计报表
         * 
         * 7. 系统功能
         *    - GET /api/Parking/Status - API状态检查
         *    - GET /api/Parking/GetMemoryConfig - 获取内存配置
         *    - GET /api/Parking/DiagnoseParkingData - 数据诊断
         */
        private readonly ParkingContext _parkingContext;
        private readonly AccountDbContext _accountContext;
        private readonly ILogger<ParkingController> _logger;

        public ParkingController(ParkingContext parkingContext, AccountDbContext accountContext, ILogger<ParkingController> logger)
        {
            _parkingContext = parkingContext;
            _accountContext = accountContext;
            _logger = logger;
        }

        #region DTO类定义

        /// <summary>
        /// 停车场信息查询DTO
        /// </summary>
        public class ParkingLotQueryDto
        {
            public int AreaId { get; set; }
            public string? OperatorAccount { get; set; }
        }

        /// <summary>
        /// 停车场状态更新DTO
        /// </summary>
        public class ParkingLotStatusUpdateDto
        {
            [Required(ErrorMessage = "停车场区域ID为必填项")]
            public int AreaId { get; set; }

            [Required(ErrorMessage = "状态为必填项")]
            public string Status { get; set; } = string.Empty;

            [Required(ErrorMessage = "操作员账号为必填项")]
            public string OperatorAccount { get; set; } = string.Empty;

            public string? Remarks { get; set; }
        }

        /// <summary>
        /// 停车场信息响应DTO
        /// </summary>
        public class ParkingLotInfoResponseDto
        {
            public int AreaId { get; set; }
            public int ParkingFee { get; set; }
            public int TotalSpaces { get; set; }
            public int OccupiedSpaces { get; set; }
            public int AvailableSpaces { get; set; }
            public double OccupancyRate { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime LastUpdateTime { get; set; }
            public bool CanPark { get; set; }
        }

        /// <summary>
        /// 车位状态查询请求DTO (2.8.2功能)
        /// </summary>
        public class ParkingSpaceQueryDto
        {
            public int? AreaId { get; set; }
            public string? OperatorAccount { get; set; }
        }

        /// <summary>
        /// 停车场概览响应DTO (2.8.2功能)
        /// </summary>
        public class ParkingLotOverviewDto
        {
            public int AreaId { get; set; }
            public int ParkingFee { get; set; }
            public int AreaSize { get; set; }
            public string Status { get; set; } = string.Empty;
            public int TotalSpaces { get; set; }
            public int OccupiedSpaces { get; set; }
            public int AvailableSpaces { get; set; }
            public double OccupancyRate { get; set; }
            public bool CanPark { get; set; }
            public DateTime LastUpdateTime { get; set; }
            public string? Error { get; set; }
        }

        /// <summary>
        /// 车位状态详情DTO (2.8.2功能)
        /// </summary>
        public class ParkingSpaceStatusDto
        {
            public int ParkingSpaceId { get; set; }
            public int AreaId { get; set; }
            public bool IsOccupied { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? LicensePlateNumber { get; set; }
            public DateTime? ParkStart { get; set; }
            public string? ParkDuration { get; set; }
            public DateTime UpdateTime { get; set; }
        }

        /// <summary>
        /// 停车场车位状态统计DTO (2.8.2功能)
        /// </summary>
        public class ParkingStatisticsDto
        {
            public int TotalSpaces { get; set; }
            public int OccupiedSpaces { get; set; }
            public int AvailableSpaces { get; set; }
            public double OccupancyRate { get; set; }
        }

        /// <summary>
        /// 停车场车位状态响应DTO (2.8.2功能)
        /// </summary>
        public class ParkingSpaceStatusResponseDto
        {
            public int AreaId { get; set; }
            public string ParkingLotStatus { get; set; } = string.Empty;
            public ParkingStatisticsDto Statistics { get; set; } = new();
            public List<ParkingSpaceStatusDto> ParkingSpaces { get; set; } = new();
            public DateTime Timestamp { get; set; }
            public bool CanPark { get; set; }
            public string? Message { get; set; }
        }

        /// <summary>
        /// API标准响应DTO (2.8.2功能)
        /// </summary>
        public class ApiResponseDto<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
            public DateTime Timestamp { get; set; }
            public int Total { get; set; }
            public string? Message { get; set; }
            public string? Error { get; set; }
        }

        #region 缺失的DTO类定义

        /// <summary>
        /// 停车场信息更新DTO
        /// </summary>
        public class ParkingLotInfoDto
        {
            [Required(ErrorMessage = "停车场区域ID为必填项")]
            public int AreaId { get; set; }

            [Required(ErrorMessage = "停车费为必填项")]
            [Range(0, int.MaxValue, ErrorMessage = "停车费必须大于等于0")]
            public int ParkingFee { get; set; }

            [Required(ErrorMessage = "状态为必填项")]
            [StringLength(20, ErrorMessage = "状态长度不能超过20个字符")]
            public string Status { get; set; } = string.Empty;

            // [Range(1, int.MaxValue, ErrorMessage = "车位数必须大于0")]
            // public int? ParkingSpaceCount { get; set; }

            [Required(ErrorMessage = "操作员账号为必填项")]
            [StringLength(50, ErrorMessage = "操作员账号长度不能超过50个字符")]
            public string OperatorAccount { get; set; } = string.Empty;

            public string? Remarks { get; set; }
        }

        /// <summary>
        /// 车辆入场DTO
        /// </summary>
        public class VehicleEntryDto
        {
            [Required(ErrorMessage = "车牌号为必填项")]
            [StringLength(20, ErrorMessage = "车牌号长度不能超过20个字符")]
            public string LicensePlateNumber { get; set; } = string.Empty;

            [Required(ErrorMessage = "车位ID为必填项")]
            [Range(1, int.MaxValue, ErrorMessage = "车位ID必须大于0")]
            public int ParkingSpaceId { get; set; }

            [Required(ErrorMessage = "操作员账号为必填项")]
            [StringLength(50, ErrorMessage = "操作员账号长度不能超过50个字符")]
            public string OperatorAccount { get; set; } = string.Empty;

            public string? Remarks { get; set; }
        }

        /// <summary>
        /// 车辆出场DTO
        /// </summary>
        public class VehicleExitDto
        {
            [Required(ErrorMessage = "车牌号为必填项")]
            [StringLength(20, ErrorMessage = "车牌号长度不能超过20个字符")]
            public string LicensePlateNumber { get; set; } = string.Empty;

            [Required(ErrorMessage = "操作员账号为必填项")]
            [StringLength(50, ErrorMessage = "操作员账号长度不能超过50个字符")]
            public string OperatorAccount { get; set; } = string.Empty;

            public string? Remarks { get; set; }
        }

        /// <summary>
        /// 生成支付记录请求DTO
        /// </summary>
        public class GeneratePaymentRecordsDto
        {
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public bool ForceRegenerate { get; set; } = false; // 是否强制重新生成
        }



        #endregion

        #endregion

        #region API状态检测

        /// <summary>
        /// API状态检测 - 简单的健康检查端点
        /// </summary>
        [HttpGet("Status")]
        public IActionResult GetApiStatus()
        {
            return Ok(new { 
                status = "online", 
                message = "停车场API服务正常运行",
                timestamp = DateTime.Now 
            });
        }

        #endregion

        #region 停车场信息管理功能 (用例2.8.1)

        /// <summary>
        /// 停车场信息管理 - 获取停车场信息
        /// </summary>
        [HttpGet("GetParkingLotInfo/{areaId}")]
        public async Task<IActionResult> GetParkingLotInfo(int areaId, [FromQuery] string? operatorAccount = null)
        {
            try
            {
                _logger.LogInformation("获取停车场信息：区域ID {AreaId}, 操作员 {OperatorAccount}", areaId, operatorAccount);

                // 验证操作员权限
                if (!string.IsNullOrEmpty(operatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(operatorAccount, 2);
                    if (!hasPermission)
                    {
                        _logger.LogWarning("操作员 {OperatorAccount} 权限不足", operatorAccount);
                        return BadRequest(new { error = "权限不足，需要管理员或部门经理权限" });
                    }
                }

                // 检查停车场是否存在
                var parkingLotExists = await _parkingContext.ParkingLotExists(areaId);
                if (!parkingLotExists)
                {
                    _logger.LogWarning("停车场区域 {AreaId} 不存在", areaId);
                    return NotFound(new { error = "停车场区域不存在" });
                }

                // 获取停车场信息
                var parkingLot = await _parkingContext.GetParkingLotById(areaId);
                if (parkingLot == null)
                {
                    return NotFound(new { error = "停车场信息不存在" });
                }

                // 获取车位信息
                var parkingSpaces = await _parkingContext.GetParkingSpacesByArea(areaId);
                var totalSpaces = await _parkingContext.GetParkingLotSpaceCount(areaId); // 使用新方法获取车位数
                // 使用统计方法获取占用车位数，避免直接布尔比较
                var statistics = await _parkingContext.GetParkingStatusStatistics(areaId);
                var occupiedSpaces = statistics.OccupiedSpaces;
                var availableSpaces = totalSpaces - occupiedSpaces;

                // 获取停车场实际状态
                var currentStatus = _parkingContext.GetParkingLotStatus(areaId);
                
                var parkingInfo = new ParkingLotInfoResponseDto
                {
                    AreaId = parkingLot.AREA_ID,
                    ParkingFee = parkingLot.PARKING_FEE,
                    TotalSpaces = totalSpaces,
                    OccupiedSpaces = occupiedSpaces,
                    AvailableSpaces = availableSpaces,
                    OccupancyRate = totalSpaces > 0 ? Math.Round((double)occupiedSpaces / totalSpaces * 100, 2) : 0,
                    Status = currentStatus,
                    LastUpdateTime = DateTime.Now,
                    CanPark = availableSpaces > 0 && currentStatus == "正常运营"
                };

                _logger.LogInformation("成功获取停车场信息：区域ID {AreaId}", areaId);
                return Ok(parkingInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取停车场信息时发生错误：{AreaId}", areaId);
                return StatusCode(500, new { 
                    error = "服务器内部错误", 
                    details = ex.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        /// <summary>
        /// 停车场信息管理 - 更新停车场信息（合并了原来的UpdateParkingLotInfo和UpdateStatus）
        /// </summary>
        [HttpPatch("UpdateParkingLotInfo/{areaId}")]
        public async Task<IActionResult> UpdateParkingLotInfo(int areaId, [FromBody] ParkingLotInfoDto dto)
        {
            _logger.LogInformation("开始更新停车场信息：区域ID {AreaId}", areaId);

            // 检查模型验证
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                _logger.LogWarning("更新停车场信息模型验证失败：{Errors}", string.Join(", ", errors));
                return BadRequest(new { error = "输入数据验证失败", details = errors });
            }

            try
            {
                // 1. 验证操作员权限（管理员权限）
                if (!string.IsNullOrEmpty(dto.OperatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(dto.OperatorAccount, 1);
                    if (!hasPermission)
                    {
                        _logger.LogWarning("操作员 {OperatorAccount} 权限不足", dto.OperatorAccount);
                        return BadRequest(new { error = "操作员权限不足，需要管理员权限" });
                    }
                }

                // 2. 检查停车场是否存在
                var parkingLotExists = await _parkingContext.ParkingLotExists(areaId);
                if (!parkingLotExists)
                {
                    _logger.LogWarning("停车场区域 {AreaId} 不存在", areaId);
                    return BadRequest(new { error = "停车场区域不存在" });
                }

                // 3. 验证状态值
                var validStatuses = new[] { "正常运营", "维护中", "暂停服务" };
                if (!validStatuses.Contains(dto.Status))
                {
                    return BadRequest(new { error = $"无效的状态值。有效值为：{string.Join(", ", validStatuses)}" });
                }

                // 4. 检查是否有车辆在停车场（如果要设置为维护中）
                if (dto.Status == "维护中")
                {
                    // 使用统计方法获取占用车位数，避免直接布尔比较
                    var statistics = await _parkingContext.GetParkingStatusStatistics(areaId);
                    var occupiedSpaces = statistics.OccupiedSpaces;
                    
                    if (occupiedSpaces > 0)
                    {
                        _logger.LogWarning("停车场 {AreaId} 当前有 {OccupiedSpaces} 辆车，无法设置为维护中", areaId, occupiedSpaces);
                        return BadRequest(new { 
                            error = "当前有车辆，是否强制设为维护中",
                            occupiedVehicles = occupiedSpaces,
                            canForceMaintenance = true
                        });
                    }
                }

                // 5. 更新停车场信息（不包含车位数修改）
                var updateSuccess = await _parkingContext.UpdateParkingLotInfo(areaId, dto.ParkingFee, dto.Status, null);
                if (!updateSuccess)
                {
                    _logger.LogError("更新停车场信息失败：区域ID {AreaId}", areaId);
                    return BadRequest(new { error = "更新停车场信息失败" });
                }

                _logger.LogInformation("成功更新停车场信息：区域ID {AreaId}, 停车费 {ParkingFee}, 状态 {Status}", 
                    areaId, dto.ParkingFee, dto.Status);

                // 返回更新后的信息
                return Ok(new
                {
                    message = "停车场信息更新成功",
                    areaId = areaId,
                    parkingFee = dto.ParkingFee,
                    status = dto.Status,
                    updateTime = DateTime.Now,
                    operatorAccount = dto.OperatorAccount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新停车场信息时发生错误：{AreaId}", areaId);
                return StatusCode(500, new { 
                    error = "服务器内部错误，更新停车场信息失败",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// 停车场信息管理 - 强制设置维护状态（当有车辆时）
        /// </summary>
        [HttpPut("ForceMaintenanceStatus")]
        public async Task<IActionResult> ForceMaintenanceStatus([FromBody] ParkingLotInfoDto dto)
        {
            _logger.LogInformation("强制设置停车场维护状态：区域ID {AreaId}", dto.AreaId);

            try
            {
                // 1. 验证操作员权限（需要管理员权限）
                if (!string.IsNullOrEmpty(dto.OperatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(dto.OperatorAccount, 1);
                    if (!hasPermission)
                    {
                        _logger.LogWarning("操作员 {OperatorAccount} 权限不足", dto.OperatorAccount);
                        return BadRequest(new { error = "操作员权限不足，需要管理员权限" });
                    }
                }

                // 2. 检查停车场是否存在
                var parkingLotExists = await _parkingContext.ParkingLotExists(dto.AreaId);
                if (!parkingLotExists)
                {
                    return BadRequest(new { error = "停车场区域不存在" });
                }

                // 3. 强制更新为维护状态
                var updateSuccess = await _parkingContext.UpdateParkingLotInfo(dto.AreaId, dto.ParkingFee, "维护中");
                if (!updateSuccess)
                {
                    return BadRequest(new { error = "更新停车场信息失败" });
                }

                _logger.LogInformation("强制设置停车场维护状态成功：区域ID {AreaId}", dto.AreaId);

                return Ok(new
                {
                    message = "停车场已强制设置为维护状态",
                    areaId = dto.AreaId,
                    status = "维护中",
                    updateTime = DateTime.Now,
                    warning = "当前有车辆在停车场，请确保安全后再进行维护"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制设置维护状态时发生错误：{AreaId}", dto.AreaId);
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }



        #endregion

        #region 车位状态查询功能 (用例2.8.2)

        /// <summary>
        /// 车位状态查询 - 获取停车场概览统计 (RESTful风格)
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetParkingSummary([FromQuery] string? operatorAccount = null)
        {
            try
            {
                _logger.LogInformation("获取所有停车场概览，操作员 {OperatorAccount}", operatorAccount);

                // 验证操作员权限（员工/商户/管理员都可以查询）
                if (!string.IsNullOrEmpty(operatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(operatorAccount, 3);
                    if (!hasPermission)
                    {
                        _logger.LogWarning("操作员 {OperatorAccount} 权限不足", operatorAccount);
                        return BadRequest(new { success = false, error = "权限不足，需要员工及以上权限" });
                    }
                }

                // 获取所有停车场
                var allParkingLots = await _parkingContext.PARKING_LOT.ToListAsync();
                
                if (!allParkingLots.Any())
                {
                    _logger.LogInformation("系统中暂无停车场数据");
                    return Ok(new { 
                        success = true,
                        data = new List<object>(),
                        timestamp = DateTime.Now,
                        total = 0,
                        message = "系统中暂无停车场数据"
                    });
                }

                var overviewList = new List<ParkingLotOverviewDto>();

                foreach (var lot in allParkingLots)
                {
                    try
                    {
                        // 获取停车场基本信息
                        var area = await _parkingContext.AREA.FirstOrDefaultAsync(a => a.AREA_ID == lot.AREA_ID);
                        
                        // 获取车位统计信息
                        var statistics = await _parkingContext.GetParkingStatusStatistics(lot.AREA_ID);
                        
                        // 获取停车场状态
                        var status = _parkingContext.GetParkingLotStatus(lot.AREA_ID);

                        var overview = new ParkingLotOverviewDto
                        {
                            AreaId = lot.AREA_ID,
                            ParkingFee = lot.PARKING_FEE,
                            AreaSize = area?.AREA_SIZE ?? 0,
                            Status = status,
                            TotalSpaces = statistics.TotalSpaces,
                            OccupiedSpaces = statistics.OccupiedSpaces,
                            AvailableSpaces = statistics.AvailableSpaces,
                            OccupancyRate = statistics.OccupancyRate,
                            CanPark = statistics.AvailableSpaces > 0 && status == "正常运营",
                            LastUpdateTime = DateTime.Now
                        };

                        overviewList.Add(overview);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取停车场 {AreaId} 概览信息时发生错误", lot.AREA_ID);
                        
                        // 即使某个停车场出错，也要继续处理其他停车场
                        var errorOverview = new ParkingLotOverviewDto
                        {
                            AreaId = lot.AREA_ID,
                            ParkingFee = lot.PARKING_FEE,
                            AreaSize = 0,
                            Status = "数据异常",
                            TotalSpaces = 0,
                            OccupiedSpaces = 0,
                            AvailableSpaces = 0,
                            OccupancyRate = 0.0,
                            CanPark = false,
                            LastUpdateTime = DateTime.Now,
                            Error = "数据获取异常"
                        };
                        
                        overviewList.Add(errorOverview);
                    }
                }

                var response = new ApiResponseDto<List<ParkingLotOverviewDto>>
                {
                    Success = true,
                    Data = overviewList.OrderBy(lot => lot.AreaId).ToList(),
                    Timestamp = DateTime.Now,
                    Total = overviewList.Count
                };

                _logger.LogInformation("成功获取所有停车场概览，共 {Count} 个停车场", overviewList.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取停车场概览时发生错误");
                return StatusCode(500, new { 
                    success = false,
                    error = "服务器内部错误", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// 车位状态查询 - 获取车位状态（支持查询所有停车场或指定停车场）
        /// </summary>
        [HttpGet("spaces")]
        public async Task<IActionResult> GetParkingSpaces([FromQuery] string? operatorAccount = null, [FromQuery] int? areaId = null)
        {
            try
            {
                _logger.LogInformation("查询车位状态，操作员 {OperatorAccount}, 区域ID {AreaId}", operatorAccount, areaId);

                // 验证操作员权限
                if (!string.IsNullOrEmpty(operatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(operatorAccount, 3);
                    if (!hasPermission)
                    {
                        _logger.LogWarning("操作员 {OperatorAccount} 权限不足", operatorAccount);
                        return BadRequest(new { error = "权限不足，需要员工及以上权限" });
                    }
                }

                var allSpaces = new List<object>();

                if (areaId.HasValue)
                {
                    _logger.LogInformation("查询指定停车场 {AreaId} 的车位状态", areaId.Value);
                    
                    // 查询指定停车场
                    var parkingLotExists = await _parkingContext.ParkingLotExists(areaId.Value);
                    if (!parkingLotExists)
                    {
                        _logger.LogWarning("停车场区域 {AreaId} 不存在", areaId.Value);
                        return NotFound(new { error = "停车场区域不存在" });
                    }

                    _logger.LogInformation("停车场 {AreaId} 存在，开始查询车位状态", areaId.Value);
                    var spaces = await _parkingContext.GetParkingSpaceStatuses(areaId.Value);
                    _logger.LogInformation("查询到 {Count} 个车位状态", spaces.Count);
                    
                    foreach (var space in spaces)
                    {
                        allSpaces.Add(new
                        {
                            parkingSpaceId = space.ParkingSpaceId,
                            areaId = areaId.Value,
                            occupied = space.IsOccupied ? 1 : 0,
                            status = space.IsOccupied ? "占用" : "空闲",
                            licensePlateNumber = space.LicensePlateNumber,
                            parkStart = space.ParkStart,
                            updateTime = space.UpdateTime
                        });
                    }
                }
                else
                {
                    // 查询所有停车场
                    var allParkingLots = await _parkingContext.PARKING_LOT.ToListAsync();
                    foreach (var lot in allParkingLots)
                    {
                        var spaces = await _parkingContext.GetParkingSpaceStatuses(lot.AREA_ID);
                        foreach (var space in spaces)
                        {
                            allSpaces.Add(new
                            {
                                parkingSpaceId = space.ParkingSpaceId,
                                areaId = lot.AREA_ID,
                                occupied = space.IsOccupied ? 1 : 0,
                                status = space.IsOccupied ? "占用" : "空闲",
                                licensePlateNumber = space.LicensePlateNumber,
                                parkStart = space.ParkStart,
                                updateTime = space.UpdateTime
                            });
                        }
                    }
                }

                _logger.LogInformation("准备返回响应，总共 {Count} 个车位", allSpaces.Count);
                
                var response = new
                {
                    success = true,
                    data = allSpaces.OrderBy(s => ((dynamic)s).areaId).ThenBy(s => ((dynamic)s).parkingSpaceId),
                    timestamp = DateTime.Now,
                    total = allSpaces.Count,
                    areaId = areaId
                };

                _logger.LogInformation("响应准备完成，返回成功");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取车位状态时发生错误");
                return StatusCode(500, new { 
                    success = false,
                    error = "服务器内部错误", 
                    details = ex.Message 
                });
            }
        }





        /// <summary>
        /// 车位状态查询 - 获取单个车位的详细状态
        /// </summary>
        [HttpGet("GetSingleParkingSpaceStatus/{parkingSpaceId}")]
        public async Task<IActionResult> GetSingleParkingSpaceStatus(int parkingSpaceId, [FromQuery] string? operatorAccount = null)
        {
            try
            {
                _logger.LogInformation("查询单个车位状态：车位ID {ParkingSpaceId}, 操作员 {OperatorAccount}", parkingSpaceId, operatorAccount);

                // 验证操作员权限
                if (!string.IsNullOrEmpty(operatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(operatorAccount, 3);
                    if (!hasPermission)
                    {
                        _logger.LogWarning("操作员 {OperatorAccount} 权限不足", operatorAccount);
                        return BadRequest(new { error = "权限不足，需要员工及以上权限" });
                    }
                }

                // 检查车位是否存在
                var parkingSpace = await _parkingContext.PARKING_SPACE.FirstOrDefaultAsync(ps => ps.PARKING_SPACE_ID == parkingSpaceId);
                if (parkingSpace == null)
                {
                    _logger.LogWarning("车位 {ParkingSpaceId} 不存在", parkingSpaceId);
                    return NotFound(new { error = "车位不存在" });
                }

                // 获取车位所在的停车场区域
                var areaInfo = await (from psd in _parkingContext.PARKING_SPACE_DISTRIBUTION
                                     where psd.PARKING_SPACE_ID == parkingSpaceId
                                     select psd.AREA_ID).FirstOrDefaultAsync();

                if (areaInfo == 0)
                {
                    _logger.LogWarning("车位 {ParkingSpaceId} 未分配到任何停车场区域", parkingSpaceId);
                    return NotFound(new { error = "车位未分配到停车场区域" });
                }

                // 获取停车记录
                var parkRecord = await _parkingContext.PARK
                    .Where(p => p.PARKING_SPACE_ID == parkingSpaceId)
                    .OrderByDescending(p => p.PARK_START)
                    .FirstOrDefaultAsync();

                // 安全地获取占用状态，避免直接布尔比较
                var isOccupied = !await _parkingContext.IsParkingSpaceAvailable(parkingSpaceId);
                
                var response = new
                {
                    parkingSpaceId = parkingSpaceId,
                    areaId = areaInfo,
                    isOccupied = isOccupied,
                    status = isOccupied ? "占用" : "空闲",
                    licensePlateNumber = parkRecord?.LICENSE_PLATE_NUMBER,
                    parkStart = parkRecord?.PARK_START,
                    parkDuration = parkRecord?.PARK_START != null ? 
                        DateTime.Now.Subtract(parkRecord.PARK_START).ToString(@"hh\:mm\:ss") : null,
                    updateTime = DateTime.Now
                };

                _logger.LogInformation("成功获取单个车位状态：车位ID {ParkingSpaceId}, 状态 {Status}", 
                    parkingSpaceId, response.status);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取单个车位状态时发生错误：{ParkingSpaceId}", parkingSpaceId);
                return StatusCode(500, new { 
                    error = "服务器内部错误", 
                    details = ex.Message 
                });
            }
        }



        #endregion

        #region 车辆出入计费功能 (用例2.8.3)

        /// <summary>
        /// 车辆入场（记录起始时间并占用车位）
        /// </summary>
        [HttpPost("Entry")]
        public async Task<IActionResult> VehicleEntry([FromBody] VehicleEntryDto dto)
        {
            _logger.LogInformation("车辆入场，请求: 车牌 {LicensePlate}, 车位 {SpaceId}, 操作员 {Operator}", dto.LicensePlateNumber, dto.ParkingSpaceId, dto.OperatorAccount);

            // 模型验证
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { error = "输入数据验证失败", details = errors });
            }

            try
            {
                // 权限校验：管理员
                var hasPermission = await _accountContext.CheckAuthority(dto.OperatorAccount, 1);
                if (!hasPermission)
                {
                    return BadRequest(new { error = "权限不足，需要管理员权限" });
                }

                // 调用重写的VehicleEntry方法
                var (success, message) = await _parkingContext.VehicleEntry(dto.LicensePlateNumber, dto.ParkingSpaceId);
                if (!success)
                {
                    return BadRequest(new { error = message });
                }

                // 读取刚刚写入的起始时间
                var record = await _parkingContext.PARK
                    .Where(p => p.LICENSE_PLATE_NUMBER == dto.LicensePlateNumber)
                    .OrderByDescending(p => p.PARK_START)
                    .FirstOrDefaultAsync();

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Data = new
                    {
                        LicensePlateNumber = dto.LicensePlateNumber,
                        ParkingSpaceId = dto.ParkingSpaceId,
                        ParkStart = record?.PARK_START ?? DateTime.Now
                    },
                    Timestamp = DateTime.Now,
                    Total = 1,
                    Message = "车辆入场成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "车辆入场时发生错误");
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        /// <summary>
        /// 车辆出场（计算费用并释放车位）
        /// </summary>
        [HttpPost("Exit")]
        public async Task<IActionResult> VehicleExit([FromBody] VehicleExitDto dto)
        {
            _logger.LogInformation("车辆出场，请求: 车牌 {LicensePlate}, 操作员 {Operator}", dto.LicensePlateNumber, dto.OperatorAccount);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { error = "输入数据验证失败", details = errors });
            }

            try
            {
                var hasPermission = await _accountContext.CheckAuthority(dto.OperatorAccount, 1);
                if (!hasPermission)
                {
                    return BadRequest(new { error = "权限不足，需要管理员权限" });
                }

                var result = await _parkingContext.VehicleExit(dto.LicensePlateNumber);
                if (result == null)
                {
                    return NotFound(new { error = "未找到车辆当日停车记录" });
                }

                return Ok(new ApiResponseDto<VehicleExitResult>
                {
                    Success = true,
                    Data = result,
                    Timestamp = DateTime.Now,
                    Total = 1,
                    Message = "已计算停车费用"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "车辆出场时发生错误");
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        /// <summary>
        /// 停车费支付（确认收款）
        /// </summary>
        [HttpPost("Pay")]
        public async Task<IActionResult> ProcessPayment([FromBody] ParkingPaymentRequest request, [FromQuery] string operatorAccount)
        {
            _logger.LogInformation("停车费支付，请求: 车牌 {LicensePlate}, 车位 {SpaceId}, 操作员 {Operator}", request.LicensePlateNumber, request.ParkingSpaceId, operatorAccount);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { error = "输入数据验证失败", details = errors });
            }

            try
            {
                var hasPermission = await _accountContext.CheckAuthority(operatorAccount, 1);
                if (!hasPermission)
                {
                    return BadRequest(new { error = "权限不足，需要管理员权限" });
                }

                var ok = await _parkingContext.ProcessParkingPayment(request);
                if (!ok)
                {
                    return BadRequest(new { error = "支付处理失败" });
                }

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Data = new
                    {
                        request.LicensePlateNumber,
                        request.ParkingSpaceId,
                        request.TotalFee,
                        request.PaymentMethod,
                        request.PaymentReference
                    },
                    Timestamp = DateTime.Now,
                    Total = 1,
                    Message = "支付成功，已确认收款"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停车费支付时发生错误");
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        #endregion

        #region 配置查询API（用于SQL脚本获取内存数据）

        /// <summary>
        /// 获取所有停车场的内存配置数据（供SQL脚本调用）
        /// </summary>
        [HttpGet("GetMemoryConfig")]
        public IActionResult GetMemoryConfig()
        {
            try
            {
                var configs = new List<object>();
                
                // 获取所有停车场ID
                var parkingLotIds = new[] { 3001, 3002, 3003 };
                
                foreach (var areaId in parkingLotIds)
                {
                    // 获取状态
                    var status = _parkingContext.GetParkingLotStatus(areaId);
                    configs.Add(new
                    {
                        ConfigKey = $"parking_status_{areaId}",
                        ConfigValue = status,
                        ConfigType = "PARKING_STATUS",
                        AreaId = areaId
                    });
                    
                    // 获取车位数
                    var spaceCount = _parkingContext.GetParkingLotSpaceCount(areaId).Result;
                    configs.Add(new
                    {
                        ConfigKey = $"parking_space_count_{areaId}",
                        ConfigValue = spaceCount.ToString(),
                        ConfigType = "PARKING_SPACE_COUNT",
                        AreaId = areaId
                    });
                }
                
                return Ok(new
                {
                    message = "获取内存配置成功",
                    timestamp = DateTime.Now,
                    configs = configs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取内存配置时发生错误");
                return StatusCode(500, new { error = "获取内存配置失败", details = ex.Message });
            }
        }

        #endregion

        #region 支付记录查询

        /// <summary>
        /// 获取支付记录（支持查询已支付或未支付记录）
        /// </summary>
        [HttpGet("PaymentRecords")]
        public async Task<IActionResult> GetPaymentRecords([FromQuery] string status = "all")
        {
            try
            {
                List<Models.VehicleExitResult> records;
                string message;

                switch (status.ToLower())
                {
                    case "paid":
                        records = await _parkingContext.GetPaidParkingRecords();
                        message = $"获取到{records.Count}条已支付记录";
                        break;
                    case "unpaid":
                        records = await _parkingContext.GetUnpaidParkingRecords();
                        message = $"获取到{records.Count}条未支付记录";
                        break;
                    case "all":
                    default:
                        var paidRecords = await _parkingContext.GetPaidParkingRecords();
                        var unpaidRecords = await _parkingContext.GetUnpaidParkingRecords();
                        records = paidRecords.Concat(unpaidRecords).ToList();
                        message = $"获取到{paidRecords.Count}条已支付记录，{unpaidRecords.Count}条未支付记录";
                        break;
                }
                
                return Ok(new ApiResponseDto<List<Models.VehicleExitResult>>
                {
                    Success = true,
                    Data = records,
                    Message = message,
                    Total = records.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        #endregion

        #region 2.8.4 停车场内车辆状态查询

        /// <summary>
        /// 获取所有停车场列表
        /// </summary>
        [HttpGet("ParkingLots")]
        public async Task<IActionResult> GetParkingLots()
        {
            try
            {
                var parkingLots = await _parkingContext.GetParkingLotList();
                
                return Ok(new ApiResponseDto<List<dynamic>>
                {
                    Success = true,
                    Data = parkingLots,
                    Message = $"获取到{parkingLots.Count}个停车场",
                    Total = parkingLots.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        /// <summary>
        /// 获取在停车辆信息（支持查询所有停车场或指定停车场）
        /// </summary>
        [HttpGet("CurrentVehicles")]
        public async Task<IActionResult> GetCurrentVehicles([FromQuery] int? areaId = null)
        {
            try
            {
                if (areaId.HasValue)
                {
                    // 查询指定停车场
                    var currentVehicles = await _parkingContext.GetCurrentVehiclesByParkingLot(areaId.Value);
                    
                    if (currentVehicles.Count == 0)
                    {
                        return Ok(new ApiResponseDto<List<Models.VehicleStatusResult>>
                        {
                            Success = true,
                            Data = currentVehicles,
                            Message = $"停车场{areaId.Value}当前无在停车辆",
                            Total = 0
                        });
                    }

                    return Ok(new ApiResponseDto<List<Models.VehicleStatusResult>>
                    {
                        Success = true,
                        Data = currentVehicles,
                        Message = $"停车场{areaId.Value}有{currentVehicles.Count}辆在停车辆",
                        Total = currentVehicles.Count
                    });
                }
                else
                {
                    // 查询所有停车场
                    var currentVehicles = await _parkingContext.GetCurrentVehicles();
                    
                    if (currentVehicles.Count == 0)
                    {
                        return Ok(new ApiResponseDto<List<Models.VehicleStatusResult>>
                        {
                            Success = true,
                            Data = currentVehicles,
                            Message = "当前停车场无在停车辆",
                            Total = 0
                        });
                    }

                    return Ok(new ApiResponseDto<List<Models.VehicleStatusResult>>
                    {
                        Success = true,
                        Data = currentVehicles,
                        Message = $"获取到{currentVehicles.Count}辆在停车辆",
                        Total = currentVehicles.Count
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        /// <summary>
        /// 根据车牌号查询车辆状态
        /// </summary>
        [HttpGet("VehicleStatus/{licensePlate}")]
        public async Task<IActionResult> GetVehicleStatus(string licensePlate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(licensePlate))
                {
                    return BadRequest(new { error = "车牌号不能为空" });
                }

                var vehicleStatus = await _parkingContext.GetVehicleStatusByLicensePlate(licensePlate.Trim().ToUpper());
                
                if (vehicleStatus == null)
                {
                    return Ok(new ApiResponseDto<Models.VehicleStatusResult>
                    {
                        Success = false,
                        Data = null,
                        Message = "未查询到该车牌号的在停记录",
                        Total = 0
                    });
                }

                return Ok(new ApiResponseDto<Models.VehicleStatusResult>
                {
                    Success = true,
                    Data = vehicleStatus,
                    Message = "查询成功",
                    Total = 1
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        #endregion

        #region 调试和诊断功能

        /// <summary>
        /// 获取停车数据诊断信息
        /// </summary>
        [HttpGet("DiagnoseParkingData")]
        public async Task<IActionResult> DiagnoseParkingData([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddDays(-7);
                var end = endDate ?? DateTime.Today.AddDays(1);

                // 1. 查询数据库中的停车记录
                var allCarsInDb = await _parkingContext.CAR
                    .Where(c => c.PARK_START >= start && c.PARK_START <= end)
                    .ToListAsync();

                var completedCarsInDb = allCarsInDb.Where(c => c.PARK_END.HasValue).ToList();
                var activeCarsInDb = allCarsInDb.Where(c => !c.PARK_END.HasValue).ToList();

                // 2. 查询内存中的支付记录
                var paymentRecordsInMemory = await _parkingContext.GetPaymentRecordsInTimeRange(start, end);

                // 3. 查询PARK表记录
                var parkRecordsInDb = await _parkingContext.PARK
                    .Where(p => p.PARK_START >= start && p.PARK_START <= end)
                    .ToListAsync();

                var diagnosticInfo = new
                {
                    QueryTimeRange = new { StartDate = start, EndDate = end },
                    DatabaseRecords = new
                    {
                        TotalCarsInCAR = allCarsInDb.Count,
                        CompletedCars = completedCarsInDb.Count,
                        ActiveCars = activeCarsInDb.Count,
                        ParkRecords = parkRecordsInDb.Count,
                        CarDetails = allCarsInDb.Take(5).Select(c => new
                        {
                            c.LICENSE_PLATE_NUMBER,
                            c.PARK_START,
                            c.PARK_END,
                            Status = c.PARK_END.HasValue ? "已出场" : "在停中"
                        }),
                        ParkDetails = parkRecordsInDb.Take(5).Select(p => new
                        {
                            p.LICENSE_PLATE_NUMBER,
                            p.PARKING_SPACE_ID,
                            p.PARK_START
                        })
                    },
                    MemoryRecords = new
                    {
                        TotalPaymentRecords = paymentRecordsInMemory.Count,
                        PaymentDetails = paymentRecordsInMemory.Take(5).Select(pr => new
                        {
                            LicensePlateNumber = pr.LicensePlateNumber,
                            ParkingSpaceId = pr.ParkingSpaceId,
                            ParkStart = pr.ParkStart,
                            ParkEnd = pr.ParkEnd,
                            TotalFee = pr.TotalFee,
                            PaymentStatus = pr.PaymentStatus
                        })
                    },
                    DataConsistencyCheck = new
                    {
                        DatabaseHasData = allCarsInDb.Count > 0,
                        MemoryHasData = paymentRecordsInMemory.Count > 0,
                        DataSourceMismatch = allCarsInDb.Count != paymentRecordsInMemory.Count,
                        Recommendation = allCarsInDb.Count == 0 && paymentRecordsInMemory.Count > 0 
                            ? "数据库中无停车记录，但内存中有支付记录。这说明支付记录与数据库记录不同步。"
                            : allCarsInDb.Count > 0 && paymentRecordsInMemory.Count == 0
                            ? "数据库中有停车记录，但内存中无支付记录。"
                            : "数据状态正常。"
                    }
                };

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Data = diagnosticInfo,
                    Message = "停车数据诊断完成",
                    Total = 1
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "诊断停车数据时发生错误");
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        #endregion

        #region 数据修复功能

        /// <summary>
        /// 为现有停车记录生成支付记录
        /// </summary>
        [HttpPost("GeneratePaymentRecords")]
        public async Task<IActionResult> GeneratePaymentRecords([FromBody] GeneratePaymentRecordsDto dto)
        {
            try
            {
                var start = dto.StartDate ?? DateTime.Today.AddDays(-7);
                var end = dto.EndDate ?? DateTime.Today.AddDays(1);

                _logger.LogInformation("开始为停车记录生成支付记录：时间范围 {StartDate} - {EndDate}", start, end);
                Console.WriteLine($"[DEBUG] 开始为停车记录生成支付记录：时间范围 {start} - {end}");

                // 查询数据库中已完成的停车记录（放宽查询条件）
                var completedCars = await _parkingContext.CAR
                    .Where(c => c.PARK_END.HasValue) // 只要有出场时间的都算完成
                    .ToListAsync();

                // 如果指定了时间范围，则过滤
                if (start != DateTime.MinValue && end != DateTime.MaxValue)
                {
                    completedCars = completedCars
                        .Where(c => c.PARK_START >= start && c.PARK_START <= end)
                        .ToList();
                }

                Console.WriteLine($"[DEBUG] 查询到 {completedCars.Count} 条已完成的停车记录");
                _logger.LogInformation("查询到 {Count} 条已完成的停车记录", completedCars.Count);

                var generatedCount = 0;
                var random = new Random();

                Console.WriteLine($"[DEBUG] ========== 支付记录生成过程 ==========");
                Console.WriteLine($"[DEBUG] 说明: 系统将获取每个停车场的实际费用设置");
                Console.WriteLine($"[DEBUG] 当前内存中支付记录总数: {_parkingContext.PaymentRecords.Count}");
                
                foreach (var car in completedCars)
                {
                    Console.WriteLine($"[DEBUG] 处理车辆: {car.LICENSE_PLATE_NUMBER}");
                    Console.WriteLine($"[DEBUG]   入场时间: {car.PARK_START:yyyy-MM-dd HH:mm}");
                    Console.WriteLine($"[DEBUG]   出场时间: {car.PARK_END:yyyy-MM-dd HH:mm}");
                    
                    // 检查是否已存在支付记录
                    var existingPayment = _parkingContext.PaymentRecords.Values
                        .FirstOrDefault(p => p.LicensePlateNumber == car.LICENSE_PLATE_NUMBER && 
                                           p.ParkStart == car.PARK_START);

                    if (existingPayment == null || dto.ForceRegenerate)
                    {
                        // 如果强制重新生成，先删除已存在的记录
                        if (dto.ForceRegenerate && existingPayment != null)
                        {
                            var existingKey = _parkingContext.PaymentRecords.FirstOrDefault(kvp => 
                                kvp.Value.LicensePlateNumber == car.LICENSE_PLATE_NUMBER && 
                                kvp.Value.ParkStart == car.PARK_START).Key;
                            
                            if (!string.IsNullOrEmpty(existingKey))
                            {
                                _parkingContext.PaymentRecords.Remove(existingKey);
                                Console.WriteLine($"[DEBUG]   强制重新生成：删除已存在的支付记录，ID: {existingKey}");
                            }
                        }
                        
                        // 计算停车时长和费用
                        var duration = car.PARK_END.Value - car.PARK_START;
                        var hours = Math.Ceiling(duration.TotalHours);
                        
                        // 获取停车场实际费用（从PARK表关联到PARKING_SPACE_DISTRIBUTION再到PARKING_LOT）
                        var parkRecord = await _parkingContext.PARK
                            .FirstOrDefaultAsync(p => p.LICENSE_PLATE_NUMBER == car.LICENSE_PLATE_NUMBER);
                        
                        if (parkRecord == null)
                        {
                            // 如果找不到PARK记录，使用默认费用
                            Console.WriteLine($"[DEBUG]   警告: 无法找到车辆 {car.LICENSE_PLATE_NUMBER} 的PARK记录，使用默认费率¥5/小时");
                            var defaultFee = (decimal)(hours * 5.0);
                            
                            // 生成支付记录（使用默认费用）
                            var defaultPaymentRecord = new Models.ParkingPaymentRecord
                            {
                                LicensePlateNumber = car.LICENSE_PLATE_NUMBER,
                                ParkingSpaceId = random.Next(1001, 2000), // 随机车位ID
                                ParkStart = car.PARK_START,
                                ParkEnd = car.PARK_END,
                                TotalFee = defaultFee,
                                PaymentStatus = "已支付",
                                PaymentTime = car.PARK_END.Value.AddMinutes(random.Next(1, 30)), // 随机支付时间
                                PaymentMethod = "现金"
                            };

                            // 添加到内存中的支付记录
                            var defaultRecordId = Guid.NewGuid().ToString();
                            _parkingContext.PaymentRecords[defaultRecordId] = defaultPaymentRecord;
                            generatedCount++;

                            Console.WriteLine($"[DEBUG]   使用默认费率生成支付记录成功，ID: {defaultRecordId}");
                            Console.WriteLine($"[DEBUG]   当前内存中支付记录总数: {_parkingContext.PaymentRecords.Count}");
                            continue; // 跳过后续的复杂查询
                        }
                        
                        // 尝试获取实际停车场费用，如果失败则使用默认费用
                        decimal fee;
                        decimal actualParkingFee = 5m; // 默认费率
                        try
                        {
                            var spaceDistribution = await _parkingContext.PARKING_SPACE_DISTRIBUTION
                                .FirstOrDefaultAsync(psd => psd.PARKING_SPACE_ID == parkRecord.PARKING_SPACE_ID);
                            
                            if (spaceDistribution != null)
                            {
                                var parkingLot = await _parkingContext.PARKING_LOT
                                    .FirstOrDefaultAsync(pl => pl.AREA_ID == spaceDistribution.AREA_ID);
                                
                                if (parkingLot != null)
                                {
                                    actualParkingFee = parkingLot.PARKING_FEE;
                                    fee = (decimal)(hours * (double)actualParkingFee);
                                    Console.WriteLine($"[DEBUG]   使用实际停车场费率: ¥{actualParkingFee}/小时");
                                }
                                else
                                {
                                    fee = (decimal)(hours * 5.0);
                                    Console.WriteLine($"[DEBUG]   无法找到停车场费用设置，使用默认费率: ¥5/小时");
                                }
                            }
                            else
                            {
                                fee = (decimal)(hours * 5.0);
                                Console.WriteLine($"[DEBUG]   无法找到车位分布信息，使用默认费率: ¥5/小时");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[DEBUG]   获取停车场费用时出错: {ex.Message}，使用默认费率: ¥5/小时");
                            fee = (decimal)(hours * 5.0);
                        }

                        Console.WriteLine($"[DEBUG]   停车时长: {duration.TotalHours:F1}小时");
                        Console.WriteLine($"[DEBUG]   计费时长: {hours}小时 (向上取整)");
                        Console.WriteLine($"[DEBUG]   停车场费率: ¥{actualParkingFee}/小时");
                        Console.WriteLine($"[DEBUG]   计算费用: {hours} × ¥{actualParkingFee} = ¥{fee}");

                        // 生成支付记录
                        var finalPaymentRecord = new Models.ParkingPaymentRecord
                        {
                            LicensePlateNumber = car.LICENSE_PLATE_NUMBER,
                            ParkingSpaceId = random.Next(1001, 2000), // 随机车位ID
                            ParkStart = car.PARK_START,
                            ParkEnd = car.PARK_END,
                            TotalFee = fee,
                            PaymentStatus = "已支付",
                            PaymentTime = car.PARK_END.Value.AddMinutes(random.Next(1, 30)), // 随机支付时间
                            PaymentMethod = "现金"
                        };

                        // 添加到内存中的支付记录
                        var finalRecordId = Guid.NewGuid().ToString();
                        _parkingContext.PaymentRecords[finalRecordId] = finalPaymentRecord;
                        generatedCount++;

                        Console.WriteLine($"[DEBUG]   生成支付记录成功，ID: {finalRecordId}");
                        Console.WriteLine($"[DEBUG]   当前内存中支付记录总数: {_parkingContext.PaymentRecords.Count}");
                        
                        _logger.LogInformation("为车辆 {LicensePlate} 生成支付记录：费用 {Fee} 元", 
                            car.LICENSE_PLATE_NUMBER, fee);
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG]   已存在支付记录，跳过生成");
                    }
                    Console.WriteLine($"[DEBUG]   --------------------");
                }
                
                Console.WriteLine($"[DEBUG] ========== 支付记录生成总结 ==========");
                Console.WriteLine($"[DEBUG] 成功生成支付记录: {generatedCount} 条");
                Console.WriteLine($"[DEBUG] 内存中支付记录总数: {_parkingContext.PaymentRecords.Count}");
                Console.WriteLine($"[DEBUG] ======================================");

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Data = new
                    {
                        GeneratedCount = generatedCount,
                        TotalCompletedCars = completedCars.Count,
                        TimeRange = new { StartDate = start, EndDate = end }
                    },
                    Message = $"成功为 {generatedCount} 条停车记录生成支付记录",
                    Total = generatedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成支付记录时发生错误");
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        #endregion

        #region 导出报表数据API

        /// <summary>
        /// 获取所有停车记录（用于导出报表）
        /// </summary>
        [HttpGet("GetAllParkingRecords")]
        public async Task<IActionResult> GetAllParkingRecords([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] int? areaId = null)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddDays(-7);
                var end = endDate ?? DateTime.Today.AddDays(1);

                // 查询所有停车记录
                var allCars = await _parkingContext.CAR
                    .Where(c => c.PARK_START >= start && c.PARK_START <= end)
                    .ToListAsync();

                // 查询PARK表获取车位信息
                var parkRecords = await _parkingContext.PARK
                    .Where(p => p.PARK_START >= start && p.PARK_START <= end)
                    .ToListAsync();

                // 合并数据
                var records = allCars.Select(car => {
                    var parkRecord = parkRecords.FirstOrDefault(p => 
                        p.LICENSE_PLATE_NUMBER == car.LICENSE_PLATE_NUMBER && 
                        p.PARK_START == car.PARK_START);
                    
                    return new
                    {
                        car.LICENSE_PLATE_NUMBER,
                        car.PARK_START,
                        car.PARK_END,
                        PARKING_SPACE_ID = parkRecord?.PARKING_SPACE_ID
                    };
                }).ToList();

                var completedRecords = records.Count(r => r.PARK_END.HasValue);
                var activeRecords = records.Count(r => !r.PARK_END.HasValue);
                // 计算总收入（使用实际停车场费用）
                var totalRevenue = 0m;
                foreach (var record in records.Where(r => r.PARK_END.HasValue))
                {
                    var duration = Math.Ceiling((record.PARK_END.Value - record.PARK_START).TotalHours);
                    
                    // 获取停车场实际费用
                    var parkRecord = await _parkingContext.PARK
                        .FirstOrDefaultAsync(p => p.LICENSE_PLATE_NUMBER == record.LICENSE_PLATE_NUMBER && 
                                               p.PARK_START == record.PARK_START);
                    
                    if (parkRecord == null)
                    {
                        throw new Exception($"无法找到车辆 {record.LICENSE_PLATE_NUMBER} 的停车记录");
                    }
                    
                    var spaceDistribution = await _parkingContext.PARKING_SPACE_DISTRIBUTION
                        .FirstOrDefaultAsync(psd => psd.PARKING_SPACE_ID == parkRecord.PARKING_SPACE_ID);
                    
                    if (spaceDistribution == null)
                    {
                        throw new Exception($"无法找到车位 {parkRecord.PARKING_SPACE_ID} 的分布信息");
                    }
                    
                    var parkingLot = await _parkingContext.PARKING_LOT
                        .FirstOrDefaultAsync(pl => pl.AREA_ID == spaceDistribution.AREA_ID);
                    
                    if (parkingLot == null)
                    {
                        throw new Exception($"无法找到停车场 {spaceDistribution.AREA_ID} 的费用设置");
                    }
                    
                    var parkingFee = parkingLot.PARKING_FEE;
                    var fee = (decimal)(duration * (double)parkingFee);
                    totalRevenue += fee;
                }
                
                Console.WriteLine($"[DEBUG] GetAllParkingRecords 收入计算:");
                Console.WriteLine($"[DEBUG]   总记录数: {records.Count}");
                Console.WriteLine($"[DEBUG]   已完成记录数: {completedRecords}");
                Console.WriteLine($"[DEBUG]   在停记录数: {activeRecords}");
                Console.WriteLine($"[DEBUG]   说明: 使用实际停车场费用，非硬编码费率");
                Console.WriteLine($"[DEBUG]   最终总收入: ¥{totalRevenue}");
                var averageHours = completedRecords > 0 ? 
                    records.Where(r => r.PARK_END.HasValue)
                        .Average(r => (r.PARK_END.Value - r.PARK_START).TotalHours) : 0;

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Data = new
                    {
                        TotalRecords = records.Count,
                        CompletedRecords = completedRecords,
                        ActiveRecords = activeRecords,
                        TotalRevenue = totalRevenue,
                        AverageHours = averageHours,
                        Records = records
                    },
                    Message = $"成功获取 {records.Count} 条停车记录",
                    Total = records.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有停车记录时发生错误");
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        /// <summary>
        /// 获取所有支付记录（用于导出报表）
        /// </summary>
        [HttpGet("GetAllPaymentRecords")]
        public async Task<IActionResult> GetAllPaymentRecords([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddDays(-7);
                var end = endDate ?? DateTime.Today.AddDays(1);

                // 获取内存中的支付记录
                var paymentRecords = await _parkingContext.GetPaymentRecordsInTimeRange(start, end);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Data = new
                    {
                        TotalRecords = paymentRecords.Count,
                        TotalRevenue = paymentRecords.Sum(p => p.TotalFee),
                        Records = paymentRecords
                    },
                    Message = $"成功获取 {paymentRecords.Count} 条支付记录",
                    Total = paymentRecords.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有支付记录时发生错误");
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        #endregion

        #region 2.8.5 停车场信息统计报表功能

        /// <summary>
        /// 停车场统计报表DTO
        /// </summary>
        public class ParkingStatisticsReportDto
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int? AreaId { get; set; }
            public string OperatorAccount { get; set; } = string.Empty;
        }

        /// <summary>
        /// 停车场统计报表响应DTO
        /// </summary>
        public class ParkingStatisticsReportResponseDto
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int? AreaId { get; set; }
            public string AreaName { get; set; } = string.Empty;
            public int TotalParkingCount { get; set; }
            public decimal TotalRevenue { get; set; }
            public double AverageParkingHours { get; set; }
            public List<HourlyTrafficDto> HourlyTraffic { get; set; } = new();
            public List<DailyStatisticsDto> DailyStatistics { get; set; } = new();
            public DateTime GeneratedAt { get; set; }
        }

        /// <summary>
        /// 每小时收入统计DTO
        /// </summary>
        public class HourlyTrafficDto
        {
            public int Hour { get; set; }
            public decimal Revenue { get; set; } // 该时间段出场的车辆总费用
            public int EntryCount { get; set; } // 该时间段入场的车辆数量
            public int ExitCount { get; set; } // 该时间段出场的车辆数量
        }

        /// <summary>
        /// 每日统计DTO
        /// </summary>
        public class DailyStatisticsDto
        {
            public DateTime Date { get; set; }
            public int ParkingCount { get; set; }
            public decimal Revenue { get; set; }
            public double AverageParkingHours { get; set; }
            public double PeakUtilizationRate { get; set; } // 高峰时段车位利用率
        }

        /// <summary>
        /// 高峰时段车位利用率DTO
        /// </summary>
        public class PeakHourUtilizationDto
        {
            public int Hour { get; set; }
            public double UtilizationRate { get; set; } // 利用率百分比
            public int OccupiedSpaces { get; set; } // 已占用车位数
            public int TotalSpaces { get; set; } // 总车位数
        }

        /// <summary>
        /// 报表类型枚举
        /// </summary>
        public enum ReportType
        {
            Daily = 1,    // 日报
            Weekly = 2,   // 周报
            Monthly = 3   // 月报
        }

        /// <summary>
        /// 运营报表请求DTO
        /// </summary>
        public class OperationReportDto
        {
            public ReportType ReportType { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int? AreaId { get; set; }
            public string OperatorAccount { get; set; } = string.Empty;
        }

        /// <summary>
        /// 运营报表响应DTO
        /// </summary>
        public class OperationReportResponseDto
        {
            public ReportType ReportType { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int? AreaId { get; set; }
            public string AreaName { get; set; } = string.Empty;
            public int TotalParkingCount { get; set; }
            public decimal TotalRevenue { get; set; }
            public double AverageParkingHours { get; set; }
            public double PeakUtilizationRate { get; set; } // 整体高峰利用率
            public List<HourlyTrafficDto> HourlyTraffic { get; set; } = new();
            public List<DailyStatisticsDto> DailyStatistics { get; set; } = new();
            public List<PeakHourUtilizationDto> PeakHourUtilization { get; set; } = new();
            public List<ParkingDetailRecordDto> DetailedRecords { get; set; } = new(); // 新增：详细停车记录
            public DateTime GeneratedAt { get; set; }
        }

        /// <summary>
        /// 详细停车记录DTO
        /// </summary>
        public class ParkingDetailRecordDto
        {
            public string LicensePlateNumber { get; set; } = string.Empty;
            public int ParkingSpaceId { get; set; }
            public int AreaId { get; set; }
            public DateTime ParkStart { get; set; }
            public DateTime? ParkEnd { get; set; }
            public string Status { get; set; } = string.Empty; // "在停中" 或 "已完成"
            public decimal? TotalFee { get; set; }
            public string? PaymentStatus { get; set; }
            public DateTime? PaymentTime { get; set; }
            public string? PaymentMethod { get; set; }
            public double? ParkingDuration { get; set; } // 停车时长（小时）
        }

        /// <summary>
        /// 获取停车场统计报表（原有功能）
        /// </summary>
        [HttpPost("StatisticsReport")]
        public async Task<IActionResult> GetParkingStatisticsReport([FromBody] ParkingStatisticsReportDto dto)
        {
            try
            {
                _logger.LogInformation("生成停车场统计报表：时间范围 {StartDate} - {EndDate}, 区域 {AreaId}, 操作员 {Operator}", 
                    dto.StartDate, dto.EndDate, dto.AreaId, dto.OperatorAccount);

                // 简单权限验证（不查询数据库）
                if (string.IsNullOrEmpty(dto.OperatorAccount))
                {
                    return BadRequest(new { error = "操作员账号不能为空" });
                }
                
                // 简单验证：只允许admin账号
                if (dto.OperatorAccount != "admin")
                {
                    return BadRequest(new { 
                        error = "权限不足，需要管理员权限",
                        debug = new {
                            account = dto.OperatorAccount,
                            message = "只有admin账号可以生成统计报表"
                        }
                    });
                }
                
                _logger.LogInformation("[DEBUG] 权限验证通过，操作员：{Account}", dto.OperatorAccount);

                // 验证时间范围
                if (dto.StartDate >= dto.EndDate)
                {
                    return BadRequest(new { 
                        error = "开始时间必须早于结束时间",
                        debug = new {
                            startDate = dto.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                            endDate = dto.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
                            startDateTicks = dto.StartDate.Ticks,
                            endDateTicks = dto.EndDate.Ticks,
                            timeSpan = dto.EndDate - dto.StartDate,
                            days = (dto.EndDate - dto.StartDate).Days
                        }
                    });
                }

                // 查询真实数据库数据
                var reportData = await GetRealParkingStatistics(dto.StartDate, dto.EndDate, dto.AreaId);

                // 检查是否有支付记录
                var hasPaymentRecords = await _parkingContext.GetPaymentRecordsInTimeRange(dto.StartDate, dto.EndDate);
                
                if (reportData.TotalParkingCount == 0)
                {
                    return Ok(new ApiResponseDto<ParkingStatisticsReportResponseDto>
                    {
                        Success = true,
                        Data = reportData,
                        Message = "该时间段内无停车记录",
                        Total = 0
                    });
                }
                
                if (!hasPaymentRecords.Any())
                {
                    return Ok(new ApiResponseDto<ParkingStatisticsReportResponseDto>
                    {
                        Success = true,
                        Data = new ParkingStatisticsReportResponseDto
                        {
                            StartDate = dto.StartDate,
                            EndDate = dto.EndDate,
                            AreaId = dto.AreaId,
                            AreaName = dto.AreaId.HasValue ? $"停车场{dto.AreaId.Value}" : "所有停车场",
                            TotalParkingCount = reportData.TotalParkingCount,
                            TotalRevenue = 0,
                            AverageParkingHours = 0,
                            HourlyTraffic = new List<HourlyTrafficDto>(),
                            DailyStatistics = new List<DailyStatisticsDto>(),
                            GeneratedAt = DateTime.Now
                        },
                        Message = $"该时间段内有{reportData.TotalParkingCount}条停车记录，但无支付记录。请先生成支付记录后再生成统计报表。",
                        Total = 0
                    });
                }

                return Ok(new ApiResponseDto<ParkingStatisticsReportResponseDto>
                {
                    Success = true,
                    Data = reportData,
                    Message = $"成功生成统计报表，共{reportData.TotalParkingCount}条停车记录",
                    Total = 1
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成停车场统计报表时发生错误");
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        /// <summary>
        /// 查询真实的停车统计数据
        /// </summary>
        private async Task<ParkingStatisticsReportResponseDto> GetRealParkingStatistics(DateTime startDate, DateTime endDate, int? areaId)
        {
            try
            {
                _logger.LogInformation("[DEBUG] 开始查询停车统计数据，时间范围：{StartDate} - {EndDate}", startDate, endDate);
                Console.WriteLine($"[DEBUG] 开始查询停车统计数据，时间范围：{startDate} - {endDate}");
                Console.WriteLine($"[DEBUG] 详细时间信息:");
                Console.WriteLine($"[DEBUG]   StartDate: {startDate:yyyy-MM-dd HH:mm:ss} (Ticks: {startDate.Ticks})");
                Console.WriteLine($"[DEBUG]   EndDate: {endDate:yyyy-MM-dd HH:mm:ss} (Ticks: {endDate.Ticks})");
                Console.WriteLine($"[DEBUG]   时间差: {endDate - startDate}");
                Console.WriteLine($"[DEBUG]   天数差: {(endDate - startDate).Days}");
                Console.WriteLine($"[DEBUG]   小时差: {(endDate - startDate).TotalHours:F2}");

                // 使用EF Core LINQ查询，避免SQL参数绑定问题
                var allCarsInTimeRange = await _parkingContext.CAR
                    .Where(c => c.PARK_START >= startDate && c.PARK_START <= endDate)
                    .ToListAsync();

                var completedCarsInTimeRange = allCarsInTimeRange
                    .Where(c => c.PARK_END.HasValue)
                    .ToList();

                var totalAllParkingCount = allCarsInTimeRange.Count;
                var totalCompletedParkingCount = completedCarsInTimeRange.Count;

                Console.WriteLine($"[DEBUG] EF Core查询结果: 总记录={totalAllParkingCount}, 已完成={totalCompletedParkingCount}");

                // 统计内存中的支付记录
                var paymentRecordsInTimeRange = await _parkingContext.GetPaymentRecordsInTimeRange(startDate, endDate);
                var paymentRecordsCount = paymentRecordsInTimeRange.Count;

                _logger.LogInformation("[DEBUG] 内存中支付记录数：{Count}", paymentRecordsCount);
                Console.WriteLine($"[DEBUG] 内存中支付记录数：{paymentRecordsCount}");

                // 如果指定了区域，需要根据车位ID筛选支付记录
                if (areaId.HasValue)
                {
                    Console.WriteLine($"[DEBUG] 开始区域筛选，目标区域: {areaId.Value}");
                    
                    // 获取该区域下的所有车位ID
                    var areaParkingSpaces = await _parkingContext.PARKING_SPACE_DISTRIBUTION
                        .Where(psd => psd.AREA_ID == areaId.Value)
                        .Select(psd => psd.PARKING_SPACE_ID)
                        .ToListAsync();
                    
                    Console.WriteLine($"[DEBUG] 区域 {areaId.Value} 下的车位数量: {areaParkingSpaces.Count}");
                    
                    // 筛选出在该区域车位的支付记录
                    var filteredPaymentRecords = new List<Models.ParkingPaymentRecord>();
                    
                    foreach (var paymentRecord in paymentRecordsInTimeRange)
                    {
                        // 根据车牌号和停车开始时间查找对应的停车记录
                        var parkingRecord = await _parkingContext.PARK
                            .Where(p => p.LICENSE_PLATE_NUMBER == paymentRecord.LicensePlateNumber && 
                                      p.PARK_START == paymentRecord.ParkStart)
                            .FirstOrDefaultAsync();
                        
                        if (parkingRecord != null && areaParkingSpaces.Contains(parkingRecord.PARKING_SPACE_ID))
                        {
                            filteredPaymentRecords.Add(paymentRecord);
                        }
                    }
                    
                    paymentRecordsInTimeRange = filteredPaymentRecords;
                    Console.WriteLine($"[DEBUG] 区域筛选后支付记录数: {paymentRecordsInTimeRange.Count}");
                }

                // 使用包含所有停车记录的数据（包括未出场的）
                var totalParkingCount = totalAllParkingCount; // 直接使用数据库中的记录数
                
                // 计算收入：只使用支付记录中的实际收入，如果没有支付记录则收入为0
                var totalRevenue = 0m;
                var totalParkingHours = 0.0;
                var recordsWithRevenue = 0;

                Console.WriteLine($"[DEBUG] ========== 收入计算详细过程 ==========");
                Console.WriteLine($"[DEBUG] 支付记录总数: {paymentRecordsInTimeRange.Count}");
                Console.WriteLine($"[DEBUG] 已完成停车记录数: {totalCompletedParkingCount}");

                // 只使用支付记录中的实际收入
                if (paymentRecordsInTimeRange.Any())
                {
                    Console.WriteLine($"[DEBUG] 使用支付记录计算收入:");
                    foreach (var paymentRecord in paymentRecordsInTimeRange)
                    {
                        var duration = paymentRecord.ParkEnd.HasValue ? 
                            (paymentRecord.ParkEnd.Value - paymentRecord.ParkStart).TotalHours : 0;
                        var calculatedFee = paymentRecord.TotalFee;
                        
                        Console.WriteLine($"[DEBUG]   车牌号: {paymentRecord.LicensePlateNumber}, 入场: {paymentRecord.ParkStart:yyyy-MM-dd HH:mm}, 出场: {paymentRecord.ParkEnd:yyyy-MM-dd HH:mm}, 时长: {duration:F1}小时, 费用: ¥{calculatedFee}");
                        
                        totalRevenue += calculatedFee;
                        if (paymentRecord.ParkEnd.HasValue)
                        {
                            totalParkingHours += duration;
                            recordsWithRevenue++;
                        }
                    }
                    Console.WriteLine($"[DEBUG]   支付记录总收入: ¥{totalRevenue}");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] 无支付记录，收入为0");
                    Console.WriteLine($"[DEBUG]   说明: 系统只统计实际存在的支付记录，不会估算收入");
                    totalRevenue = 0m;
                    totalParkingHours = 0.0;
                    recordsWithRevenue = 0;
                }

                var averageParkingHours = recordsWithRevenue > 0 ? totalParkingHours / recordsWithRevenue : 3.5;

                Console.WriteLine($"[DEBUG] ========== 平均停车时长计算 ==========");
                if (recordsWithRevenue > 0)
                {
                    Console.WriteLine($"[DEBUG] 总停车时长: {totalParkingHours:F1}小时");
                    Console.WriteLine($"[DEBUG] 有收入记录数: {recordsWithRevenue}");
                    Console.WriteLine($"[DEBUG] 计算过程: {totalParkingHours:F1} ÷ {recordsWithRevenue} = {averageParkingHours:F1}小时");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] 无收入记录，使用默认值: 3.5小时");
                }
                Console.WriteLine($"[DEBUG] 最终平均停车时长: {averageParkingHours:F1}小时");
                Console.WriteLine($"[DEBUG] =================================");

                Console.WriteLine($"[DEBUG] ========== 收入计算总结 ==========");
                Console.WriteLine($"[DEBUG] 最终总收入: ¥{totalRevenue}");
                Console.WriteLine($"[DEBUG] 总停车时长: {totalParkingHours:F1}小时");
                Console.WriteLine($"[DEBUG] 有收入记录数: {recordsWithRevenue}");
                Console.WriteLine($"[DEBUG] 平均停车时长: {averageParkingHours:F1}小时");
                Console.WriteLine($"[DEBUG] =================================");

                // 生成每日统计 - 基于真实数据库数据
                var dailyStats = new List<DailyStatisticsDto>();
                var days = (endDate - startDate).Days + 1;

                for (int i = 0; i < days; i++)
                {
                    var date = startDate.AddDays(i);
                    var nextDate = date.AddDays(1);
                    
                    // 使用EF Core LINQ查询该日期的停车记录
                    var dailyCars = allCarsInTimeRange
                        .Where(c => c.PARK_START.Date == date.Date)
                        .ToList();
                    
                    var dailyCompletedCars = dailyCars
                        .Where(c => c.PARK_END.HasValue)
                        .ToList();
                    
                    var dailyCount = dailyCars.Count;
                    var completedCount = dailyCompletedCars.Count;
                    
                    // 计算该日期的收入：只使用支付记录，不使用估算值
                    var dailyRevenue = 0m;
                    var dailyPaymentRecords = paymentRecordsInTimeRange
                        .Where(p => p.ParkStart.Date == date.Date)
                        .ToList();
                    
                    if (dailyPaymentRecords.Any())
                    {
                        dailyRevenue = dailyPaymentRecords.Sum(p => p.TotalFee);
                        Console.WriteLine($"[DEBUG] {date:yyyy-MM-dd} 使用支付记录: {dailyPaymentRecords.Count}条, 收入: ¥{dailyRevenue}");
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] {date:yyyy-MM-dd} 无支付记录，收入为0");
                    }
                    
                    dailyStats.Add(new DailyStatisticsDto
                    {
                        Date = date,
                        ParkingCount = dailyCount,
                        Revenue = dailyRevenue,
                        AverageParkingHours = 3.5
                    });
                }

                // 生成每小时收入统计 - 基于支付记录，统计每个时间段出场的车辆费用
                var hourlyTraffic = new List<HourlyTrafficDto>();
                Console.WriteLine($"[DEBUG] ========== 每小时收入统计开始 ==========");
                Console.WriteLine($"[DEBUG] 基于支付记录统计每小时收入，支付记录总数: {paymentRecordsInTimeRange.Count}");
                
                for (int hour = 0; hour < 24; hour++)
                {
                                         // 统计该时间段内入场的车辆数
                     var hourlyEntries = paymentRecordsInTimeRange
                         .Where(p => p.ParkStart.Hour == hour)
                         .ToList();
                     
                     // 统计该时间段内出场的车辆及其费用
                     var hourlyExits = paymentRecordsInTimeRange
                         .Where(p => p.ParkEnd.HasValue && p.ParkEnd.Value.Hour == hour)
                         .ToList();
                     
                     var entryCount = hourlyEntries.Count;
                     var exitCount = hourlyExits.Count;
                     var revenue = hourlyExits.Sum(p => p.TotalFee);
                     
                     Console.WriteLine($"[DEBUG] {hour:00}:00 - 入场车辆: {entryCount}辆, 出场车辆: {exitCount}辆, 收入: ¥{revenue}");
                     
                     hourlyTraffic.Add(new HourlyTrafficDto
                     {
                         Hour = hour,
                         Revenue = revenue,
                         EntryCount = entryCount,
                         ExitCount = exitCount
                     });
                }
                
                Console.WriteLine($"[DEBUG] ========== 每小时收入统计完成 ==========");

                var areaName = areaId.HasValue ? $"停车场{areaId.Value}" : "所有停车场";

                var result = new ParkingStatisticsReportResponseDto
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

                Console.WriteLine($"[DEBUG] ========== 返回结果总结 ==========");
                Console.WriteLine($"[DEBUG] 总停车记录数: {result.TotalParkingCount}");
                Console.WriteLine($"[DEBUG] 总收入: ¥{result.TotalRevenue}");
                Console.WriteLine($"[DEBUG] 平均停车时长: {result.AverageParkingHours:F1}小时");
                Console.WriteLine($"[DEBUG] 每小时收入记录数: {result.HourlyTraffic.Count}");
                Console.WriteLine($"[DEBUG] 每日统计记录数: {result.DailyStatistics.Count}");
                Console.WriteLine($"[DEBUG] =================================");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询停车统计数据时发生错误");
                // 如果查询失败，返回空数据
                return new ParkingStatisticsReportResponseDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    AreaId = areaId,
                    AreaName = areaId.HasValue ? $"停车场{areaId.Value}" : "所有停车场",
                    TotalParkingCount = 0,
                    TotalRevenue = 0,
                    AverageParkingHours = 0,
                    HourlyTraffic = new List<HourlyTrafficDto>(),
                    DailyStatistics = new List<DailyStatisticsDto>(),
                    GeneratedAt = DateTime.Now
                };
            }
        }

        /// <summary>
        /// 生成停车场运营报表（按日/周/月）
        /// </summary>
        [HttpPost("OperationReport")]
        public async Task<IActionResult> GetOperationReport([FromBody] OperationReportDto dto)
        {
            try
            {
                _logger.LogInformation("生成停车场运营报表：类型 {ReportType}, 时间范围 {StartDate} - {EndDate}, 区域 {AreaId}, 操作员 {Operator}", 
                    dto.ReportType, dto.StartDate, dto.EndDate, dto.AreaId, dto.OperatorAccount);

                // 权限验证
                if (string.IsNullOrEmpty(dto.OperatorAccount) || dto.OperatorAccount != "admin")
                {
                    return BadRequest(new { error = "权限不足，需要管理员权限" });
                }

                // 验证时间范围
                if (dto.StartDate >= dto.EndDate)
                {
                    return BadRequest(new { 
                        error = "开始时间必须早于结束时间",
                        debug = new {
                            startDate = dto.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                            endDate = dto.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
                            startDateTicks = dto.StartDate.Ticks,
                            endDateTicks = dto.EndDate.Ticks,
                            timeSpan = dto.EndDate - dto.StartDate,
                            days = (dto.EndDate - dto.StartDate).Days
                        }
                    });
                }

                // 根据报表类型调整时间范围
                var (adjustedStartDate, adjustedEndDate) = AdjustTimeRangeForReportType(dto.ReportType, dto.StartDate, dto.EndDate);

                // 生成运营报表数据
                var reportData = await GenerateOperationReport(adjustedStartDate, adjustedEndDate, dto.AreaId, dto.ReportType);

                // 检查是否有支付记录
                var hasPaymentRecords = await _parkingContext.GetPaymentRecordsInTimeRange(adjustedStartDate, adjustedEndDate);
                
                if (reportData.TotalParkingCount == 0)
                {
                    return Ok(new ApiResponseDto<OperationReportResponseDto>
                    {
                        Success = true,
                        Data = reportData,
                        Message = "该时间段内无停车记录",
                        Total = 0
                    });
                }
                
                if (!hasPaymentRecords.Any())
                {
                    return Ok(new ApiResponseDto<OperationReportResponseDto>
                    {
                        Success = true,
                        Data = reportData,
                        Message = $"该时间段内有{reportData.TotalParkingCount}条停车记录，但无支付记录。请先生成支付记录后再生成运营报表。",
                        Total = 0
                    });
                }

                return Ok(new ApiResponseDto<OperationReportResponseDto>
                {
                    Success = true,
                    Data = reportData,
                    Message = $"成功生成{dto.ReportType}运营报表，共{reportData.TotalParkingCount}条停车记录",
                    Total = 1
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成停车场运营报表时发生错误");
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        /// <summary>
        /// 导出Excel报表
        /// </summary>
        [HttpPost("ExportExcel")]
        public async Task<IActionResult> ExportExcelReport([FromBody] OperationReportDto dto)
        {
            try
            {
                _logger.LogInformation("导出Excel报表：类型 {ReportType}, 时间范围 {StartDate} - {EndDate}", 
                    dto.ReportType, dto.StartDate, dto.EndDate);

                // 权限验证
                if (string.IsNullOrEmpty(dto.OperatorAccount) || dto.OperatorAccount != "admin")
                {
                    return BadRequest(new { error = "权限不足，需要管理员权限" });
                }

                // 生成报表数据
                var (adjustedStartDate, adjustedEndDate) = AdjustTimeRangeForReportType(dto.ReportType, dto.StartDate, dto.EndDate);
                var reportData = await GenerateOperationReport(adjustedStartDate, adjustedEndDate, dto.AreaId, dto.ReportType);

                // 生成Excel文件
                var excelBytes = GenerateExcelReport(reportData, dto.ReportType);
                
                var fileName = $"停车场运营报表_{dto.ReportType}_{adjustedStartDate:yyyyMMdd}_{adjustedEndDate:yyyyMMdd}.xlsx";
                
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出Excel报表时发生错误");
                return StatusCode(500, new { error = "服务器内部错误", details = ex.Message });
            }
        }

        /// <summary>
        /// 根据报表类型调整时间范围
        /// </summary>
        private (DateTime startDate, DateTime endDate) AdjustTimeRangeForReportType(ReportType reportType, DateTime startDate, DateTime endDate)
        {
            switch (reportType)
            {
                case ReportType.Daily:
                    // 日报：使用用户选择的具体日期，不调整时间范围
                    return (startDate, endDate);
                case ReportType.Weekly:
                    // 周报：使用用户选择的时间范围（前端已经计算好周一到周日）
                    return (startDate.Date, endDate.Date.AddDays(1).AddSeconds(-1));
                case ReportType.Monthly:
                    // 月报：使用用户选择的时间范围（前端已经计算好整月）
                    return (startDate.Date, endDate.Date.AddDays(1).AddSeconds(-1));
                default:
                    return (startDate, endDate);
            }
        }

        /// <summary>
        /// 生成运营报表数据
        /// </summary>
        private async Task<OperationReportResponseDto> GenerateOperationReport(DateTime startDate, DateTime endDate, int? areaId, ReportType reportType)
        {
            try
            {
                // 获取基础统计数据
                var baseStats = await GetRealParkingStatistics(startDate, endDate, areaId);
                
                // 计算高峰时段车位利用率
                var peakUtilization = await CalculatePeakHourUtilization(startDate, endDate, areaId);
                
                // 计算整体高峰利用率
                var overallPeakUtilization = peakUtilization.Any() ? peakUtilization.Max(p => p.UtilizationRate) : 0;

                // 更新每日统计，添加高峰利用率
                var dailyStatsWithPeak = new List<DailyStatisticsDto>();
                foreach (var dailyStat in baseStats.DailyStatistics)
                {
                    var dailyPeakUtilization = peakUtilization
                        .Where(p => p.Hour >= 8 && p.Hour <= 20) // 8:00-20:00为高峰时段
                        .DefaultIfEmpty(new PeakHourUtilizationDto { UtilizationRate = 0 })
                        .Max(p => p.UtilizationRate);

                    dailyStatsWithPeak.Add(new DailyStatisticsDto
                    {
                        Date = dailyStat.Date,
                        ParkingCount = dailyStat.ParkingCount,
                        Revenue = dailyStat.Revenue,
                        AverageParkingHours = dailyStat.AverageParkingHours,
                        PeakUtilizationRate = dailyPeakUtilization
                    });
                }

                // 生成详细停车记录
                var detailedRecords = await GenerateDetailedParkingRecords(startDate, endDate, areaId);

                return new OperationReportResponseDto
                {
                    ReportType = reportType,
                    StartDate = startDate,
                    EndDate = endDate,
                    AreaId = areaId,
                    AreaName = baseStats.AreaName,
                    TotalParkingCount = baseStats.TotalParkingCount,
                    TotalRevenue = baseStats.TotalRevenue,
                    AverageParkingHours = baseStats.AverageParkingHours,
                    PeakUtilizationRate = overallPeakUtilization,
                    HourlyTraffic = baseStats.HourlyTraffic,
                    DailyStatistics = dailyStatsWithPeak,
                    PeakHourUtilization = peakUtilization,
                    DetailedRecords = detailedRecords,
                    GeneratedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成运营报表数据时发生错误");
                return new OperationReportResponseDto
                {
                    ReportType = reportType,
                    StartDate = startDate,
                    EndDate = endDate,
                    AreaId = areaId,
                    AreaName = areaId.HasValue ? $"停车场{areaId.Value}" : "所有停车场",
                    TotalParkingCount = 0,
                    TotalRevenue = 0,
                    AverageParkingHours = 0,
                    PeakUtilizationRate = 0,
                    HourlyTraffic = new List<HourlyTrafficDto>(),
                    DailyStatistics = new List<DailyStatisticsDto>(),
                    PeakHourUtilization = new List<PeakHourUtilizationDto>(),
                    DetailedRecords = new List<ParkingDetailRecordDto>(),
                    GeneratedAt = DateTime.Now
                };
            }
        }

        /// <summary>
        /// 计算高峰时段车位利用率
        /// </summary>
        private async Task<List<PeakHourUtilizationDto>> CalculatePeakHourUtilization(DateTime startDate, DateTime endDate, int? areaId)
        {
            try
            {
                var peakUtilization = new List<PeakHourUtilizationDto>();
                
                // 从数据库获取实际的总车位数
                var totalSpaces = await GetTotalParkingSpaces(areaId);
                Console.WriteLine($"[DEBUG] 计算车位利用率 - 总车位数: {totalSpaces}");
                
                for (int hour = 0; hour < 24; hour++)
                {
                    // 计算该小时内的实际占用车位数
                    // 包括：1. 在该小时内入场的车辆 2. 在该小时内仍在停车的车辆
                    var occupiedSpaces = await CalculateOccupiedSpacesAtHour(startDate, endDate, hour, areaId);
                    
                    var utilizationRate = totalSpaces > 0 ? (double)occupiedSpaces / totalSpaces * 100 : 0;
                    
                    Console.WriteLine($"[DEBUG] {hour:00}:00 - 占用: {occupiedSpaces}, 总数: {totalSpaces}, 利用率: {utilizationRate:F1}%");
                    
                    peakUtilization.Add(new PeakHourUtilizationDto
                    {
                        Hour = hour,
                        UtilizationRate = Math.Round(utilizationRate, 2),
                        OccupiedSpaces = occupiedSpaces,
                        TotalSpaces = totalSpaces
                    });
                }

                return peakUtilization;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算高峰时段车位利用率时发生错误");
                return new List<PeakHourUtilizationDto>();
            }
        }

        /// <summary>
        /// 生成详细停车记录
        /// </summary>
        private async Task<List<ParkingDetailRecordDto>> GenerateDetailedParkingRecords(DateTime startDate, DateTime endDate, int? areaId)
        {
            try
            {
                Console.WriteLine($"[DEBUG] 开始生成详细停车记录，时间范围：{startDate:yyyy-MM-dd} 至 {endDate:yyyy-MM-dd}");
                
                var detailedRecords = new List<ParkingDetailRecordDto>();
                
                // 查询指定时间范围内的所有停车记录
                var query = from c in _parkingContext.CAR
                           join p in _parkingContext.PARK on new { LicensePlate = c.LICENSE_PLATE_NUMBER, ParkStart = c.PARK_START } 
                           equals new { LicensePlate = p.LICENSE_PLATE_NUMBER, ParkStart = p.PARK_START }
                           join psd in _parkingContext.PARKING_SPACE_DISTRIBUTION on p.PARKING_SPACE_ID equals psd.PARKING_SPACE_ID
                           where c.PARK_START >= startDate && c.PARK_START <= endDate
                           select new
                           {
                               LicensePlateNumber = c.LICENSE_PLATE_NUMBER,
                               ParkingSpaceId = p.PARKING_SPACE_ID,
                               AreaId = psd.AREA_ID,
                               ParkStart = c.PARK_START,
                               ParkEnd = c.PARK_END
                           };

                // 如果指定了区域，则过滤
                if (areaId.HasValue)
                {
                    query = query.Where(q => q.AreaId == areaId.Value);
                }

                var parkingRecords = await query.ToListAsync();
                Console.WriteLine($"[DEBUG] 查询到 {parkingRecords.Count} 条停车记录");

                // 获取支付记录信息
                var paymentRecords = await _parkingContext.GetPaymentRecordsInTimeRange(startDate, endDate);
                var paymentRecordsDict = paymentRecords.ToDictionary(
                    pr => $"{pr.LicensePlateNumber}_{pr.ParkStart:yyyyMMddHHmmss}",
                    pr => pr
                );

                foreach (var record in parkingRecords)
                {
                    // 计算停车时长
                    double? parkingDuration = null;
                    if (record.ParkEnd.HasValue)
                    {
                        parkingDuration = Math.Round((record.ParkEnd.Value - record.ParkStart).TotalHours, 2);
                    }

                    // 查找对应的支付记录
                    var paymentKey = $"{record.LicensePlateNumber}_{record.ParkStart:yyyyMMddHHmmss}";
                    paymentRecordsDict.TryGetValue(paymentKey, out var paymentRecord);

                    var detailRecord = new ParkingDetailRecordDto
                    {
                        LicensePlateNumber = record.LicensePlateNumber,
                        ParkingSpaceId = record.ParkingSpaceId,
                        AreaId = record.AreaId,
                        ParkStart = record.ParkStart,
                        ParkEnd = record.ParkEnd,
                        Status = record.ParkEnd.HasValue ? "已完成" : "在停中",
                        TotalFee = paymentRecord?.TotalFee,
                        PaymentStatus = paymentRecord?.PaymentStatus,
                        PaymentTime = paymentRecord?.PaymentTime,
                        PaymentMethod = paymentRecord?.PaymentMethod,
                        ParkingDuration = parkingDuration
                    };

                    detailedRecords.Add(detailRecord);
                }

                // 按入场时间排序
                detailedRecords = detailedRecords.OrderByDescending(r => r.ParkStart).ToList();

                Console.WriteLine($"[DEBUG] 成功生成 {detailedRecords.Count} 条详细停车记录");
                return detailedRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成详细停车记录时发生错误");
                Console.WriteLine($"[ERROR] 生成详细停车记录时发生错误: {ex.Message}");
                return new List<ParkingDetailRecordDto>();
            }
        }

        /// <summary>
        /// 获取总车位数
        /// </summary>
        private async Task<int> GetTotalParkingSpaces(int? areaId)
        {
            try
            {
                if (areaId.HasValue)
                {
                    // 查询指定区域的车位数
                    var areaSpaces = await _parkingContext.PARKING_SPACE_DISTRIBUTION
                        .Where(psd => psd.AREA_ID == areaId.Value)
                        .CountAsync();
                    return areaSpaces > 0 ? areaSpaces : 50; // 默认50个车位
                }
                else
                {
                    // 查询所有区域的车位数
                    var totalSpaces = await _parkingContext.PARKING_SPACE_DISTRIBUTION
                        .CountAsync();
                    return totalSpaces > 0 ? totalSpaces : 100; // 默认100个车位
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取总车位数时发生错误");
                return areaId.HasValue ? 50 : 100; // 返回默认值
            }
        }

        /// <summary>
        /// 计算指定小时内的占用车位数
        /// </summary>
        private async Task<int> CalculateOccupiedSpacesAtHour(DateTime startDate, DateTime endDate, int hour, int? areaId)
        {
            try
            {
                // 构建查询条件
                var query = _parkingContext.CAR.AsQueryable();
                
                // 添加区域过滤
                if (areaId.HasValue)
                {
                    // 通过PARK表关联到PARKING_SPACE_DISTRIBUTION来过滤区域
                    var areaCarIds = await _parkingContext.PARK
                        .Join(_parkingContext.PARKING_SPACE_DISTRIBUTION,
                              p => p.PARKING_SPACE_ID,
                              psd => psd.PARKING_SPACE_ID,
                              (p, psd) => new { p.LICENSE_PLATE_NUMBER, psd.AREA_ID })
                        .Where(x => x.AREA_ID == areaId.Value)
                        .Select(x => x.LICENSE_PLATE_NUMBER)
                        .ToListAsync();
                    
                    query = query.Where(c => areaCarIds.Contains(c.LICENSE_PLATE_NUMBER));
                }
                
                // 计算在该小时内占用车位的车辆数
                // 简化逻辑：计算在该小时内处于停车状态的车辆
                var totalOccupiedSpaces = await query
                    .Where(c => 
                        // 车辆在该小时内处于停车状态的条件：
                        // 1. 入场时间 <= 该小时
                        // 2. 出场时间 > 该小时 或者 还没出场
                        c.PARK_START.Hour <= hour && 
                        (!c.PARK_END.HasValue || c.PARK_END.Value.Hour > hour))
                    .CountAsync();
                
                // 特别处理0点：需要加上前一天没出去的车
                if (hour == 0) {
                    var previousDayCars = await query
                        .Where(c => 
                            // 前一天的车
                            c.PARK_START.Date == startDate.AddDays(-1) &&
                            // 还没出去的车
                            !c.PARK_END.HasValue)
                        .CountAsync();
                    
                    totalOccupiedSpaces += previousDayCars;
                    Console.WriteLine($"[DEBUG] {hour:00}:00 - 前一天未出场车辆: {previousDayCars}");
                }
                
                // 根据报表类型返回不同的数据
                var totalDays = (endDate - startDate).Days + 1;
                int occupiedSpaces;
                
                if (totalDays == 1) {
                    // 日报：显示当天的实际占用车位数
                    occupiedSpaces = totalOccupiedSpaces;
                    Console.WriteLine($"[DEBUG] {hour:00}:00 - 日报模式 - 占用: {occupiedSpaces}");
                } else {
                    // 周报/月报：显示平均每天的占用车位数
                    occupiedSpaces = (int)Math.Round((double)totalOccupiedSpaces / totalDays);
                    Console.WriteLine($"[DEBUG] {hour:00}:00 - 周报/月报模式 - 总占用: {totalOccupiedSpaces}, 天数: {totalDays}, 平均占用: {occupiedSpaces}");
                }
                
                return occupiedSpaces;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"计算{hour}时占用车位数时发生错误");
                return 0;
            }
        }

        /// <summary>
        /// 生成Excel报表
        /// </summary>
        private byte[] GenerateExcelReport(OperationReportResponseDto reportData, ReportType reportType)
        {
            try
            {
                // 这里应该使用EPPlus或其他Excel库生成Excel文件
                // 由于没有安装相关包，这里返回一个简单的CSV格式的字节数组作为示例
                var csvContent = GenerateCsvContent(reportData, reportType);
                return System.Text.Encoding.UTF8.GetBytes(csvContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成Excel报表时发生错误");
                return new byte[0];
            }
        }

        /// <summary>
        /// 生成CSV内容（临时替代Excel）
        /// </summary>
        private string GenerateCsvContent(OperationReportResponseDto reportData, ReportType reportType)
        {
            var csv = new System.Text.StringBuilder();
            
            // 报表标题
            csv.AppendLine($"停车场运营报表 ({reportType})");
            csv.AppendLine($"生成时间: {reportData.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine($"统计时间: {reportData.StartDate:yyyy-MM-dd} 至 {reportData.EndDate:yyyy-MM-dd}");
            csv.AppendLine($"统计区域: {reportData.AreaName}");
            csv.AppendLine();
            
            // 总体统计
            csv.AppendLine("总体统计");
            csv.AppendLine($"总停车记录数,{reportData.TotalParkingCount}");
            csv.AppendLine($"总收入,¥{reportData.TotalRevenue}");
            csv.AppendLine($"平均停车时长,{reportData.AverageParkingHours:F1}小时");
            csv.AppendLine($"高峰时段车位利用率,{reportData.PeakUtilizationRate:F1}%");
            csv.AppendLine();
            
            // 每日统计
            csv.AppendLine("每日统计");
            csv.AppendLine("日期,停车记录数,收入,平均停车时长,高峰利用率");
            foreach (var daily in reportData.DailyStatistics)
            {
                csv.AppendLine($"{daily.Date:yyyy-MM-dd},{daily.ParkingCount},{daily.Revenue},{daily.AverageParkingHours:F1},{daily.PeakUtilizationRate:F1}%");
            }
            csv.AppendLine();
            
            // 每小时收入统计
            csv.AppendLine("每小时收入统计");
            csv.AppendLine("小时,收入金额,出场车辆数");
            foreach (var hourly in reportData.HourlyTraffic)
            {
                csv.AppendLine($"{hourly.Hour:00}:00,¥{hourly.Revenue},{hourly.ExitCount}");
            }
            csv.AppendLine();
            
            // 高峰时段车位利用率
            csv.AppendLine("高峰时段车位利用率");
            csv.AppendLine("小时,利用率,已占用车位数,总车位数");
            foreach (var peak in reportData.PeakHourUtilization)
            {
                csv.AppendLine($"{peak.Hour:00}:00,{peak.UtilizationRate:F1}%,{peak.OccupiedSpaces},{peak.TotalSpaces}");
            }
            
            return csv.ToString();
        }

        #endregion
    }
}
