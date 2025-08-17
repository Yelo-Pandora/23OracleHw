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
    public class StaffController : ControllerBase
    {
    private readonly CollaborationDbContext _collabContext;
    private readonly AccountDbContext _accountContext;
    private readonly ILogger<StaffController> _logger;
    private readonly ILogger<AccountController> _accountLogger;

        public StaffController(
            CollaborationDbContext collabContext,
            AccountDbContext accountContext,
            ILogger<StaffController> logger,
            ILogger<AccountController> accountLogger)
        {
            _collabContext = collabContext;
            _accountContext = accountContext;
            _logger = logger;
            _accountLogger = accountLogger;
        }

        // DTO:
        public class StaffDto
        {
            [Required(ErrorMessage = "员工姓名是必填项")]
            [StringLength(50, ErrorMessage = "名称长度不能超过50个字符")]
            public string STAFF_NAME { get; set; }

            [StringLength(10, ErrorMessage = "性别长度不能超过10个字符")]
            public string STAFF_SEX { get; set; }

            [Required(ErrorMessage = "员工部门是必填项")]
            [StringLength(50, ErrorMessage = "部门长度不能超过50个字符")]
            public string STAFF_APARTMENT { get; set; }

            [Required(ErrorMessage = "员工职位是必填项")]
            [StringLength(50, ErrorMessage = "职位长度不能超过50个字符")]
            public string STAFF_POSITION { get; set; }

            [Required(ErrorMessage = "员工薪资是必填项")]
            [Range(0, 9999999999.99, ErrorMessage = "薪资必须大于等于0，且不能超过10位整数和2位小数")]
            public double STAFF_SALARY { get; set; }
        }

        // 权限与临时权限检查
        private async Task<bool> CheckPermission(string operatorAccountId, int requiredAuthority){
            // 检查账号是否存在
            var account = await _accountContext.FindAccount(operatorAccountId);
            // 检查权限
            bool hasPermission = await _accountContext.CheckAuthority(operatorAccountId, requiredAuthority);
            if (!hasPermission)
            {
                // 检查临时权限
                var tempPermission = await _accountContext.FindTempAuthorities(operatorAccountId);
                hasPermission = tempPermission.Any(ta =>
                    ta.TEMP_AUTHORITY.HasValue &&
                    ta.TEMP_AUTHORITY.Value <= requiredAuthority);
            }
            if (!hasPermission)
            {
                return false;
            }
            return true;
        }

        // 目前权限等级(取temp authority与authority中更小值)
        private async Task<int> GetCurrentAuthorityLevel(string operatorAccountId)
        {
            var account = await _accountContext.FindAccount(operatorAccountId);
            if (account == null)
            {
                return 0; // 无权限
            }

            var tempAuthority = await _accountContext.FindTempAuthorities(operatorAccountId);
            var currentAuthority = account.AUTHORITY;

            if (tempAuthority.Any())
            {
                currentAuthority = Math.Min(currentAuthority, tempAuthority.Min(ta => ta.TEMP_AUTHORITY.Value));
            }

            return currentAuthority;
        }

        // 是否有权限修改员工信息
        private async Task<ActionResult?> CanModifyStaff(string operatorAccount, string apartment){
            var isAdmin = await CheckPermission(operatorAccount, 1);
            var isManager = await CheckPermission(operatorAccount, 2);
            if(!isAdmin && !isManager) {
                return BadRequest("无权限");
            }
            else if (isManager != null){
                // 查看manager的部门
                Staff manager = await _collabContext.FindStaffByAccount(operatorAccount);
                if (manager == null)
                {
                    return BadRequest("无效的操作员账号");
                }
                // 部门和员工不同则无权操作
                if (manager.STAFF_APARTMENT != apartment)
                {
                    return BadRequest("无权操作该部门员工");
                }
            }
            return null;
        }

        //获取所有员工信息
        [HttpGet("AllStaffs")]
        public async Task<IActionResult> GetAllStaffs()
        {
            var staffs = await _collabContext.Staffs.ToListAsync();
            return Ok(staffs);
        }

        // 2.6.1 添加新员工
        [HttpPost("add staff")]
        public async Task<IActionResult> AddStaff(
            [FromQuery, Required] string operatorAccount,
            [FromBody] StaffDto dto)
        {
            // 权限检查
            var Permission = await CanModifyStaff(operatorAccount, dto.STAFF_APARTMENT);
            if (Permission != null){
                return Permission;
            }
            try {
                // 自动生成员工ID
                var maxStaffId = await _collabContext.Staffs.MaxAsync(s => (int?)s.STAFF_ID) ?? 0;
                var newStaffId = maxStaffId + 1;
                var staff = new Staff
                {
                    STAFF_ID = newStaffId,
                    STAFF_NAME = dto.STAFF_NAME,
                    STAFF_SEX = dto.STAFF_SEX,
                    STAFF_APARTMENT = dto.STAFF_APARTMENT,
                    STAFF_POSITION = dto.STAFF_POSITION,
                    STAFF_SALARY = dto.STAFF_SALARY
                };

                // 生成员工新账号（账号为字符串，建议用员工ID或姓名+ID）
                var accountStr = $"staff{newStaffId}";
                var accountDto = new AccountController.AccountRegisterDto {
                    ACCOUNT = accountStr,
                    USERNAME = dto.STAFF_NAME,
                    PASSWORD = "DefaultPassword",
                    IDENTITY = "员工"
                };
                var accountController = new AccountController(_accountContext, _accountLogger);
                var registerResult = await accountController.Register(accountDto) as ObjectResult;
                _logger.LogInformation("账号注册完成");
                if (registerResult == null || registerResult.StatusCode != 200)
                {
                    return BadRequest("创建员工账号失败");
                }

                // 添加员工
                _collabContext.Staffs.Add(staff);
                await _collabContext.SaveChangesAsync();

                // 建立员工与账号关联
                var staffAccount = new StaffAccount {
                    STAFF_ID = newStaffId,
                    ACCOUNT = accountStr
                };
                _accountContext.STAFF_ACCOUNT.Add(staffAccount);
                await _accountContext.SaveChangesAsync();
                _accountLogger.LogInformation($"员工与账号关联: ID={staff.STAFF_ID}, 账号={accountStr}");
                
                return Ok(new { message = "添加成功", id = staff.STAFF_ID, account = accountStr });
            }
            catch (Exception ex)
            {
                // await transaction.RollbackAsync();
                _logger.LogError(ex, "添加员工时出错");
                return StatusCode(500, "内部服务器错误");
            }
        }

        // 2.6.2 员工权限管理
        [HttpPost("modify staff authority")]
        public async Task<IActionResult> ModifyStaffAuthority(
            [FromQuery, Required] string operatorAccount,
            [FromQuery] int staffId,
            [FromQuery] int newAuthority)
        {
            // 权限检查
            // 检查被修改的员工是否有临时权限
            var staffAccount = await _accountContext.AccountFromStaffID(staffId);
            if (staffAccount != null)
            {
                var tempAuthorities = await _accountContext.FindTempAuthorities(staffAccount.ACCOUNT);
                if (tempAuthorities != null && tempAuthorities.Count > 0)
                {
                    return BadRequest("请先收回临时权限再调整长期权限");
                }
            }

            var staff = await _collabContext.FindStaffById(staffId);
            if (staff == null)
            {
                return BadRequest("员工不存在");
            }

            var Permission = await CanModifyStaff(operatorAccount, staff.STAFF_APARTMENT);
            if (Permission != null) return Permission;

            // 操作员权限要高于被修改员工的权限(值越小权限越高)
            var currentAuthority = await GetCurrentAuthorityLevel(operatorAccount);
            var staffAuthority = await GetCurrentAuthorityLevel(staffAccount.ACCOUNT);
            if (currentAuthority >= staffAuthority)
            {
                return BadRequest("操作员权限要高于被修改员工的权限");
            }

            // newAuthority不可高于修改者的权限
            if (newAuthority < currentAuthority)
            {
                return BadRequest("不可分配高于自身的权限等级");
            }

            staffAccount.AUTHORITY = newAuthority;
            await _collabContext.SaveChangesAsync();
            await _accountContext.SaveChangesAsync();

            return Ok("员工权限修改成功");
        }

    }
}