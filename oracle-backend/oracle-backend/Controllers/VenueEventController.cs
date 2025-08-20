using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using System.ComponentModel.DataAnnotations;

namespace oracle_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VenueEventController : ControllerBase
    {
        private readonly ComplexDbContext _context;
        private readonly ILogger<VenueEventController> _logger;

        public VenueEventController(ComplexDbContext context, ILogger<VenueEventController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTO for venue event reservation
        public class VenueEventReservationDto
        {
            [Required]
            public int CollaborationId { get; set; }
            [Required]
            public int AreaId { get; set; }
            [Required]
            public DateTime RentStartTime { get; set; }
            [Required]
            public DateTime RentEndTime { get; set; }
            public string? RentPurpose { get; set; }
            [Required]
            public string CollaborationName { get; set; }
            [Required]
            public string StaffPosition { get; set; }
            [Required]
            public string EventName { get; set; }
            public int? ExpectedHeadcount { get; set; }
            public double? ExpectedFee { get; set; }
            public int? Capacity { get; set; }
            public int? Expense { get; set; }
        }

        // DTO for venue event management
        public class VenueEventUpdateDto
        {
            public string? EventName { get; set; }
            public int? Headcount { get; set; }
            public string? Description { get; set; }
            public string? Status { get; set; } // 筹备中/进行中/已结束/已取消
            public List<string>? ParticipantAccounts { get; set; }
        }

        // DTO for venue event settlement
        public class VenueEventSettlementDto
        {
            [Required]
            public int EventId { get; set; }
            [Required]
            public double VenueFee { get; set; }
            public double? AdditionalServiceFee { get; set; }
            [Required]
            public string PaymentMethod { get; set; }
            public string? InvoiceInfo { get; set; }
        }

        // DTO for venue event report
        public class VenueEventReportRequestDto
        {
            [Required]
            public DateTime StartDate { get; set; }
            [Required]
            public DateTime EndDate { get; set; }
            public string? ReportType { get; set; } // "daily", "weekly", "monthly"
        }

        // DTO for venue event report response
        public class VenueEventReportDto
        {
            public int TotalEvents { get; set; }
            public double TotalRentHours { get; set; }
            public double TotalRevenue { get; set; }
            public double AverageOccupancy { get; set; }
            public List<PopularVenueDto> PopularVenues { get; set; } = new List<PopularVenueDto>();
            public List<VenueEventSummaryDto> EventDetails { get; set; } = new List<VenueEventSummaryDto>();
        }

        public class PopularVenueDto
        {
            public int AreaId { get; set; }
            public int EventCount { get; set; }
            public double TotalRevenue { get; set; }
        }

        public class VenueEventSummaryDto
        {
            public int EventId { get; set; }
            public string? EventName { get; set; }
            public int AreaId { get; set; }
            public DateTime RentStart { get; set; }
            public DateTime RentEnd { get; set; }
            public double RentHours { get; set; }
            public double VenueFee { get; set; }
            public int? ActualHeadcount { get; set; }
            public string Status { get; set; }
        }

        // 1. 场地预约功能 (对应需求 1.1.8)
        [HttpPost("reservations")]
        public async Task<IActionResult> CreateReservation([FromBody] VenueEventReservationDto dto)
        {
            // 验证时间有效性
            if (dto.RentEndTime <= dto.RentStartTime)
            {
                return BadRequest(new { message = "结束时间需晚于起始时间" });
            }

            // 验证活动区域ID是否有效且未被占用
            var eventArea = await _context.EventAreas.FindAsync(dto.AreaId);
            if (eventArea == null)
            {
                return BadRequest(new { message = "活动区域ID无效" });
            }

            // 检查时间段内是否已被占用
            var conflictingReservation = await _context.VenueEventDetails
                .Where(ved => ved.AREA_ID == dto.AreaId &&
                             ved.STATUS != "已取消" &&
                             ((ved.RENT_START <= dto.RentStartTime && ved.RENT_END > dto.RentStartTime) ||
                              (ved.RENT_START < dto.RentEndTime && ved.RENT_END >= dto.RentEndTime) ||
                              (ved.RENT_START >= dto.RentStartTime && ved.RENT_END <= dto.RentEndTime)))
                .FirstOrDefaultAsync();

            if (conflictingReservation != null)
            {
                return BadRequest(new { message = "该区域在指定时间内已被占用" });
            }

            // 验证合作方是否存在
            var collaboration = await _context.Collaborations.FindAsync(dto.CollaborationId);
            if (collaboration == null)
            {
                return BadRequest(new { message = "合作方信息不存在" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 创建场地活动记录
                var venueEvent = new VenueEvent
                {
                    EVENT_NAME = dto.EventName,
                    EVENT_START = dto.RentStartTime,
                    EVENT_END = dto.RentEndTime,
                    HEADCOUNT = dto.ExpectedHeadcount,
                    FEE = dto.ExpectedFee ?? 0,
                    CAPACITY = dto.Capacity ?? eventArea.CAPACITY ?? 0,
                    EXPENSE = dto.Expense ?? 0
                };

                _context.VenueEvents.Add(venueEvent);
                await _context.SaveChangesAsync();

                // 创建场地活动详情记录
                var venueEventDetail = new VenueEventDetail
                {
                    EVENT_ID = venueEvent.EVENT_ID,
                    AREA_ID = dto.AreaId,
                    COLLABORATION_ID = dto.CollaborationId,
                    RENT_START = dto.RentStartTime,
                    RENT_END = dto.RentEndTime,
                    STATUS = "待审批",
                    FUNDING = 0 // 初始资金为0，后续可以更新
                };

                _context.VenueEventDetails.Add(venueEventDetail);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new 
                { 
                    message = "场地预约申请提交成功，等待审批",
                    eventId = venueEvent.EVENT_ID
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "创建场地预约失败");
                return StatusCode(500, new { message = "创建场地预约失败" });
            }
        }

        // 2. 场地预约审批
        [HttpPut("reservations/{eventId}/approve")]
        public async Task<IActionResult> ApproveReservation(int eventId, [FromBody] string? approvalNote)
        {
            var venueEventDetail = await _context.VenueEventDetails
                .FirstOrDefaultAsync(ved => ved.EVENT_ID == eventId);

            if (venueEventDetail == null)
            {
                return NotFound(new { message = "找不到对应的预约记录" });
            }

            if (venueEventDetail.STATUS != "待审批")
            {
                return BadRequest(new { message = "该预约已处理，无法重复审批" });
            }

            try
            {
                venueEventDetail.STATUS = "已通过";
                await _context.SaveChangesAsync();

                return Ok(new { message = "预约审批通过" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"审批预约 {eventId} 失败");
                return StatusCode(500, new { message = "审批失败" });
            }
        }

        // 3. 场地预约拒绝
        [HttpPut("reservations/{eventId}/reject")]
        public async Task<IActionResult> RejectReservation(int eventId, [FromBody] string? rejectionReason)
        {
            var venueEventDetail = await _context.VenueEventDetails
                .FirstOrDefaultAsync(ved => ved.EVENT_ID == eventId);

            if (venueEventDetail == null)
            {
                return NotFound(new { message = "找不到对应的预约记录" });
            }

            if (venueEventDetail.STATUS != "待审批")
            {
                return BadRequest(new { message = "该预约已处理，无法重复审批" });
            }

            try
            {
                venueEventDetail.STATUS = "已驳回";
                await _context.SaveChangesAsync();

                return Ok(new { message = "预约已驳回" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"驳回预约 {eventId} 失败");
                return StatusCode(500, new { message = "驳回失败" });
            }
        }

        // 4. 场地活动管理功能 (对应需求 1.1.9)
        [HttpPut("events/{eventId}")]
        public async Task<IActionResult> UpdateVenueEvent(int eventId, [FromBody] VenueEventUpdateDto dto)
        {
            var venueEvent = await _context.VenueEvents.FindAsync(eventId);
            if (venueEvent == null)
            {
                return NotFound(new { message = "找不到对应的活动记录" });
            }

            var venueEventDetail = await _context.VenueEventDetails
                .FirstOrDefaultAsync(ved => ved.EVENT_ID == eventId);

            if (venueEventDetail == null)
            {
                return NotFound(new { message = "找不到对应的活动详情记录" });
            }

            // 检查活动是否已结束
            if (venueEventDetail.STATUS == "已结束")
            {
                return BadRequest(new { message = "活动已结束，不可修改或取消" });
            }

            try
            {
                // 更新活动信息
                if (!string.IsNullOrEmpty(dto.EventName))
                    venueEvent.EVENT_NAME = dto.EventName;

                if (dto.Headcount.HasValue)
                    venueEvent.HEADCOUNT = dto.Headcount.Value;

                if (!string.IsNullOrEmpty(dto.Status))
                    venueEventDetail.STATUS = dto.Status;

                // 处理批量导入参与人员
                if (dto.ParticipantAccounts != null && dto.ParticipantAccounts.Any())
                {
                    // 删除现有临时权限
                    var existingTempAuthorities = await _context.TempAuthorities
                        .Where(ta => ta.EVENT_ID == eventId)
                        .ToListAsync();
                    _context.TempAuthorities.RemoveRange(existingTempAuthorities);

                    // 添加新的临时权限
                    foreach (var account in dto.ParticipantAccounts)
                    {
                        var tempAuthority = new TempAuthority
                        {
                            ACCOUNT = account,
                            EVENT_ID = eventId,
                            TEMP_AUTHORITY = 3 // 普通员工权限
                        };
                        _context.TempAuthorities.Add(tempAuthority);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "活动信息更新成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新活动 {eventId} 失败");
                return StatusCode(500, new { message = "更新活动失败" });
            }
        }

        // 5. 取消活动
        [HttpPut("events/{eventId}/cancel")]
        public async Task<IActionResult> CancelVenueEvent(int eventId)
        {
            var venueEventDetail = await _context.VenueEventDetails
                .FirstOrDefaultAsync(ved => ved.EVENT_ID == eventId);

            if (venueEventDetail == null)
            {
                return NotFound(new { message = "找不到对应的活动记录" });
            }

            if (venueEventDetail.STATUS == "已结束")
            {
                return BadRequest(new { message = "活动已结束，不可修改或取消" });
            }

            try
            {
                venueEventDetail.STATUS = "已取消";
                await _context.SaveChangesAsync();

                return Ok(new { message = "活动已取消，场地资源已释放" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"取消活动 {eventId} 失败");
                return StatusCode(500, new { message = "取消活动失败" });
            }
        }

        // 6. 查询活动列表
        [HttpGet("events")]
        public async Task<IActionResult> GetVenueEvents([FromQuery] string? status, [FromQuery] int? areaId)
        {
            var query = _context.VenueEventDetails
                .Include(ved => ved.venueEventNavigation)
                .Include(ved => ved.eventAreaNavigation)
                .Include(ved => ved.collaborationNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(ved => ved.STATUS == status);
            }

            if (areaId.HasValue)
            {
                query = query.Where(ved => ved.AREA_ID == areaId.Value);
            }

            var events = await query.Select(ved => new
            {
                ved.EVENT_ID,
                EventName = ved.venueEventNavigation.EVENT_NAME,
                ved.AREA_ID,
                ved.COLLABORATION_ID,
                CollaborationName = ved.collaborationNavigation.COLLABORATION_NAME,
                ved.RENT_START,
                ved.RENT_END,
                ved.STATUS,
                Headcount = ved.venueEventNavigation.HEADCOUNT,
                Fee = ved.venueEventNavigation.FEE,
                Capacity = ved.venueEventNavigation.CAPACITY,
                AreaFee = ved.eventAreaNavigation.AREA_FEE
            }).ToListAsync();

            return Ok(events);
        }

        // 7. 场地活动结算收费功能 (对应需求 1.1.10)
        [HttpPost("events/{eventId}/settlement")]
        public async Task<IActionResult> CreateSettlement(int eventId, [FromBody] VenueEventSettlementDto dto)
        {
            var venueEventDetail = await _context.VenueEventDetails
                .Include(ved => ved.venueEventNavigation)
                .Include(ved => ved.eventAreaNavigation)
                .FirstOrDefaultAsync(ved => ved.EVENT_ID == eventId);

            if (venueEventDetail == null)
            {
                return NotFound(new { message = "找不到对应的活动记录" });
            }

            if (venueEventDetail.STATUS != "已结束")
            {
                return BadRequest(new { message = "只有已结束的活动才能进行结算" });
            }

            try
            {
                // 计算租用时长（小时）
                var rentHours = (venueEventDetail.RENT_END - venueEventDetail.RENT_START).TotalHours;
                
                // 计算总费用
                var totalFee = dto.VenueFee + (dto.AdditionalServiceFee ?? 0);

                // 创建结算信息用于返回
                var settlementInfo = new
                {
                    EventId = eventId,
                    EventName = venueEventDetail.venueEventNavigation.EVENT_NAME,
                    AreaId = venueEventDetail.AREA_ID,
                    RentStart = venueEventDetail.RENT_START,
                    RentEnd = venueEventDetail.RENT_END,
                    RentHours = Math.Round(rentHours, 2),
                    VenueFee = dto.VenueFee,
                    AdditionalServiceFee = dto.AdditionalServiceFee ?? 0,
                    TotalFee = totalFee,
                    PaymentMethod = dto.PaymentMethod,
                    InvoiceInfo = dto.InvoiceInfo,
                    SettlementTime = DateTime.Now
                };

                // 更新活动状态为已结算，并将总费用存储到FUNDING字段
                venueEventDetail.STATUS = "已结算";
                venueEventDetail.FUNDING = totalFee;  // 新增：将总费用存储到FUNDING字段
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "结算单生成成功",
                    settlement = settlementInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建结算单 {eventId} 失败");
                return StatusCode(500, new { message = "创建结算单失败" });
            }
        }

        // 8. 场地活动统计报表功能 (对应需求 1.1.11)
        [HttpGet("reports")]
        public async Task<IActionResult> GenerateReport([FromQuery] VenueEventReportRequestDto dto)
        {
            if (dto.EndDate <= dto.StartDate)
            {
                return BadRequest(new { message = "结束时间需晚于起始时间" });
            }

            try
            {
                var events = await _context.VenueEventDetails
                    .Include(ved => ved.venueEventNavigation)
                    .Include(ved => ved.eventAreaNavigation)
                    .Where(ved => ved.RENT_START >= dto.StartDate && ved.RENT_END <= dto.EndDate)
                    .ToListAsync();

                if (!events.Any())
                {
                    return Ok(new { message = "该时间段内无场地活动记录" });
                }

                // 计算统计数据
                var totalEvents = events.Count;
                var totalRentHours = events.Sum(e => (e.RENT_END - e.RENT_START).TotalHours);
                var totalRevenue = events.Sum(e => (e.eventAreaNavigation?.AREA_FEE ?? 0) * 
                                                   (e.RENT_END - e.RENT_START).TotalHours);

                // 计算平均上座率
                var eventsWithCapacity = events.Where(e => e.venueEventNavigation.CAPACITY > 0).ToList();
                var averageOccupancy = eventsWithCapacity.Any() ? 
                    eventsWithCapacity.Average(e => (double)(e.venueEventNavigation.HEADCOUNT ?? 0) / e.venueEventNavigation.CAPACITY * 100) : 0;

                // 热门场地排行
                var popularVenues = events
                    .GroupBy(e => e.AREA_ID)
                    .Select(g => new PopularVenueDto
                    {
                        AreaId = g.Key,
                        EventCount = g.Count(),
                        TotalRevenue = g.Sum(e => (e.eventAreaNavigation?.AREA_FEE ?? 0) * 
                                                  (e.RENT_END - e.RENT_START).TotalHours)
                    })
                    .OrderByDescending(p => p.EventCount)
                    .Take(10)
                    .ToList();

                // 活动详情
                var eventDetails = events.Select(e => new VenueEventSummaryDto
                {
                    EventId = e.EVENT_ID,
                    EventName = e.venueEventNavigation.EVENT_NAME,
                    AreaId = e.AREA_ID,
                    RentStart = e.RENT_START,
                    RentEnd = e.RENT_END,
                    RentHours = Math.Round((e.RENT_END - e.RENT_START).TotalHours, 2),
                    VenueFee = (e.eventAreaNavigation?.AREA_FEE ?? 0) * (e.RENT_END - e.RENT_START).TotalHours,
                    ActualHeadcount = e.venueEventNavigation.HEADCOUNT,
                    Status = e.STATUS
                }).ToList();

                var report = new VenueEventReportDto
                {
                    TotalEvents = totalEvents,
                    TotalRentHours = Math.Round(totalRentHours, 2),
                    TotalRevenue = Math.Round(totalRevenue, 2),
                    AverageOccupancy = Math.Round(averageOccupancy, 2),
                    PopularVenues = popularVenues,
                    EventDetails = eventDetails
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成统计报表失败");
                return StatusCode(500, new { message = "生成统计报表失败" });
            }
        }

        // 9. 查询单个活动详情
        [HttpGet("events/{eventId}")]
        public async Task<IActionResult> GetVenueEventDetail(int eventId)
        {
            var venueEventDetail = await _context.VenueEventDetails
                .Include(ved => ved.venueEventNavigation)
                .Include(ved => ved.eventAreaNavigation)
                .Include(ved => ved.collaborationNavigation)
                .FirstOrDefaultAsync(ved => ved.EVENT_ID == eventId);

            if (venueEventDetail == null)
            {
                return NotFound(new { message = "找不到对应的活动记录" });
            }

            // 获取参与人员列表
            var participants = await _context.TempAuthorities
                .Where(ta => ta.EVENT_ID == eventId)
                .Select(ta => ta.ACCOUNT)
                .ToListAsync();

            var result = new
            {
                venueEventDetail.EVENT_ID,
                EventName = venueEventDetail.venueEventNavigation.EVENT_NAME,
                venueEventDetail.AREA_ID,
                venueEventDetail.COLLABORATION_ID,
                CollaborationName = venueEventDetail.collaborationNavigation.COLLABORATION_NAME,
                venueEventDetail.RENT_START,
                venueEventDetail.RENT_END,
                venueEventDetail.STATUS,
                Headcount = venueEventDetail.venueEventNavigation.HEADCOUNT,
                Fee = venueEventDetail.venueEventNavigation.FEE,
                Capacity = venueEventDetail.venueEventNavigation.CAPACITY,
                Expense = venueEventDetail.venueEventNavigation.EXPENSE,
                AreaFee = venueEventDetail.eventAreaNavigation?.AREA_FEE,
                Participants = participants
            };

            return Ok(result);
        }
    }
}
