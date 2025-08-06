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

        // 新增店面区域的DTO类
        public class CreateRetailAreaDto
        {
            [Required(ErrorMessage = "区域ID为必填项")]
            public int AreaId { get; set; }

            [Required(ErrorMessage = "区域面积为必填项")]
            [Range(1, int.MaxValue, ErrorMessage = "区域面积必须大于0")]
            public int AreaSize { get; set; }

            [Required(ErrorMessage = "基础租金为必填项")]
            [Range(0.01, double.MaxValue, ErrorMessage = "基础租金必须大于0")]
            public double BaseRent { get; set; }

            [Required(ErrorMessage = "操作员账号为必填项")]
            public string OperatorAccount { get; set; } = string.Empty;
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

        // 商户信息管理的DTO类
        public class UpdateMerchantInfoDto
        {
            [Required(ErrorMessage = "店铺ID为必填项")]
            public int StoreId { get; set; }

            // 商户可修改的非核心信息
            [StringLength(50, ErrorMessage = "联系方式长度不能超过50个字符")]
            public string? ContactInfo { get; set; }

            [StringLength(500, ErrorMessage = "店铺简介长度不能超过500个字符")]
            public string? Description { get; set; }

            // 管理员可修改的核心信息
            [StringLength(50, ErrorMessage = "租户类型长度不能超过50个字符")]
            public string? StoreType { get; set; }

            public DateTime? RentStart { get; set; }

            public DateTime? RentEnd { get; set; }

            [StringLength(20, ErrorMessage = "店铺状态长度不能超过20个字符")]
            public string? StoreStatus { get; set; }

            [StringLength(100, ErrorMessage = "店铺名称长度不能超过100个字符")]
            public string? StoreName { get; set; }

            [Required(ErrorMessage = "操作员账号为必填项")]
            public string OperatorAccount { get; set; } = string.Empty;
        }

        // 新增店面区域接口
        [HttpPost("CreateRetailArea")]
        public async Task<IActionResult> CreateRetailArea([FromBody] CreateRetailAreaDto dto)
        {
            _logger.LogInformation("开始新增店面区域：{AreaId}", dto.AreaId);

            // 检查模型验证
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                _logger.LogWarning("新增店面模型验证失败：{Errors}", string.Join(", ", errors));
                return BadRequest(new { error = "输入数据验证失败", details = errors });
            }

            try
            {
                // 1. 验证操作员权限（管理员权限）
                if (!string.IsNullOrEmpty(dto.OperatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(dto.OperatorAccount, 1);
                    if (!hasPermission)
                    {
                        _logger.LogWarning("操作员 {OperatorAccount} 权限不足", dto.OperatorAccount);
                        return BadRequest(new { error = "操作员权限不足，需要管理员权限" });
                    }
                }

                // 2. 校验区域ID唯一性
                var existingArea = await _storeContext.AREA.FirstOrDefaultAsync(a => a.AREA_ID == dto.AreaId);
                if (existingArea != null)
                {
                    _logger.LogWarning("区域ID {AreaId} 已存在", dto.AreaId);
                    return BadRequest(new { error = "该区域ID已存在，请重新设置" });
                }

                // 3. 创建基础区域记录
                var area = new Area
                {
                    AREA_ID = dto.AreaId,
                    ISEMPTY = 1, // 新建区域默认为空置状态
                    AREA_SIZE = dto.AreaSize
                };

                _storeContext.AREA.Add(area);
                await _storeContext.SaveChangesAsync();

                // 4. 创建零售区域记录
                var retailArea = new RetailArea
                {
                    AREA_ID = dto.AreaId,
                    RENT_STATUS = "空置", // 新建店面默认为空置状态
                    BASE_RENT = dto.BaseRent
                };

                _storeContext.RETAIL_AREA.Add(retailArea);
                await _storeContext.SaveChangesAsync();

                _logger.LogInformation("成功创建店面区域：{AreaId}，面积：{AreaSize}，租金：{BaseRent}", 
                    dto.AreaId, dto.AreaSize, dto.BaseRent);

                // 返回创建成功的信息
                return Ok(new
                {
                    message = "店面区域创建成功",
                    areaId = dto.AreaId,
                    areaSize = dto.AreaSize,
                    baseRent = dto.BaseRent,
                    rentStatus = "空置",
                    isEmpty = 1  // 返回数值以保持与GetAvailableAreas的一致性
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建店面区域时发生错误：{AreaId}", dto.AreaId);
                
                // 检查是否是主键冲突错误
                if (ex.InnerException?.Message?.Contains("ORA-00001") == true || 
                    ex.InnerException?.Message?.Contains("UNIQUE") == true)
                {
                    return BadRequest(new { error = "该区域ID已存在，请重新设置" });
                }
                
                return StatusCode(500, new { 
                    error = "服务器内部错误，创建店面区域失败",
                    details = ex.Message
                });
            }
        }

        // 新增商户接口
        [HttpPost("CreateMerchant")]
        public async Task<IActionResult> CreateMerchant([FromBody] CreateMerchantDto dto)
        {
            _logger.LogInformation("开始创建新商户：{StoreName}", dto.StoreName);

            // 检查模型验证
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                _logger.LogWarning("创建商户模型验证失败：{Errors}", string.Join(", ", errors));
                return BadRequest(new { error = "输入数据验证失败", details = errors });
            }

            try
            {
                // 1. 验证操作员权限（管理员权限）
                if (!string.IsNullOrEmpty(dto.OperatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(dto.OperatorAccount, 1);
                    if (!hasPermission)
                    {
                        _logger.LogWarning("操作员 {OperatorAccount} 权限不足", dto.OperatorAccount);
                        return BadRequest(new { error = "操作员权限不足，需要管理员权限" });
                    }
                }

                // 2. 验证租用时间的合理性
                if (dto.RentEnd <= dto.RentStart)
                {
                    return BadRequest(new { error = "租用结束时间必须晚于起始时间" });
                }

                if (dto.RentStart.Date < DateTime.Today)
                {
                    return BadRequest(new { error = "租用起始时间不能早于今天" });
                }

                // 3. 检查店面是否为空置状态
                var isAreaAvailable = await _storeContext.IsAreaAvailable(dto.AreaId);
                if (!isAreaAvailable)
                {
                    _logger.LogWarning("店面 {AreaId} 不是空置状态", dto.AreaId);
                    return BadRequest(new { error = "该店面已租用，请选择其他店面" });
                }

                // 4. 验证租户信息是否重复
                var tenantExists = await _storeContext.TenantExists(dto.TenantName, dto.ContactInfo);
                if (tenantExists)
                {
                    _logger.LogWarning("租户 {TenantName} 或联系方式 {ContactInfo} 已存在", dto.TenantName, dto.ContactInfo);
                    return BadRequest(new { error = "该租户已在本综合体有店铺" });
                }

                // 开始数据库事务 - 使用分布式事务或者改为不使用事务
                // using var transaction = await _storeContext.Database.BeginTransactionAsync();
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

                    _logger.LogInformation("成功创建商户账号：{AccountName}", accountName);

                    // 9. 创建店铺账号关联记录
                    _logger.LogInformation("开始步骤9：创建店铺账号关联记录");
                    try
                    {
                        _logger.LogInformation("准备执行SQL插入STORE_ACCOUNT，账号：{AccountName}，店铺ID：{StoreId}", accountName, storeId);
                        
                        // 使用正确的Oracle SQL语法
                        var sql = "INSERT INTO STORE_ACCOUNT (ACCOUNT, STORE_ID) VALUES (:account, :storeId)";
                        _logger.LogInformation("执行SQL：{Sql}，参数：账号={AccountName}，店铺ID={StoreId}", sql, accountName, storeId);
                        
                        // 使用FormattableString或直接传参数
                        var formattableSql = $"INSERT INTO STORE_ACCOUNT (ACCOUNT, STORE_ID) VALUES ('{accountName}', {storeId})";
                        await _accountContext.Database.ExecuteSqlRawAsync(formattableSql);
                        _logger.LogInformation("SQL执行完成");

                        _logger.LogInformation("成功创建店铺账号关联：{AccountName} -> {StoreId}", accountName, storeId);
                    }
                    catch (Exception storeAccountEx)
                    {
                        _logger.LogError(storeAccountEx, "创建店铺账号关联时发生错误：{AccountName} -> {StoreId}，异常详情：{ExceptionDetails}", 
                            accountName, storeId, storeAccountEx.ToString());
                        
                        // 尝试替代方案：直接使用EF Core实体
                        _logger.LogInformation("尝试使用EF Core实体方式插入");
                        try
                        {
                            var storeAccount = new StoreAccount
                            {
                                ACCOUNT = accountName,
                                STORE_ID = storeId
                            };
                            
                            _accountContext.Entry(storeAccount).State = EntityState.Added;
                            await _accountContext.SaveChangesAsync();
                            _logger.LogInformation("使用EF Core实体方式成功创建店铺账号关联");
                        }
                        catch (Exception efEx)
                        {
                            _logger.LogError(efEx, "使用EF Core实体方式也失败：{EfException}", efEx.ToString());
                            throw;
                        }
                    }

                    _logger.LogInformation("所有步骤完成，无需事务提交");

                    _logger.LogInformation("商户创建完成：{StoreName}，店铺ID：{StoreId}，账号：{Account}", 
                        dto.StoreName, storeId, accountName);

                    _logger.LogInformation("开始步骤11：构造返回结果");
                    // 返回创建成功的信息
                    var result = new
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
                    };
                    
                    _logger.LogInformation("准备返回结果：{Result}", System.Text.Json.JsonSerializer.Serialize(result));
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "创建商户过程中发生异常。商户名：{StoreName}，异常类型：{ExceptionType}，异常消息：{ExceptionMessage}，堆栈跟踪：{StackTrace}", 
                        dto.StoreName, ex.GetType().Name, ex.Message, ex.StackTrace);
                    
                    // 无事务，所以不需要回滚
                    _logger.LogInformation("由于没有使用事务，无需回滚操作");
                    
                    throw; // 重新抛出异常，让外层catch处理
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建商户时发生错误：{StoreName}，异常类型：{ExceptionType}，错误详情：{ExceptionMessage}，内部异常：{InnerException}，堆栈跟踪：{StackTrace}", 
                    dto.StoreName, ex.GetType().Name, ex.Message, ex.InnerException?.Message ?? "无", ex.StackTrace);
                
                return StatusCode(500, new { 
                    error = "服务器内部错误，创建商户失败",
                    details = ex.Message,
                    innerException = ex.InnerException?.Message,
                    exceptionType = ex.GetType().Name,
                    storeName = dto.StoreName
                });
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
                        return BadRequest(new { error = "权限不足，需要管理员或部门经理权限" });
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
                    return NotFound(new { error = "店铺不存在" });
                }

                return Ok(store);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取店铺信息时发生错误：{StoreId}", storeId);
                return StatusCode(500, new { error = "服务器内部错误" });
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
                        return BadRequest(new { error = "权限不足，需要管理员或部门经理权限" });
                    }
                }

                // 验证状态值
                var validStatuses = new[] { "正常营业", "歇业中", "翻新中" };
                if (!validStatuses.Contains(newStatus))
                {
                    return BadRequest(new { error = $"无效的状态值。有效值为：{string.Join(", ", validStatuses)}" });
                }

                var store = await _storeContext.GetStoreById(storeId);
                if (store == null)
                {
                    return NotFound(new { error = "店铺不存在" });
                }

                store.STORE_STATUS = newStatus;
                await _storeContext.SaveChangesAsync();

                _logger.LogInformation("成功更新店铺 {StoreId} 状态为 {NewStatus}", storeId, newStatus);
                return Ok(new { message = "店铺状态更新成功", storeId, newStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新店铺状态时发生错误：{StoreId}", storeId);
                return StatusCode(500, new { error = "服务器内部错误" });
            }
        }

        // 商户信息管理 - 获取商户信息用于编辑
        [HttpGet("GetMerchantInfo/{storeId}")]
        public async Task<IActionResult> GetMerchantInfo(int storeId, [FromQuery] string operatorAccount)
        {
            try
            {
                // 验证操作员权限或商户身份
                var operator_account = await _accountContext.FindAccount(operatorAccount);
                if (operator_account == null)
                {
                    return BadRequest(new { error = "操作员账号不存在" });
                }

                // 检查是否是管理员或对应的商户
                bool isAdmin = operator_account.AUTHORITY <= 2; // 管理员或部门经理
                bool isMerchant = false;

                if (!isAdmin)
                {
                    // 检查是否是对应的商户
                    var storeAccount = await _accountContext.CheckStore(operatorAccount);
                    isMerchant = storeAccount != null && storeAccount.STORE_ID == storeId;
                }

                if (!isAdmin && !isMerchant)
                {
                    return BadRequest(new { error = "无权限访问该商户信息" });
                }

                var store = await _storeContext.GetStoreById(storeId);
                if (store == null)
                {
                    return NotFound(new { error = "店铺不存在" });
                }

                // 根据权限返回不同的信息
                var merchantInfo = new
                {
                    storeId = store.STORE_ID,
                    storeName = store.STORE_NAME,
                    storeStatus = store.STORE_STATUS,
                    storeType = store.STORE_TYPE,
                    tenantName = store.TENANT_NAME,
                    contactInfo = store.CONTACT_INFO,
                    rentStart = store.RENT_START,
                    rentEnd = store.RENT_END,
                    canModifyCore = isAdmin, // 是否可以修改核心信息
                    canModifyNonCore = true // 都可以修改非核心信息
                };

                return Ok(merchantInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取商户信息时发生错误：{StoreId}", storeId);
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 商户信息管理 - 更新商户信息
        [HttpPut("UpdateMerchantInfo")]
        public async Task<IActionResult> UpdateMerchantInfo([FromBody] UpdateMerchantInfoDto dto)
        {
            _logger.LogInformation("开始更新商户信息：{StoreId}", dto.StoreId);

            try
            {
                // 1. 验证操作员权限
                var operator_account = await _accountContext.FindAccount(dto.OperatorAccount);
                if (operator_account == null)
                {
                    return BadRequest(new { error = "操作员账号不存在" });
                }

                // 检查是否是管理员或对应的商户
                bool isAdmin = operator_account.AUTHORITY <= 2; // 管理员或部门经理
                bool isMerchant = false;

                if (!isAdmin)
                {
                    var storeAccount = await _accountContext.CheckStore(dto.OperatorAccount);
                    isMerchant = storeAccount != null && storeAccount.STORE_ID == dto.StoreId;
                }

                if (!isAdmin && !isMerchant)
                {
                    return BadRequest(new { error = "无权限修改该商户信息" });
                }

                // 2. 验证商户状态
                var store = await _storeContext.GetStoreById(dto.StoreId);
                if (store == null)
                {
                    return NotFound(new { error = "店铺不存在" });
                }

                if (store.STORE_STATUS != "正常营业" && store.STORE_STATUS != "歇业中")
                {
                    return BadRequest(new { error = "当前商户状态不允许修改信息" });
                }

                // 3. 检查修改权限并应用更改
                bool hasChanges = false;

                // 商户可修改的非核心信息
                if (!string.IsNullOrEmpty(dto.ContactInfo))
                {
                    store.CONTACT_INFO = dto.ContactInfo;
                    hasChanges = true;
                }

                // 管理员可修改的核心信息
                if (isAdmin)
                {
                    if (!string.IsNullOrEmpty(dto.StoreType))
                    {
                        store.STORE_TYPE = dto.StoreType;
                        hasChanges = true;
                    }

                    if (!string.IsNullOrEmpty(dto.StoreName))
                    {
                        store.STORE_NAME = dto.StoreName;
                        hasChanges = true;
                    }

                    if (!string.IsNullOrEmpty(dto.StoreStatus))
                    {
                        var validStatuses = new[] { "正常营业", "歇业中", "翻新中" };
                        if (!validStatuses.Contains(dto.StoreStatus))
                        {
                            return BadRequest(new { error = $"无效的店铺状态。有效值为：{string.Join(", ", validStatuses)}" });
                        }
                        store.STORE_STATUS = dto.StoreStatus;
                        hasChanges = true;
                    }

                    if (dto.RentStart.HasValue)
                    {
                        if (dto.RentEnd.HasValue && dto.RentStart.Value >= dto.RentEnd.Value)
                        {
                            return BadRequest(new { error = "租用起始时间必须早于结束时间" });
                        }
                        store.RENT_START = dto.RentStart.Value;
                        hasChanges = true;
                    }

                    if (dto.RentEnd.HasValue)
                    {
                        if (dto.RentEnd.Value <= store.RENT_START)
                        {
                            return BadRequest(new { error = "租用结束时间必须晚于起始时间" });
                        }
                        store.RENT_END = dto.RentEnd.Value;
                        hasChanges = true;
                    }
                }
                else
                {
                    // 商户试图修改核心信息
                    if (!string.IsNullOrEmpty(dto.StoreType) || !string.IsNullOrEmpty(dto.StoreStatus) ||
                        !string.IsNullOrEmpty(dto.StoreName) || dto.RentStart.HasValue || dto.RentEnd.HasValue)
                    {
                        return BadRequest(new { error = "无权限修改核心信息，需联系管理人员" });
                    }
                }

                if (!hasChanges)
                {
                    return BadRequest(new { error = "没有需要更新的信息" });
                }

                // 4. 保存更改
                await _storeContext.SaveChangesAsync();

                _logger.LogInformation("成功更新商户 {StoreId} 信息", dto.StoreId);

                return Ok(new
                {
                    message = "商户信息更新成功",
                    storeId = dto.StoreId,
                    updatedBy = isAdmin ? "管理员" : "商户",
                    changesCount = hasChanges ? 1 : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新商户信息时发生错误：{StoreId}", dto.StoreId);
                
                // 检查是否是数据库字段长度限制错误
                if (ex.InnerException?.Message?.Contains("ORA-12899") == true)
                {
                    return BadRequest(new { error = "输入的数据长度超过了字段限制，请检查联系方式等字段长度" });
                }
                
                return StatusCode(500, new { error = "服务器内部错误，请稍后重试" });
            }
        }

        // 获取商户可修改的字段信息
        [HttpGet("GetEditableFields/{storeId}")]
        public async Task<IActionResult> GetEditableFields(int storeId, [FromQuery] string operatorAccount)
        {
            try
            {
                var operator_account = await _accountContext.FindAccount(operatorAccount);
                if (operator_account == null)
                {
                    return BadRequest(new { error = "操作员账号不存在" });
                }

                bool isAdmin = operator_account.AUTHORITY <= 2;
                bool isMerchant = false;

                if (!isAdmin)
                {
                    var storeAccount = await _accountContext.CheckStore(operatorAccount);
                    isMerchant = storeAccount != null && storeAccount.STORE_ID == storeId;
                }

                if (!isAdmin && !isMerchant)
                {
                    return BadRequest(new { error = "无权限访问该商户信息" });
                }

                var editableFields = new
                {
                    // 商户可编辑的非核心字段
                    nonCoreFields = new[] { "contactInfo", "description" },
                    
                    // 管理员可编辑的核心字段
                    coreFields = isAdmin ? new[] { "storeType", "rentStart", "rentEnd", "storeStatus", "storeName" } : new string[0],
                    
                    // 权限信息
                    permissions = new
                    {
                        canModifyCore = isAdmin,
                        canModifyNonCore = true,
                        role = isAdmin ? "管理员" : "商户"
                    }
                };

                return Ok(editableFields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可编辑字段时发生错误：{StoreId}", storeId);
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

        #region 店面状态管理功能

        // DTO类定义
        public class StoreStatusChangeRequestDto
        {
            [Required(ErrorMessage = "店铺ID为必填项")]
            public int StoreId { get; set; }
            
            [Required(ErrorMessage = "变更类型为必填项")]
            public string ChangeType { get; set; } // 退租/维修/暂停营业/恢复营业
            
            [Required(ErrorMessage = "申请原因为必填项")]
            public string Reason { get; set; }
            
            [Required(ErrorMessage = "目标状态为必填项")]
            public string TargetStatus { get; set; } // 目标状态
            
            [Required(ErrorMessage = "申请人账号为必填项")]
            public string ApplicantAccount { get; set; }
        }

        public class StoreStatusApprovalDto
        {
            public int StoreId { get; set; }
            public string ApprovalAction { get; set; } // 通过/驳回
            public string ApprovalComment { get; set; } // 审批意见（可选）
            public string ApproverAccount { get; set; }
            public string TargetStatus { get; set; } // 目标状态（可选，系统可以自动确定）
        }

        // 简化的测试审批DTO（专门用于测试界面）
        public class TestApprovalDto
        {
            public int StoreId { get; set; }
            public string ApprovalAction { get; set; }
            public string ApprovalComment { get; set; }
            public string ApproverAccount { get; set; }
            public string TargetStatus { get; set; }
        }

        // 商户提交状态变更申请
        [HttpPost("StatusChangeRequest")]
        public async Task<IActionResult> SubmitStatusChangeRequest([FromBody] StoreStatusChangeRequestDto dto)
        {
            try
            {
                _logger.LogInformation("开始处理店面状态变更申请：店铺ID {StoreId}, 变更类型 {ChangeType}", 
                    dto.StoreId, dto.ChangeType);

                // 验证店面是否存在
                var store = await _storeContext.GetStoreById(dto.StoreId);
                if (store == null)
                {
                    return BadRequest(new { error = "店面不存在" });
                }

                // 验证前置条件：店面必须有有效租用记录且状态为正常营业
                if (store.STORE_STATUS != "正常营业")
                {
                    return BadRequest(new { error = "只有正常营业状态的店面才能申请状态变更" });
                }

                // 验证是否有租用记录
                var rentRecord = await _storeContext.RENT_STORE.FirstOrDefaultAsync(rs => rs.STORE_ID == dto.StoreId);
                if (rentRecord == null)
                {
                    return BadRequest(new { error = "店面没有有效的租用记录" });
                }

                // 生成申请编号
                var applicationNo = GenerateApplicationNumber();

                // 模拟申请记录（因为不能新增表，我们用日志记录申请信息）
                _logger.LogInformation("状态变更申请详情：申请编号 {ApplicationNo}, 店铺 {StoreName}, " +
                    "申请类型 {ChangeType}, 申请原因 {Reason}, 目标状态 {TargetStatus}, 申请人 {Applicant}", 
                    applicationNo, store.STORE_NAME, dto.ChangeType, dto.Reason, dto.TargetStatus, dto.ApplicantAccount);

                return Ok(new
                {
                    message = "状态变更申请提交成功，待物业管理人员审批",
                    applicationNo = applicationNo,
                    storeId = dto.StoreId,
                    storeName = store.STORE_NAME,
                    changeType = dto.ChangeType,
                    targetStatus = dto.TargetStatus,
                    status = "待审批"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交状态变更申请时发生错误：{StoreId}", dto.StoreId);
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 物业管理人员审批状态变更申请
        [HttpPost("ApproveStatusChange")]
        public async Task<IActionResult> ApproveStatusChange([FromBody] StoreStatusApprovalDto dto)
        {
            try
            {
                _logger.LogInformation("开始处理状态变更审批：店铺ID {StoreId}, 审批动作 {ApprovalAction}", 
                    dto.StoreId, dto.ApprovalAction);

                // 基本验证
                if (dto.StoreId <= 0)
                {
                    return BadRequest(new { error = "店铺ID无效" });
                }

                if (string.IsNullOrEmpty(dto.ApprovalAction))
                {
                    return BadRequest(new { error = "审批动作不能为空" });
                }

                if (string.IsNullOrEmpty(dto.ApproverAccount))
                {
                    return BadRequest(new { error = "审批人账号不能为空" });
                }

                // 验证审批人权限（需要管理员或部门经理权限）
                var approverAccount = await _accountContext.FindAccount(dto.ApproverAccount);
                if (approverAccount == null)
                {
                    return BadRequest(new { error = $"审批人账号 '{dto.ApproverAccount}' 不存在" });
                }

                var hasPermission = await _accountContext.CheckAuthority(dto.ApproverAccount, 2);
                if (!hasPermission)
                {
                    return BadRequest(new { error = "权限不足，需要管理员或部门经理权限进行审批" });
                }

                // 验证店面是否存在
                var store = await _storeContext.GetStoreById(dto.StoreId);
                if (store == null)
                {
                    return BadRequest(new { error = "店面不存在" });
                }

                var originalStatus = store.STORE_STATUS;

                if (dto.ApprovalAction == "通过")
                {
                    // 确定目标状态 - 如果没有明确指定，则根据当前状态进行合理推测
                    string targetStatus = dto.TargetStatus;
                    if (string.IsNullOrEmpty(targetStatus))
                    {
                        // 根据当前状态和业务逻辑确定默认的目标状态
                        targetStatus = DetermineDefaultTargetStatus(originalStatus);
                    }

                    // 审批通过：更新店面状态
                    await UpdateStoreStatusInternal(dto.StoreId, targetStatus, dto.ApproverAccount);
                    
                    _logger.LogInformation("状态变更审批通过：店铺 {StoreName} 状态从 {OriginalStatus} 变更为 {TargetStatus}, " +
                        "审批人 {Approver}, 审批意见 {Comment}", 
                        store.STORE_NAME, originalStatus, targetStatus, dto.ApproverAccount, dto.ApprovalComment);

                    return Ok(new
                    {
                        message = "审批通过，店面状态已更新",
                        storeId = dto.StoreId,
                        storeName = store.STORE_NAME,
                        originalStatus = originalStatus,
                        newStatus = targetStatus,
                        approvalResult = "通过",
                        approver = dto.ApproverAccount,
                        approvalTime = DateTime.Now
                    });
                }
                else
                {
                    // 审批驳回
                    _logger.LogInformation("状态变更审批驳回：店铺 {StoreName}, 审批人 {Approver}, 驳回原因 {Comment}", 
                        store.STORE_NAME, dto.ApproverAccount, dto.ApprovalComment);

                    return Ok(new
                    {
                        message = "审批驳回，商户可补充材料重新申请",
                        storeId = dto.StoreId,
                        storeName = store.STORE_NAME,
                        approvalResult = "驳回",
                        reason = dto.ApprovalComment,
                        approver = dto.ApproverAccount,
                        approvalTime = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理状态变更审批时发生错误：{StoreId}", dto.StoreId);
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 简化的测试审批接口（仅用于测试目的）
        [HttpPost("TestApproveStatusChange")]
        public async Task<IActionResult> TestApproveStatusChange([FromBody] TestApprovalDto dto)
        {
            try
            {
                _logger.LogInformation("测试审批：店铺ID {StoreId}, 审批动作 {ApprovalAction}", 
                    dto.StoreId, dto.ApprovalAction);

                // 验证审批人权限
                var hasPermission = await _accountContext.CheckAuthority(dto.ApproverAccount, 2);
                if (!hasPermission)
                {
                    return BadRequest(new { error = "权限不足，需要管理员或部门经理权限进行审批" });
                }

                // 验证店面是否存在
                var store = await _storeContext.GetStoreById(dto.StoreId);
                if (store == null)
                {
                    return BadRequest(new { error = "店面不存在" });
                }

                var originalStatus = store.STORE_STATUS;

                if (dto.ApprovalAction == "通过")
                {
                    // 使用指定的目标状态，如果没有则使用默认状态转换
                    string targetStatus = string.IsNullOrEmpty(dto.TargetStatus) ? 
                        DetermineDefaultTargetStatus(originalStatus) : dto.TargetStatus;

                    await UpdateStoreStatusInternal(dto.StoreId, targetStatus, dto.ApproverAccount);
                    
                    return Ok(new
                    {
                        message = "测试审批通过，店面状态已更新",
                        storeId = dto.StoreId,
                        storeName = store.STORE_NAME,
                        originalStatus = originalStatus,
                        newStatus = targetStatus,
                        approvalResult = "通过",
                        approver = dto.ApproverAccount,
                        approvalTime = DateTime.Now
                    });
                }
                else
                {
                    return Ok(new
                    {
                        message = "测试审批驳回",
                        storeId = dto.StoreId,
                        storeName = store.STORE_NAME,
                        approvalResult = "驳回",
                        reason = dto.ApprovalComment ?? "测试驳回",
                        approver = dto.ApproverAccount,
                        approvalTime = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理测试审批时发生错误：{StoreId}", dto.StoreId);
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 查询店面当前状态
        [HttpGet("StoreStatus/{storeId}")]
        public async Task<IActionResult> GetStoreStatus(int storeId)
        {
            try
            {
                var store = await _storeContext.GetStoreById(storeId);
                if (store == null)
                {
                    return BadRequest(new { error = "店面不存在" });
                }

                var rentRecord = await _storeContext.RENT_STORE
                    .FirstOrDefaultAsync(rs => rs.STORE_ID == storeId);

                return Ok(new
                {
                    storeId = store.STORE_ID,
                    storeName = store.STORE_NAME,
                    currentStatus = store.STORE_STATUS,
                    tenantName = store.TENANT_NAME,
                    hasRentRecord = rentRecord != null,
                    canApplyStatusChange = store.STORE_STATUS == "正常营业" && rentRecord != null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询店面状态时发生错误：{StoreId}", storeId);
                return StatusCode(500, "服务器内部错误");
            }
        }

        // 辅助方法：生成申请编号
        private string GenerateApplicationNumber()
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random();
            var sequence = random.Next(1, 1000).ToString("D3");
            return $"SA{today}{sequence}";
        }

        // 辅助方法：更新店面状态
        private async Task UpdateStoreStatusInternal(int storeId, string targetStatus, string approver)
        {
            var store = await _storeContext.STORE.FirstOrDefaultAsync(s => s.STORE_ID == storeId);
            if (store != null)
            {
                // 直接使用目标状态
                _logger.LogInformation("将店铺 {StoreId} 状态从 {CurrentStatus} 更新为 {TargetStatus}", 
                    storeId, store.STORE_STATUS, targetStatus);
                
                store.STORE_STATUS = targetStatus;
                
                await _storeContext.SaveChangesAsync();
                
                _logger.LogInformation("店面状态已更新：店铺ID {StoreId}, 新状态 {NewStatus}, 操作人 {Approver}", 
                    storeId, targetStatus, approver);
            }
        }

        // 辅助方法：根据当前状态确定默认目标状态（用于测试场景）
        private string DetermineDefaultTargetStatus(string currentStatus)
        {
            // 为测试场景提供默认的状态转换逻辑
            switch (currentStatus)
            {
                case "正常营业":
                    return "维修中"; // 默认申请维修
                case "维修中":
                    return "正常营业"; // 维修完成恢复营业
                case "暂停营业":
                    return "正常营业"; // 恢复营业
                default:
                    return "正常营业"; // 默认恢复到正常营业状态
            }
        }

        #region 商户信息统计报表功能 (用例2.7.5)

        /// <summary>
        /// 获取商户信息统计报表
        /// </summary>
        /// <param name="operatorAccount">操作员账号</param>
        /// <param name="dimension">统计维度：type(按类型)/area(按区域)/status(按状态)/all(全部)</param>
        /// <returns>统计报表数据</returns>
        [HttpGet("MerchantStatisticsReport")]
        public async Task<IActionResult> GetMerchantStatisticsReport([FromQuery] string operatorAccount, [FromQuery] string dimension = "all")
        {
            try
            {
                _logger.LogInformation("开始生成商户统计报表：操作员 {OperatorAccount}, 统计维度 {Dimension}", operatorAccount, dimension);

                // 验证操作员权限（需要管理员权限）
                _logger.LogInformation("正在验证权限：操作员账号 '{OperatorAccount}'", operatorAccount);
                
                var operatorAccount_obj = await _accountContext.FindAccount(operatorAccount);
                if (operatorAccount_obj == null)
                {
                    _logger.LogWarning("账号不存在：'{OperatorAccount}'", operatorAccount);
                    return BadRequest(new { error = $"账号 '{operatorAccount}' 不存在" });
                }
                
                _logger.LogInformation("找到账号：'{Account}', 权限等级：{Authority}", 
                    operatorAccount_obj.ACCOUNT, operatorAccount_obj.AUTHORITY);
                
                var hasPermission = await _accountContext.CheckAuthority(operatorAccount, 2);
                if (!hasPermission)
                {
                    _logger.LogWarning("权限验证失败：账号 '{Account}', 权限等级：{Authority}，要求权限等级 ≤ 2", 
                        operatorAccount_obj.ACCOUNT, operatorAccount_obj.AUTHORITY);
                    return BadRequest(new { error = $"权限不足，需要管理员权限查看统计报表。当前权限等级：{operatorAccount_obj.AUTHORITY}，要求：≤ 2" });
                }
                
                _logger.LogInformation("权限验证成功：账号 '{Account}', 权限等级：{Authority}", 
                    operatorAccount_obj.ACCOUNT, operatorAccount_obj.AUTHORITY);

                var report = new
                {
                    reportTitle = "商户信息统计报表",
                    generateTime = DateTime.Now,
                    operatorAccount = operatorAccount,
                    dimension = dimension,
                    data = await GenerateReportData(dimension)
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成商户统计报表时发生异常");
                return StatusCode(500, new { error = "生成报表失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 获取基础统计数据（无权限验证，用于仪表板）
        /// </summary>
        [HttpGet("BasicStatistics")]
        public async Task<IActionResult> GetBasicStatistics()
        {
            try
            {
                _logger.LogInformation("获取基础统计数据");

                var stats = await _storeContext.GetBasicStatistics();
                
                return Ok(new
                {
                    totalStores = stats.TotalStores,
                    activeStores = stats.ActiveStores,
                    vacantAreas = stats.VacantAreas,
                    totalAreas = stats.TotalAreas,
                    occupancyRate = stats.OccupancyRate,
                    averageRent = stats.AverageRent
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取基础统计数据时发生异常");
                return StatusCode(500, new { error = "获取统计数据失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 生成报表数据
        /// </summary>
        private async Task<object> GenerateReportData(string dimension)
        {
            switch (dimension.ToLower())
            {
                case "type":
                    return await GenerateByTypeReport();
                case "area":
                    return await GenerateByAreaReport();
                case "status":
                    return await GenerateByStatusReport();
                case "all":
                default:
                    return await GenerateCompleteReport();
            }
        }

        /// <summary>
        /// 按租户类型生成报表
        /// </summary>
        private async Task<object> GenerateByTypeReport()
        {
            var typeStats = await _storeContext.GetStoreStatisticsByType();
            
            return new
            {
                title = "按租户类型统计",
                summary = new
                {
                    totalTypes = typeStats.Count,
                    totalStores = typeStats.Sum(t => t.StoreCount),
                    totalRevenue = typeStats.Sum(t => t.TotalRent)
                },
                details = typeStats.Select(t => new
                {
                    storeType = t.StoreType,
                    storeCount = t.StoreCount,
                    percentage = Math.Round((double)t.StoreCount / typeStats.Sum(x => x.StoreCount) * 100, 2),
                    totalRent = t.TotalRent,
                    averageRent = Math.Round(t.TotalRent / (t.StoreCount > 0 ? t.StoreCount : 1), 2),
                    activeStores = t.ActiveStores,
                    inactiveStores = t.StoreCount - t.ActiveStores
                }).ToList()
            };
        }

        /// <summary>
        /// 按店面区域生成报表
        /// </summary>
        private async Task<object> GenerateByAreaReport()
        {
            var areaStats = await _storeContext.GetStoreStatisticsByArea();
            
            return new
            {
                title = "按店面区域统计",
                summary = new
                {
                    totalAreas = areaStats.Count,
                    occupiedAreas = areaStats.Count(a => a.IsOccupied),
                    vacantAreas = areaStats.Count(a => !a.IsOccupied),
                    occupancyRate = Math.Round((double)areaStats.Count(a => a.IsOccupied) / areaStats.Count * 100, 2)
                },
                details = areaStats.Select(a => new
                {
                    areaId = a.AreaId,
                    areaSize = a.AreaSize,
                    baseRent = a.BaseRent,
                    rentStatus = a.RentStatus,
                    isOccupied = a.IsOccupied,
                    storeName = a.StoreName,
                    tenantName = a.TenantName,
                    storeType = a.StoreType,
                    rentStart = a.RentStart?.ToString("yyyy-MM-dd"),
                    rentEnd = a.RentEnd?.ToString("yyyy-MM-dd"),
                    rentDuration = a.RentStart.HasValue && a.RentEnd.HasValue 
                        ? (int)(a.RentEnd.Value - a.RentStart.Value).TotalDays 
                        : 0
                }).OrderBy(a => a.areaId).ToList()
            };
        }

        /// <summary>
        /// 按店面状态生成报表
        /// </summary>
        private async Task<object> GenerateByStatusReport()
        {
            var statusStats = await _storeContext.GetStoreStatisticsByStatus();
            
            return new
            {
                title = "按店面状态统计",
                summary = new
                {
                    totalStatuses = statusStats.Count,
                    totalStores = statusStats.Sum(s => s.StoreCount)
                },
                details = statusStats.Select(s => new
                {
                    storeStatus = s.StoreStatus,
                    storeCount = s.StoreCount,
                    percentage = Math.Round((double)s.StoreCount / statusStats.Sum(x => x.StoreCount) * 100, 2),
                    averageRent = s.AverageRent,
                    totalRent = s.TotalRent,
                    statusDescription = GetStatusDescription(s.StoreStatus)
                }).ToList()
            };
        }

        /// <summary>
        /// 生成完整报表
        /// </summary>
        private async Task<object> GenerateCompleteReport()
        {
            var basicStats = await _storeContext.GetBasicStatistics();
            var typeReport = await GenerateByTypeReport();
            var areaReport = await GenerateByAreaReport();
            var statusReport = await GenerateByStatusReport();

            return new
            {
                title = "商户信息完整统计报表",
                overview = new
                {
                    totalStores = basicStats.TotalStores,
                    activeStores = basicStats.ActiveStores,
                    inactiveStores = basicStats.TotalStores - basicStats.ActiveStores,
                    totalAreas = basicStats.TotalAreas,
                    vacantAreas = basicStats.VacantAreas,
                    occupiedAreas = basicStats.TotalAreas - basicStats.VacantAreas,
                    occupancyRate = basicStats.OccupancyRate,
                    averageRent = basicStats.AverageRent,
                    totalRevenue = await CalculateTotalRevenue()
                },
                byType = typeReport,
                byArea = areaReport,
                byStatus = statusReport
            };
        }

        /// <summary>
        /// 计算总收入
        /// </summary>
        private async Task<decimal> CalculateTotalRevenue()
        {
            return await _storeContext.CalculateTotalRevenue();
        }

        /// <summary>
        /// 获取状态描述
        /// </summary>
        private string GetStatusDescription(string status)
        {
            return status switch
            {
                "正常营业" => "店铺正常运营中",
                "暂停营业" => "店铺暂时停业",
                "维修中" => "店铺维修装修中",
                "已退租" => "租约已结束",
                "装修中" => "店铺装修中",
                _ => "其他状态"
            };
        }

        #endregion

        #region 租金收取功能 (用例2.7.6)

        /// <summary>
        /// 生成月度租金单
        /// </summary>
        [HttpPost("GenerateMonthlyRentBills")]
        public async Task<IActionResult> GenerateMonthlyRentBills([FromBody] string billPeriod)
        {
            try
            {
                _logger.LogInformation("开始生成月度租金单：账期 {BillPeriod}", billPeriod);

                if (string.IsNullOrEmpty(billPeriod) || billPeriod.Length != 6)
                {
                    return BadRequest(new { error = "账期格式错误，应为YYYYMM格式，如202412" });
                }

                var bills = await _storeContext.GenerateMonthlyRentBills(billPeriod);

                _logger.LogInformation("成功生成 {Count} 张租金单，账期：{BillPeriod}", bills.Count, billPeriod);

                return Ok(new
                {
                    message = $"成功生成{bills.Count}张租金单",
                    billPeriod = billPeriod,
                    generatedBills = bills.Count,
                    generateTime = DateTime.Now,
                    bills = bills.Select(b => new
                    {
                        storeId = b.StoreId,
                        storeName = b.StoreName,
                        totalAmount = b.TotalAmount,
                        dueDate = b.DueDate,
                        billStatus = b.BillStatus
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成月度租金单时发生异常");
                return StatusCode(500, new { error = "生成租金单失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 获取租金单列表
        /// </summary>
        [HttpPost("GetRentBills")]
        public async Task<IActionResult> GetRentBills([FromBody] RentBillQueryRequest request)
        {
            try
            {
                _logger.LogInformation("查询租金单：操作员 {OperatorAccount}", request.OperatorAccount);

                // 验证操作员账号
                var operator_account = await _accountContext.FindAccount(request.OperatorAccount);
                if (operator_account == null)
                {
                    return BadRequest(new { error = "操作员账号不存在" });
                }

                var rentBills = await _storeContext.GetRentBillsDetails(request);

                return Ok(new
                {
                    message = "查询租金单成功",
                    totalCount = rentBills.Count,
                    queryTime = DateTime.Now,
                    operatorAccount = request.OperatorAccount,
                    bills = rentBills
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询租金单时发生异常");
                return StatusCode(500, new { error = "查询租金单失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 商户缴纳租金
        /// </summary>
        [HttpPost("PayRent")]
        public async Task<IActionResult> PayRent([FromBody] PayRentRequest request)
        {
            try
            {
                _logger.LogInformation("处理租金支付：商铺ID {StoreId}, 支付方式 {PaymentMethod}", 
                    request.StoreId, request.PaymentMethod);

                var success = await _storeContext.ProcessRentPayment(request);

                if (!success)
                {
                    return BadRequest(new { error = "支付失败，商铺不存在或租金已经支付" });
                }

                _logger.LogInformation("租金支付成功：商铺ID {StoreId}", request.StoreId);

                return Ok(new
                {
                    message = "租金支付成功",
                    storeId = request.StoreId,
                    paymentMethod = request.PaymentMethod,
                    paymentTime = DateTime.Now,
                    status = "已缴纳"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理租金支付时发生异常");
                return StatusCode(500, new { error = "支付处理失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 财务确认支付
        /// </summary>
        [HttpPost("ConfirmPayment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            try
            {
                _logger.LogInformation("财务确认支付：商铺ID {StoreId}, 确认人 {ConfirmedBy}", 
                    request.StoreId, request.ConfirmedBy);

                // 验证确认人权限（需要财务权限）
                var confirmer = await _accountContext.FindAccount(request.ConfirmedBy);
                if (confirmer == null)
                {
                    return BadRequest(new { error = "确认人账号不存在" });
                }

                bool hasFinancePermission = confirmer.AUTHORITY <= 2; // 管理员或财务权限
                if (!hasFinancePermission)
                {
                    return BadRequest(new { error = "权限不足，需要财务权限确认支付" });
                }

                var success = await _storeContext.ConfirmPayment(request);

                if (!success)
                {
                    return BadRequest(new { error = "确认失败，商铺不存在或租金未缴纳" });
                }

                _logger.LogInformation("支付确认成功：商铺ID {StoreId}, 确认人 {ConfirmedBy}", 
                    request.StoreId, request.ConfirmedBy);

                return Ok(new
                {
                    message = "支付确认成功",
                    storeId = request.StoreId,
                    confirmedBy = request.ConfirmedBy,
                    confirmedTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "财务确认支付时发生异常");
                return StatusCode(500, new { error = "确认失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 更新逾期状态（系统定时任务）
        /// </summary>
        [HttpPost("UpdateOverdueStatus")]
        public async Task<IActionResult> UpdateOverdueStatus()
        {
            try
            {
                _logger.LogInformation("开始更新逾期状态");

                var updatedCount = await _storeContext.UpdateOverdueStatus();

                _logger.LogInformation("逾期状态更新完成，影响 {Count} 条记录", updatedCount);

                return Ok(new
                {
                    message = "逾期状态更新成功",
                    updatedCount = updatedCount,
                    updateTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新逾期状态时发生异常");
                return StatusCode(500, new { error = "更新逾期状态失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 获取租金收取统计
        /// </summary>
        [HttpGet("GetRentCollectionStatistics")]
        public async Task<IActionResult> GetRentCollectionStatistics([FromQuery] string period)
        {
            try
            {
                _logger.LogInformation("获取租金收取统计：账期 {Period}", period);

                if (string.IsNullOrEmpty(period))
                {
                    return BadRequest(new { error = "账期参数不能为空" });
                }

                var statistics = await _storeContext.GetRentCollectionStatistics(period);

                return Ok(new
                {
                    message = "获取租金收取统计成功",
                    period = period,
                    queryTime = DateTime.Now,
                    statistics = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取租金收取统计时发生异常");
                return StatusCode(500, new { error = "获取统计数据失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 获取商户自己的租金单（商户端接口）
        /// </summary>
        [HttpGet("GetMyRentBills")]
        public async Task<IActionResult> GetMyRentBills([FromQuery] string merchantAccount)
        {
            try
            {
                _logger.LogInformation("商户查询自己的租金单：商户账号 {MerchantAccount}", merchantAccount);

                // 验证商户账号并获取关联的店铺ID
                var storeAccount = await _accountContext.GetStoreAccountByAccount(merchantAccount);
                if (storeAccount == null)
                {
                    return BadRequest(new { error = "商户账号不存在或未关联店铺" });
                }

                var request = new RentBillQueryRequest
                {
                    StoreId = storeAccount.STORE_ID,
                    OperatorAccount = merchantAccount
                };

                var rentBills = await _storeContext.GetRentBillsDetails(request);

                return Ok(new
                {
                    message = "查询租金单成功",
                    storeId = storeAccount.STORE_ID,
                    merchantAccount = merchantAccount,
                    totalBills = rentBills.Count,
                    bills = rentBills
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "商户查询租金单时发生异常");
                return StatusCode(500, new { error = "查询租金单失败", details = ex.Message });
            }
        }

        #endregion

        #region 商户租金统计报表功能 (用例2.7.7)

        /// <summary>
        /// 生成商户租金统计报表
        /// </summary>
        /// <param name="period">统计时间段 (YYYYMM格式)</param>
        /// <param name="dimension">统计维度：time(时间)/area(区域)/all(全部)</param>
        /// <param name="operatorAccount">操作员账号</param>
        /// <returns>租金统计报表数据</returns>
        [HttpGet("RentStatisticsReport")]
        public async Task<IActionResult> GetRentStatisticsReport([FromQuery] string period, [FromQuery] string dimension = "all", [FromQuery] string operatorAccount = "")
        {
            try
            {
                _logger.LogInformation("生成商户租金统计报表：时间段 {Period}, 维度 {Dimension}, 操作员 {OperatorAccount}", period, dimension, operatorAccount);

                // 验证操作员权限（需要管理员或财务权限）
                if (!string.IsNullOrEmpty(operatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(operatorAccount, 2);
                    if (!hasPermission)
                    {
                        return BadRequest(new { error = "权限不足，需要管理员或财务权限查看租金统计报表" });
                    }
                }

                // 验证时间格式
                if (string.IsNullOrEmpty(period))
                {
                    return BadRequest(new { error = "请提供统计时间段 (YYYYMM格式)" });
                }

                if (period.Length != 6 || !int.TryParse(period, out _))
                {
                    return BadRequest(new { error = "时间格式错误，应为YYYYMM格式，如202412" });
                }

                var report = await GenerateRentStatisticsReport(period, dimension);

                return Ok(new
                {
                    message = "租金统计报表生成成功",
                    period = period,
                    dimension = dimension,
                    generateTime = DateTime.Now,
                    operatorAccount = operatorAccount,
                    report = report
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成租金统计报表时发生异常");
                return StatusCode(500, new { error = "生成报表失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 获取租金收缴明细数据
        /// </summary>
        /// <param name="period">统计时间段</param>
        /// <param name="operatorAccount">操作员账号</param>
        /// <returns>租金收缴明细</returns>
        [HttpGet("RentCollectionDetails")]
        public async Task<IActionResult> GetRentCollectionDetails([FromQuery] string period, [FromQuery] string operatorAccount = "")
        {
            try
            {
                _logger.LogInformation("获取租金收缴明细：时间段 {Period}, 操作员 {OperatorAccount}", period, operatorAccount);

                // 验证操作员权限
                if (!string.IsNullOrEmpty(operatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(operatorAccount, 2);
                    if (!hasPermission)
                    {
                        return BadRequest(new { error = "权限不足，需要管理员或财务权限" });
                    }
                }

                var details = await _storeContext.GetRentCollectionDetails(period);

                return Ok(new
                {
                    message = "获取收缴明细成功",
                    period = period,
                    totalRecords = details.Count,
                    details = details
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取租金收缴明细时发生异常");
                return StatusCode(500, new { error = "获取明细失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 获取历史收缴趋势数据
        /// </summary>
        /// <param name="startPeriod">开始时间段</param>
        /// <param name="endPeriod">结束时间段</param>
        /// <param name="operatorAccount">操作员账号</param>
        /// <returns>历史趋势数据</returns>
        [HttpGet("RentTrendAnalysis")]
        public async Task<IActionResult> GetRentTrendAnalysis([FromQuery] string startPeriod, [FromQuery] string endPeriod, [FromQuery] string operatorAccount = "")
        {
            try
            {
                _logger.LogInformation("获取租金收缴趋势：时间段 {StartPeriod} - {EndPeriod}, 操作员 {OperatorAccount}", startPeriod, endPeriod, operatorAccount);

                // 验证操作员权限
                if (!string.IsNullOrEmpty(operatorAccount))
                {
                    var hasPermission = await _accountContext.CheckAuthority(operatorAccount, 2);
                    if (!hasPermission)
                    {
                        return BadRequest(new { error = "权限不足，需要管理员或财务权限" });
                    }
                }

                var trendData = await _storeContext.GetRentTrendAnalysis(startPeriod, endPeriod);

                return Ok(new
                {
                    message = "获取趋势分析成功",
                    startPeriod = startPeriod,
                    endPeriod = endPeriod,
                    trendData = trendData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取租金收缴趋势时发生异常");
                return StatusCode(500, new { error = "获取趋势失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 生成租金统计报表数据
        /// </summary>
        private async Task<object> GenerateRentStatisticsReport(string period, string dimension)
        {
            switch (dimension.ToLower())
            {
                case "time":
                    return await GenerateTimeBasedReport(period);
                case "area":
                    return await GenerateAreaBasedReport(period);
                case "all":
                default:
                    return await GenerateComprehensiveReport(period);
            }
        }

        /// <summary>
        /// 生成基于时间的报表
        /// </summary>
        private async Task<object> GenerateTimeBasedReport(string period)
        {
            var collectionStats = await _storeContext.GetRentCollectionStatistics(period);
            var monthlyTrend = await _storeContext.GetMonthlyTrend(period);

            return new
            {
                title = $"{period}年月租金收缴统计报表",
                type = "时间维度分析",
                summary = new
                {
                    period = period,
                    totalBills = collectionStats.TotalBills,
                    paidBills = collectionStats.PaidBills,
                    overdueBills = collectionStats.OverdueBills,
                    totalAmount = collectionStats.TotalAmount,
                    paidAmount = collectionStats.PaidAmount,
                    overdueAmount = collectionStats.OverdueAmount,
                    collectionRate = collectionStats.CollectionRate,
                    avgRentPerStore = collectionStats.TotalBills > 0 ? Math.Round(collectionStats.TotalAmount / collectionStats.TotalBills, 2) : 0
                },
                trend = monthlyTrend,
                insights = GenerateInsights(collectionStats)
            };
        }

        /// <summary>
        /// 生成基于区域的报表
        /// </summary>
        private async Task<object> GenerateAreaBasedReport(string period)
        {
            var areaStats = await _storeContext.GetRentStatisticsByArea();
            var collectionStats = await _storeContext.GetRentCollectionStatistics(period);

            return new
            {
                title = $"{period}年月租金收缴区域分析报表",
                type = "区域维度分析",
                summary = new
                {
                    totalAreas = areaStats.Count,
                    occupiedAreas = areaStats.Count(a => a.IsOccupied),
                    totalRentRevenue = areaStats.Sum(a => a.BaseRent),
                    avgRentPerArea = areaStats.Count > 0 ? Math.Round((double)areaStats.Average(a => a.BaseRent), 2) : 0
                },
                areaDetails = areaStats.Select(a => new
                {
                    areaId = a.AreaId,
                    areaSize = a.AreaSize,
                    baseRent = a.BaseRent,
                    rentStatus = a.RentStatus,
                    storeName = a.StoreName,
                    tenantName = a.TenantName,
                    collectionStatus = GenerateAreaCollectionStatus(a.AreaId, period),
                    rentPerSqm = a.AreaSize > 0 ? Math.Round((double)a.BaseRent / a.AreaSize, 2) : 0
                }).OrderByDescending(a => a.baseRent).ToList(),
                statistics = new
                {
                    highestRent = areaStats.Count > 0 ? areaStats.Max(a => a.BaseRent) : 0,
                    lowestRent = areaStats.Count > 0 ? areaStats.Min(a => a.BaseRent) : 0,
                    occupancyRate = areaStats.Count > 0 ? Math.Round((double)areaStats.Count(a => a.IsOccupied) / areaStats.Count * 100, 2) : 0
                }
            };
        }

        /// <summary>
        /// 生成综合报表
        /// </summary>
        private async Task<object> GenerateComprehensiveReport(string period)
        {
            var timeReport = await GenerateTimeBasedReport(period);
            var areaReport = await GenerateAreaBasedReport(period);
            var collectionStats = await _storeContext.GetRentCollectionStatistics(period);

            return new
            {
                title = $"{period}年月商户租金综合统计报表",
                type = "综合分析",
                executiveSummary = new
                {
                    reportPeriod = period,
                    totalStores = collectionStats.TotalBills,
                    totalRevenue = collectionStats.TotalAmount,
                    collectionRate = collectionStats.CollectionRate,
                    status = GetOverallStatus(collectionStats.CollectionRate),
                    riskLevel = GetRiskLevel(collectionStats.OverdueBills, collectionStats.TotalBills)
                },
                financialSummary = new
                {
                    totalAmount = collectionStats.TotalAmount,
                    collectedAmount = collectionStats.PaidAmount,
                    outstandingAmount = collectionStats.OverdueAmount,
                    collectionRate = collectionStats.CollectionRate,
                    avgRentPerStore = collectionStats.TotalBills > 0 ? Math.Round(collectionStats.TotalAmount / collectionStats.TotalBills, 2) : 0
                },
                operationalMetrics = new
                {
                    totalBills = collectionStats.TotalBills,
                    paidBills = collectionStats.PaidBills,
                    overdueBills = collectionStats.OverdueBills,
                    pendingBills = collectionStats.TotalBills - collectionStats.PaidBills - collectionStats.OverdueBills,
                    onTimePaymentRate = collectionStats.TotalBills > 0 ? Math.Round((double)collectionStats.PaidBills / collectionStats.TotalBills * 100, 2) : 0
                },
                timeAnalysis = ((dynamic)timeReport).summary,
                areaAnalysis = ((dynamic)areaReport).statistics,
                recommendations = GenerateRecommendations(collectionStats)
            };
        }

        /// <summary>
        /// 生成区域收缴状态
        /// </summary>
        private string GenerateAreaCollectionStatus(int areaId, string period)
        {
            // 模拟区域收缴状态
            var random = new Random(areaId + int.Parse(period));
            var statusOptions = new[] { "已收缴", "待收缴", "逾期", "部分收缴" };
            var weights = new[] { 0.6, 0.2, 0.15, 0.05 }; // 权重分布
            
            var randomValue = random.NextDouble();
            var cumulativeWeight = 0.0;
            
            for (int i = 0; i < statusOptions.Length; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue <= cumulativeWeight)
                {
                    return statusOptions[i];
                }
            }
            
            return statusOptions[0];
        }

        /// <summary>
        /// 生成洞察建议
        /// </summary>
        private List<string> GenerateInsights(RentCollectionStatistics stats)
        {
            var insights = new List<string>();
            
            if (stats.CollectionRate >= 90)
            {
                insights.Add("✅ 租金收缴率优秀，超过90%");
            }
            else if (stats.CollectionRate >= 80)
            {
                insights.Add("⚡ 租金收缴率良好，建议加强催缴工作");
            }
            else
            {
                insights.Add("⚠️ 租金收缴率偏低，需要重点关注");
            }
            
            if (stats.OverdueBills > 0)
            {
                insights.Add($"🔔 发现{stats.OverdueBills}笔逾期账单，涉及金额{stats.OverdueAmount:C}");
            }
            
            if (stats.TotalBills > 0)
            {
                var avgRent = stats.TotalAmount / stats.TotalBills;
                if (avgRent > 10000)
                {
                    insights.Add("💰 平均租金水平较高，属于高端商业区域");
                }
                else if (avgRent < 5000)
                {
                    insights.Add("💡 平均租金水平较低，可考虑优化租金策略");
                }
            }
            
            return insights;
        }

        /// <summary>
        /// 获取整体状态
        /// </summary>
        private string GetOverallStatus(double collectionRate)
        {
            if (collectionRate >= 90) return "优秀";
            if (collectionRate >= 80) return "良好";
            if (collectionRate >= 70) return "一般";
            return "需改进";
        }

        /// <summary>
        /// 获取风险等级
        /// </summary>
        private string GetRiskLevel(int overdueBills, int totalBills)
        {
            if (totalBills == 0) return "无";
            
            var overdueRate = (double)overdueBills / totalBills;
            if (overdueRate < 0.1) return "低";
            if (overdueRate < 0.2) return "中";
            return "高";
        }

        /// <summary>
        /// 生成改进建议
        /// </summary>
        private List<string> GenerateRecommendations(RentCollectionStatistics stats)
        {
            var recommendations = new List<string>();
            
            if (stats.CollectionRate < 85)
            {
                recommendations.Add("建议加强租金催缴流程，设置自动提醒机制");
            }
            
            if (stats.OverdueBills > 0)
            {
                recommendations.Add("针对逾期账单，建议制定分级催缴策略");
            }
            
            if (stats.TotalBills > 0 && (double)stats.OverdueBills / stats.TotalBills > 0.15)
            {
                recommendations.Add("逾期率较高，建议审查租户信用状况和租金定价策略");
            }
            
            recommendations.Add("建议定期分析租金收缴数据，优化资金流管理");
            
            return recommendations;
        }

        #endregion

        #endregion
    }
}
