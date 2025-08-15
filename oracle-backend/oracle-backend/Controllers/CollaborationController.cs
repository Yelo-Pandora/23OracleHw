// CollaborationController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace oracle_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollaborationController : ControllerBase
    {
        private readonly ComplexDbContext _context;
        private readonly ILogger<CollaborationController> _logger;

        public CollaborationController(ComplexDbContext context, ILogger<CollaborationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 2.4.1 添加新合作方
        [HttpPost]
        public async Task<IActionResult> AddCollaboration([FromBody] CollaborationDto dto)
        {
            try
            {
                // 检查ID唯一性 - 使用COUNT避免布尔转换
                var exists = await _context.Collaborations
                    .CountAsync(c => c.COLLABORATION_ID == dto.CollaborationId) > 0;

                if (exists)
                    return BadRequest("合作方ID已存在");

                // 创建实体
                var collaboration = new Collaboration
                {
                    COLLABORATION_ID = dto.CollaborationId,
                    COLLABORATION_NAME = dto.CollaborationName,
                    CONTACTOR = dto.Contactor,
                    PHONE_NUMBER = dto.PhoneNumber,
                    EMAIL = dto.Email
                };

                _context.Collaborations.Add(collaboration);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"新增合作方: ID={dto.CollaborationId}");
                return Ok(new { message = "添加成功", id = collaboration.COLLABORATION_ID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加合作方时出错");
                return StatusCode(500, "内部服务器错误");
            }
        }

        // 2.4.2 合作方信息查询
        [HttpGet]
        public async Task<IActionResult> SearchCollaborations(
            [FromQuery] int? id,
            [FromQuery] string? name,
            [FromQuery] string? contactor)
        {
            var query = _context.Collaborations.AsQueryable();

            if (id.HasValue)
                query = query.Where(c => c.COLLABORATION_ID == id);

            if (!string.IsNullOrEmpty(name))
                query = query.Where(c => c.COLLABORATION_NAME.Contains(name));

            if (!string.IsNullOrEmpty(contactor))
                query = query.Where(c => c.CONTACTOR.Contains(contactor));

            var results = await query.ToListAsync();
            return Ok(results);
        }

        // 2.4.3 修改合作方信息
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCollaboration(int id, [FromBody] CollaborationUpdateDto dto)
        {
            try
            {
                // 查找指定ID的合作方
                var collaboration = await _context.Collaborations
                    .FirstOrDefaultAsync(c => c.COLLABORATION_ID == id);

                if (collaboration == null)
                    return NotFound("合作方不存在");

                // 检查活动状态冲突(需要表VenueEventDetails)
                if (await HasActiveEvents(id))
                    return BadRequest("存在进行中的合作活动，无法修改");

                // 更新可修改字段
                if (!string.IsNullOrEmpty(dto.CollaborationName))
                    collaboration.COLLABORATION_NAME = dto.CollaborationName;

                if (!string.IsNullOrEmpty(dto.Contactor))
                    collaboration.CONTACTOR = dto.Contactor;

                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                    collaboration.PHONE_NUMBER = dto.PhoneNumber;

                if (!string.IsNullOrEmpty(dto.Email))
                    collaboration.EMAIL = dto.Email;

                // 标记实体为已修改
                _context.Entry(collaboration).State = EntityState.Modified;

                // 保存更改
                await _context.SaveChangesAsync();

                _logger.LogInformation($"修改合作方信息: ID={id}");
                return Ok(new
                {
                    message = "更新成功",
                    id = collaboration.COLLABORATION_ID,
                    updatedFields = new
                    {
                        name = dto.CollaborationName,
                        contactor = dto.Contactor,
                        phone = dto.PhoneNumber,
                        email = dto.Email
                    }
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "更新合作方时发生并发冲突");
                return StatusCode(500, "更新失败：数据已被其他操作修改" + ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新合作方时出错");
                return StatusCode(500, "内部服务器错误" + ex);
            }
        }

        // 2.4.4 合作方统计报表
        [HttpGet("report")]
        public async Task<IActionResult> GenerateReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? industry)
        {
            if (startDate >= endDate)
            {
                return BadRequest("开始日期必须早于结束日期");
            }
            if (endDate > DateTime.Now)
            {
                return BadRequest("结束日期不能晚于当前日期");
            }
            var report = await _context.VenueEventDetails
                .Where(ved => ved.RENT_START >= startDate && ved.RENT_END <= endDate)
                .GroupBy(ved => ved.COLLABORATION_ID)
                .Select(g => new {
                    CollaborationId = g.Key,
                    EventCount = g.Count(),
                    TotalInvestment = g.Sum(x => x.FUNDING),
                    AvgRevenue = g.Average(x => x.FUNDING)
                })
                .ToListAsync();

            return Ok(report);
        }

        // DTO类
        public class CollaborationDto
        {
            [Required(ErrorMessage = "合作方ID是必填项")]
            [Range(1, 99999999999, ErrorMessage = "合作方ID必须大于0，且不能超过11位")]
            public int CollaborationId { get; set; }

            [Required(ErrorMessage = "合作方名称是必填项")]
            [StringLength(50, ErrorMessage = "名称长度不能超过50个字符")]
            public string CollaborationName { get; set; }

            [StringLength(50, ErrorMessage = "联系人姓名长度不能超过50个字符")]
            public string Contactor { get; set; }

            [Phone(ErrorMessage = "无效的电话号码格式")]
            [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
            public string PhoneNumber { get; set; }

            [EmailAddress(ErrorMessage = "无效的电子邮件格式")]
            [StringLength(50, ErrorMessage = "电子邮件长度不能超过50个字符")]
            public string Email { get; set; }
        }

        public class CollaborationUpdateDto
        {
            [StringLength(50, ErrorMessage = "名称长度不能超过50个字符")]
            public string CollaborationName { get; set; }

            [StringLength(50, ErrorMessage = "联系人姓名长度不能超过50个字符")]
            public string Contactor { get; set; }

            [Phone(ErrorMessage = "无效的电话号码格式")]
            [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
            public string PhoneNumber { get; set; }

            [EmailAddress(ErrorMessage = "无效的电子邮件格式")]
            [StringLength(50, ErrorMessage = "电子邮件长度不能超过50个字符")]
            public string Email { get; set; }
        }

        private async Task<bool> HasActiveEvents(int collaborationId)
        {
            // 实现检查逻辑
            var query = _context.VenueEventDetails.Where(v => v.COLLABORATION_ID == collaborationId && v.STATUS == "ACTIVE");
            return await query.CountAsync() > 0;
        }
    }
}