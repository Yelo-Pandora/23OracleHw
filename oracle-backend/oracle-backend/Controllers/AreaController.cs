using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using System.Linq;
using System.Threading.Tasks;

namespace oracle_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AreasController : ControllerBase
    {
        private readonly ComplexDbContext _context;
        private readonly ILogger<AreasController> _logger;

        public AreasController(ComplexDbContext context, ILogger<AreasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // DTO for creating an area
        public class AreaCreateDto
        {
            public int AreaId { get; set; }
            public int IsEmpty { get; set; }
            public int? AreaSize { get; set; }
            public string Category { get; set; } // "RETAIL", "EVENT"
            // Retail-specific properties
            public string? RentStatus { get; set; }
            public double? BaseRent { get; set; }
            // Event-specific properties
            public int? Capacity { get; set; }
            public string? AreaFee { get; set; }
        }

        // POST: api/Areas (对应 2.3.1 添加新区域)
        [HttpPost]
        public async Task<IActionResult> CreateArea([FromBody] AreaCreateDto dto)
        {
            if (await _context.Areas.AnyAsync(a => a.AREA_ID == dto.AreaId))
            {
                return BadRequest($"区域ID '{dto.AreaId}' 已存在。");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newArea = new Area
                {
                    AREA_ID = dto.AreaId,
                    ISEMPTY = dto.IsEmpty,
                    AREA_SIZE = dto.AreaSize,
                    CATEGORY = dto.Category
                };

                await _context.Areas.AddAsync(newArea);
                await _context.SaveChangesAsync(); // Save the base area first

                if (dto.Category.ToUpper() == "RETAIL")
                {
                    var retailArea = new RetailArea
                    {
                        AREA_ID = dto.AreaId,
                        RENT_STATUS = dto.RentStatus,
                        BASE_RENT = dto.BaseRent ?? 0
                    };
                    _context.Entry(retailArea).State = EntityState.Added;
                }
                else if (dto.Category.ToUpper() == "EVENT")
                {
                    var eventArea = new EventArea
                    {
                        AREA_ID = dto.AreaId,
                        CAPACITY = dto.Capacity,
                        AREA_FEE = dto.AreaFee
                    };
                    _context.Entry(eventArea).State = EntityState.Added;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "区域创建成功" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"创建区域 {dto.AreaId} 失败。");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // GET: api/Areas (对应 2.3.2 区域信息查询)
        [HttpGet]
        public async Task<IActionResult> GetAreas([FromQuery] string? category, [FromQuery] int? isEmpty)
        {
            var query = _context.Areas.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(a => a.CATEGORY.ToUpper() == category.ToUpper());
            }

            if (isEmpty.HasValue)
            {
                query = query.Where(a => a.ISEMPTY == isEmpty.Value);
            }

            var result = await query.Select(a => new
            {
                a.AREA_ID,
                a.ISEMPTY,
                a.AREA_SIZE,
                a.CATEGORY,
                BaseRent = _context.RetailAreas
                                .Where(ra => ra.AREA_ID == a.AREA_ID)
                                .Select(ra => (double?)ra.BASE_RENT)
                                .FirstOrDefault(),
                AreaFee = _context.EventAreas
                               .Where(ea => ea.AREA_ID == a.AREA_ID)
                               .Select(ea => ea.AREA_FEE)
                               .FirstOrDefault()
            }).ToListAsync();

            return Ok(result);
        }

        // DELETE: api/Areas/5 (对应 2.3.3 区域信息管理 - 删除)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArea(int id)
        {
            // 关键前提检查：目标区域不能有正在执行的租用或活动记录
            var hasActiveRent = await _context.RentStores.AnyAsync(rs => rs.AREA_ID == id);
            if (hasActiveRent)
            {
                return BadRequest("无法删除：该区域已被店铺租用。");
            }

            var hasActiveEvent = await _context.VenueEventDetails.AnyAsync(ved => ved.AREA_ID == id);
            if (hasActiveEvent)
            {
                return BadRequest("无法删除：该区域已有关联的场地活动。");
            }

            var area = await _context.Areas.FindAsync(id);
            if (area == null)
            {
                return NotFound();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 先删除子表记录
                if (area.CATEGORY.ToUpper() == "RETAIL")
                {
                    var retailArea = await _context.RetailAreas.FindAsync(id);
                    if (retailArea != null) _context.RetailAreas.Remove(retailArea);
                }
                else if (area.CATEGORY.ToUpper() == "EVENT")
                {
                    var eventArea = await _context.EventAreas.FindAsync(id);
                    if (eventArea != null) _context.EventAreas.Remove(eventArea);
                }

                // 再删除主表记录
                _context.Areas.Remove(area);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "区域删除成功" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"删除区域 {id} 失败。");
                return StatusCode(500, "服务器内部错误");
            }
        }
    }
}