using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oracle_backend.Models
{
    #region 停车场信息管理相关模型

    /// <summary>
    /// 停车场信息管理DTO
    /// </summary>
    public class ParkingLotInfoDto
    {
        [Required(ErrorMessage = "停车场区域ID为必填项")]
        public int AreaId { get; set; }

        [Required(ErrorMessage = "车位数为必填项")]
        [Range(1, int.MaxValue, ErrorMessage = "车位数必须大于0")]
        public int ParkingSpaceCount { get; set; }

        [Required(ErrorMessage = "单位时间停车费为必填项")]
        [Range(0.01, double.MaxValue, ErrorMessage = "停车费必须大于0")]
        public int ParkingFee { get; set; }

        [Required(ErrorMessage = "停车场状态为必填项")]
        [StringLength(20, ErrorMessage = "状态长度不能超过20个字符")]
        public string Status { get; set; } = string.Empty;

        [Required(ErrorMessage = "操作员账号为必填项")]
        public string OperatorAccount { get; set; } = string.Empty;
    }

    /// <summary>
    /// 停车场信息响应模型
    /// </summary>
    public class ParkingLotInfoResponse
    {
        public int AreaId { get; set; }
        public int ParkingFee { get; set; }
        public int TotalSpaces { get; set; }
        public int OccupiedSpaces { get; set; }
        public int AvailableSpaces { get; set; }
        public double OccupancyRate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastUpdateTime { get; set; }
    }

    #endregion

    #region 停车场状态统计相关Model

    /// <summary>
    /// 停车场状态统计
    /// </summary>
    public class ParkingStatusStatistics
    {
        public int AreaId { get; set; }
        public int TotalSpaces { get; set; }
        public int OccupiedSpaces { get; set; }
        public int AvailableSpaces { get; set; }
        public double OccupancyRate { get; set; }
    }

    /// <summary>
    /// 车位状态详情
    /// </summary>
    public class ParkingSpaceStatus
    {
        public int ParkingSpaceId { get; set; }
        public bool IsOccupied { get; set; }
        public string? LicensePlateNumber { get; set; }
        public DateTime? ParkStart { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    /// <summary>
    /// 车位状态查询请求DTO
    /// </summary>
    public class ParkingStatusQueryDto
    {
        public int AreaId { get; set; }
        public int? ParkingSpaceId { get; set; }
        public string? OperatorAccount { get; set; }
    }

    #endregion

    #region 车辆管理相关Model

    /// <summary>
    /// 车辆入场请求DTO
    /// </summary>
    public class VehicleEntryDto
    {
        [Required(ErrorMessage = "车牌号为必填项")]
        [StringLength(20, ErrorMessage = "车牌号长度不能超过20个字符")]
        public string LicensePlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "车位ID为必填项")]
        public int ParkingSpaceId { get; set; }

        [Required(ErrorMessage = "操作员账号为必填项")]
        public string OperatorAccount { get; set; } = string.Empty;
    }

    /// <summary>
    /// 车辆出场请求DTO
    /// </summary>
    public class VehicleExitDto
    {
        [Required(ErrorMessage = "车牌号为必填项")]
        public string LicensePlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "操作员账号为必填项")]
        public string OperatorAccount { get; set; } = string.Empty;
    }

    /// <summary>
    /// 车辆出场结果
    /// </summary>
    public class VehicleExitResult
    {
        public string LicensePlateNumber { get; set; } = string.Empty;
        public int ParkingSpaceId { get; set; }
        public DateTime ParkStart { get; set; }
        public DateTime ParkEnd { get; set; }
        public TimeSpan ParkingDuration { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalFee { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? PaymentTime { get; set; }
        public string? PaymentMethod { get; set; }
    }

    /// <summary>
    /// 车辆状态结果
    /// </summary>
    public class VehicleStatusResult
    {
        public string LicensePlateNumber { get; set; } = string.Empty;
        public int ParkingSpaceId { get; set; }
        public int AreaId { get; set; }
        public DateTime ParkStart { get; set; }
        public TimeSpan CurrentDuration { get; set; }
        public string ParkingLotName { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsCurrentlyParked { get; set; }
    }

    /// <summary>
    /// 已停车车辆信息
    /// </summary>
    public class ParkedVehicleInfo
    {
        public string LicensePlateNumber { get; set; } = string.Empty;
        public int ParkingSpaceId { get; set; }
        public int AreaId { get; set; }
        public DateTime ParkStart { get; set; }
        public TimeSpan CurrentDuration { get; set; }
        public string ParkingLotName { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public decimal EstimatedFee { get; set; }
    }

    /// <summary>
    /// 车辆查询请求DTO
    /// </summary>
    public class VehicleQueryDto
    {
        public string? LicensePlateNumber { get; set; }
        public int? AreaId { get; set; }
        public string? OperatorAccount { get; set; }
    }

    #endregion

    #region 支付相关Model

    /// <summary>
    /// 停车费支付记录
    /// </summary>
    public class ParkingPaymentRecord
    {
        public string LicensePlateNumber { get; set; } = string.Empty;
        public int ParkingSpaceId { get; set; }
        public DateTime ParkStart { get; set; }
        public DateTime? ParkEnd { get; set; }
        public decimal TotalFee { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? PaymentTime { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// 停车费支付请求DTO
    /// </summary>
    public class ParkingPaymentRequest
    {
        [Required(ErrorMessage = "车牌号为必填项")]
        public string LicensePlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "车位ID为必填项")]
        public int ParkingSpaceId { get; set; }

        [Required(ErrorMessage = "停车开始时间为必填项")]
        public DateTime ParkStart { get; set; }

        public DateTime? ParkEnd { get; set; }

        [Required(ErrorMessage = "总费用为必填项")]
        [Range(0.01, double.MaxValue, ErrorMessage = "费用必须大于0")]
        public decimal TotalFee { get; set; }

        [Required(ErrorMessage = "支付方式为必填项")]
        public string PaymentMethod { get; set; } = string.Empty;

        public string? PaymentReference { get; set; }
        public string? Remarks { get; set; }
    }

    #endregion

    #region 统计报表相关Model

    /// <summary>
    /// 停车场运营统计
    /// </summary>
    public class ParkingOperationStatistics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AreaId { get; set; }
        public int TotalVehicles { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageParkingHours { get; set; }
        public int PeakHour { get; set; }
        public int PeakHourVehicleCount { get; set; }
        public List<PeakHourData> PeakHourAnalysis { get; set; } = new List<PeakHourData>();
    }

    /// <summary>
    /// 高峰时段数据
    /// </summary>
    public class PeakHourData
    {
        public int Hour { get; set; }
        public int VehicleCount { get; set; }
        public decimal Revenue { get; set; }
        public double UtilizationRate { get; set; }
    }

    /// <summary>
    /// 停车场统计报表请求DTO
    /// </summary>
    public class ParkingStatisticsReportDto
    {
        [Required(ErrorMessage = "操作员账号为必填项")]
        public string OperatorAccount { get; set; } = string.Empty;

        [Required(ErrorMessage = "停车场区域ID为必填项")]
        public int AreaId { get; set; }

        [Required(ErrorMessage = "开始日期为必填项")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "结束日期为必填项")]
        public DateTime EndDate { get; set; }

        public string ReportType { get; set; } = "daily"; // daily, weekly, monthly
        public string Dimension { get; set; } = "all"; // time, area, all
    }

    /// <summary>
    /// 停车场收入统计
    /// </summary>
    public class ParkingRevenueStatistics
    {
        public int AreaId { get; set; }
        public DateTime Date { get; set; }
        public int TotalVehicles { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageRevenuePerVehicle { get; set; }
        public int PeakHour { get; set; }
        public double PeakUtilizationRate { get; set; }
    }

    /// <summary>
    /// 停车场车流量统计
    /// </summary>
    public class ParkingTrafficStatistics
    {
        public int AreaId { get; set; }
        public DateTime Date { get; set; }
        public int TotalEntries { get; set; }
        public int TotalExits { get; set; }
        public int PeakHourEntries { get; set; }
        public int PeakHourExits { get; set; }
        public TimeSpan AverageParkingDuration { get; set; }
        public double AverageOccupancyRate { get; set; }
    }

    #endregion

    #region 统计报表响应Model

    /// <summary>
    /// 每小时收入统计
    /// </summary>
    public class HourlyTrafficDto
    {
        public int Hour { get; set; }
        public decimal Revenue { get; set; } // 该时间段出场的车辆总费用
        public int EntryCount { get; set; } // 该时间段入场的车辆数量
        public int ExitCount { get; set; } // 该时间段出场的车辆数量
    }

    /// <summary>
    /// 每日统计
    /// </summary>
    public class DailyStatisticsDto
    {
        public DateTime Date { get; set; }
        public int ParkingCount { get; set; }
        public decimal Revenue { get; set; }
        public double AverageParkingHours { get; set; }
    }

    #endregion

    #region 通用响应模型

    /// <summary>
    /// 通用API响应模型
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 分页响应模型
    /// </summary>
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }

    #endregion

    #region 枚举定义

    /// <summary>
    /// 停车场状态枚举
    /// </summary>
    public enum ParkingLotStatus
    {
        正常运营,
        维护中,
        暂停服务,
        已满
    }

    /// <summary>
    /// 车位状态枚举
    /// </summary>
    public enum ParkingSpaceStatusEnum
    {
        空闲,
        占用,
        预留,
        维护
    }

    /// <summary>
    /// 支付状态枚举
    /// </summary>
    public enum PaymentStatus
    {
        未支付,
        已支付,
        部分支付,
        已退款
    }

    /// <summary>
    /// 支付方式枚举
    /// </summary>
    public enum PaymentMethod
    {
        现金,
        微信支付,
        支付宝,
        银行卡,
        无感支付
    }

    #endregion
}
