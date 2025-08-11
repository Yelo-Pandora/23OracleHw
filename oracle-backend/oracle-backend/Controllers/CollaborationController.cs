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
            // 检查ID唯一性
            var exists = await _context.Collaborations
                .AnyAsync(c => c.COLLABORATION_ID == dto.CollaborationId);

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
            var collaboration = await _context.Collaborations.FindAsync(id);
            if (collaboration == null)
                return NotFound();

            // 检查活动状态冲突（伪代码）
            if (await HasActiveEvents(id))
                return BadRequest("存在进行中的合作活动，无法修改");

            // 更新字段
            collaboration.COLLABORATION_NAME = dto.CollaborationName;
            collaboration.CONTACTOR = dto.Contactor;
            collaboration.PHONE_NUMBER = dto.PhoneNumber;
            collaboration.EMAIL = dto.Email;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // 2.4.4 合作方统计报表
        [HttpGet("report")]
        public async Task<IActionResult> GenerateReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? industry)
        {
            var report = await _context.VenueEventDetails
                .Where(ved => ved.RENT_START >= startDate && ved.RENT_END <= endDate)
                .GroupBy(ved => ved.COLLABORATION_ID)
                .Select(g => new {
                    CollaborationId = g.Key,
                    EventCount = g.Count(),
                    TotalInvestment = g.Sum(x => x.FUNDING),
                    AvgRevenue = g.Average(x => x.FUNDING * 0.3) // 示例计算逻辑
                })
                .ToListAsync();

            return Ok(report);
        }

        // DTO类
        public class CollaborationDto
        {
            [Required] public int CollaborationId { get; set; }
            [Required] public string CollaborationName { get; set; }
            public string Contactor { get; set; }

            [Phone]
            public string PhoneNumber { get; set; }

            [EmailAddress]
            public string Email { get; set; }
        }

        public class CollaborationUpdateDto
        {
            public string CollaborationName { get; set; }
            public string Contactor { get; set; }
            public string PhoneNumber { get; set; }
            public string Email { get; set; }
        }

        private async Task<bool> HasActiveEvents(int collaborationId)
        {
            // 实现检查逻辑
            return await _context.VenueEventDetails
                .AnyAsync(v => v.COLLABORATION_ID == collaborationId &&
                              v.STATUS == "ACTIVE");
        }
    }
}