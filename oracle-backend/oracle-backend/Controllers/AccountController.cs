using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using System.ComponentModel.DataAnnotations;

namespace oracle_backend.Controllers
{
    [Route("api/Accounts")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        //注入AccountDbContext和logger字段
        private readonly AccountDbContext _context;
        private readonly ILogger<AccountController> _logger;

        //构造函数
        public AccountController(AccountDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        //获取所有账号信息
        [HttpGet("AllAccount")]
        public async Task<IActionResult> GetAllAccounts()
        {
            _logger.LogInformation("正在尝试获取所有账户信息...");
            try
            {
                var accounts = await _context.ACCOUNT
                .ToListAsync();
                _logger.LogInformation($"成功获取到 {accounts.Count} 个账户。");
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取账户信息时发生错误。");
                return StatusCode(500, "服务器内部错误");
            }
        }

        //注册账号，根据是否是数据库第一个账号或身份（员工/商户）授予权限  
        public class AccountRegisterDto
        {
            [Required(ErrorMessage = "账号为必填项")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "账号长度必须在3到50个字符之间")]
            public string ACCOUNT { get; set; }

            [Required(ErrorMessage = "密码为必填项")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度至少为6个字符")]
            public string PASSWORD { get; set; }

            [Required(ErrorMessage = "用户名为必填项")]
            [StringLength(50)]
            public string USERNAME { get; set; }

            [Required(ErrorMessage = "必须指定用户身份")]
            [StringLength(50)]
            public string IDENTITY { get; set; }
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AccountRegisterDto registerDto)
        {
            _logger.LogInformation("正在尝试为 {AccountName} 注册新账号...", registerDto.ACCOUNT);

            try
            {
                //检查账号是否已存在，如果存在则立即返回错误
                var existingAccount = await _context.ACCOUNT
                    .FirstOrDefaultAsync(a => a.ACCOUNT == registerDto.ACCOUNT);

                if (existingAccount != null)
                {
                    _logger.LogWarning("账号 {AccountName} 已存在，注册失败。", registerDto.ACCOUNT);
                    return Conflict("该账号已存在。");
                }

                //提取用户提交的身份
                int authorityCode;
                string identityValue = registerDto.IDENTITY;

                //检查数据库中是否还没有任何用户
                bool isFirstUser = await _context.ACCOUNT.CountAsync() == 0;

                if (isFirstUser)
                {
                    //如果是第一个用户，强制设置为数据库管理员
                    _logger.LogInformation("数据库中无用户，将 {AccountName} 设置为第一个管理员。", registerDto.ACCOUNT);
                    authorityCode = 1; // 数据库管理员权限编码
                    identityValue = "员工"; // 第一个管理员的身份默认为员工
                }
                else
                {
                    //如果不是第一个用户，则根据其选择的身份分配权限
                    switch (registerDto.IDENTITY)
                    {
                        case "员工":
                            authorityCode = 3; // 员工权限编码
                            break;
                        case "商户":
                            authorityCode = 4; // 商铺租户权限编码
                            break;
                        default:
                            _logger.LogWarning("无效的身份类型 '{Identity}'，注册失败。", registerDto.IDENTITY);
                            return BadRequest($"无效的身份类型：'{registerDto.IDENTITY}'。有效值为：员工, 商户。");
                    }
                }

                //DTO映射到数据库实体Account
                var account = new Account
                {
                    ACCOUNT = registerDto.ACCOUNT,
                    USERNAME = registerDto.USERNAME,
                    PASSWORD = registerDto.PASSWORD,
                    IDENTITY = identityValue,
                    AUTHORITY = authorityCode
                };

                //添加实体并保存到数据库
                _context.ACCOUNT.Add(account);
                await _context.SaveChangesAsync();

                _logger.LogInformation("账号 {AccountName} 注册成功，身份为 '{Identity}'，权限级别为 {Authority}。",
                    account.ACCOUNT, account.IDENTITY, account.AUTHORITY);

                //成功响应
                return Ok(new { message = "账号注册成功。" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为账号 {AccountName} 进行注册时发生内部错误。", registerDto.ACCOUNT);
                return StatusCode(500, "服务器内部错误，注册失败。");
            }
        }

        //登陆账号,成功则返回该账号的信息
        public class toLogin
        {
            public string acc { get; set; }
            public string pass { get; set; }
            public string identity { get; set; }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] toLogin goal)
        {
            var target = await _context.FindAccount(goal.acc);
            if (target == null)
            {
                _logger.LogWarning("不存在该账号：{acc}", goal.acc);
                return BadRequest("用户不存在");
            }
            else if (target.PASSWORD != goal.pass)
            {
                _logger.LogWarning("密码不正确：{pass}", goal.pass);
                return BadRequest("密码不正确");
            }
            else if (target.AUTHORITY == 5)
            {
                _logger.LogWarning("账号已被封禁，禁止登录：{acc}", goal.acc);
                return BadRequest("账号封禁中");
            }
            else if (target.IDENTITY != goal.identity)
            {
                _logger.LogWarning("身份不正确或不存在该账号：{identity}", goal.identity);
                return BadRequest("身份错误");
            }
            else
                return Ok(target);
        }

        //修改指定账号信息
        [HttpPatch("alter/{currAccount}")]
        public async Task<IActionResult> UpdateAccount(string currAccount, [FromQuery] string operatorAccountId, [FromBody] Account updatedAccount)
        {
            var cur = await _context.FindAccount(currAccount);
            var oper = await _context.FindAccount(operatorAccountId);
            //找不到要修改的账号
            if (cur == null)
            {
                _logger.LogWarning("指定的账号不存在");
                return BadRequest("重新指定账号");
            }
            //操作员账号缺失
            if (oper == null)
            {
                _logger.LogWarning("操作员账号异常");
                return BadRequest("请重试");
            }
            //只有数据库管理员能管理其它的的账号
            if (oper.AUTHORITY != 1 && currAccount.ToLower() != updatedAccount.ACCOUNT)
            {
                _logger.LogWarning("操作者权限不足");
                return BadRequest("权限不足");
            }
            // 判断账号名是否被修改，且新账号名是否已存在
            if (updatedAccount.ACCOUNT != currAccount)
            {
                _logger.LogWarning("不允许直接修改账号");
                return BadRequest("不允许直接修改账号");
            }
            // 修改成了无效身份
            if (updatedAccount.IDENTITY != "员工" && updatedAccount.IDENTITY != "商户")
            {
                _logger.LogWarning("无效的身份");
                return BadRequest("无效的身份");
            }

            //执行更新
            cur.ACCOUNT = updatedAccount.ACCOUNT;
            cur.PASSWORD = updatedAccount.PASSWORD;
            cur.USERNAME = updatedAccount.USERNAME;
            cur.IDENTITY = updatedAccount.IDENTITY;
            //不允许修改自己的权限，且修改的权限不能超过操作员权限
            if (oper.AUTHORITY == 1 && currAccount.ToLower() != operatorAccountId.ToLower())
            {
                cur.AUTHORITY = updatedAccount.AUTHORITY < oper.AUTHORITY ? oper.AUTHORITY : updatedAccount.AUTHORITY;
            }

            //提交更改
            await _context.SaveChangesAsync();
            return Ok("更新成功");
        }

        //删除指定账号
        [HttpDelete("delete/{accountId}")]
        public async Task<IActionResult> DeleteAccount(string accountId, [FromQuery] string operatorAccountId)
        {
            var account = await _context.FindAccount(accountId);
            //检查是否是管理员
            if (!await _context.CheckAuthority(operatorAccountId, 1))
            {
                _logger.LogWarning("非数据库管理员，无法删除");
                return BadRequest("非数据库管理员，无法删除");
            }
            //检查账号是否存在
            if (account == null)
            {
                _logger.LogWarning($"账号 {accountId} 不存在，无法删除");
                return NotFound("账号不存在");
            }

            _context.ACCOUNT.Remove(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"账号 {accountId} 删除成功");
            return Ok("账号删除成功");
        }

        //查询指定账号的权限
        [HttpPost("chkauth")]
        public async Task<IActionResult> ChkAuthority(string account, int goalAuth)
        {
            var goalaccount = await _context.FindAccount(account);
            if (goalaccount == null)
            {
                _logger.LogWarning("指定的账号不存在");
                return BadRequest("账号不存在，请重新指定账号");
            }
            else
            {
                return Ok(await _context.CheckAuthority(account, goalAuth));
            }
        }

        [HttpPost("getauth")]
        public async Task<IActionResult> GetAuthority(string account)
        {

            var goalaccount = await _context.FindAccount(account);
            if (goalaccount == null)
            {
                _logger.LogWarning("指定的账号不存在");
                return BadRequest("账号不存在，请重新指定账号");
            }
            else
            {
                return Ok(goalaccount.AUTHORITY);
            }
        }

        //查询指定账号的信息
        [HttpGet("info/{accountId}")]
        public async Task<IActionResult> GetAccountInfo(string accountId)
        {
            _logger.LogInformation($"--- GetAccountInfo 被调用，收到的 accountId 是: [{accountId}] ---");
            var account = await _context.FindAccount(accountId);
            if (account == null)
            {
                _logger.LogWarning("指定的账号不存在");
                return NotFound("账号不存在");
            }
            return Ok(account);
        }
        
        // 根据员工ID获取Account
        [HttpGet("GetAccById")]
        public async Task<IActionResult> GetAccountByStaffId(int staffId)
        {
            var account = await _context.AccountFromStaffID(staffId);
            if (account == null)
            {
                _logger.LogWarning("指定的员工ID不存在");
                return BadRequest("员工ID不存在，请重新指定");
            }
            return Ok(account);
        }
    }
}
