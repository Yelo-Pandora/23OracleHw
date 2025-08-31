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

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 删除关联的员工账号 (StaffAccount)
                    var staffLink = await _context.STAFF_ACCOUNT.FirstOrDefaultAsync(sa => sa.ACCOUNT == accountId);
                    if (staffLink != null)
                    {
                        _context.STAFF_ACCOUNT.Remove(staffLink);
                    }

                    // 删除关联的商家账号 (StoreAccount)
                    var storeLink = await _context.STORE_ACCOUNT.FirstOrDefaultAsync(sa => sa.ACCOUNT == accountId);
                    if (storeLink != null)
                    {
                        _context.STORE_ACCOUNT.Remove(storeLink);
                    }

                    // 删除所有关联的临时权限 (TempAuthority)
                    var tempAuthorities = await _context.TEMP_AUTHORITY
                                                        .Where(ta => ta.ACCOUNT == accountId)
                                                        .ToListAsync();
                    if (tempAuthorities.Any())
                    {
                        _context.TEMP_AUTHORITY.RemoveRange(tempAuthorities);
                    }

                    // 删除主账号
                    _context.ACCOUNT.Remove(account);

                    // 操作提交到数据库
                    await _context.SaveChangesAsync();

                    // 提交事务
                    await transaction.CommitAsync();

                    _logger.LogInformation($"账号 {accountId} 及其所有关联信息已由管理员 {operatorAccountId} 成功删除。");
                    return Ok($"账号 '{accountId}' 已被成功删除。");
                }
                catch (DbUpdateException ex) // 捕获数据库更新异常
                {
                    // 如果在SaveChangesAsync过程中发生错误，则回滚事务
                    await transaction.RollbackAsync();

                    _logger.LogError(ex, $"删除账号 {accountId} 时发生数据库更新错误。");
                    return StatusCode(500, "删除账号时发生数据库错误，操作已回滚。");
                }
            }
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

        //查询指定账号的临时权限情况
        [HttpGet("tempauth/{accountId}")]
        public async Task<IActionResult> GetTempAuthorities(string accountId)
        {
            var account = await _context.FindAccount(accountId);
            if (account == null)
            {
                _logger.LogWarning("指定的账号不存在");
                return NotFound("账号不存在");
            }
            var tempAuths = await _context.FindTempAuthorities(accountId);
            if (tempAuths == null)
            {
                _logger.LogInformation("该账号没有任何临时权限");
                return Ok(new List<TempAuthority>());
            }
            return Ok(tempAuths);
        }

        // 员工信息的 DTO
        public class StaffInfoDto
        {
            public int StaffId { get; set; }
            public string StaffName { get; set; }
            public string Department { get; set; }
        }
        // 商家信息的 DTO
        public class StoreInfoDto
        {
            public int StoreId { get; set; }
            public string StoreName { get; set; }
            public string TenantName { get; set; }
        }
        //用来返回详细信息的DTO
        public class AccountDetailDto
        {
            // 从 Account 表获取的信息
            public string Account { get; set; }
            public string Username { get; set; }
            public string Identity { get; set; }
            public int Authority { get; set; }

            // 关联的员工信息 (如果存在)
            public StaffInfoDto? StaffInfo { get; set; }

            // 关联的商家信息 (如果存在)
            public StoreInfoDto? StoreInfo { get; set; }
        }
        //查询所有账号信息，附带关联的员工或商铺信息
        [HttpGet("AllAccount/detailed")]
        public async Task<IActionResult> GetAccountDetails(string account)
        {
            // 查找基本的账号信息
            var accountEntity = await _context.FindAccount(account);

            if (accountEntity == null)
            {
                return NotFound("未找到指定的账号。");
            }

            // 创建DTO并填充基本信息
            var responseDto = new AccountDetailDto
            {
                Account = accountEntity.ACCOUNT,
                Username = accountEntity.USERNAME,
                Identity = accountEntity.IDENTITY,
                Authority = accountEntity.AUTHORITY
            };

            // 检查并获取关联的员工信息
            var staffLink = await _context.STAFF_ACCOUNT
                                          .Include(sa => sa.staffNavigation) // 关键：预加载员工实体
                                          .FirstOrDefaultAsync(sa => sa.ACCOUNT == account);

            if (staffLink != null && staffLink.staffNavigation != null)
            {
                responseDto.StaffInfo = new StaffInfoDto
                {
                    StaffId = staffLink.STAFF_ID,
                    StaffName = staffLink.staffNavigation.STAFF_NAME,
                    Department = staffLink.staffNavigation.STAFF_APARTMENT
                };
            }

            // 4. 检查并获取关联的商家信息
            // 使用 Include() 来同时加载 StoreAccount 及其导航属性 Store 的数据
            var storeLink = await _context.STORE_ACCOUNT
                                          .Include(sa => sa.storeNavigation) // 关键：预加载店铺实体
                                          .FirstOrDefaultAsync(sa => sa.ACCOUNT == account);

            if (storeLink != null && storeLink.storeNavigation != null)
            {
                responseDto.StoreInfo = new StoreInfoDto
                {
                    StoreId = storeLink.STORE_ID,
                    // 以下属性需要您根据实际的 Store Model 修改
                    StoreName = storeLink.storeNavigation.STORE_NAME, // 假设 Store 类有 Name 属性
                    TenantName = storeLink.storeNavigation.TENANT_NAME
                };
            }

            // 5. 返回整合后的 DTO
            return Ok(responseDto);
        }

        //绑定账号和员工/商铺
        public class BindAccountDto
        {
            [Required(ErrorMessage = "账号为必填项")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "账号长度必须在3到50个字符之间")]
            public string ACCOUNT { get; set; }
            [Required(ErrorMessage = "ID为必填项")]
            public int ID { get; set; }
            [Required(ErrorMessage = "类型为必填项")]
            [StringLength(10)]
            public string TYPE { get; set; }
        }
        [HttpPost("bind")]
        public async Task<IActionResult> BindAccount([FromBody] BindAccountDto bindDto)
        {
            _logger.LogInformation("正在尝试绑定账号 {AccountName} 到 {Type} ID {Id}...", bindDto.ACCOUNT, bindDto.TYPE, bindDto.ID);
            try
            {
                //检查账号是否存在
                var account = await _context.FindAccount(bindDto.ACCOUNT);
                if (account == null)
                {
                    _logger.LogWarning("账号 {AccountName} 不存在，绑定失败。", bindDto.ACCOUNT);
                    return NotFound("该账号不存在。");
                }
                else if (bindDto.TYPE == "员工")
                {
                    //检查员工ID是否存在
                    var staff = await _context.STAFF.FirstOrDefaultAsync(s => s.STAFF_ID == bindDto.ID);
                    if (staff == null)
                    {
                        _logger.LogWarning("员工 ID {StaffId} 不存在，绑定失败。", bindDto.ID);
                        return NotFound("该员工 ID 不存在。");
                    }
                    //检查该账号是否已经绑定了员工
                    var existingStaffLink = await _context.STAFF_ACCOUNT.FirstOrDefaultAsync(sa => sa.ACCOUNT == bindDto.ACCOUNT);
                    if (existingStaffLink != null)
                    {
                        _logger.LogWarning("账号 {AccountName} 已经绑定了员工 ID {StaffId}，绑定失败。", bindDto.ACCOUNT, existingStaffLink.STAFF_ID);
                        return Conflict("该账号已经绑定了一个员工。");
                    }
                    //检查该员工是否已经绑定了账号
                    var existingStaffIdLink = await _context.STAFF_ACCOUNT.FirstOrDefaultAsync(sa => sa.STAFF_ID == bindDto.ID);
                    if (existingStaffIdLink != null)
                    {
                        _logger.LogWarning("员工 ID {StaffId} 已经绑定了账号 {AccountName}，绑定失败。", bindDto.ID, existingStaffIdLink.ACCOUNT);
                        return Conflict("该员工已经绑定了一个账号。");
                    }
                    //创建新的 StaffAccount 关联
                    var staffAccount = new StaffAccount
                    {
                        ACCOUNT = bindDto.ACCOUNT,
                        STAFF_ID = bindDto.ID
                    };
                    _context.STAFF_ACCOUNT.Add(staffAccount);
                    await _context.SaveChangesAsync();
                    return Ok("绑定成功");
                }
                else if (bindDto.TYPE == "商户")
                {
                    //检查商铺ID是否存在
                    var store = await _context.STORE.FirstOrDefaultAsync(s => s.STORE_ID == bindDto.ID);
                    if (store != null)
                    {
                        //检查该账号是否已经绑定了商铺
                        var existingStoreLink = await _context.STORE_ACCOUNT.FirstOrDefaultAsync(sa => sa.ACCOUNT == bindDto.ACCOUNT);
                        if (existingStoreLink != null)
                        {
                            _logger.LogWarning("账号 {AccountName} 已经绑定了商铺 ID {StoreId}，绑定失败。", bindDto.ACCOUNT, existingStoreLink.STORE_ID);
                            return Conflict("该账号已经绑定了一个商铺。");
                        }
                        //检查该商铺是否已经绑定了账号
                        var existingStoreIdLink = await _context.STORE_ACCOUNT.FirstOrDefaultAsync(sa => sa.STORE_ID == bindDto.ID);
                        if (existingStoreIdLink != null)
                        {
                            _logger.LogWarning("商铺 ID {StoreId} 已经绑定了账号 {AccountName}，绑定失败。", bindDto.ID, existingStoreIdLink.ACCOUNT);
                            return Conflict("该商铺已经绑定了一个账号。");
                        }
                        //创建新的 StoreAccount 关联
                        var storeAccount = new StoreAccount
                        {
                            ACCOUNT = bindDto.ACCOUNT,
                            STORE_ID = bindDto.ID
                        };
                        _context.STORE_ACCOUNT.Add(storeAccount);
                        await _context.SaveChangesAsync();
                        return Ok("绑定成功");
                    }
                    else
                    {
                        _logger.LogWarning("商铺 ID {StoreId} 不存在，绑定失败。", bindDto.ID);
                        return NotFound("该商铺 ID 不存在。");
                    }

                }
                else
                {
                    _logger.LogWarning("无效的绑定类型 '{Type}'，绑定失败。", bindDto.TYPE);
                    return BadRequest($"无效的绑定类型：'{bindDto.TYPE}'。有效值为：员工, 商户。");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为账号 {AccountName} 进行绑定时发生内部错误。", bindDto.ACCOUNT);
                return StatusCode(500, "服务器内部错误，绑定失败。");
            }
        }


        // 解绑账号和员工/商户
        [HttpDelete("unbind")]
        public async Task<IActionResult> UnbindAccount([FromQuery] string account, [FromQuery] int ID, [FromQuery] string type)
        {
            _logger.LogInformation("正在尝试为账号 {AccountName} 与 {Type} ID {Id} 解绑...", account, type, ID);

            // 1. 输入验证
            if (string.IsNullOrWhiteSpace(account) || ID <= 0 || string.IsNullOrWhiteSpace(type))
            {
                return BadRequest("必须提供有效的账号、ID 和类型。");
            }

            try
            {
                if (type == "员工")
                {
                    // 2. 查找并删除员工绑定关系
                    var staffAccountLink = await _context.STAFF_ACCOUNT
                        .FirstOrDefaultAsync(sa => sa.ACCOUNT == account && sa.STAFF_ID == ID);

                    if (staffAccountLink == null)
                    {
                        _logger.LogWarning("未找到账号 {AccountName} 与员工 ID {StaffId} 的绑定关系。", account, ID);
                        return NotFound("未找到指定的员工绑定关系。");
                    }

                    _context.STAFF_ACCOUNT.Remove(staffAccountLink);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("账号 {AccountName} 与员工 ID {StaffId} 解绑成功。", account, ID);
                    return Ok("员工解绑成功");
                }
                else if (type == "商户")
                {
                    // 3. 查找并删除商户绑定关系
                    var storeAccountLink = await _context.STORE_ACCOUNT
                        .FirstOrDefaultAsync(sa => sa.ACCOUNT == account && sa.STORE_ID == ID);

                    if (storeAccountLink == null)
                    {
                        _logger.LogWarning("未找到账号 {AccountName} 与商铺 ID {StoreId} 的绑定关系。", account, ID);
                        return NotFound("未找到指定的商户绑定关系。");
                    }

                    _context.STORE_ACCOUNT.Remove(storeAccountLink);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("账号 {AccountName} 与商铺 ID {StoreId} 解绑成功。", account, ID);
                    return Ok("商户解绑成功");
                }
                else
                {
                    // 4. 处理无效的类型参数
                    _logger.LogWarning("无效的解绑类型 '{Type}'。", type);
                    return BadRequest($"无效的解绑类型：'{type}'。有效值为：员工, 商户。");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为账号 {AccountName} 进行解绑时发生内部错误。", account);
                return StatusCode(500, "服务器内部错误，解绑失败。");
            }
        }
    }
}
