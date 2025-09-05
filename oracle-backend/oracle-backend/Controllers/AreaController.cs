using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
            [Required]
            public string Category { get; set; } // "RETAIL", "EVENT"
            // Retail-specific properties
            public string? RentStatus { get; set; }
            public double? BaseRent { get; set; }
            // Event-specific properties
            public int? Capacity { get; set; }
            public int? AreaFee { get; set; }
            // 标识其它区域的类型，如卫生间、杂物间等
            public string? Type { get; set; }
            public int? ParkingFee { get; set; }
        }

        // POST: api/Areas (对应 2.3.1 添加新区域)
        [HttpPost]
        public async Task<IActionResult> CreateArea([FromBody] AreaCreateDto dto)
        {
            var existingAreaCheck = await _context.Areas.FindAsync(dto.AreaId);

            if (existingAreaCheck != null)
            {
                return BadRequest($"区域ID '{dto.AreaId}' 已存在。");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto.Category.ToUpper() == "RETAIL")
                {
                    var retailArea = new RetailArea
                    {
                        AREA_ID = dto.AreaId,
                        ISEMPTY = dto.IsEmpty,
                        AREA_SIZE = dto.AreaSize,
                        CATEGORY = dto.Category,
                        RENT_STATUS = dto.RentStatus ?? "营业中",
                        BASE_RENT = dto.BaseRent ?? 0
                    };
                    _context.Entry(retailArea).State = EntityState.Added;
                }
                else if (dto.Category.ToUpper() == "EVENT")
                {
                    var eventArea = new EventArea
                    {
                        AREA_ID = dto.AreaId,
                        ISEMPTY = dto.IsEmpty,
                        AREA_SIZE = dto.AreaSize,
                        CATEGORY = dto.Category,
                        CAPACITY = dto.Capacity,
                        AREA_FEE = dto.AreaFee ?? 0
                    };
                    _context.Entry(eventArea).State = EntityState.Added;
                }
                else if (dto.Category.ToUpper() == "PARKING")
                {
                    var parkingLot = new ParkingLot
                    {
                        AREA_ID = dto.AreaId,
                        ISEMPTY = dto.IsEmpty,
                        AREA_SIZE = dto.AreaSize,
                        CATEGORY = dto.Category,
                        PARKING_FEE = dto.ParkingFee ?? 0
                    };
                    _context.Entry(parkingLot).State = EntityState.Added;
                }
                else if (dto.Category.ToUpper() == "OTHER")
                {
                    var otherArea = new OtherArea
                    {
                        AREA_ID = dto.AreaId,
                        ISEMPTY = dto.IsEmpty,
                        AREA_SIZE = dto.AreaSize,
                        CATEGORY = dto.Category,
                        TYPE = dto.Type ?? "未使用"
                    };
                    _context.Entry(otherArea).State = EntityState.Added;
                }
                else
                {
                    throw new ArgumentException("类别参数不合法");
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "区域创建成功" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"创建区域 {dto.AreaId} 失败。");
                return StatusCode(500, "创建区域失败");
            }
        }

        // GET: api/Areas (对应 2.3.2 区域信息查询)
        [HttpGet("ByCategory")]
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
                RentStatus = _context.RetailAreas
                               .Where(ea => ea.AREA_ID == a.AREA_ID)
                               .Select(ea => ea.RENT_STATUS)
                               .FirstOrDefault(),
                AreaFee = _context.EventAreas
                               .Where(ea => ea.AREA_ID == a.AREA_ID)
                               .Select(ea => (double?)ea.AREA_FEE)
                               .FirstOrDefault(),
                Capacity = _context.EventAreas
                               .Where(ea => ea.AREA_ID == a.AREA_ID)
                               .Select(ea => (int?)ea.CAPACITY)
                               .FirstOrDefault(),
                ParkingFee = _context.ParkingLots
                               .Where(ea => ea.AREA_ID == a.AREA_ID)
                               .Select(ea => ea.PARKING_FEE)
                               .FirstOrDefault(),
                Type = _context.OtherAreas
                               .Where(ea => ea.AREA_ID == a.AREA_ID)
                               .Select(ea => ea.TYPE)
                               .FirstOrDefault()
            }).ToListAsync();
            if (result == null)
                return NotFound();
            return Ok(result);
        }
        [HttpGet("ByID")]
        public async Task<IActionResult> GetAreasByID([FromQuery] int id)
        {
            var query = _context.Areas.AsQueryable();

            query = query.Where(a => a.AREA_ID == id);

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
                RentStatus = _context.RetailAreas
                               .Where(ea => ea.AREA_ID == a.AREA_ID)
                               .Select(ea => ea.RENT_STATUS)
                               .FirstOrDefault(),
                AreaFee = _context.EventAreas
                               .Where(ea => ea.AREA_ID == a.AREA_ID)
                               .Select(ea => (double?)ea.AREA_FEE)
                               .FirstOrDefault(),
                Capacity = _context.EventAreas
                               .Where(ea => ea.AREA_ID == a.AREA_ID)
                               .Select(ea => (int?)ea.CAPACITY)
                               .FirstOrDefault(),
                ParkingFee = _context.ParkingLots
                               .Where(ea => ea.AREA_ID == a.AREA_ID)
                               .Select(ea => ea.PARKING_FEE)
                               .FirstOrDefault(),
                Type = _context.OtherAreas
                               .Where(ea => ea.AREA_ID == a.AREA_ID)
                               .Select(ea => ea.TYPE)
                               .FirstOrDefault()
            }).FirstOrDefaultAsync();
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        // DELETE: api/Areas/5 (对应 2.3.3 区域信息管理 - 删除)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArea(int id)
        {
            // 关键前提检查：目标区域不能有正在执行的租用或活动记录
            //var hasActiveRent = await _context.RentStores.AnyAsync(rs => rs.AREA_ID == id);
            //if (hasActiveRent)
            //{
            //    return BadRequest("无法删除：该区域已被店铺租用。");
            //}
            var hasActiveRent = await _context.RentStores.FirstOrDefaultAsync(rs => rs.AREA_ID == id);
            if (hasActiveRent != null)
            {
                return BadRequest("无法删除：该区域已被店铺租用。");
            }

            //var hasActiveEvent = await _context.VenueEventDetails.AnyAsync(ved => ved.AREA_ID == id);
            //if (hasActiveEvent)
            //{
            //    return BadRequest("无法删除：该区域已有关联的场地活动。");
            //}
            var hasActiveEvent = await _context.RentStores.FirstOrDefaultAsync(rs => rs.AREA_ID == id);
            if (hasActiveEvent != null)
            {
                return BadRequest("无法删除：该区域已被店铺租用。");
            }

            var hasParkingSpaces = await _context.ParkingSpaceDistributions.FirstOrDefaultAsync(rs => rs.AREA_ID == id);
            if (hasActiveEvent != null)
            {
                return BadRequest("无法删除：请先清理该停车场上的停车位。");
            }

            var area = await _context.Areas.FindAsync(id);
            if (area == null)
            {
                return NotFound();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                //// 先删除子表记录
                //if (area.CATEGORY.ToUpper() == "RETAIL")
                //{
                //    var retailArea = await _context.RetailAreas.FindAsync(id);
                //    if (retailArea != null) _context.RetailAreas.Remove(retailArea);
                //}
                //else if (area.CATEGORY.ToUpper() == "EVENT")
                //{
                //    var eventArea = await _context.EventAreas.FindAsync(id);
                //    if (eventArea != null) _context.EventAreas.Remove(eventArea);
                //}

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

        //用来更新区域信息的DTO
        public class AreaUpdateDto
        {
            // Area 表中的通用属性
            public int? IsEmpty { get; set; }
            public int? AreaSize { get; set; }

            // RetailArea 特有属性
            public string? RentStatus { get; set; }
            public double? BaseRent { get; set; }

            // EventArea 特有属性
            public int? Capacity { get; set; }
            public int? AreaFee { get; set; }

            // ParkingLot 特有属性
            public int? ParkingFee { get; set; }

            // OtherArea 特有属性
            public string? Type { get; set; }
        }
        // 修改区域信息的接口
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateArea(int id, [FromBody] AreaUpdateDto dto)
        {
            // 开启事务，确保所有操作的原子性
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 查找并更新基表记录
                var areaToUpdate = await _context.Areas.FindAsync(id);

                if (areaToUpdate == null)
                {
                    return NotFound($"未找到ID为 '{id}' 的区域。");
                }

                // 更新基表属性
                if (dto.IsEmpty.HasValue) areaToUpdate.ISEMPTY = dto.IsEmpty.Value;
                if (dto.AreaSize.HasValue) areaToUpdate.AREA_SIZE = dto.AreaSize.Value;
                _context.Entry(areaToUpdate).State = EntityState.Modified;

                // 在一个 switch 语句内处理子表的修复（INSERT）或更新（UPDATE）
                switch (areaToUpdate.CATEGORY.ToUpper())
                {
                    case "RETAIL":
                        var retailArea = await _context.RetailAreas.FindAsync(id);
                        if (retailArea == null)
                        {
                            // 子表记录缺失，插入数据
                            var rentStatus = dto.RentStatus ?? "正常营业";
                            var baseRent = dto.BaseRent ?? 0;
                            await _context.Database.ExecuteSqlInterpolatedAsync(
                                $"INSERT INTO RETAIL_AREA (AREA_ID, RENT_STATUS, BASE_RENT) VALUES ({id}, {rentStatus}, {baseRent})");
                        }
                        else
                        {
                            // 子表记录存在，执行更新
                            if (dto.RentStatus != null) retailArea.RENT_STATUS = dto.RentStatus;
                            if (dto.BaseRent.HasValue) retailArea.BASE_RENT = dto.BaseRent.Value;
                            _context.Entry(retailArea).State = EntityState.Modified;
                        }
                        break;

                    case "EVENT":
                        var eventArea = await _context.EventAreas.FindAsync(id);
                        if (eventArea == null)
                        {
                            // 子表记录缺失，插入数据
                            var capacity = dto.Capacity ?? 0;
                            var areaFee = dto.AreaFee ?? 0;
                            await _context.Database.ExecuteSqlInterpolatedAsync(
                                $"INSERT INTO EVENT_AREA (AREA_ID, CAPACITY, AREA_FEE) VALUES ({id}, {capacity}, {areaFee})");
                        }
                        else
                        {
                            // 子表记录存在，执行更新
                            if (dto.Capacity.HasValue) eventArea.CAPACITY = dto.Capacity.Value;
                            if (dto.AreaFee.HasValue) eventArea.AREA_FEE = dto.AreaFee.Value;
                            _context.Entry(eventArea).State = EntityState.Modified;
                        }
                        break;

                    case "PARKING":
                        var parkingLot = await _context.ParkingLots.FindAsync(id);
                        if (parkingLot == null)
                        {
                            // 子表记录缺失，插入数据
                            var parkingFee = dto.ParkingFee ?? 0;
                            await _context.Database.ExecuteSqlInterpolatedAsync(
                                $"INSERT INTO PARKING_LOT (AREA_ID, PARKING_FEE) VALUES ({id}, {parkingFee})");
                        }
                        else
                        {
                            // 子表记录存在，执行更新
                            if (dto.ParkingFee.HasValue) parkingLot.PARKING_FEE = dto.ParkingFee.Value;
                            _context.Entry(parkingLot).State = EntityState.Modified;
                        }
                        break;

                    case "OTHER":
                        var otherArea = await _context.OtherAreas.FindAsync(id);
                        if (otherArea == null)
                        {
                            // 子表记录缺失，插入数据
                            var type = dto.Type ?? "未知";
                            await _context.Database.ExecuteSqlInterpolatedAsync(
                                $"INSERT INTO OTHER_AREA (AREA_ID, TYPE) VALUES ({id}, {type})");
                        }
                        else
                        {
                            // 子表记录存在，执行更新
                            if (dto.Type != null) otherArea.TYPE = dto.Type;
                            _context.Entry(otherArea).State = EntityState.Modified;
                        }
                        break;
                }

                // 提交所有更改
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "区域信息更新成功" });
            }
            catch (Exception ex)
            {
                // 如果发生任何错误，回滚所有操作
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"更新区域 {id} 失败。");
                return StatusCode(500, "更新区域信息失败，服务器内部错误。");
            }
        }
    }
}