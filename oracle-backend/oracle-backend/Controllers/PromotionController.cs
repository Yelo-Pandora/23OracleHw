using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using oracle_backend.Models.Promotion;
using oracle_backend.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace oracle_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionController : ControllerBase
    {
        private readonly PromotionService _promotionService;
        private readonly DiscountRuleService _discountRuleService;
        private readonly ILogger<PromotionController> _logger;

        public PromotionController(
            PromotionService promotionService,
            DiscountRuleService discountRuleService,
            ILogger<PromotionController> logger)
        {
            _promotionService = promotionService;
            _discountRuleService = discountRuleService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePromotion([FromBody] PromotionDto dto)
        {
            try
            {
                var promotion = await _promotionService.CreatePromotionAsync(dto);
                return CreatedAtAction(nameof(GetPromotion), new { id = promotion.PromotionId }, promotion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating promotion");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePromotion(string id, [FromBody] PromotionDto dto)
        {
            try
            {
                var promotion = await _promotionService.UpdatePromotionAsync(id, dto);
                return Ok(promotion);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating promotion {id}");
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromotion(string id)
        {
            try
            {
                var result = await _promotionService.DeletePromotionAsync(id);
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting promotion {id}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPromotion(string id)
        {
            try
            {
                var promotion = await _promotionService.GetPromotionAsync(id);
                return promotion != null ? Ok(promotion) : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting promotion {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPromotions()
        {
            try
            {
                var promotions = await _promotionService.GetAllPromotionsAsync();
                return Ok(promotions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all promotions");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}/report")]
        public async Task<IActionResult> GenerateReport(string id)
        {
            try
            {
                var report = await _promotionService.GenerateReportAsync(id);
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating report for promotion {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("calculate")]
        public IActionResult CalculateDiscount([FromBody] DiscountCalculationRequest request)
        {
            try
            {
                var result = _discountRuleService.ApplyDiscount(request.OriginalPrice, request.RuleJson);
                return Ok(new { DiscountedPrice = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating discount");
                return BadRequest(ex.Message);
            }
        }

        public class DiscountCalculationRequest
        {
            public decimal OriginalPrice { get; set; }
            public string RuleJson { get; set; }
        }
    }
}