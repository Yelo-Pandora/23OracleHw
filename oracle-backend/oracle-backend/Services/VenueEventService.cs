using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using oracle_backend.Dbcontexts;
using System.Text.Json;
using oracle_backend.Models.venueEvent;

namespace oracle_backend.Services
{
    public class VenueEventService
    {
        private readonly VenueEventDbContext _context;
        private readonly ILogger<VenueEventService> _logger;

        public VenueEventService(VenueEventDbContext context, ILogger<VenueEventService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region 预约申请功能

        // 提交场地预约申请
        public async Task<VenueEventResponseDto> CreateReservationAsync(VenueReservationDto reservationDto, VenueEventDto eventDto)
        {
            // 验证时间有效性
            ValidateTimeRange(reservationDto.RentStart, reservationDto.RentEnd, "租用");
            ValidateTimeRange(eventDto.EventStart, eventDto.EventEnd.Value, "活动");

            // 验证关联数据存在性
            await ValidateReferencesAsync(reservationDto.AreaId, reservationDto.CollaborationId);

            // 检查场地冲突
            await CheckAreaConflictAsync(reservationDto.AreaId, reservationDto.RentStart, reservationDto.RentEnd);

            // 创建活动记录
            var venueEvent = new VenueEvent
            {
                EVENT_NAME = eventDto.EventName,
                EVENT_START = eventDto.EventStart,
                EVENT_END = eventDto.EventEnd,
                HEADCOUNT = eventDto.HeadCount,
                FEE = eventDto.Fee,
                CAPACITY = eventDto.Capacity,
                EXPENSE = eventDto.Expense,
                ACTIVITY_STATUS = eventDto.ActivityStatus ?? "PREPARING",
                DESCRIPTION = eventDto.Description
            };

            _context.VenueEvents.Add(venueEvent);
            await _context.SaveChangesAsync();

            // 创建预约详情
            var venueEventDetail = new VenueEventDetail
            {
                EVENT_ID = venueEvent.EVENT_ID,
                AREA_ID = reservationDto.AreaId,
                COLLABORATION_ID = reservationDto.CollaborationId,
                RENT_START = reservationDto.RentStart,
                RENT_END = reservationDto.RentEnd,
                STATUS = "PENDING",
                FUNDING = reservationDto.Funding
            };

            _context.VenueEventDetails.Add(venueEventDetail);
            await _context.SaveChangesAsync();

            // 记录操作日志
            await LogOperationAsync(venueEvent.EVENT_ID, "CREATE", null, eventDto, "创建场地预约申请", null);

            _logger.LogInformation($"创建场地预约申请: EventId={venueEvent.EVENT_ID}, AreaId={reservationDto.AreaId}");

            var collaboration = await _context.Collaborations.FindAsync(reservationDto.CollaborationId);
            return BuildResponseDto(venueEvent, venueEventDetail, collaboration.COLLABORATION_NAME);
        }

        // 审批场地预约申请
        public async Task<VenueEventResponseDto> ApproveReservationAsync(VenueReservationApprovalDto approvalDto)
        {
            var venueEventDetail = await GetVenueEventDetailAsync(approvalDto.EventId, approvalDto.AreaId, approvalDto.CollaborationId);

            if (venueEventDetail.STATUS != "PENDING")
                throw new InvalidOperationException("只能审批待处理状态的预约申请");

            // 审批通过时检查冲突
            if (approvalDto.Status == "APPROVED")
            {
                await CheckAreaConflictAsync(approvalDto.AreaId, venueEventDetail.RENT_START, venueEventDetail.RENT_END, approvalDto.EventId);
            }

            var oldStatus = venueEventDetail.STATUS;
            venueEventDetail.STATUS = approvalDto.Status;
            await _context.SaveChangesAsync();

            // 记录操作日志
            await LogOperationAsync(approvalDto.EventId, "APPROVE", 
                new { Status = oldStatus }, 
                new { Status = approvalDto.Status, RejectReason = approvalDto.RejectReason },
                $"预约申请{(approvalDto.Status == "APPROVED" ? "审批通过" : "被拒绝")}", null);

            _logger.LogInformation($"场地预约审批完成: EventId={approvalDto.EventId}, Status={approvalDto.Status}");

            return BuildResponseDto(venueEventDetail.venueEventNavigation, venueEventDetail, 
                venueEventDetail.collaborationNavigation.COLLABORATION_NAME);
        }

        #endregion

        #region 活动管理功能

        // 更新场地活动信息
        public async Task<VenueEventResponseDto> UpdateVenueEventAsync(int eventId, VenueEventUpdateDto updateDto)
        {
            var venueEvent = await _context.VenueEvents.FindAsync(eventId);
            if (venueEvent == null)
                throw new KeyNotFoundException("活动不存在");

            var venueEventDetail = await _context.VenueEventDetails
                .Include(v => v.collaborationNavigation)
                .FirstOrDefaultAsync(v => v.EVENT_ID == eventId);

            if (venueEventDetail?.STATUS != "APPROVED")
                throw new InvalidOperationException("只能修改已审批通过的活动");

            // 检查活动状态是否允许修改
            if (venueEvent.ACTIVITY_STATUS == "COMPLETED")
                throw new InvalidOperationException("活动已结束，不可修改");

            // 记录变更前的值
            var oldValues = new
            {
                EventName = venueEvent.EVENT_NAME,
                HeadCount = venueEvent.HEADCOUNT,
                ActivityStatus = venueEvent.ACTIVITY_STATUS,
                Description = venueEvent.DESCRIPTION
            };

            // 更新字段
            var hasChanges = false;
            if (!string.IsNullOrEmpty(updateDto.EventName) && updateDto.EventName != venueEvent.EVENT_NAME)
            {
                venueEvent.EVENT_NAME = updateDto.EventName;
                hasChanges = true;
            }

            if (updateDto.HeadCount.HasValue && updateDto.HeadCount != venueEvent.HEADCOUNT)
            {
                if (updateDto.HeadCount > venueEvent.CAPACITY)
                    throw new ArgumentException("参与人数不能超过活动容量");
                venueEvent.HEADCOUNT = updateDto.HeadCount;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(updateDto.ActivityStatus) && updateDto.ActivityStatus != venueEvent.ACTIVITY_STATUS)
            {
                ValidateStatusTransition(venueEvent.ACTIVITY_STATUS, updateDto.ActivityStatus);
                venueEvent.ACTIVITY_STATUS = updateDto.ActivityStatus;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(updateDto.Description) && updateDto.Description != venueEvent.DESCRIPTION)
            {
                venueEvent.DESCRIPTION = updateDto.Description;
                hasChanges = true;
            }

            if (!hasChanges)
                throw new ArgumentException("没有检测到任何变更");

            await _context.SaveChangesAsync();

            // 记录变更后的值
            var newValues = new
            {
                EventName = venueEvent.EVENT_NAME,
                HeadCount = venueEvent.HEADCOUNT,
                ActivityStatus = venueEvent.ACTIVITY_STATUS,
                Description = venueEvent.DESCRIPTION
            };

            // 记录操作日志
            await LogOperationAsync(eventId, "UPDATE", oldValues, newValues, "更新活动信息", updateDto.OperatorId);

            _logger.LogInformation($"活动信息更新完成: EventId={eventId}");

            return BuildResponseDto(venueEvent, venueEventDetail, venueEventDetail.collaborationNavigation.COLLABORATION_NAME);
        }

        // 取消场地活动
        public async Task<bool> CancelVenueEventAsync(int eventId, string operatorId, string reason)
        {
            var venueEvent = await _context.VenueEvents.FindAsync(eventId);
            if (venueEvent == null)
                throw new KeyNotFoundException("活动不存在");

            if (venueEvent.ACTIVITY_STATUS == "COMPLETED")
                throw new InvalidOperationException("活动已结束，不能取消");

            if (venueEvent.ACTIVITY_STATUS == "CANCELLED")
                throw new InvalidOperationException("活动已取消");

            var oldStatus = venueEvent.ACTIVITY_STATUS;
            venueEvent.ACTIVITY_STATUS = "CANCELLED";

            // 释放场地资源 - 更新预约详情状态
            var venueEventDetails = await _context.VenueEventDetails
                .Where(v => v.EVENT_ID == eventId)
                .ToListAsync();

            foreach (var detail in venueEventDetails)
            {
                detail.STATUS = "CANCELLED";
            }

            await _context.SaveChangesAsync();

            // 记录操作日志
            await LogOperationAsync(eventId, "CANCEL", 
                new { ActivityStatus = oldStatus }, 
                new { ActivityStatus = "CANCELLED" },
                $"取消活动: {reason}", operatorId);

            _logger.LogInformation($"活动已取消: EventId={eventId}, Reason={reason}");

            return true;
        }

        #endregion

        #region 查询功能

        // 根据状态获取预约记录
        public async Task<List<VenueEventResponseDto>> GetReservationsByStatusAsync(string status = null)
        {
            var query = _context.VenueEventDetails
                .Include(v => v.venueEventNavigation)
                .Include(v => v.collaborationNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(v => v.STATUS == status);
            }

            return await query.Select(v => new VenueEventResponseDto
            {
                EventId = v.venueEventNavigation.EVENT_ID,
                EventName = v.venueEventNavigation.EVENT_NAME,
                HeadCount = v.venueEventNavigation.HEADCOUNT,
                Fee = v.venueEventNavigation.FEE,
                Capacity = v.venueEventNavigation.CAPACITY,
                Expense = v.venueEventNavigation.EXPENSE,
                EventStart = v.venueEventNavigation.EVENT_START,
                EventEnd = v.venueEventNavigation.EVENT_END,
                AreaId = v.AREA_ID,
                CollaborationId = v.COLLABORATION_ID,
                RentStart = v.RENT_START,
                RentEnd = v.RENT_END,
                Status = v.STATUS,
                Funding = v.FUNDING,
                CollaborationName = v.collaborationNavigation.COLLABORATION_NAME,
                AreaInfo = $"区域ID: {v.AREA_ID}"
            }).ToListAsync();
        }

        // 根据活动ID获取预约信息
        public async Task<VenueEventResponseDto> GetReservationByEventIdAsync(int eventId)
        {
            var reservation = await _context.VenueEventDetails
                .Include(v => v.venueEventNavigation)
                .Include(v => v.collaborationNavigation)
                .Where(v => v.EVENT_ID == eventId)
                .FirstOrDefaultAsync();

            return reservation == null ? null : BuildResponseDto(
                reservation.venueEventNavigation, 
                reservation, 
                reservation.collaborationNavigation.COLLABORATION_NAME);
        }

        // 检查场地可用性
        public async Task<bool> CheckAreaAvailabilityAsync(int areaId, DateTime startTime, DateTime endTime)
        {
            return !(await _context.VenueEventDetails.AnyAsync(v => 
                v.AREA_ID == areaId &&
                v.STATUS == "APPROVED" &&
                IsTimeOverlap(v.RENT_START, v.RENT_END, startTime, endTime)));
        }

        // 获取活动操作日志
        public async Task<List<VenueEventLog>> GetEventLogsAsync(int eventId)
        {
            return await _context.VenueEventLogs
                .Where(l => l.EVENT_ID == eventId)
                .OrderByDescending(l => l.OPERATION_TIME)
                .ToListAsync();
        }

        #endregion

        #region 结算收费功能

        // 生成结算单
        public async Task<BillingResponseDto> CreateBillingAsync(CreateBillingDto billingDto)
        {
            var venueEventDetail = await _context.VenueEventDetails
                .Include(v => v.venueEventNavigation)
                .Include(v => v.collaborationNavigation)
                .FirstOrDefaultAsync(v => v.EVENT_ID == billingDto.EventId);

            if (venueEventDetail == null)
                throw new KeyNotFoundException("活动不存在");

            if (venueEventDetail.venueEventNavigation.ACTIVITY_STATUS != "COMPLETED")
                throw new InvalidOperationException("只能为已结束的活动生成结算单");

            // 检查是否已存在结算单
            var existingBilling = await _context.VenueEventBillings
                .FirstOrDefaultAsync(b => b.EVENT_ID == billingDto.EventId);
            if (existingBilling != null)
                throw new InvalidOperationException("该活动已存在结算单");

            // 验证时间有效性
            ValidateTimeRange(billingDto.ActualStartTime, billingDto.ActualEndTime, "实际租用");

            // 计算租用时长（小时）
            var rentalHours = CalculateRentalHours(billingDto.ActualStartTime, billingDto.ActualEndTime);

            // 获取场地费用配置
            var feeConfig = await GetActiveFeeConfigAsync(venueEventDetail.AREA_ID);

            // 计算基础费用
            var baseFee = CalculateBaseFee(rentalHours, feeConfig, billingDto.ActualStartTime);

            // 计算附加服务费用
            var additionalFee = await CalculateAdditionalFeesAsync(billingDto.AdditionalServices);

            // 创建结算单
            var billing = new VenueEventBilling
            {
                EVENT_ID = billingDto.EventId,
                AREA_ID = venueEventDetail.AREA_ID,
                COLLABORATION_ID = venueEventDetail.COLLABORATION_ID,
                ACTUAL_START_TIME = billingDto.ActualStartTime,
                ACTUAL_END_TIME = billingDto.ActualEndTime,
                RENTAL_HOURS = rentalHours,
                BASE_FEE = baseFee,
                ADDITIONAL_FEE = additionalFee.TotalFee,
                TOTAL_FEE = baseFee + additionalFee.TotalFee,
                BILLING_STATUS = "PENDING",
                REMARKS = billingDto.Remarks,
                CREATED_TIME = DateTime.Now
            };

            _context.VenueEventBillings.Add(billing);
            await _context.SaveChangesAsync();

            // 保存附加服务明细
            if (additionalFee.ServiceDetails.Any())
            {
                foreach (var detail in additionalFee.ServiceDetails)
                {
                    detail.BILLING_ID = billing.BILLING_ID;
                    _context.BillingServiceDetails.Add(detail);
                }
                await _context.SaveChangesAsync();
            }

            // 记录操作日志
            await LogOperationAsync(billingDto.EventId, "CREATE_BILLING", null, 
                new { BillingId = billing.BILLING_ID, TotalFee = billing.TOTAL_FEE },
                "生成结算单", billingDto.OperatorId);

            _logger.LogInformation($"生成结算单: BillingId={billing.BILLING_ID}, EventId={billingDto.EventId}");

            return await BuildBillingResponseAsync(billing.BILLING_ID);
        }

        // 确认结算单
        public async Task<BillingResponseDto> ConfirmBillingAsync(ConfirmBillingDto confirmDto)
        {
            var billing = await _context.VenueEventBillings
                .Include(b => b.VenueEventNavigation)
                .Include(b => b.CollaborationNavigation)
                .FirstOrDefaultAsync(b => b.BILLING_ID == confirmDto.BillingId);

            if (billing == null)
                throw new KeyNotFoundException("结算单不存在");

            if (billing.BILLING_STATUS != "PENDING")
                throw new InvalidOperationException("只能确认待处理状态的结算单");

            var oldValues = new { BillingStatus = billing.BILLING_STATUS };

            // 更新结算单信息
            billing.BILLING_STATUS = "CONFIRMED";
            billing.PAYMENT_METHOD = confirmDto.PaymentMethod;
            billing.CONFIRMED_TIME = DateTime.Now;
            billing.CONFIRMED_BY = confirmDto.ConfirmedBy;
            
            if (!string.IsNullOrEmpty(confirmDto.Remarks))
                billing.REMARKS = confirmDto.Remarks;

            if (confirmDto.InvoiceInfo != null)
                billing.INVOICE_INFO = JsonSerializer.Serialize(confirmDto.InvoiceInfo);

            await _context.SaveChangesAsync();

            // 记录操作日志
            await LogOperationAsync(billing.EVENT_ID, "CONFIRM_BILLING", oldValues,
                new { BillingStatus = "CONFIRMED", PaymentMethod = confirmDto.PaymentMethod },
                "确认结算单", confirmDto.ConfirmedBy);

            _logger.LogInformation($"结算单已确认: BillingId={confirmDto.BillingId}");

            return await BuildBillingResponseAsync(billing.BILLING_ID);
        }

        // 确认支付
        public async Task<BillingResponseDto> ConfirmPaymentAsync(PaymentConfirmationDto paymentDto)
        {
            var billing = await _context.VenueEventBillings.FindAsync(paymentDto.BillingId);
            if (billing == null)
                throw new KeyNotFoundException("结算单不存在");

            if (billing.BILLING_STATUS != "CONFIRMED")
                throw new InvalidOperationException("只能为已确认的结算单确认支付");

            var oldValues = new { BillingStatus = billing.BILLING_STATUS, PaymentTime = billing.PAYMENT_TIME };

            billing.BILLING_STATUS = "PAID";
            billing.PAYMENT_TIME = paymentDto.PaymentTime;
            
            if (!string.IsNullOrEmpty(paymentDto.PaymentRemarks))
                billing.REMARKS = billing.REMARKS + "; " + paymentDto.PaymentRemarks;

            await _context.SaveChangesAsync();

            // 记录操作日志
            await LogOperationAsync(billing.EVENT_ID, "CONFIRM_PAYMENT", oldValues,
                new { BillingStatus = "PAID", PaymentTime = paymentDto.PaymentTime },
                "确认支付", paymentDto.ConfirmedBy);

            _logger.LogInformation($"支付已确认: BillingId={paymentDto.BillingId}");

            return await BuildBillingResponseAsync(billing.BILLING_ID);
        }

        // 获取结算单列表
        public async Task<List<BillingResponseDto>> GetBillingsAsync(string status = null, int? eventId = null)
        {
            var query = _context.VenueEventBillings
                .Include(b => b.VenueEventNavigation)
                .Include(b => b.CollaborationNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.BILLING_STATUS == status);

            if (eventId.HasValue)
                query = query.Where(b => b.EVENT_ID == eventId.Value);

            var billings = await query.ToListAsync();
            var results = new List<BillingResponseDto>();

            foreach (var billing in billings)
            {
                var response = await BuildBillingResponseAsync(billing.BILLING_ID);
                results.Add(response);
            }

            return results;
        }

        // 获取单个结算单详情
        public async Task<BillingResponseDto> GetBillingAsync(int billingId)
        {
            return await BuildBillingResponseAsync(billingId);
        }

        // 获取活动可用的附加服务
        public async Task<List<AdditionalService>> GetAvailableServicesAsync()
        {
            return await _context.AdditionalServices
                .Where(s => s.IS_ACTIVE)
                .ToListAsync();
        }

        #endregion

        #region 统计报表功能

        // 生成统计报表
        public async Task<VenueReportResponseDto> GenerateReportAsync(GenerateReportDto reportDto)
        {
            // 验证时间范围
            ValidateTimeRange(reportDto.StartDate, reportDto.EndDate, "统计");

            // 检查是否有数据
            var recordCount = await _context.VenueEventDetails
                .Where(v => v.RENT_START >= reportDto.StartDate && v.RENT_START <= reportDto.EndDate && v.STATUS == "APPROVED")
                .CountAsync();

            if (recordCount == 0)
                throw new ArgumentException("该时间段内无场地活动记录");

            // 生成报表标题
            var reportTitle = reportDto.ReportTitle ?? $"场地活动统计报表({reportDto.StartDate:yyyy-MM-dd}至{reportDto.EndDate:yyyy-MM-dd})";

            // 计算基本统计数据
            var statsData = await GetBasicStatsAsync(reportDto.StartDate, reportDto.EndDate);

            // 创建报表记录
            var report = new VenueEventReport
            {
                REPORT_TYPE = reportDto.ReportType,
                START_DATE = reportDto.StartDate,
                END_DATE = reportDto.EndDate,
                REPORT_TITLE = reportTitle,
                AREA_ID = reportDto.AreaId,
                COLLABORATION_ID = reportDto.CollaborationId,
                TOTAL_EVENTS = statsData.TotalEvents,
                TOTAL_RENTAL_HOURS = statsData.TotalRentalHours,
                TOTAL_REVENUE = statsData.TotalRevenue,
                AVERAGE_ATTENDANCE = statsData.AverageAttendance,
                AVERAGE_OCCUPANCY_RATE = statsData.AverageOccupancyRate,
                GENERATED_BY = reportDto.GeneratedBy,
                STATUS = "COMPLETED"
            };

            _context.VenueEventReports.Add(report);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"统计报表生成完成: ReportId={report.REPORT_ID}");

            return new VenueReportResponseDto
            {
                ReportId = report.REPORT_ID,
                ReportType = report.REPORT_TYPE,
                StartDate = report.START_DATE,
                EndDate = report.END_DATE,
                ReportTitle = report.REPORT_TITLE,
                TotalEvents = report.TOTAL_EVENTS,
                TotalRentalHours = report.TOTAL_RENTAL_HOURS,
                TotalRevenue = report.TOTAL_REVENUE,
                AverageAttendance = report.AVERAGE_ATTENDANCE,
                AverageOccupancyRate = report.AVERAGE_OCCUPANCY_RATE,
                GeneratedTime = report.GENERATED_TIME,
                GeneratedBy = report.GENERATED_BY,
                Status = report.STATUS
            };
        }

        // 获取报表列表
        public async Task<List<VenueEventReport>> GetReportsAsync()
        {
            return await _context.VenueEventReports
                .OrderByDescending(r => r.GENERATED_TIME)
                .ToListAsync();
        }

        #endregion

        #region 私有辅助方法

        // 验证时间范围
        private void ValidateTimeRange(DateTime startTime, DateTime endTime, string timeType)
        {
            if (startTime >= endTime)
                throw new ArgumentException($"{timeType}结束时间必须晚于起始时间");
        }

        // 验证关联数据存在性
        private async Task ValidateReferencesAsync(int areaId, int collaborationId)
        {
            var eventArea = await _context.EventAreas.FindAsync(areaId);
            if (eventArea == null)
                throw new ArgumentException("指定的活动区域不存在");

            var collaboration = await _context.Collaborations.FindAsync(collaborationId);
            if (collaboration == null)
                throw new ArgumentException("指定的合作方不存在");
        }

        // 检查场地冲突
        private async Task CheckAreaConflictAsync(int areaId, DateTime startTime, DateTime endTime, int? excludeEventId = null)
        {
            var query = _context.VenueEventDetails.Where(v => 
                v.AREA_ID == areaId &&
                v.STATUS == "APPROVED" &&
                IsTimeOverlap(v.RENT_START, v.RENT_END, startTime, endTime));

            if (excludeEventId.HasValue)
            {
                query = query.Where(v => v.EVENT_ID != excludeEventId.Value);
            }

            if (await query.AnyAsync())
                throw new InvalidOperationException("该区域在指定时间内已被占用");
        }

        // 时间重叠检测
        private bool IsTimeOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            return (start1 <= start2 && end1 > start2) ||
                   (start1 < end2 && end1 >= end2) ||
                   (start1 >= start2 && end1 <= end2);
        }

        // 获取场地活动详情
        private async Task<VenueEventDetail> GetVenueEventDetailAsync(int eventId, int areaId, int collaborationId)
        {
            var detail = await _context.VenueEventDetails
                .Include(v => v.venueEventNavigation)
                .Include(v => v.collaborationNavigation)
                .FirstOrDefaultAsync(v => 
                    v.EVENT_ID == eventId && 
                    v.AREA_ID == areaId && 
                    v.COLLABORATION_ID == collaborationId);

            if (detail == null)
                throw new KeyNotFoundException("预约记录不存在");

            return detail;
        }

        // 状态转换验证
        private void ValidateStatusTransition(string currentStatus, string newStatus)
        {
            var validTransitions = new Dictionary<string, string[]>
            {
                ["PREPARING"] = new[] { "ONGOING", "CANCELLED" },
                ["ONGOING"] = new[] { "COMPLETED", "CANCELLED" },
                ["COMPLETED"] = new string[0], // 已完成状态不能转换
                ["CANCELLED"] = new string[0]  // 已取消状态不能转换
            };

            if (!validTransitions.ContainsKey(currentStatus) || 
                !validTransitions[currentStatus].Contains(newStatus))
            {
                throw new ArgumentException($"不能从状态 {currentStatus} 转换到 {newStatus}");
            }
        }

        // 记录操作日志
        private async Task LogOperationAsync(int eventId, string operationType, object oldValues, object newValues, string description, string operatorId)
        {
            var log = new VenueEventLog
            {
                EVENT_ID = eventId,
                OPERATION_TYPE = operationType,
                OLD_VALUES = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NEW_VALUES = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                DESCRIPTION = description,
                OPERATOR_ID = operatorId,
                OPERATION_TIME = DateTime.Now
            };

            _context.VenueEventLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        // 构建响应DTO
        private VenueEventResponseDto BuildResponseDto(VenueEvent venueEvent, VenueEventDetail venueEventDetail, string collaborationName)
        {
            return new VenueEventResponseDto
            {
                EventId = venueEvent.EVENT_ID,
                EventName = venueEvent.EVENT_NAME,
                HeadCount = venueEvent.HEADCOUNT,
                Fee = venueEvent.FEE,
                Capacity = venueEvent.CAPACITY,
                Expense = venueEvent.EXPENSE,
                EventStart = venueEvent.EVENT_START,
                EventEnd = venueEvent.EVENT_END,
                AreaId = venueEventDetail.AREA_ID,
                CollaborationId = venueEventDetail.COLLABORATION_ID,
                RentStart = venueEventDetail.RENT_START,
                RentEnd = venueEventDetail.RENT_END,
                Status = venueEventDetail.STATUS,
                Funding = venueEventDetail.FUNDING,
                CollaborationName = collaborationName,
                AreaInfo = $"区域ID: {venueEventDetail.AREA_ID}"
            };
        }

        // 计算租用时长（小时）
        private decimal CalculateRentalHours(DateTime startTime, DateTime endTime)
        {
            var duration = endTime - startTime;
            return (decimal)Math.Round(duration.TotalHours, 2);
        }

        // 获取有效的费用配置
        private async Task<VenueFeeConfig> GetActiveFeeConfigAsync(int areaId)
        {
            var feeConfig = await _context.VenueFeeConfigs
                .Where(f => f.AREA_ID == areaId && 
                           f.IS_ACTIVE && 
                           f.EFFECTIVE_DATE <= DateTime.Now &&
                           (f.EXPIRY_DATE == null || f.EXPIRY_DATE > DateTime.Now))
                .OrderByDescending(f => f.EFFECTIVE_DATE)
                .FirstOrDefaultAsync();

            if (feeConfig == null)
            {
                // 如果没有配置，使用默认费率
                return new VenueFeeConfig
                {
                    AREA_ID = areaId,
                    HOURLY_RATE = 100, // 默认每小时100元
                    MIN_HOURS = 1,
                    OVERTIME_RATE = 0,
                    HOLIDAY_MULTIPLIER = 1,
                    WEEKEND_MULTIPLIER = 1
                };
            }

            return feeConfig;
        }

        // 计算基础费用
        private decimal CalculateBaseFee(decimal rentalHours, VenueFeeConfig feeConfig, DateTime startTime)
        {
            // 确保满足最低收费时长
            var billableHours = Math.Max(rentalHours, feeConfig.MIN_HOURS);

            // 基础费率
            var baseRate = feeConfig.HOURLY_RATE;

            // 判断是否为周末
            if (startTime.DayOfWeek == DayOfWeek.Saturday || startTime.DayOfWeek == DayOfWeek.Sunday)
            {
                baseRate *= feeConfig.WEEKEND_MULTIPLIER;
            }

            // 这里可以添加节假日判断逻辑
            // if (IsHoliday(startTime))
            // {
            //     baseRate *= feeConfig.HOLIDAY_MULTIPLIER;
            // }

            return billableHours * baseRate;
        }

        // 计算附加服务费用
        private async Task<(decimal TotalFee, List<BillingServiceDetail> ServiceDetails)> CalculateAdditionalFeesAsync(List<AdditionalServiceItemDto> serviceItems)
        {
            var totalFee = 0m;
            var serviceDetails = new List<BillingServiceDetail>();

            if (serviceItems?.Any() == true)
            {
                foreach (var item in serviceItems)
                {
                    var service = await _context.AdditionalServices.FindAsync(item.ServiceId);
                    if (service != null && service.IS_ACTIVE)
                    {
                        var subtotal = service.SERVICE_FEE * item.Quantity;
                        totalFee += subtotal;

                        serviceDetails.Add(new BillingServiceDetail
                        {
                            SERVICE_ID = item.ServiceId,
                            QUANTITY = item.Quantity,
                            UNIT_PRICE = service.SERVICE_FEE,
                            SUBTOTAL = subtotal
                        });
                    }
                }
            }

            return (totalFee, serviceDetails);
        }

        // 构建结算单响应DTO
        private async Task<BillingResponseDto> BuildBillingResponseAsync(int billingId)
        {
            var billing = await _context.VenueEventBillings
                .Include(b => b.VenueEventNavigation)
                .Include(b => b.CollaborationNavigation)
                .FirstOrDefaultAsync(b => b.BILLING_ID == billingId);

            if (billing == null)
                throw new KeyNotFoundException("结算单不存在");

            // 获取服务明细
            var serviceDetails = await _context.BillingServiceDetails
                .Include(d => d.ServiceNavigation)
                .Where(d => d.BILLING_ID == billingId)
                .Select(d => new BillingServiceDetailDto
                {
                    ServiceId = d.SERVICE_ID,
                    ServiceName = d.ServiceNavigation.SERVICE_NAME,
                    Quantity = d.QUANTITY,
                    UnitPrice = d.UNIT_PRICE,
                    Subtotal = d.SUBTOTAL,
                    BillingUnit = d.ServiceNavigation.BILLING_UNIT
                })
                .ToListAsync();

            // 解析开票信息
            InvoiceInfoDto invoiceInfo = null;
            if (!string.IsNullOrEmpty(billing.INVOICE_INFO))
            {
                try
                {
                    invoiceInfo = JsonSerializer.Deserialize<InvoiceInfoDto>(billing.INVOICE_INFO);
                }
                catch (JsonException)
                {
                    // 解析失败时忽略
                }
            }

            return new BillingResponseDto
            {
                BillingId = billing.BILLING_ID,
                EventId = billing.EVENT_ID,
                EventName = billing.VenueEventNavigation.EVENT_NAME,
                AreaId = billing.AREA_ID,
                CollaborationId = billing.COLLABORATION_ID,
                CollaborationName = billing.CollaborationNavigation.COLLABORATION_NAME,
                ActualStartTime = billing.ACTUAL_START_TIME,
                ActualEndTime = billing.ACTUAL_END_TIME,
                RentalHours = billing.RENTAL_HOURS,
                BaseFee = billing.BASE_FEE,
                AdditionalFee = billing.ADDITIONAL_FEE,
                TotalFee = billing.TOTAL_FEE,
                BillingStatus = billing.BILLING_STATUS,
                PaymentMethod = billing.PAYMENT_METHOD,
                PaymentTime = billing.PAYMENT_TIME,
                InvoiceInfo = invoiceInfo,
                Remarks = billing.REMARKS,
                CreatedTime = billing.CREATED_TIME,
                ConfirmedTime = billing.CONFIRMED_TIME,
                ConfirmedBy = billing.CONFIRMED_BY,
                ServiceDetails = serviceDetails
            };
        }

        // 计算基本统计数据
        private async Task<(int TotalEvents, decimal TotalRentalHours, decimal TotalRevenue, decimal AverageAttendance, decimal AverageOccupancyRate)> 
            GetBasicStatsAsync(DateTime startDate, DateTime endDate)
        {
            var data = await (from detail in _context.VenueEventDetails
                             join venueEvent in _context.VenueEvents on detail.EVENT_ID equals venueEvent.EVENT_ID
                             join billing in _context.VenueEventBillings on detail.EVENT_ID equals billing.EVENT_ID into billings
                             from billing in billings.DefaultIfEmpty()
                             where detail.RENT_START >= startDate && detail.RENT_START <= endDate && detail.STATUS == "APPROVED"
                             select new
                             {
                                 detail.RENT_START,
                                 detail.RENT_END,
                                 venueEvent.HEADCOUNT,
                                 venueEvent.CAPACITY,
                                 Revenue = billing != null ? billing.TOTAL_FEE : 0
                             }).ToListAsync();

            if (!data.Any())
                return (0, 0, 0, 0, 0);

            var totalEvents = data.Count;
            var totalRentalHours = data.Sum(x => CalculateRentalHours(x.RENT_START, x.RENT_END));
            var totalRevenue = data.Sum(x => x.Revenue);
            var averageAttendance = data.Where(x => x.HEADCOUNT.HasValue).Any() ? 
                data.Where(x => x.HEADCOUNT.HasValue).Average(x => x.HEADCOUNT.Value) : 0;
            var averageOccupancyRate = data.Where(x => x.HEADCOUNT.HasValue && x.CAPACITY > 0).Any() ?
                data.Where(x => x.HEADCOUNT.HasValue && x.CAPACITY > 0).Average(x => (decimal)x.HEADCOUNT.Value / x.CAPACITY * 100) : 0;

            return (totalEvents, totalRentalHours, totalRevenue, (decimal)averageAttendance, averageOccupancyRate);
        }

        #endregion
    }
}
