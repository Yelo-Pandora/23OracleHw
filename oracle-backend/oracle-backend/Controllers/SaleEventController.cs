using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using oracle_backend.Models;
using oracle_backend.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace oracle_backend.Controllers
{
    [ApiController]
    [Route("api/sale-event")]
    public class SaleEventController : ControllerBase
    {
        private readonly SaleEventService _saleEventService;
        private readonly DiscountRuleService _discountRuleService;
        private readonly ILogger<SaleEventController> _logger;

        public SaleEventController(
            SaleEventService saleEventService,
            DiscountRuleService discountRuleService,
            ILogger<SaleEventController> logger)
        {
            _saleEventService = saleEventService;
            _discountRuleService = discountRuleService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSaleEvent([FromBody] SaleEventDto dto)
        {
            try
            {
                var saleEvent = await _saleEventService.CreateSaleEventAsync(dto);
                return CreatedAtAction(nameof(GetSaleEvent), new { id = saleEvent.EventId }, saleEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建促销活动错误");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSaleEvent(string id, [FromBody] SaleEventDto dto)
        {
            try
            {
                var saleEvent = await _saleEventService.UpdateSaleEventAsync(id, dto);
                return Ok(saleEvent);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新促销活动错误 {id}");
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSaleEvent(string id)
        {
            try
            {
                var result = await _saleEventService.DeleteSaleEventAsync(id);
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除促销活动错误 {id}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSaleEvent(string id)
        {
            try
            {
                var saleEvent = await _saleEventService.GetSaleEventAsync(id);
                return saleEvent != null ? Ok(saleEvent) : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取促销活动错误 {id}");
                return StatusCode(500, "服务器内部错误");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSaleEvents()
        {
            try
            {
                var saleEvents = await _saleEventService.GetAllSaleEventsAsync();
                return Ok(saleEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有促销活动错误");
                return StatusCode(500, "服务器内部错误");
            }
        }

        [HttpGet("{id}/report")]
        public async Task<IActionResult> GenerateSaleEventReport(string id)
        {
            try
            {
                var report = await _saleEventService.GenerateSaleEventReportAsync(id);
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"生成报表错误 {id}");
                return StatusCode(500, "服务器内部错误");
            }
        }

        [HttpPost("calculate")]
        public IActionResult CalculateDiscount([FromBody] DiscountCalculationRequest request)
        {
            try
            {
                var result = _discountRuleService.ApplyDiscount(request.OriginalPrice, request.Rule);
                return Ok(new { DiscountedPrice = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "折扣计算错误");
                return BadRequest(ex.Message);
            }
        }

        public class DiscountCalculationRequest
        {
            public double OriginalPrice { get; set; }
            public Dictionary<string, object> Rule { get; set; }
        }
    }
}