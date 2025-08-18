using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using oracle_backend.Models;
using oracle_backend.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace oracle_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SaleEventController : ControllerBase
    {
        private readonly SaleEventService _saleEventService;
        private readonly ILogger<SaleEventController> _logger;

        public SaleEventController(
            SaleEventService saleEventService,
            ILogger<SaleEventController> logger)
        {
            _saleEventService = saleEventService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSaleEvent([FromBody] SaleEventDto dto)
        {
            try
            {
                var saleEvent = await _saleEventService.CreateSaleEventAsync(dto);
                return CreatedAtAction(nameof(GetSaleEvent), new { id = saleEvent.EVENT_ID }, saleEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建促销活动错误");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSaleEvent(int id, [FromBody] SaleEventDto dto)
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
        public async Task<IActionResult> DeleteSaleEvent(int id)
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
        public async Task<IActionResult> GetSaleEvent(int id)
        {
            try
            {
                var saleEvent = await _saleEventService.GetSaleEventAsync(id);
                return saleEvent != null ? Ok(saleEvent) : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取促销活动错误 {id}");
                return StatusCode(500, "获取促销活动错误");
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
                return StatusCode(500, "获取所有促销活动错误");
            }
        }

        [HttpGet("{id}/report")]
        public async Task<IActionResult> GenerateSaleEventReport(int id)
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
                return StatusCode(500, "生成报表错误");
            }
        }
    }
}