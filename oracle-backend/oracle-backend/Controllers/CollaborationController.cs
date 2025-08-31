// CollaborationController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace oracle_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollaborationController : ControllerBase
    {
        private readonly CollaborationDbContext _context;
        private readonly AccountDbContext _accountContext; // 账号相关的功能，注入AccountDbContext
        private readonly ILogger<CollaborationController> _logger;

        public CollaborationController(
            CollaborationDbContext context,
            AccountDbContext accountContext,
            ILogger<CollaborationController> logger)
        {
            _context = context;
            _accountContext = accountContext;
            _logger = logger;
        }

        // 权限验证方法
        private async Task<ActionResult?> CheckPermission(string operatorAccountId, int requiredAuthority)
        {
            if (string.IsNullOrEmpty(operatorAccountId))
            {
                return BadRequest("操作员账号不能为空");
            }
            // 检查账号是否存在
            var account = await _accountContext.FindAccount(operatorAccountId);
            if (account == null)
            {
                return BadRequest("操作员账号不存在");
            }
            // 检查账号是否被封禁
            if (account.AUTHORITY == 5)
            {
                return BadRequest("账号已被封禁，无法执行操作");
            }
            // 检查常驻权限
            bool hasPermission = await _accountContext.CheckAuthority(operatorAccountId, requiredAuthority);
            // 如果没有常驻权限，检查临时权限
            if (!hasPermission)
            {
                var tempAuthorities = await _accountContext.FindTempAuthorities(operatorAccountId);
                hasPermission = tempAuthorities.Any(ta =>
                    ta.TEMP_AUTHORITY.HasValue &&
                    ta.TEMP_AUTHORITY.Value <= requiredAuthority);
            }
            if (!hasPermission)
            {
                return BadRequest($"权限不足，需要权限级别 {requiredAuthority} 或更高");
            }
            return null;
        }

        // 2.4.1 添加新合作方
        [HttpPost]
        public async Task<IActionResult> AddCollaboration(
            [FromQuery, Required] string operatorAccountId,
            [FromBody] CollaborationDto dto)
        {
            // 验证权限 - 需要数据库管理员权限(1)
            var permissionCheck = await CheckPermission(operatorAccountId, 1);
            if (permissionCheck != null) return permissionCheck;
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

                _logger.LogInformation($"操作员 {operatorAccountId} 添加了合作方: ID={dto.CollaborationId}");
                return Ok(new { message = "添加成功", id = collaboration.COLLABORATION_ID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"操作员 {operatorAccountId} 添加合作方时出错");
                return StatusCode(500, "内部服务器错误");
            }
        }

        // 2.4.2 合作方信息查询
        [HttpGet]
        public async Task<IActionResult> SearchCollaborations(
            [FromQuery, Required] string operatorAccountId,
            [FromQuery] int? id,
            [FromQuery] string? name,
            [FromQuery] string? contactor)
        {
            // 验证权限 - 需要数据库管理员权限(1)或部门经理权限(2)
            var permissionCheck = await CheckPermission(operatorAccountId, 2);
            if (permissionCheck != null) return permissionCheck;

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
        public async Task<IActionResult> UpdateCollaboration(
            int id, 
            [FromQuery, Required] string operatorAccountId,
            [FromBody] CollaborationUpdateDto dto)
        {
            // 验证权限 - 需要数据库管理员权限(1)
            var permissionCheck = await CheckPermission(operatorAccountId, 1);
            if (permissionCheck != null) return permissionCheck;

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

                _logger.LogInformation($"操作员 {operatorAccountId} 修改了合作方信息: ID={id}");
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
                _logger.LogError(ex, $"操作员 {operatorAccountId} 更新合作方时发生并发冲突");
                return StatusCode(500, "更新失败：数据已被其他操作修改" + ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"操作员 {operatorAccountId} 更新合作方时出错");
                return StatusCode(500, "内部服务器错误" + ex);
            }
        }

        // 2.4.4 合作方统计报表
        [HttpGet("report")]
        public async Task<IActionResult> GenerateReport(
            [FromQuery, Required] string operatorAccountId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? industry)
        {
            // 验证权限 - 需要数据库管理员权限(1)或部门经理权限(2)
            var permissionCheck = await CheckPermission(operatorAccountId, 2);
            if (permissionCheck != null) return permissionCheck;

            if (startDate >= endDate)
            {
                return BadRequest("开始日期必须早于结束日期");
            }
            if (endDate > DateTime.Now)
            {
                return BadRequest("结束日期不能晚于当前日期");
            }
            // 基础查询（时间范围）
            var query = _context.VenueEventDetails
                .Where(ved => ved.RENT_START >= startDate && ved.RENT_END <= endDate);

            // 按合作方名称包含 industry 过滤
            if (!string.IsNullOrWhiteSpace(industry))
            {
                // 使用导航属性过滤；EF 会生成相应 JOIN
                string pattern = $"%{industry.Trim()}%";
                query = query.Where(ved =>
                    EF.Functions.Like(ved.collaborationNavigation.COLLABORATION_NAME, pattern));
            }

            var report = await query
                .GroupBy(ved => new
                {
                    ved.COLLABORATION_ID
                })
                .Select(g => new
                {
                    CollaborationId = g.Key.COLLABORATION_ID,
                    EventCount = g.Count(),
                    TotalInvestment = g.Sum(x => x.FUNDING),
                    AvgRevenue = g.Average(x => x.FUNDING)
                })
                .ToListAsync();

            return Ok(report);
        }

        // 2.4.5 合作方数据删除
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCollaboration(
            int id,
            [FromQuery, Required] string operatorAccountId)
        {
            // 验证权限 - 需要数据库管理员权限(1)
            var permissionCheck = await CheckPermission(operatorAccountId, 1);
            if (permissionCheck != null) return permissionCheck;
            try
            {
                var collaboration = await _context.Collaborations
                    .FirstOrDefaultAsync(c => c.COLLABORATION_ID == id);
                if (collaboration == null)
                    return NotFound("合作方不存在");
                // 检查活动状态冲突(需要表VenueEventDetails)
                if (await HasActiveEvents(id))
                    return BadRequest("存在进行中的合作活动，无法删除");
                // 删除合作方
                _context.Collaborations.Remove(collaboration);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"操作员 {operatorAccountId} 删除了合作方: ID={id}");
                return Ok(new { message = "删除成功", id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"操作员 {operatorAccountId} 删除合作方时出错");
                return StatusCode(500, "内部服务器错误");
            }
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