using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using oracle_backend.Models;
using oracle_backend.Services; 

namespace oracle_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartStoreController : ControllerBase
    {
        private readonly ISaleEventService _saleEventService;

        public PartStoreController(ISaleEventService saleEventService)
        {
            _saleEventService = saleEventService;
        }

        [HttpPost("add-store-to-event")]
        public async Task<IActionResult> AddStoreToEvent([FromBody] PartStoreDto dto)
        {
            try
            {
                await _saleEventService.AddStoreToEventAsync(dto.EventId, dto.StoreId);
                return Ok("商铺成功参与活动");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("remove-store-from-event/{eventId}/{storeId}")]
        public async Task<IActionResult> RemoveStoreFromEvent(int eventId, int storeId)
        {
            try
            {
                await _saleEventService.RemoveStoreFromEventAsync(eventId, storeId);
                return Ok("商铺已退出活动");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("stores-by-event/{eventId}")]
        public async Task<IActionResult> GetStoresByEvent(int eventId)
        {
            try
            {
                var stores = await _saleEventService.GetStoresByEventAsync(eventId);
                return Ok(stores);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("events-by-store/{storeId}")]
        public async Task<IActionResult> GetEventsByStore(int storeId)
        {
            try
            {
                var events = await _saleEventService.GetEventsByStoreAsync(storeId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
