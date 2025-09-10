using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace oracle_backend.Models
{
    public class CashFlowDto
    {
        public class CashFlowOverviewRequestDto
        {
            [Required(ErrorMessage = "开始时间不能为空")]
            [JsonPropertyName("startDate")]
            public DateTime StartDate { get; set; }

            [Required(ErrorMessage = "结束时间不能为空")]
            [JsonPropertyName("endDate")]
            public DateTime EndDate { get; set; }

            [Required(ErrorMessage = "账户ID不能为空")]
            [JsonPropertyName("accountID")]
            public string AccountID { get; set; }

            [JsonPropertyName("timeGranularity")]
            [RegularExpression("day|week|month|quarter|year", ErrorMessage = "时间颗粒度必须是 day, week, month, quarter 或 year")]
            public string TimeGranularity { get; set; } = "month";
        }

        // 详细页面请求DTO
        public class CashFlowDetailRequestDto
        {
            [Required(ErrorMessage = "开始时间不能为空")]
            [JsonPropertyName("startDate")]
            public DateTime StartDate { get; set; }

            [Required(ErrorMessage = "结束时间不能为空")]
            [JsonPropertyName("endDate")]
            public DateTime EndDate { get; set; }

            [Required(ErrorMessage = "账户ID不能为空")]
            [JsonPropertyName("accountID")]
            public string AccountID { get; set; }

            [JsonPropertyName("timeGranularity")]
            [RegularExpression("day|week|month|quarter|year", ErrorMessage = "时间颗粒度必须是 day, week, month, quarter 或 year")]
            public string TimeGranularity { get; set; } = "month";

            [Required(ErrorMessage = "模块类型不能为空")]
            [JsonPropertyName("moduleType")]
            [RegularExpression("商户租金|活动结算|停车场收费|设备维修|促销活动|员工工资",
                ErrorMessage = "模块类型必须是: 商户租金, 活动结算, 停车场收费, 设备维修, 促销活动, 员工工资")]
            public string ModuleType { get; set; }

            [JsonPropertyName("relatedPartyType")]
            [RegularExpression("商户|合作方", ErrorMessage = "关联方类型必须是 商户 或 合作方")]
            public string? RelatedPartyType { get; set; }

            [JsonPropertyName("relatedPartyId")]
            [Range(1, int.MaxValue, ErrorMessage = "关联方ID必须是正整数")]
            public int? RelatedPartyId { get; set; }

            [JsonPropertyName("includeDetails")]
            public bool IncludeDetails { get; set; } = false;
        }

        // 导出请求DTO
        public class CashFlowExportRequestDto
        {
            [Required(ErrorMessage = "开始时间不能为空")]
            [JsonPropertyName("startDate")]
            public DateTime StartDate { get; set; }

            [Required(ErrorMessage = "结束时间不能为空")]
            [JsonPropertyName("endDate")]
            public DateTime EndDate { get; set; }

            [Required(ErrorMessage = "账户ID不能为空")]
            [JsonPropertyName("accountID")]
            public string AccountID { get; set; }

            [JsonPropertyName("timeGranularity")]
            [RegularExpression("day|week|month|quarter|year", ErrorMessage = "时间颗粒度必须是 day, week, month, quarter 或 year")]
            public string TimeGranularity { get; set; } = "month";

            [JsonPropertyName("moduleType")]
            [RegularExpression("商户租金|活动结算|停车场收费|设备维修|促销活动|员工工资|所有模块",
                ErrorMessage = "模块类型必须是: 商户租金, 活动结算, 停车场收费, 设备维修, 促销活动, 员工工资, 所有模块")]
            public string? ModuleType { get; set; } = "所有模块";

            [JsonPropertyName("relatedPartyType")]
            [RegularExpression("商户|合作方", ErrorMessage = "关联方类型必须是 商户 或 合作方")]
            public string? RelatedPartyType { get; set; }

            [JsonPropertyName("relatedPartyId")]
            [Range(1, int.MaxValue, ErrorMessage = "关联方ID必须是正整数")]
            public int? RelatedPartyId { get; set; }

            [JsonPropertyName("exportType")]
            [RegularExpression("summary|detailed", ErrorMessage = "导出类型必须是 summary 或 detailed")]
            public string ExportType { get; set; } = "summary";

            [JsonPropertyName("format")]
            [RegularExpression("excel|pdf", ErrorMessage = "格式必须是 excel 或 pdf")]
            public string Format { get; set; } = "excel";
        }

        //首页折线图响应DTO
        public class CashFlowOverviewResponseDto
        {
            public string ReportTitle { get; set; }
            public DateTime GenerateTime { get; set; }
            public CashFlowOverviewRequestDto Criteria { get; set; }
            public List<CashFlowOverviewModuleDto> Modules { get; set; }
            public SummaryInfo Summary { get; set; }
        }

        // 每个模块的时间序列汇总
        public class CashFlowOverviewModuleDto
        {
            public string ModuleType { get; set; }
            public List<TimeSeriesData> TimeSeriesDatas { get; set; }
            public double TotalIncome { get; set; }
            public double TotalExpense { get; set; }
            public double NetFlow { get; set; }
        }

        // 详细页面响应DTO
        public class CashFlowDetailResponseDto
        {
            public string ReportTitle { get; set; }
            public DateTime GenerateTime { get; set; }
            public CashFlowDetailRequestDto Criteria { get; set; }
            public List<TimeSeriesData> TimeSeriesDatas { get; set; }
            public List<CashFlowRecord> Details { get; set; }
            public SummaryInfo Summary { get; set; }
            public List<RelatedPartySummary> RelatedPartySummaries { get; set; }
        }

        // 关联方汇总
        public class RelatedPartySummary
        {
            public string PartyType { get; set; }
            public int PartyId { get; set; }
            public double TotalAmount { get; set; }
            public int RecordCount { get; set; }
        }

        // 其他DTO保持不变
        public class SummaryInfo
        {
            public double TotalIncome { get; set; }
            public double TotalExpense { get; set; }
            public double NetFlow { get; set; }
            public int RecordCount { get; set; }
        }

        public class TimeSeriesData
        {
            public string Period { get; set; }
            public double Income { get; set; }
            public double Expense { get; set; }
            public double NetFlow { get; set; }
            public int RecordCount { get; set; }
            public string? RelatedPartyType { get; set; }
            public int? RelatedPartyId { get; set; }
        }

        public class CashFlowRecord
        {
            public DateTime Date { get; set; }
            public string Type { get; set; }
            public string Category { get; set; }
            public string Description { get; set; }
            public string Reference { get; set; }
            public double Amount { get; set; }
            public string RelatedPartyType { get; set; }
            public int? RelatedPartyId { get; set; }
        }
    }
}
