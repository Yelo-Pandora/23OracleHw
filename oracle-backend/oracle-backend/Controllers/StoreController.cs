using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace oracle_backend.Controllers
{
    [Route("api/Store")]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private readonly StoreDbContext _storeContext;
        private readonly AccountDbContext _accountContext;
        private readonly ILogger<StoreController> _logger;

        public StoreController(StoreDbContext storeContext, AccountDbContext accountContext, ILogger<StoreController> logger)
        {
            _storeContext = storeContext;
            _accountContext = accountContext;
            _logger = logger;
        }

        // 新增商户的DTO类
        public class CreateMerchantDto
        {
            [Required(ErrorMessage = "店铺名称为必填项")]
            [StringLength(100, ErrorMessage = "店铺名称长度不能超过100个字符")]
            public string StoreName { get; set; } = string.Empty;

            [Required(ErrorMessage = "租户类型为必填项")]
            [StringLength(50, ErrorMessage = "租户类型长度不能超过50个字符")]
            public string StoreType { get; set; } = string.Empty;

            [Required(ErrorMessage = "租户名为必填项")]
            [StringLength(100, ErrorMessage = "租户名长度不能超过100个字符")]
            public string TenantName { get; set; } = string.Empty;

            [Required(ErrorMessage = "联系方式为必填项")]
            [StringLength(50, ErrorMessage = "联系方式长度不能超过50个字符")]
            public string ContactInfo { get; set; } = string.Empty;

            [Required(ErrorMessage = "区域ID为必填项")]
            public int AreaId { get; set; }

            [Required(ErrorMessage = "租用起始时间为必填项")]
            public DateTime RentStart { get; set; }

            [Required(ErrorMessage = "租用结束时间为必填项")]
            public DateTime RentEnd { get; set; }

            public string? OperatorAccount { get; set; } // 操作员账号
        }

        // 新增商户接口
        [HttpPost("CreateMerchant")]
        public async Task<IActionResult> CreateMerchant([FromBody] CreateMerchantDto dto)
        {
            _logger.LogInformation("开始创建新商户：{StoreName}", dto.StoreName);

            try
            {
                // 1. 验证操作员权限（管理员权限）
                if (!string.IsNullOrEmpty(dto.OperatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(dto.OperatorAccount, 1);
                    if (!hasPermission)
                    {
                        _logger.LogWarning("操作员 {OperatorAccount} 权限不足", dto.OperatorAccount);
                        return BadRequest("操作员权限不足，需要管理员权限");
                    }
                }

                // 2. 验证租用时间的合理性
                if (dto.RentEnd <= dto.RentStart)
                {
                    return BadRequest("租用结束时间必须晚于起始时间");
                }

                if (dto.RentStart < DateTime.Today)
                {
                    return BadRequest("租用起始时间不能早于今天");
                }

                // 3. 检查店面是否为空置状态
                var isAreaAvailable = await _storeContext.IsAreaAvailable(dto.AreaId);
                if (!isAreaAvailable)
                {
                    _logger.LogWarning("店面 {AreaId} 不是空置状态", dto.AreaId);
                    return BadRequest("该店面已租用，请选择其他店面");
                }

                // 4. 验证租户信息是否重复
                var tenantExists = await _storeContext.TenantExists(dto.TenantName, dto.ContactInfo);
                if (tenantExists)
                {
                    _logger.LogWarning("租户 {TenantName} 或联系方式 {ContactInfo} 已存在", dto.TenantName, dto.ContactInfo);
                    return BadRequest("该租户已在本综合体有店铺");
                }

                // 开始数据库事务
                using var transaction = await _storeContext.Database.BeginTransactionAsync();
                try
                {
                    // 5. 生成店铺ID并创建店铺记录
                    var storeId = await _storeContext.GetNextStoreId();
                    
                    var store = new Store
                    {
                        STORE_ID = storeId,
                        STORE_NAME = dto.StoreName,
                        STORE_STATUS = "正常营业",
                        STORE_TYPE = dto.StoreType,
                        TENANT_NAME = dto.TenantName,
                        CONTACT_INFO = dto.ContactInfo,
                        RENT_START = dto.RentStart,
                        RENT_END = dto.RentEnd
                    };

                    _storeContext.STORE.Add(store);
                    await _storeContext.SaveChangesAsync();

                    // 6. 创建租用关系记录
                    var rentStore = new RentStore
                    {
                        STORE_ID = storeId,
                        AREA_ID = dto.AreaId
                    };

                    _storeContext.RENT_STORE.Add(rentStore);
                    await _storeContext.SaveChangesAsync();

                    // 7. 更新店面状态为已租用
                    await _storeContext.UpdateAreaStatus(dto.AreaId, false, "已租用");
                    await _storeContext.SaveChangesAsync();

                    // 8. 生成商户账号和初始密码
                    var accountName = $"store_{storeId:D6}"; // 格式如：store_000001
                    var initialPassword = GenerateRandomPassword(8);

                    var merchantAccount = new Account
                    {
                        ACCOUNT = accountName,
                        PASSWORD = initialPassword,
                        USERNAME = dto.TenantName,
                        IDENTITY = "商户",
                        AUTHORITY = 4 // 商户权限
                    };

                    _accountContext.ACCOUNT.Add(merchantAccount);
                    await _accountContext.SaveChangesAsync();

                    // 9. 创建店铺账号关联记录
                    var storeAccount = new StoreAccount
                    {
                        ACCOUNT = accountName,
                        STORE_ID = storeId
                    };

                    _accountContext.STORE_ACCOUNT.Add(storeAccount);
                    await _accountContext.SaveChangesAsync();

                    // 提交事务
                    await transaction.CommitAsync();

                    _logger.LogInformation("成功创建商户：{StoreName}，店铺ID：{StoreId}，账号：{Account}", 
                        dto.StoreName, storeId, accountName);

                    // 返回创建成功的信息
                    return Ok(new
                    {
                        message = "商户创建成功",
                        storeId = storeId,
                        storeName = dto.StoreName,
                        account = accountName,
                        initialPassword = initialPassword,
                        tenantName = dto.TenantName,
                        areaId = dto.AreaId,
                        rentStart = dto.RentStart,
                        rentEnd = dto.RentEnd
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建商户时发生错误：{StoreName}", dto.StoreName);
                return StatusCode(500, "服务器内部错误，创建商户失败");
            }
        }

        // 获取所有可用店面
        [HttpGet("AvailableAreas")]
        public async Task<IActionResult> GetAvailableAreas()
        {
            try
            {
                var availableAreas = await _storeContext.GetAvailableAreas();
                _logger.LogInformation("获取到 {Count} 个可用店面", availableAreas.Count);
                
                return Ok(availableAreas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用店面时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 获取所有商户信息
        [HttpGet("AllStores")]
        public async Task<IActionResult> GetAllStores([FromQuery] string? operatorAccount = null)
        {
            try
            {
                // 验证操作员权限（至少需要部门经理权限）
                if (!string.IsNullOrEmpty(operatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(operatorAccount, 2);
                    if (!hasPermission)
                    {
                        return BadRequest("权限不足，需要管理员或部门经理权限");
                    }
                }

                var stores = await _storeContext.STORE.ToListAsync();
                _logger.LogInformation("获取到 {Count} 个商户信息", stores.Count);
                
                return Ok(stores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取商户信息时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 根据店铺ID获取店铺信息
        [HttpGet("{storeId}")]
        public async Task<IActionResult> GetStoreById(int storeId)
        {
            try
            {
                var store = await _storeContext.GetStoreById(storeId);
                if (store == null)
                {
                    return NotFound("店铺不存在");
                }

                return Ok(store);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取店铺信息时发生错误：{StoreId}", storeId);
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 更新商户状态
        [HttpPatch("UpdateStoreStatus/{storeId}")]
        public async Task<IActionResult> UpdateStoreStatus(int storeId, [FromQuery] string newStatus, [FromQuery] string? operatorAccount = null)
        {
            try
            {
                // 验证操作员权限
                if (!string.IsNullOrEmpty(operatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(operatorAccount, 2);
                    if (!hasPermission)
                    {
                        return BadRequest("权限不足，需要管理员或部门经理权限");
                    }
                }

                // 验证状态值
                var validStatuses = new[] { "正常营业", "歇业中", "翻新中" };
                if (!validStatuses.Contains(newStatus))
                {
                    return BadRequest($"无效的状态值。有效值为：{string.Join(", ", validStatuses)}");
                }

                var store = await _storeContext.GetStoreById(storeId);
                if (store == null)
                {
                    return NotFound("店铺不存在");
                }

                store.STORE_STATUS = newStatus;
                await _storeContext.SaveChangesAsync();

                _logger.LogInformation("成功更新店铺 {StoreId} 状态为 {NewStatus}", storeId, newStatus);
                return Ok(new { message = "店铺状态更新成功", storeId, newStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新店铺状态时发生错误：{StoreId}", storeId);
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 生成随机密码的私有方法
        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // 验证营业执照有效期的辅助方法（基于租户类型和联系方式字段）
        private bool ValidateLicenseExpiry(string storeType, string contactInfo)
        {
            // 这里可以根据具体的业务逻辑来验证营业执照有效期
            // 例如：企业连锁可能需要更严格的验证
            // 目前简单验证联系方式格式
            
            if (storeType == "企业连锁")
            {
                // 企业连锁需要更完整的联系方式信息
                return !string.IsNullOrWhiteSpace(contactInfo) && contactInfo.Length >= 10;
            }
            
            return !string.IsNullOrWhiteSpace(contactInfo);
        }
    }
}
