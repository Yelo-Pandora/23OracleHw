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
            [FromQuery, Required] string operatorAccountId,
            [FromBody] StaffDto dto)
        {
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
    }
}