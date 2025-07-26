using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using Oracle.ManagedDataAccess.Client;

namespace oracle_backend.Controllers
{
    [Route("api/allAccounts")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        //注入AccountDbContext和logger字段
        private readonly AccountDbContext _context;
        private readonly ILogger<AccountController> _logger;

        // 2. 构造函数
        public AccountController(AccountDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAccounts()
        {
            _logger.LogInformation("正在尝试获取所有账户信息...");
            try
            {
                var accounts = await _context.Accounts.ToListAsync(); // 也可以使用 context
                _logger.LogInformation($"成功获取到 {accounts.Count} 个账户。");
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取账户信息时发生错误。");
                return StatusCode(500, "服务器内部错误");
            }
        }
    }
}
