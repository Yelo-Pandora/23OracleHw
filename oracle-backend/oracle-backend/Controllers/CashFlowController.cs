using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using oracle_backend.Services;
using System.Data;
using System.Globalization;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using iText.Kernel.Font;
using iText.IO.Font;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.Text;
using Microsoft.Extensions.Logging;
using static oracle_backend.Models.CashFlowDto;
using System.ComponentModel;
using Oracle.ManagedDataAccess.Client;

namespace oracle_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CashFlowController : ControllerBase
    {
        private readonly CashFlowDbContext _context;
        private readonly ILogger<CashFlowController> _logger;
        private readonly StoreDbContext _storeContext;
        private readonly SaleEventService _saleEventService;
        private readonly AccountDbContext _accountContext;
        private readonly CollaborationDbContext _collabContext;

        public CashFlowController(
            CashFlowDbContext context,
            ILogger<CashFlowController> logger,
            StoreDbContext storeContext,
            SaleEventService saleEventService,
            AccountDbContext accountContext,
            CollaborationDbContext collabContext)
        {
            _context = context;
            _logger = logger;
            _storeContext = storeContext;
            _saleEventService = saleEventService;
            _accountContext = accountContext;
            _collabContext = collabContext;
        }

        #region 首页接口 - 获取六个模块的汇总数据

        [HttpPost("overview")]
        public async Task<IActionResult> GetCashFlowOverview([FromBody] CashFlowOverviewRequestDto request)
        {
            _logger.LogInformation("正在获取现金流总览数据");

            try
            {
                if (!await ValidateUserPermission(request.AccountID))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "用户无权限访问现金流报表"
                    });
                }

                if (request.EndDate < request.StartDate)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "结束时间必须晚于开始时间"
                    });
                }

                LogQueryAudit("overview", request.AccountID, request.StartDate, request.EndDate, request.TimeGranularity);

                // 生成总览数据
                var response = await GenerateOverviewDataAsync(request);

                return Ok(new ApiResponse<CashFlowOverviewResponseDto>
                {
                    Success = true,
                    Data = response,
                    Message = "成功获取现金流总览数据"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取现金流总览数据时发生错误");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "获取数据时发生错误，请稍后重试",
                });
            }
        }

        private async Task<CashFlowOverviewResponseDto> GenerateOverviewDataAsync(CashFlowOverviewRequestDto request)
        {
            var modules = new[] { "商户租金", "活动结算", "停车场收费", "设备维修", "促销活动", "员工工资" };
            var moduleDataList = new List<CashFlowOverviewModuleDto>();

            double totalIncome = 0;
            double totalExpense = 0;
            int totalRecordCount = 0;

            //获取所有模块的数据
            foreach (var module in modules)
            {
                var records = await GetRecordsByModuleAsync(module, request.StartDate, request.EndDate);

                if (records.Any())
                {
                    var timeSeriesData = AggregateByTimeGranularity(records, request.TimeGranularity);
                    var moduleIncome = records.Where(r => r.Type == "收入").Sum(r => r.Amount);
                    var moduleExpense = records.Where(r => r.Type == "支出").Sum(r => r.Amount);

                    moduleDataList.Add(new CashFlowOverviewModuleDto
                    {
                        ModuleType = module,
                        TimeSeriesDatas = timeSeriesData,
                        TotalIncome = moduleIncome,
                        TotalExpense = moduleExpense,
                        NetFlow = moduleIncome - moduleExpense
                    });

                    totalIncome += moduleIncome;
                    totalExpense += moduleExpense;
                    totalRecordCount += records.Count;
                }
            }

            return new CashFlowOverviewResponseDto
            {
                ReportTitle = $"现金流总览 - {request.StartDate:yyyy-MM-dd} 至 {request.EndDate:yyyy-MM-dd}",
                GenerateTime = DateTime.Now,
                Criteria = request,
                Modules = moduleDataList,
                Summary = new SummaryInfo
                {
                    TotalIncome = totalIncome,
                    TotalExpense = totalExpense,
                    NetFlow = totalIncome - totalExpense,
                    RecordCount = totalRecordCount
                }
            };
        }

        #endregion

        #region 详细页面接口 - 获取特定模块的详细数据

        [HttpPost("detail")]
        public async Task<IActionResult> GetCashFlowDetail([FromBody] CashFlowDetailRequestDto request)
        {
            _logger.LogInformation("正在获取现金流详细数据");

            try
            {
                if (!await ValidateUserPermission(request.AccountID))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "用户无权限访问现金流报表"
                    });
                }

                if (request.EndDate < request.StartDate)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "结束时间必须晚于开始时间"
                    });
                }

                var validationResult = ValidateFilterCombination(request.ModuleType, request.RelatedPartyType);
                if (!string.IsNullOrEmpty(validationResult))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = validationResult
                    });
                }

                LogQueryAudit("detail", request.AccountID, request.StartDate, request.EndDate,
                    request.TimeGranularity, request.ModuleType, request.RelatedPartyType, request.RelatedPartyId);

                // 生成详细数据
                var response = await GenerateDetailDataAsync(request);

                if (!response.TimeSeriesDatas.Any() && (!request.IncludeDetails || !response.Details.Any()))
                {
                    return Ok(new ApiResponse<CashFlowDetailResponseDto>
                    {
                        Success = true,
                        Data = response,
                        Message = "当前时间段无现金流记录",
                    });
                }

                return Ok(new ApiResponse<CashFlowDetailResponseDto>
                {
                    Success = true,
                    Data = response,
                    Message = $"成功获取现金流详细数据，共{response.Summary.RecordCount}条记录",
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取现金流详细数据时发生错误");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "获取数据时发生错误，请稍后重试",
                });
            }
        }

        private async Task<CashFlowDetailResponseDto> GenerateDetailDataAsync(CashFlowDetailRequestDto request)
        {
            // 获取指定模块的记录
            var records = await GetRecordsByModuleAsync(request.ModuleType, request.StartDate, request.EndDate);

            // 应用筛选条件
            var filteredRecords = ApplyFilters(records, request.ModuleType, request.RelatedPartyType, request.RelatedPartyId);

            // 生成时间序列数据
            var timeSeriesData = AggregateByTimeGranularity(filteredRecords, request.TimeGranularity);

            // 生成关联方汇总数据
            var relatedPartySummaries = new List<RelatedPartySummary>();
            if (!string.IsNullOrEmpty(request.RelatedPartyType))
            {
                relatedPartySummaries = await GetRelatedPartySummariesAsync(
                    filteredRecords, request.RelatedPartyType, request.RelatedPartyId);
            }

            return new CashFlowDetailResponseDto
            {
                ReportTitle = $"{request.ModuleType}现金流详情 - {request.StartDate:yyyy-MM-dd} 至 {request.EndDate:yyyy-MM-dd}",
                GenerateTime = DateTime.Now,
                Criteria = request,
                TimeSeriesDatas = timeSeriesData,
                Details = request.IncludeDetails ? filteredRecords : new List<CashFlowRecord>(),
                Summary = new SummaryInfo
                {
                    TotalIncome = filteredRecords.Where(r => r.Type == "收入").Sum(r => r.Amount),
                    TotalExpense = filteredRecords.Where(r => r.Type == "支出").Sum(r => r.Amount),
                    NetFlow = filteredRecords.Sum(r => r.Type == "收入" ? r.Amount : -r.Amount),
                    RecordCount = filteredRecords.Count
                },
                RelatedPartySummaries = relatedPartySummaries
            };
        }

        #endregion

        #region 导出接口

        [HttpPost("export")]
        public async Task<IActionResult> ExportCashFlowReport([FromBody] CashFlowExportRequestDto request)
        {
            _logger.LogInformation("正在导出现金流报表，格式: {Format}", request.Format);

            try
            {
                if (!await ValidateUserPermission(request.AccountID))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "用户无权限导出现金流报表"
                    });
                }

                if (request.EndDate < request.StartDate)
                {
                    return BadRequest(new ApiResponse<object> 
                    {
                        Success = false,
                        Message = "结束时间必须晚于开始时间"
                    });
                }

                // 验证筛选条件组合
                if (!string.IsNullOrEmpty(request.RelatedPartyType) && !string.IsNullOrEmpty(request.ModuleType) &&
                    request.ModuleType != "所有模块")
                {
                    var validationResult = ValidateFilterCombination(request.ModuleType, request.RelatedPartyType);
                    if (!string.IsNullOrEmpty(validationResult))
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = validationResult
                        });
                    }
                }

                LogQueryAudit("export", request.AccountID, request.StartDate, request.EndDate,
                    request.TimeGranularity, request.ModuleType, request.RelatedPartyType, request.RelatedPartyId);
                // 获取导出数据
                object reportData;
                if (request.ModuleType == "所有模块")
                {
                    var overviewRequest = new CashFlowOverviewRequestDto
                    {
                        StartDate = request.StartDate,
                        EndDate = request.EndDate,
                        AccountID = request.AccountID,
                        TimeGranularity = request.TimeGranularity
                    };
                    reportData = await GenerateOverviewDataAsync(overviewRequest);
                }
                else
                {
                    var detailRequest = new CashFlowDetailRequestDto
                    {
                        StartDate = request.StartDate,
                        EndDate = request.EndDate,
                        AccountID = request.AccountID,
                        TimeGranularity = request.TimeGranularity,
                        ModuleType = request.ModuleType,
                        RelatedPartyType = request.RelatedPartyType,
                        RelatedPartyId = request.RelatedPartyId,
                        IncludeDetails = request.ExportType == "detailed"
                    };
                    reportData = await GenerateDetailDataAsync(detailRequest);
                }

                byte[] fileBytes;
                string contentType;
                string fileExtension;

                if (request.Format.ToLower() == "pdf")
                {
                    //所有模块——总览
                    (fileBytes, contentType, fileExtension) = request.ModuleType == "所有模块" ?
                        GeneratePdfOverviewReport((CashFlowOverviewResponseDto)reportData) :
                        GeneratePdfDetailReport((CashFlowDetailResponseDto)reportData);
                }
                else
                {
                    (fileBytes, contentType, fileExtension) = request.ModuleType == "所有模块" ?
                        GenerateExcelOverviewReport((CashFlowOverviewResponseDto)reportData) :
                        GenerateExcelDetailReport((CashFlowDetailResponseDto)reportData);
                }
                var fileName = $"现金流报表_{request.StartDate:yyyy年MM月dd日}-{request.EndDate:yyyy年MM月dd日}{fileExtension}";
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出现金流报表时发生错误");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "导出报表时发生错误，请稍后重试",
                });
            }
        }

        // Excel导出方法 - 总览
        private (byte[], string, string) GenerateExcelOverviewReport(CashFlowOverviewResponseDto reportData)
        {
            try
            {
                using (var package = new ExcelPackage())
                {
                    // 汇总工作表
                    var summarySheet = package.Workbook.Worksheets.Add("汇总");
                    summarySheet.Cells["A1"].Value = reportData.ReportTitle;
                    summarySheet.Cells["A1"].Style.Font.Bold = true;
                    summarySheet.Cells["A1"].Style.Font.Size = 16;

                    summarySheet.Cells["A2"].Value = $"生成时间: {reportData.GenerateTime:yyyy-MM-dd HH:mm:ss}";
                    summarySheet.Cells["A3"].Value = $"统计时间: {reportData.Criteria.StartDate:yyyy-MM-dd} 至 {reportData.Criteria.EndDate:yyyy-MM-dd}";

                    summarySheet.Cells["A5"].Value = "总体统计";
                    summarySheet.Cells["A5"].Style.Font.Bold = true;

                    summarySheet.Cells["A6"].Value = "总收入";
                    summarySheet.Cells["B6"].Value = reportData.Summary.TotalIncome;
                    summarySheet.Cells["B6"].Style.Numberformat.Format = "¥#,##0.00";

                    summarySheet.Cells["A7"].Value = "总支出";
                    summarySheet.Cells["B7"].Value = reportData.Summary.TotalExpense;
                    summarySheet.Cells["B7"].Style.Numberformat.Format = "¥#,##0.00";

                    summarySheet.Cells["A8"].Value = "净现金流";
                    summarySheet.Cells["B8"].Value = reportData.Summary.NetFlow;
                    summarySheet.Cells["B8"].Style.Numberformat.Format = "¥#,##0.00";

                    summarySheet.Cells["A9"].Value = "记录总数";
                    summarySheet.Cells["B9"].Value = reportData.Summary.RecordCount;

                    // 各模块汇总工作表
                    var modulesSheet = package.Workbook.Worksheets.Add("各模块汇总");
                    modulesSheet.Cells["A1"].Value = "模块类型";
                    modulesSheet.Cells["B1"].Value = "总收入";
                    modulesSheet.Cells["C1"].Value = "总支出";
                    modulesSheet.Cells["D1"].Value = "净现金流";

                    for (int i = 0; i < reportData.Modules.Count; i++)
                    {
                        var module = reportData.Modules[i];
                        modulesSheet.Cells[i + 2, 1].Value = module.ModuleType;
                        modulesSheet.Cells[i + 2, 2].Value = module.TotalIncome;
                        modulesSheet.Cells[i + 2, 3].Value = module.TotalExpense;
                        modulesSheet.Cells[i + 2, 4].Value = module.NetFlow;

                        modulesSheet.Cells[i + 2, 2].Style.Numberformat.Format = "¥#,##0.00";
                        modulesSheet.Cells[i + 2, 3].Style.Numberformat.Format = "¥#,##0.00";
                        modulesSheet.Cells[i + 2, 4].Style.Numberformat.Format = "¥#,##0.00";
                    }

                    // 时间序列数据工作表（按模块）
                    foreach (var module in reportData.Modules)
                    {
                        var timeSeriesSheet = package.Workbook.Worksheets.Add(module.ModuleType);
                        timeSeriesSheet.Cells["A1"].Value = "时间段";
                        timeSeriesSheet.Cells["B1"].Value = "收入";
                        timeSeriesSheet.Cells["C1"].Value = "支出";
                        timeSeriesSheet.Cells["D1"].Value = "净现金流";

                        for (int i = 0; i < module.TimeSeriesDatas.Count; i++)
                        {
                            var data = module.TimeSeriesDatas[i];
                            timeSeriesSheet.Cells[i + 2, 1].Value = data.Period;
                            timeSeriesSheet.Cells[i + 2, 2].Value = data.Income;
                            timeSeriesSheet.Cells[i + 2, 3].Value = data.Expense;
                            timeSeriesSheet.Cells[i + 2, 4].Value = data.NetFlow;

                            timeSeriesSheet.Cells[i + 2, 2].Style.Numberformat.Format = "¥#,##0.00";
                            timeSeriesSheet.Cells[i + 2, 3].Style.Numberformat.Format = "¥#,##0.00";
                            timeSeriesSheet.Cells[i + 2, 4].Style.Numberformat.Format = "¥#,##0.00";
                        }

                        using (var range = timeSeriesSheet.Cells["A1:D1"])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        }

                        timeSeriesSheet.Cells[timeSeriesSheet.Dimension.Address].AutoFitColumns();
                    }

                    //设置样式
                    using (var range = modulesSheet.Cells["A1:D1"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    summarySheet.Cells[summarySheet.Dimension.Address].AutoFitColumns();
                    modulesSheet.Cells[modulesSheet.Dimension.Address].AutoFitColumns();

                    var fileBytes = package.GetAsByteArray();
                    return (fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成Excel总览报表时发生错误");
                throw;
            }
        }

        // Excel导出方法 - 详情
        private (byte[], string, string) GenerateExcelDetailReport(CashFlowDetailResponseDto reportData)
        {
            try
            {

                using (var package = new ExcelPackage())
                {
                    var summarySheet = package.Workbook.Worksheets.Add("汇总");
                    summarySheet.Cells["A1"].Value = reportData.ReportTitle;
                    summarySheet.Cells["A1"].Style.Font.Bold = true;
                    summarySheet.Cells["A1"].Style.Font.Size = 16;

                    summarySheet.Cells["A2"].Value = $"生成时间: {reportData.GenerateTime:yyyy-MM-dd HH:mm:ss}";
                    summarySheet.Cells["A3"].Value = $"统计时间: {reportData.Criteria.StartDate:yyyy-MM-dd} 至 {reportData.Criteria.EndDate:yyyy-MM-dd}";

                    if (!string.IsNullOrEmpty(reportData.Criteria.ModuleType))
                    {
                        summarySheet.Cells["A4"].Value = $"模块类型: {reportData.Criteria.ModuleType}";
                    }

                    if (!string.IsNullOrEmpty(reportData.Criteria.RelatedPartyType) && reportData.Criteria.RelatedPartyId.HasValue)
                    {
                        summarySheet.Cells["A5"].Value = $"关联方: {reportData.Criteria.RelatedPartyType}-{reportData.Criteria.RelatedPartyId}";
                    }

                    summarySheet.Cells["A7"].Value = "总体统计";
                    summarySheet.Cells["A7"].Style.Font.Bold = true;

                    summarySheet.Cells["A8"].Value = "总收入";
                    summarySheet.Cells["B8"].Value = reportData.Summary.TotalIncome;
                    summarySheet.Cells["B8"].Style.Numberformat.Format = "¥#,##0.00";

                    summarySheet.Cells["A9"].Value = "总支出";
                    summarySheet.Cells["B9"].Value = reportData.Summary.TotalExpense;
                    summarySheet.Cells["B9"].Style.Numberformat.Format = "¥#,##0.00";

                    summarySheet.Cells["A10"].Value = "净现金流";
                    summarySheet.Cells["B10"].Value = reportData.Summary.NetFlow;
                    summarySheet.Cells["B10"].Style.Numberformat.Format = "¥#,##0.00";

                    summarySheet.Cells["A11"].Value = "记录总数";
                    summarySheet.Cells["B11"].Value = reportData.Summary.RecordCount;

                    // 时间序列数据
                    var timeSeriesSheet = package.Workbook.Worksheets.Add("时间序列");
                    timeSeriesSheet.Cells["A1"].Value = "时间段";
                    timeSeriesSheet.Cells["B1"].Value = "收入";
                    timeSeriesSheet.Cells["C1"].Value = "支出";
                    timeSeriesSheet.Cells["D1"].Value = "净现金流";

                    for (int i = 0; i < reportData.TimeSeriesDatas.Count; i++)
                    {
                        var data = reportData.TimeSeriesDatas[i];
                        timeSeriesSheet.Cells[i + 2, 1].Value = data.Period;
                        timeSeriesSheet.Cells[i + 2, 2].Value = data.Income;
                        timeSeriesSheet.Cells[i + 2, 3].Value = data.Expense;
                        timeSeriesSheet.Cells[i + 2, 4].Value = data.NetFlow;

                        timeSeriesSheet.Cells[i + 2, 2].Style.Numberformat.Format = "¥#,##0.00";
                        timeSeriesSheet.Cells[i + 2, 3].Style.Numberformat.Format = "¥#,##0.00";
                        timeSeriesSheet.Cells[i + 2, 4].Style.Numberformat.Format = "¥#,##0.00";
                    }

                    using (var range = timeSeriesSheet.Cells["A1:D1"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // 关联方汇总数据
                    if (reportData.RelatedPartySummaries != null && reportData.RelatedPartySummaries.Any())
                    {
                        var relatedPartySheet = package.Workbook.Worksheets.Add("关联方汇总");
                        relatedPartySheet.Cells["A1"].Value = "关联方类型";
                        relatedPartySheet.Cells["B1"].Value = "关联方ID";
                        relatedPartySheet.Cells["C1"].Value = "总金额";
                        relatedPartySheet.Cells["D1"].Value = "记录数";

                        for (int i = 0; i < reportData.RelatedPartySummaries.Count; i++)
                        {
                            var summary = reportData.RelatedPartySummaries[i];
                            relatedPartySheet.Cells[i + 2, 1].Value = summary.PartyType;
                            relatedPartySheet.Cells[i + 2, 2].Value = summary.PartyId;
                            relatedPartySheet.Cells[i + 2, 3].Value = summary.TotalAmount;
                            relatedPartySheet.Cells[i + 2, 4].Value = summary.RecordCount;

                            relatedPartySheet.Cells[i + 2, 4].Style.Numberformat.Format = "¥#,##0.00";
                        }

                        using (var range = relatedPartySheet.Cells["A1:D1"])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        }

                        relatedPartySheet.Cells[relatedPartySheet.Dimension.Address].AutoFitColumns();
                    }

                    // 明细数据
                    if (reportData.Details != null && reportData.Details.Any())
                    {
                        var detailsSheet = package.Workbook.Worksheets.Add("明细");
                        detailsSheet.Cells["A1"].Value = "日期";
                        detailsSheet.Cells["B1"].Value = "类型";
                        detailsSheet.Cells["C1"].Value = "类别";
                        detailsSheet.Cells["D1"].Value = "描述";
                        detailsSheet.Cells["E1"].Value = "金额";
                        detailsSheet.Cells["F1"].Value = "关联方";

                        for (int i = 0; i < reportData.Details.Count; i++)
                        {
                            var record = reportData.Details[i];
                            detailsSheet.Cells[i + 2, 1].Value = record.Date;
                            detailsSheet.Cells[i + 2, 1].Style.Numberformat.Format = "yyyy-MM-dd";

                            detailsSheet.Cells[i + 2, 2].Value = record.Type;
                            detailsSheet.Cells[i + 2, 3].Value = record.Category;
                            detailsSheet.Cells[i + 2, 4].Value = record.Description;
                            detailsSheet.Cells[i + 2, 5].Value = record.Amount;
                            detailsSheet.Cells[i + 2, 5].Style.Numberformat.Format = "¥#,##0.00";

                            detailsSheet.Cells[i + 2, 6].Value = record.RelatedPartyId.HasValue ?
                                $"{record.RelatedPartyType}-{record.RelatedPartyId}" :
                                record.RelatedPartyType;
                        }

                        using (var range = detailsSheet.Cells["A1:F1"])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        }

                        detailsSheet.Cells[detailsSheet.Dimension.Address].AutoFitColumns();
                    }

                    summarySheet.Cells[summarySheet.Dimension.Address].AutoFitColumns();
                    timeSeriesSheet.Cells[timeSeriesSheet.Dimension.Address].AutoFitColumns();

                    var fileBytes = package.GetAsByteArray();
                    return (fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成Excel详细报表时发生错误");
                throw;
            }
        }

        // PDF导出方法 - 总览
        private (byte[], string, string) GeneratePdfOverviewReport(CashFlowOverviewResponseDto reportData)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                    // 创建PdfWriter和PdfDocument
                    var writer = new iText.Kernel.Pdf.PdfWriter(memoryStream);
                    var pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer);
                    var document = new iText.Layout.Document(pdfDoc, iText.Kernel.Geom.PageSize.A4.Rotate());
                    document.SetMargins(20, 20, 30, 30);

                    //中文字体
                    PdfFont font = PdfFontFactory.CreateFont("C:/Windows/Fonts/simsun.ttc,0", PdfEncodings.IDENTITY_H, true);
                    float titleSize = 16, headerSize = 12, normalSize = 10;
                    string SafeText(string text) => text ?? "";

                    //添加标题
                    document.Add(new Paragraph(SafeText(reportData.ReportTitle))
                        .SetFont(font)
                        .SetFontSize(titleSize)
                        .SetBold()
                        .SetTextAlignment(TextAlignment.CENTER));

                    document.Add(new Paragraph(" "));

                    // 报告信息
                    document.Add(new Paragraph($"生成时间: {reportData.GenerateTime:yyyy-MM-dd HH:mm:ss}")
                        .SetFont(font)
                        .SetFontSize(normalSize));
                    document.Add(new Paragraph($"统计时间: {reportData.Criteria.StartDate:yyyy-MM-dd} 至 {reportData.Criteria.EndDate:yyyy-MM-dd}")
                        .SetFont(font)
                        .SetFontSize(normalSize));

                    document.Add(new Paragraph(" "));

                    // 总体统计表格
                    document.Add(new Paragraph("总体统计").SetFont(font).SetFontSize(headerSize).SetBold());
                    var summaryTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })).UseAllAvailableWidth();
                    summaryTable.AddHeaderCell(new Cell().Add(new Paragraph("项目").SetFont(font).SetFontSize(headerSize).SetBold()));
                    summaryTable.AddHeaderCell(new Cell().Add(new Paragraph("数值").SetFont(font).SetFontSize(headerSize).SetBold()));
                    summaryTable.AddCell(new Cell().Add(new Paragraph("总收入").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph($"¥{reportData.Summary.TotalIncome:F2}").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph("总支出").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph($"¥{reportData.Summary.TotalExpense:F2}").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph("净现金流").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph($"¥{reportData.Summary.NetFlow:F2}").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph("记录总数").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph(reportData.Summary.RecordCount.ToString()).SetFont(font).SetFontSize(normalSize)));
                    document.Add(summaryTable);
                    document.Add(new Paragraph(" "));

                    //各模块汇总表格
                    document.Add(new Paragraph("各模块汇总").SetFont(font).SetFontSize(headerSize).SetBold());
                    var modulesTable = new Table(UnitValue.CreatePercentArray(new float[] { 2, 1, 1, 1 })).UseAllAvailableWidth();
                    string[] moduleHeaders = { "模块类型", "总收入", "总支出", "净现金流" };
                    foreach (var h in moduleHeaders)
                        modulesTable.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(font).SetFontSize(headerSize).SetBold()));
                    foreach (var module in reportData.Modules)
                    {
                        modulesTable.AddCell(new Cell().Add(new Paragraph(SafeText(module.ModuleType)).SetFont(font).SetFontSize(normalSize)));
                        modulesTable.AddCell(new Cell().Add(new Paragraph($"¥{module.TotalIncome:F2}").SetFont(font).SetFontSize(normalSize)));
                        modulesTable.AddCell(new Cell().Add(new Paragraph($"¥{module.TotalExpense:F2}").SetFont(font).SetFontSize(normalSize)));
                        modulesTable.AddCell(new Cell().Add(new Paragraph($"¥{module.NetFlow:F2}").SetFont(font).SetFontSize(normalSize)));
                    }

                    document.Add(modulesTable);
                    document.Add(new Paragraph(" "));

                    //各模块时间序列数据
                    foreach (var module in reportData.Modules)
                    {
                        if (module.TimeSeriesDatas != null && module.TimeSeriesDatas.Any())
                        {
                            document.Add(new Paragraph($"{module.ModuleType} - 时间序列数据").SetFont(font).SetFontSize(headerSize).SetBold());
                            var timeSeriesTable = new Table(UnitValue.CreatePercentArray(new float[] { 2, 1, 1, 1 })).UseAllAvailableWidth();
                            string[] tsHeaders = { "时间段", "收入", "支出", "净现金流" };
                            foreach (var h in tsHeaders)
                                timeSeriesTable.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(font).SetFontSize(headerSize).SetBold()));

                            foreach (var data in module.TimeSeriesDatas)
                            {
                                timeSeriesTable.AddCell(new Cell().Add(new Paragraph(SafeText(data.Period)).SetFont(font).SetFontSize(normalSize)));
                                timeSeriesTable.AddCell(new Cell().Add(new Paragraph($"¥{data.Income:F2}").SetFont(font).SetFontSize(normalSize)));
                                timeSeriesTable.AddCell(new Cell().Add(new Paragraph($"¥{data.Expense:F2}").SetFont(font).SetFontSize(normalSize)));
                                timeSeriesTable.AddCell(new Cell().Add(new Paragraph($"¥{data.NetFlow:F2}").SetFont(font).SetFontSize(normalSize)));
                            }

                            document.Add(timeSeriesTable);
                            document.Add(new Paragraph(" "));
                        }
                    }

                    document.Close();
                    return (memoryStream.ToArray(), "application/pdf", ".pdf");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成PDF总览报表时发生错误");
                throw;
            }
        }

        // PDF导出方法 - 详情
        private (byte[], string, string) GeneratePdfDetailReport(CashFlowDetailResponseDto reportData)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    //注册编码提供器，避免 iText7 中文字体问题
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                    // 创建 PdfWriter 和 PdfDocument
                    var writer = new iText.Kernel.Pdf.PdfWriter(memoryStream);
                    var pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer);
                    var document = new iText.Layout.Document(pdfDoc, iText.Kernel.Geom.PageSize.A4.Rotate());
                    document.SetMargins(20, 20, 30, 30);

                    PdfFont font = PdfFontFactory.CreateFont("C:/Windows/Fonts/simsun.ttc,0", PdfEncodings.IDENTITY_H, true);
                    float titleSize = 16, headerSize = 12, normalSize = 10;
                    string SafeText(string text) => text ?? "";

                    //添加标题
                    document.Add(new Paragraph(SafeText(reportData.ReportTitle))
                        .SetFont(font)
                        .SetFontSize(titleSize)
                        .SetBold()
                        .SetTextAlignment(TextAlignment.CENTER));

                    document.Add(new Paragraph(" "));

                    //报告信息
                    document.Add(new Paragraph($"生成时间: {reportData.GenerateTime:yyyy-MM-dd HH:mm:ss}")
                        .SetFont(font)
                        .SetFontSize(normalSize));
                    document.Add(new Paragraph($"统计时间: {reportData.Criteria.StartDate:yyyy-MM-dd} 至 {reportData.Criteria.EndDate:yyyy-MM-dd}")
                        .SetFont(font)
                        .SetFontSize(normalSize));

                    if (!string.IsNullOrEmpty(reportData.Criteria.ModuleType))
                    {
                        document.Add(new Paragraph($"模块类型: {SafeText(reportData.Criteria.ModuleType)}")
                            .SetFont(font)
                            .SetFontSize(normalSize));
                    }

                    if (!string.IsNullOrEmpty(reportData.Criteria.RelatedPartyType) && reportData.Criteria.RelatedPartyId.HasValue)
                    {
                        document.Add(new Paragraph($"关联方: {SafeText(reportData.Criteria.RelatedPartyType)}-{reportData.Criteria.RelatedPartyId}")
                            .SetFont(font)
                            .SetFontSize(normalSize));
                    }

                    document.Add(new Paragraph(" "));

                    //总体统计表格
                    document.Add(new Paragraph("总体统计").SetFont(font).SetFontSize(headerSize).SetBold());
                    var summaryTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })).UseAllAvailableWidth();
                    summaryTable.AddHeaderCell(new Cell().Add(new Paragraph("项目").SetFont(font).SetFontSize(headerSize).SetBold()));
                    summaryTable.AddHeaderCell(new Cell().Add(new Paragraph("数值").SetFont(font).SetFontSize(headerSize).SetBold()));
                    summaryTable.AddCell(new Cell().Add(new Paragraph("总收入").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph($"¥{reportData.Summary.TotalIncome:F2}").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph("总支出").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph($"¥{reportData.Summary.TotalExpense:F2}").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph("净现金流").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph($"¥{reportData.Summary.NetFlow:F2}").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph("记录总数").SetFont(font).SetFontSize(normalSize)));
                    summaryTable.AddCell(new Cell().Add(new Paragraph(reportData.Summary.RecordCount.ToString()).SetFont(font).SetFontSize(normalSize)));
                    document.Add(summaryTable);
                    document.Add(new Paragraph(" "));

                    //时间序列表格
                    if (reportData.TimeSeriesDatas != null && reportData.TimeSeriesDatas.Any())
                    {
                        document.Add(new Paragraph("时间序列数据").SetFont(font).SetFontSize(headerSize).SetBold());
                        var timeSeriesTable = new Table(UnitValue.CreatePercentArray(new float[] { 2, 1, 1, 1 })).UseAllAvailableWidth();
                        string[] tsHeaders = { "时间段", "收入", "支出", "净现金流" };
                        foreach (var h in tsHeaders)
                            timeSeriesTable.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(font).SetFontSize(headerSize).SetBold()));

                        foreach (var data in reportData.TimeSeriesDatas)
                        {
                            timeSeriesTable.AddCell(new Cell().Add(new Paragraph(SafeText(data.Period)).SetFont(font).SetFontSize(normalSize)));
                            timeSeriesTable.AddCell(new Cell().Add(new Paragraph($"¥{data.Income:F2}").SetFont(font).SetFontSize(normalSize)));
                            timeSeriesTable.AddCell(new Cell().Add(new Paragraph($"¥{data.Expense:F2}").SetFont(font).SetFontSize(normalSize)));
                            timeSeriesTable.AddCell(new Cell().Add(new Paragraph($"¥{data.NetFlow:F2}").SetFont(font).SetFontSize(normalSize)));
                        }
                        document.Add(timeSeriesTable);
                        document.Add(new Paragraph(" "));
                    }

                    //关联方汇总表格
                    if (reportData.RelatedPartySummaries != null && reportData.RelatedPartySummaries.Any())
                    {
                        document.Add(new Paragraph("关联方汇总").SetFont(font).SetFontSize(headerSize).SetBold());
                        var relatedPartyTable = new Table(UnitValue.CreatePercentArray(new float[] { 2, 1, 1, 1 })).UseAllAvailableWidth();
                        string[] rpHeaders = { "关联方类型", "关联方ID", "总金额", "记录数" };
                        foreach (var h in rpHeaders)
                            relatedPartyTable.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(font).SetFontSize(headerSize).SetBold()));
                        foreach (var summary in reportData.RelatedPartySummaries)
                        {
                            relatedPartyTable.AddCell(new Cell().Add(new Paragraph(SafeText(summary.PartyType)).SetFont(font).SetFontSize(normalSize)));
                            relatedPartyTable.AddCell(new Cell().Add(new Paragraph(summary.PartyId.ToString()).SetFont(font).SetFontSize(normalSize)));
                            relatedPartyTable.AddCell(new Cell().Add(new Paragraph($"¥{summary.TotalAmount:F2}").SetFont(font).SetFontSize(normalSize)));
                            relatedPartyTable.AddCell(new Cell().Add(new Paragraph(summary.RecordCount.ToString()).SetFont(font).SetFontSize(normalSize)));
                        }
                        document.Add(relatedPartyTable);
                        document.Add(new Paragraph(" "));
                    }
                    // 明细数据表格
                    if (reportData.Details != null && reportData.Details.Count > 0)
                    {
                        if (reportData.Details.Count <= 100)
                        {
                            document.Add(new Paragraph("明细数据").SetFont(font).SetFontSize(headerSize).SetBold());

                            var detailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1, 2, 1, 1 })).UseAllAvailableWidth();
                            string[] detailHeaders = { "日期", "类型", "类别", "描述", "金额", "关联方" };
                            foreach (var h in detailHeaders)
                                detailsTable.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(font).SetFontSize(headerSize).SetBold()));

                            foreach (var record in reportData.Details)
                            {
                                detailsTable.AddCell(new Cell()
                                     .Add(new Paragraph(record.Date.ToString("yyyy-MM-dd"))
                                     .SetFont(font)
                                     .SetFontSize(normalSize)));

                                detailsTable.AddCell(new Cell().Add(new Paragraph(SafeText(record.Type)).SetFont(font).SetFontSize(normalSize)));
                                detailsTable.AddCell(new Cell().Add(new Paragraph(SafeText(record.Category)).SetFont(font).SetFontSize(normalSize)));
                                detailsTable.AddCell(new Cell().Add(new Paragraph(SafeText(record.Description)).SetFont(font).SetFontSize(normalSize)));
                                detailsTable.AddCell(new Cell().Add(new Paragraph($"¥{record.Amount:F2}").SetFont(font).SetFontSize(normalSize)));

                                var relatedParty = record.RelatedPartyId.HasValue ?
                                    $"{SafeText(record.RelatedPartyType)}-{record.RelatedPartyId}" :
                                    SafeText(record.RelatedPartyType);
                                detailsTable.AddCell(new Cell().Add(new Paragraph(relatedParty).SetFont(font).SetFontSize(normalSize)));
                            }

                            document.Add(detailsTable);
                        }
                        else
                        {
                            document.Add(new Paragraph($"明细数据过多({reportData.Details.Count}条记录)，请在Excel中查看详情")
                                .SetFont(font)
                                .SetFontSize(normalSize));
                        }
                    }

                    document.Close();
                    return (memoryStream.ToArray(), "application/pdf", ".pdf");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成PDF详情报表时发生错误");
                throw;
            }
        }

        #endregion

        #region 辅助方法

        // 获取指定模块的记录
        private async Task<List<CashFlowRecord>> GetRecordsByModuleAsync(string moduleType, DateTime startDate, DateTime endDate)
        {
            return moduleType switch
            {
                "商户租金" => await GetRentalIncomesAsync(startDate, endDate),
                "活动结算" => await GetEventSettlementsAsync(startDate, endDate),
                "停车场收费" => await GetParkingIncomesAsync(startDate, endDate),
                "设备维修" => await GetMaintenanceExpensesAsync(startDate, endDate),
                "促销活动" => await GetPromotionExpensesAsync(startDate, endDate),
                "员工工资" => await GetSalaryExpensesAsync(startDate, endDate),
                _ => new List<CashFlowRecord>()
            };
        }

        // 应用筛选条件
        private List<CashFlowRecord> ApplyFilters(List<CashFlowRecord> records, string moduleType, string relatedPartyType, int? relatedPartyId)
        {
            var query = records.AsQueryable();

            // 按模块类型筛选
            if (!string.IsNullOrEmpty(moduleType))
            {
                query = query.Where(r => r.Category == moduleType);
            }

            // 按关联方筛选
            if (!string.IsNullOrEmpty(relatedPartyType))
            {
                query = query.Where(r => r.RelatedPartyType == relatedPartyType);

                if (relatedPartyId.HasValue)
                {
                    query = query.Where(r => r.RelatedPartyId == relatedPartyId.Value);
                }
            }

            return query.ToList();
        }

        // 获取关联方汇总数据
        private async Task<List<RelatedPartySummary>> GetRelatedPartySummariesAsync(List<CashFlowRecord> records, string relatedPartyType, int? relatedPartyId)
        {
            var query = records.AsQueryable();

            if (!string.IsNullOrEmpty(relatedPartyType))
            {
                query = query.Where(r => r.RelatedPartyType == relatedPartyType);

                if (relatedPartyId.HasValue)
                {
                    query = query.Where(r => r.RelatedPartyId == relatedPartyId.Value);
                }
            }

            var grouped = query
                .Where(r => !string.IsNullOrEmpty(r.RelatedPartyType) && r.RelatedPartyId.HasValue)
                .GroupBy(r => new { r.RelatedPartyType, r.RelatedPartyId });

            var result = new List<RelatedPartySummary>();

            foreach (var group in grouped)
            {

                result.Add(new RelatedPartySummary
                {
                    PartyType = group.Key.RelatedPartyType,
                    PartyId = group.Key.RelatedPartyId.Value,
                    TotalAmount = group.Sum(r => r.Amount),
                    RecordCount = group.Count()
                });
            }

            return result;
        }


        // 记录查询审计日志
        private void LogQueryAudit(string endpoint, string accountId, DateTime startDate, DateTime endDate,
            string granularity, string moduleType = null, string relatedPartyType = null, int? relatedPartyId = null)
        {
            _logger.LogInformation("用户 {AccountID} 查询{Endpoint}: {StartDate} 至 {EndDate}, 颗粒度: {Granularity}, 模块: {Module}, 关联方: {RelatedParty}",
                accountId, endpoint, startDate, endDate, granularity, moduleType,
                !string.IsNullOrEmpty(relatedPartyType) ?
                    $"{relatedPartyType}{(relatedPartyId.HasValue ? $"-{relatedPartyId}" : "")}" : "无");
        }

        // 验证用户权限
        private async Task<bool> ValidateUserPermission(string accountId)
        {
            try
            {
                var account = await _accountContext.FindAccount(accountId);
                if (account == null)
                {
                    _logger.LogWarning($"账号 {accountId} 不存在");
                    return false;
                }
                if (account.AUTHORITY == 1)
                    return true;

                //部门管理员需要检查是否为财务部
                if (account.AUTHORITY == 2)
                {
                    var staff = await _collabContext.FindStaffByAccount(accountId);
                    if (staff == null)
                    {
                        _logger.LogWarning($"账号 {accountId} 对应的员工信息不存在");
                        return false;
                    }

                    if (staff.STAFF_APARTMENT == "财务部")
                        return true;

                    _logger.LogWarning($"账号 {accountId} 不是财务部经理，无财务报表权限");
                    return false;
                }

                _logger.LogWarning($"账号 {accountId} 权限等级不足，当前权限: {account.AUTHORITY}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"验证账号 {accountId} 权限时发生错误");
                return false;
            }
        }

        private string ValidateFilterCombination(string transactionType, string relatedPartyType)
        {
            //检查关联方类型与收支类型的匹配性
            if (!string.IsNullOrEmpty(relatedPartyType) && !string.IsNullOrEmpty(transactionType))
            {
                // 商户关联方只能与商户租金匹配
                if (relatedPartyType == "商户" && transactionType != "商户租金")
                {
                    return "商户关联方只能与商户租金类型一起使用";
                }

                // 合作方关联方只能与活动结算或促销活动匹配
                if (relatedPartyType == "合作方" &&
                   transactionType != "场地活动")
                {
                    return "合作方关联方只能与场地活动类型一起使用";
                }
            }

            return null;
        }

        private List<TimeSeriesData> AggregateByTimeGranularity(List<CashFlowRecord> records, string timeGranularity)
        {
            var timeSeriesData = new List<TimeSeriesData>();
            var groupedRecords = records.GroupBy(r => GetPeriodKey(r.Date, timeGranularity));

            // 计算每个时间段的收入、支出和净现金流
            foreach (var group in groupedRecords.OrderBy(g => g.Key))
            {
                var income = group.Where(r => r.Type == "收入").Sum(r => r.Amount);
                var expense = group.Where(r => r.Type == "支出").Sum(r => r.Amount);
                var recordCount = group.Count();
                timeSeriesData.Add(new TimeSeriesData
                {
                    Period = group.Key,
                    Income = income,
                    Expense = expense,
                    NetFlow = income - expense,
                    RecordCount = recordCount
                });
            }

            return timeSeriesData;
        }

        private string GetPeriodKey(DateTime date, string timeGranularity)
        {
            switch (timeGranularity.ToLower())
            {
                case "day":
                    return date.ToString("yyyy-MM-dd");
                case "week":
                    var calendar = CultureInfo.CurrentCulture.Calendar;
                    var weekNum = calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    return $"{date.Year}-W{weekNum:00}";
                case "month":
                    return date.ToString("yyyy-MM");
                case "quarter":
                    var quarter = (date.Month - 1) / 3 + 1;
                    return $"{date.Year}-Q{quarter}";
                case "year":
                    return date.Year.ToString();
                default:
                    return date.ToString("yyyy-MM");
            }
        }

        private List<string> GetPeriodsInRange(DateTime startDate, DateTime endDate)
        {
            var periods = new List<string>();
            var current = new DateTime(startDate.Year, startDate.Month, 1);

            while (current <= endDate)
            {
                periods.Add(current.ToString("yyyyMM"));
                current = current.AddMonths(1);
            }

            return periods;
        }
        #endregion

        #region 数据获取方法

        // 获取商户租金收入
        private async Task<List<CashFlowRecord>> GetRentalIncomesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var cashFlowRecords = new List<CashFlowRecord>();
                var periods = GetPeriodsInRange(startDate, endDate);

                foreach (var period in periods)
                {
                    var rentDetails = await _storeContext.GetRentCollectionDetails(period);

                    var periodRecords = rentDetails
                        .Where(d => d.Status == "已缴纳" && d.PaymentDate.HasValue &&
                                   d.PaymentDate.Value >= startDate &&
                                   d.PaymentDate.Value <= endDate)
                        .Select(d => new CashFlowRecord
                        {
                            Date = d.PaymentDate.Value,
                            Type = "收入",
                            Category = "商户租金",
                            Description = $"商户{d.StoreName}租金",
                            Amount = (double)d.ActualAmount,
                            Reference = $"Rent-{d.StoreId}-{d.Period}",
                            RelatedPartyType = "商户",
                            RelatedPartyId = d.StoreId,
                        })
                        .ToList();

                    cashFlowRecords.AddRange(periodRecords);
                }
                return cashFlowRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取商户租金数据时发生错误");
                return new List<CashFlowRecord>();
            }
        }

        // 获取场地活动收入，有合作方ID
        private async Task<List<CashFlowRecord>> GetEventSettlementsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var events = _context.VenueEventDetails
                    .Include(ved => ved.venueEventNavigation)
                    .Include(ved => ved.eventAreaNavigation)
                    .Where(ved => ved.STATUS == "已结算" &&
                                 ved.RENT_END >= startDate &&
                                 ved.RENT_START <= endDate);
                var records = await events.Select(r => new CashFlowRecord
                {
                    Date = r.RENT_END,
                    Type = "收入",
                    Category = "场地活动",
                    Description = $"活动{r.EVENT_ID}结算",
                    Amount = r.FUNDING,
                    Reference = $"VenueEventDetails-{r.EVENT_ID}-{r.RENT_START:yyyyMMddHHmmss}",
                    RelatedPartyType = "合作方",
                    RelatedPartyId = r.COLLABORATION_ID
                }).ToListAsync();
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取活动结算数据时发生错误");
                return new List<CashFlowRecord>();
            }
        }

        // 获取停车费收入
        private async Task<List<CashFlowRecord>> GetParkingIncomesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                //SQL查询：获取已完成的停车记录及其费用信息
                var sql = @"
                    SELECT 
                        c.LICENSE_PLATE_NUMBER,
                        c.PARK_START,
                        c.PARK_END,
                        p.PARKING_SPACE_ID,
                        pl.PARKING_FEE,
                        psd.AREA_ID
                    FROM CAR c
                    INNER JOIN PARK p ON c.LICENSE_PLATE_NUMBER = p.LICENSE_PLATE_NUMBER 
                                     AND c.PARK_START = p.PARK_START
                    INNER JOIN PARKING_SPACE_DISTRIBUTION psd ON p.PARKING_SPACE_ID = psd.PARKING_SPACE_ID
                    INNER JOIN PARKING_LOT pl ON psd.AREA_ID = pl.AREA_ID
                    WHERE c.PARK_END IS NOT NULL 
                      AND c.PARK_END >= :startDate 
                      AND c.PARK_END <= :endDate
                    ORDER BY c.PARK_END";

                // 使用参数化查询防止SQL注入
                var parameters = new[]
                {
            new OracleParameter(":startDate", startDate),
            new OracleParameter(":endDate", endDate)
        };

                // 执行查询
                var parkingRecords = await _context.Database.SqlQueryRaw<ParkingIncomeRecord>(sql, parameters).ToListAsync();

                // 转换为现金流记录
                var cashFlowRecords = new List<CashFlowRecord>();

                foreach (var record in parkingRecords)
                {
                    if (record.PARK_END.HasValue)
                    {
                        // 计算停车时长和费用
                        var duration = record.PARK_END.Value - record.PARK_START;
                        var hours = Math.Ceiling(duration.TotalHours);
                        var amount = hours * (double)record.PARKING_FEE;
                        // 创建现金流记录
                        cashFlowRecords.Add(new CashFlowRecord
                        {
                            Date = record.PARK_END.Value, // 使用停车结束时间作为现金流日期
                            Type = "收入",
                            Category = "停车场收费",
                            Description = $"车辆 {record.LICENSE_PLATE_NUMBER} 在区域{record.AREA_ID}停车费支付",
                            Amount = amount,
                            Reference = $"停车支付-{record.LICENSE_PLATE_NUMBER}-{record.PARK_END.Value:yyyyMMddHHmmss}",
                            RelatedPartyType = null
                        });
                    }
                }

                _logger.LogInformation("从停车场模块获取了 {Count} 条收入记录", cashFlowRecords.Count);
                return cashFlowRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取停车场收入数据时发生错误");

                // 备用方案：使用EF Core查询
                try
                {
                    var efRecords = await (from c in _context.Cars
                                           join p in _context.Parks on
                                               new { LicensePlate = c.LICENSE_PLATE_NUMBER, ParkStart = c.PARK_START }
                                               equals new { LicensePlate = p.LICENSE_PLATE_NUMBER, ParkStart = p.PARK_START }
                                           join psd in _context.ParkingSpaceDistributions on p.PARKING_SPACE_ID equals psd.PARKING_SPACE_ID
                                           join pl in _context.ParkingLots on psd.AREA_ID equals pl.AREA_ID
                                           where c.PARK_END.HasValue
                                             && c.PARK_END >= startDate
                                             && c.PARK_END <= endDate
                                           select new
                                           {
                                               c.LICENSE_PLATE_NUMBER,
                                               c.PARK_START,
                                               c.PARK_END,
                                               p.PARKING_SPACE_ID,
                                               pl.PARKING_FEE,
                                               psd.AREA_ID
                                           })
                                          .ToListAsync();

                    var cashFlowRecords = new List<CashFlowRecord>();

                    foreach (var record in efRecords)
                    {
                        if (record.PARK_END.HasValue)
                        {
                            // 计算停车时长和费用
                            var duration = record.PARK_END.Value - record.PARK_START;
                            var hours = Math.Ceiling(duration.TotalHours);
                            var amount = (double)(hours * record.PARKING_FEE);

                            cashFlowRecords.Add(new CashFlowRecord
                            {
                                Date = record.PARK_END.Value,
                                Type = "收入",
                                Category = "停车场收费",
                                Description = $"车辆 {record.LICENSE_PLATE_NUMBER} 在区域{record.AREA_ID}停车费支付",
                                Amount = amount,
                                Reference = $"停车支付-{record.LICENSE_PLATE_NUMBER}-{record.PARK_END.Value:yyyyMMddHHmmss}",
                                RelatedPartyType = null
                            });
                        }
                    }

                    _logger.LogInformation("使用EF Core查询获取了 {Count} 条停车场收入记录", cashFlowRecords.Count);
                    return cashFlowRecords;
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "使用EF Core查询停车场收入数据也失败了");
                    return new List<CashFlowRecord>();
                }
            }
        }

        // 定义ParkingIncomeRecord类用于接收SQL查询结果
        public class ParkingIncomeRecord
        {
            public string LICENSE_PLATE_NUMBER { get; set; }
            public DateTime PARK_START { get; set; }
            public DateTime? PARK_END { get; set; }
            public string PARKING_SPACE_ID { get; set; }
            public decimal PARKING_FEE { get; set; }
            public int AREA_ID { get; set; }
        }

        // 获取设备维修支出
        private async Task<List<CashFlowRecord>> GetMaintenanceExpensesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var query = _context.RepairOrders
                    .Where(r => r.REPAIR_END > r.REPAIR_START
                         && r.REPAIR_START >= startDate
                         && r.REPAIR_END <= endDate);
                var records = await query.Select(r => new CashFlowRecord
                {
                    Date = r.REPAIR_END,
                    Type = "支出",
                    Category = "设备维修",
                    Description = $"设备 {r.EQUIPMENT_ID} 维修完成",
                    Amount = Math.Abs(r.REPAIR_COST),
                    Reference = $"RepairOrder-{r.EQUIPMENT_ID}-{r.REPAIR_START:yyyyMMddHHmmss}",
                    RelatedPartyType = null

                }).ToListAsync();
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备维修数据时发生错误");
                return new List<CashFlowRecord>();
            }
        }

        // 获取促销活动支出
        private async Task<List<CashFlowRecord>> GetPromotionExpensesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var records = new List<CashFlowRecord>();
                var saleEvents = await _saleEventService.GetAllSaleEventsAsync();
                var filtered = saleEvents
                   .Where(e => e.EVENT_START >= startDate &&
                              (e.EVENT_END ?? e.EVENT_START) <= endDate)
                   .ToList();

                foreach (var e in filtered)
                {
                    records.Add(new CashFlowRecord
                    {
                        Date = e.EVENT_END ?? e.EVENT_START, //默认花费记在结束时间，如果没有结束时间就用开始时间
                        Amount = e.Cost,
                        Type = "支出",
                        Category = "促销活动",
                        Description = e.EVENT_NAME ?? "未命名活动",
                        Reference = "",
                        RelatedPartyType = null
                    });
                }
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取促销活动数据时发生错误");
                return new List<CashFlowRecord>();
            }
        }

        // 获取员工工资支出,月度员工支出表，默认每月10号发放工资
        private async Task<List<CashFlowRecord>> GetSalaryExpensesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var records = new List<CashFlowRecord>();

                //遍历起止日期之间的月份
                DateTime current = new DateTime(startDate.Year, startDate.Month, 10);
                if (current < startDate)
                    current = current.AddMonths(1);

                while (current <= endDate)
                {
                    //查找该月的工资成本
                    var salaryCost = await _context.MonthSalaryCosts
                        .FirstOrDefaultAsync(msc => msc.MONTH_TIME.Year == current.Year
                                                 && msc.MONTH_TIME.Month == current.Month);

                    if (salaryCost != null)
                    {
                        records.Add(new CashFlowRecord
                        {
                            Date = current,                //默认工资发放日 = 10号
                            Amount = salaryCost.TOTAL_COST,
                            Type = "支出",
                            Category = "员工工资",
                            Description = $"{current:yyyy-MM} 月工资发放",
                            Reference = "",
                            RelatedPartyType = null
                        });
                    }
                    current = current.AddMonths(1);
                }
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取员工工资数据时发生错误");
                return new List<CashFlowRecord>();
            }
        }

        #endregion
    }
}