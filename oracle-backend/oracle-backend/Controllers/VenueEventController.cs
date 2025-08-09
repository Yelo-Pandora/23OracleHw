using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using oracle_backend.Models.venueEvent;
using oracle_backend.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace oracle_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VenueEventController : ControllerBase
    {
        private readonly VenueEventService _venueEventService;
        private readonly ILogger<VenueEventController> _logger;

        public VenueEventController(
            VenueEventService venueEventService,
            ILogger<VenueEventController> logger)
        {
            _venueEventService = venueEventService;
            _logger = logger;
        }

        // POST api/venueevent/reservation - 提交场地预约申请
        [HttpPost("reservation")]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
        {
            try
            {
                var result = await _venueEventService.CreateReservationAsync(request.Reservation, request.Event);
                return CreatedAtAction(nameof(GetReservationByEventId), new { eventId = result.EventId }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "场地预约申请参数验证失败");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "场地预约申请业务规则验证失败");
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建场地预约申请时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // PUT api/venueevent/approval - 审批场地预约申请
        [HttpPut("approval")]
        public async Task<IActionResult> ApproveReservation([FromBody] VenueReservationApprovalDto approvalDto)
        {
            try
            {
                var result = await _venueEventService.ApproveReservationAsync(approvalDto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"审批场地预约失败: EventId={approvalDto.EventId}");
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"审批场地预约时发生错误: EventId={approvalDto.EventId}");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // GET api/venueevent/reservations - 获取预约申请（支持按状态筛选）
        [HttpGet("reservations")]
        public async Task<IActionResult> GetReservations([FromQuery] string status = null)
        {
            try
            {
                var reservations = await _venueEventService.GetReservationsByStatusAsync(status);
                return Ok(reservations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取预约申请时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // GET api/venueevent/reservations/{eventId} - 根据活动ID获取预约信息
        [HttpGet("reservations/{eventId}")]
        public async Task<IActionResult> GetReservationByEventId(int eventId)
        {
            try
            {
                var reservation = await _venueEventService.GetReservationByEventIdAsync(eventId);
                return reservation != null ? Ok(reservation) : NotFound($"未找到活动ID为{eventId}的预约记录");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取预约信息时发生错误: EventId={eventId}");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // GET api/venueevent/availability - 检查场地可用性
        [HttpGet("availability")]
        public async Task<IActionResult> CheckAreaAvailability([FromQuery] int areaId, [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            try
            {
                if (startTime >= endTime)
                    return BadRequest("结束时间必须晚于起始时间");

                var isAvailable = await _venueEventService.CheckAreaAvailabilityAsync(areaId, startTime, endTime);
                return Ok(new { AreaId = areaId, StartTime = startTime, EndTime = endTime, IsAvailable = isAvailable });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"检查场地可用性时发生错误: AreaId={areaId}");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // PUT api/venueevent/{eventId} - 更新场地活动信息
        [HttpPut("{eventId}")]
        public async Task<IActionResult> UpdateVenueEvent(int eventId, [FromBody] VenueEventUpdateDto updateDto)
        {
            try
            {
                var result = await _venueEventService.UpdateVenueEventAsync(eventId, updateDto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"更新活动信息参数错误: EventId={eventId}");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"更新活动信息业务规则验证失败: EventId={eventId}");
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新活动信息时发生错误: EventId={eventId}");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // DELETE api/venueevent/{eventId} - 取消场地活动
        [HttpDelete("{eventId}")]
        public async Task<IActionResult> CancelVenueEvent(int eventId, [FromBody] CancelVenueEventRequest request)
        {
            try
            {
                var result = await _venueEventService.CancelVenueEventAsync(eventId, request.OperatorId, request.Reason);
                return result ? NoContent() : NotFound();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"取消活动业务规则验证失败: EventId={eventId}");
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"取消活动时发生错误: EventId={eventId}");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // GET api/venueevent/{eventId}/logs - 获取活动操作日志
        [HttpGet("{eventId}/logs")]
        public async Task<IActionResult> GetEventLogs(int eventId)
        {
            try
            {
                var logs = await _venueEventService.GetEventLogsAsync(eventId);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取活动日志时发生错误: EventId={eventId}");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 用于创建预约申请的复合请求对象
        public class CreateReservationRequest
        {
            public VenueReservationDto Reservation { get; set; }
            public VenueEventDto Event { get; set; }
        }

        // POST api/venueevent/billing - 生成结算单
        [HttpPost("billing")]
        public async Task<IActionResult> CreateBilling([FromBody] CreateBillingDto billingDto)
        {
            try
            {
                var result = await _venueEventService.CreateBillingAsync(billingDto);
                return CreatedAtAction(nameof(GetBilling), new { billingId = result.BillingId }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"生成结算单参数错误: EventId={billingDto.EventId}");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"生成结算单业务规则验证失败: EventId={billingDto.EventId}");
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"生成结算单时发生错误: EventId={billingDto.EventId}");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // PUT api/venueevent/billing/confirm - 确认结算单
        [HttpPut("billing/confirm")]
        public async Task<IActionResult> ConfirmBilling([FromBody] ConfirmBillingDto confirmDto)
        {
            try
            {
                var result = await _venueEventService.ConfirmBillingAsync(confirmDto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"确认结算单业务规则验证失败: BillingId={confirmDto.BillingId}");
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"确认结算单时发生错误: BillingId={confirmDto.BillingId}");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // PUT api/venueevent/billing/payment - 确认支付
        [HttpPut("billing/payment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] PaymentConfirmationDto paymentDto)
        {
            try
            {
                var result = await _venueEventService.ConfirmPaymentAsync(paymentDto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"确认支付业务规则验证失败: BillingId={paymentDto.BillingId}");
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"确认支付时发生错误: BillingId={paymentDto.BillingId}");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // GET api/venueevent/billings - 获取结算单列表
        [HttpGet("billings")]
        public async Task<IActionResult> GetBillings([FromQuery] string status = null, [FromQuery] int? eventId = null)
        {
            try
            {
                var billings = await _venueEventService.GetBillingsAsync(status, eventId);
                return Ok(billings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取结算单列表时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // GET api/venueevent/billings/{billingId} - 获取结算单详情
        [HttpGet("billings/{billingId}")]
        public async Task<IActionResult> GetBilling(int billingId)
        {
            try
            {
                var billing = await _venueEventService.GetBillingAsync(billingId);
                return billing != null ? Ok(billing) : NotFound($"未找到结算单ID为{billingId}的记录");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取结算单详情时发生错误: BillingId={billingId}");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // GET api/venueevent/services - 获取可用的附加服务
        [HttpGet("services")]
        public async Task<IActionResult> GetAvailableServices()
        {
            try
            {
                var services = await _venueEventService.GetAvailableServicesAsync();
                return Ok(services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取附加服务列表时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // POST api/venueevent/reports - 生成统计报表
        [HttpPost("reports")]
        public async Task<IActionResult> GenerateReport([FromBody] GenerateReportDto reportDto)
        {
            try
            {
                var result = await _venueEventService.GenerateReportAsync(reportDto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "生成统计报表参数错误");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成统计报表时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // GET api/venueevent/reports - 获取报表列表
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            try
            {
                var reports = await _venueEventService.GetReportsAsync();
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取报表列表时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 用于取消活动的请求对象
        public class CancelVenueEventRequest
        {
            [Required]
            public string OperatorId { get; set; }
            
            [Required]
            [StringLength(200)]
            public string Reason { get; set; }
        }
    }
}
