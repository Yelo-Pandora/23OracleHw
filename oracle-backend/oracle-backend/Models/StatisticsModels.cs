using System.ComponentModel.DataAnnotations;

namespace oracle_backend.Models
{
    // 基础统计数据
    public class BasicStatistics
    {
        public int TotalStores { get; set; }
        public int ActiveStores { get; set; }
        public int TotalAreas { get; set; }
        public int VacantAreas { get; set; }
        public double OccupancyRate { get; set; }
        public decimal AverageRent { get; set; }
    }

    // 按类型统计
    public class StoreTypeStatistics
    {
        public string StoreType { get; set; } = string.Empty;
        public int StoreCount { get; set; }
        public int ActiveStores { get; set; }
        public decimal TotalRent { get; set; }
        public decimal AverageRent { get; set; }
    }

    // 按区域统计
    public class AreaStatistics
    {
        public int AreaId { get; set; }
        public int AreaSize { get; set; }
        public decimal BaseRent { get; set; }
        public string RentStatus { get; set; } = string.Empty;
        public bool IsOccupied { get; set; }
        public string? StoreName { get; set; }
        public string? TenantName { get; set; }
        public string? StoreType { get; set; }
        public DateTime? RentStart { get; set; }
        public DateTime? RentEnd { get; set; }
    }

    // 按状态统计
    public class StoreStatusStatistics
    {
        public string StoreStatus { get; set; } = string.Empty;
        public int StoreCount { get; set; }
        public decimal AverageRent { get; set; }
        public decimal TotalRent { get; set; }
    }

    // 统计报表请求DTO
    public class StatisticsReportDto
    {
        [Required]
        public string OperatorAccount { get; set; } = string.Empty;
        
        public string Dimension { get; set; } = "all"; // type, area, status, all
        
        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
    }
}
