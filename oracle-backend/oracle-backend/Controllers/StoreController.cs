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
    }
}
