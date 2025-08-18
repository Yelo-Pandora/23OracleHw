using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.OpenApi.Models;
using Oracle.ManagedDataAccess.Client;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using System.ComponentModel.DataAnnotations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace oracle_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EquipmentController : ControllerBase
    {
        private readonly EquipmentDbContext _context;
        private readonly AccountDbContext _accountContext;
        private readonly ILogger<EquipmentController> _logger;

        public EquipmentController(
            EquipmentDbContext context,
            ILogger<EquipmentController> logger,
            AccountDbContext accountContext)
        {
            _context = context;
            _logger = logger;
            _accountContext = accountContext;
        }

        //2.9.1 查看设备列表
        public class EquipmentListDto
        {
            public int equipment_ID { get; set; }
            public string equipment_TYPE { get; set; }
            public string equipment_STATUS { get; set; }
            public int area_ID { get; set; }
        }
        [HttpGet("EquipmentList")]
        public async Task<ActionResult<IEnumerable<Equipment>>> GetEquipmentList([FromQuery] string OperatorID)
        {
            _logger.LogInformation("正在读取设备列表信息");
            try
            {
                var operatorAccount = await _accountContext.FindAccount(OperatorID);
                if (operatorAccount == null)
                    return BadRequest("操作员账号不存在");

                if (operatorAccount.AUTHORITY > 2)
                    return BadRequest("权限不足，需要设备管理权限");
                var list = await _context.Equipments
                    .Select(e => new EquipmentListDto
                    {
                        equipment_ID = e.EQUIPMENT_ID,
                        equipment_TYPE = e.EQUIPMENT_TYPE,
                        equipment_STATUS = e.EQUIPMENT_STATUS,
                        area_ID = _context.EquipmentLocations
                                    .Where(el => el.EQUIPMENT_ID == e.EQUIPMENT_ID)
                                    .Select(el => el.AREA_ID)
                                    .FirstOrDefault()
                    })
                    .ToListAsync();
                if (!list.Any())
                    return NotFound("不存在任何设备");
                return Ok(list);
                //var equipment = await _context.Equipments.ToListAsync(); 
                ////if (!equipment.Any())
                //return NotFound("不存在任何设备");

                //return Ok(equipment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取设备列表失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        //查询具体设备
        public class EquipmentDetailDto
        {
            //设备ID
            public required int EQUIPMENT_ID { get; set; }
            //设备类型
            public string EQUIPMENT_TYPE { get; set; }
            //设备状态
            public string EQUIPMENT_STATUS { get; set; }
            //设备接口
            public string? PORT { get; set; }
            //设备购入花费
            public int? EQUIPMENT_COST { get; set; }
            //购买时间
            public DateTime BUY_TIME { get; set; }
            public int AREA_ID { get; set; }
        }
        [HttpGet("EquipmentDetail")]
        public async Task<ActionResult<Equipment>> GetEquipmentDetail([FromQuery] int equipmentID, [FromQuery] string OperatorID)
        {
            _logger.LogInformation("正在查询设备信息");
            try
            {
                var operatorAccount = await _accountContext.FindAccount(OperatorID);
                if (operatorAccount == null)
                    return BadRequest("操作员账号不存在");

                if (operatorAccount.AUTHORITY > 2)
                    return BadRequest("权限不足，需要设备管理权限");

                var equipment = await _context.Equipments.FindAsync(equipmentID);
                if (equipment == null)
                    return NotFound("未找到该设备");

                if (equipment.EQUIPMENT_STATUS == EquipmentStatus.Offline)
                    return BadRequest("该设备离线，无法获取状态");
                var area_ID = await _context.EquipmentLocations
                            .Where(el => el.EQUIPMENT_ID == equipment.EQUIPMENT_ID)
                            .Select(el => el.AREA_ID)
                            .FirstOrDefaultAsync();
                var dto = new EquipmentDetailDto
                {
                    EQUIPMENT_ID = equipment.EQUIPMENT_ID,
                    EQUIPMENT_TYPE = equipment.EQUIPMENT_TYPE,
                    EQUIPMENT_STATUS = equipment.EQUIPMENT_STATUS,
                    EQUIPMENT_COST = equipment.EQUIPMENT_COST,
                    PORT = equipment.PORT,
                    BUY_TIME = equipment.BUY_TIME,
                    AREA_ID = area_ID
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取设备信息失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        //2.9.2 设备状态常量
        public static class EquipmentStatus
        {
            public const string Running = "运行中";
            public const string Faulted = "故障";
            public const string Offline = "离线";
            public const string UnderMaintenance = "维修中";
            public const string Standby = "待机";
        }

        //显示可操作列表
        private static readonly string[] AirConditionerActions={ "关机", "制冷模式", "制热模式", "调节温度" };
        private static readonly string[] LightingActions={ "关灯", "调亮", "调暗" };
        private static readonly string[] ElevatorActions={ "停止", "开门", "关门","紧急停止"};
        private static readonly string[] StandbyAirActions={ "开机" };
        private static readonly string[] StandbyLightActions={ "开灯" };
        private static readonly string[] StandbyElevatorActions={ "启动" };

        [HttpGet("ActionsList")]
        public async Task<IActionResult> GetAvailableActions([FromQuery] int id, [FromQuery] string OperatorID)
        {
            _logger.LogInformation($"正在加载设备 {id} 的可操作列表");
            try
            {
                var operatorAccount = await _accountContext.FindAccount(OperatorID);
                if (operatorAccount == null)
                    return BadRequest("操作员账号不存在");

                if (operatorAccount.AUTHORITY > 2)
                    return BadRequest("权限不足，需要设备管理权限");

                var equipment = await _context.Equipments.FindAsync(id);
                if (equipment == null)
                    return NotFound("设备不存在");

                List<string> actions = new List<string>();
                switch (equipment.EQUIPMENT_STATUS)
                {
                    case EquipmentStatus.Running:
                        actions.AddRange(equipment.EQUIPMENT_TYPE.ToLower() switch
                        {
                            "空调" => AirConditionerActions,
                            "照明" => LightingActions,
                            "电梯" => ElevatorActions,
                            _ => Array.Empty<string>()
                        });
                        break;
                    case EquipmentStatus.Standby:
                        actions.AddRange(equipment.EQUIPMENT_TYPE.ToLower() switch
                        {
                            "空调" => StandbyAirActions,
                            "照明" => StandbyLightActions,
                            "电梯" => StandbyElevatorActions,
                            _ => Array.Empty<string>()
                        });
                        break;
                    case EquipmentStatus.UnderMaintenance:
                        actions.Add("当前状态不可操作");
                        break;
                    case EquipmentStatus.Faulted:
                        actions.Add("当前状态不可操作");
                        break;
                }
                return Ok(actions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"显示设备 {id} 可操作列表失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        public class EquipmentOperationDto
        {
            public int EquipmentID { get; set; }
            public string OperatorID { get; set; }  //操作员
            public string Operation { get; set; }     //操作类型
        }

        //操作设备
        [HttpPost("operate")]
        public async Task<IActionResult> OperateEquipment([FromBody] EquipmentOperationDto dto)
        {
            _logger.LogInformation($"正在操作设备{dto.EquipmentID}:{dto.Operation}");
            try
            {
                var operatorAccount = await _accountContext.FindAccount(dto.OperatorID);
                if (operatorAccount == null)
                    return BadRequest("操作员账号不存在");

                if (operatorAccount.AUTHORITY > 2)
                    return BadRequest("权限不足，需要设备管理权限");

                var equipment = await _context.Equipments.FindAsync(dto.EquipmentID);
                if (equipment == null)
                    return NotFound("未找到该设备");

                if (equipment.EQUIPMENT_STATUS == EquipmentStatus.Offline ||
                    equipment.EQUIPMENT_STATUS == EquipmentStatus.UnderMaintenance ||
                    equipment.EQUIPMENT_STATUS == EquipmentStatus.Faulted)
                {
                    return BadRequest($"设备当前状态为{equipment.EQUIPMENT_STATUS}，不可操作");
                }

                //验证操作是否适用于当前状态
                if (!IsOperationValidForStatus(dto.Operation, equipment.EQUIPMENT_STATUS, equipment.EQUIPMENT_TYPE))
                {
                    return BadRequest("当前状态下不支持此操作");
                }

                //调用模拟接口执行操作
                bool result = SimulateDeviceOperation();
                string originalStatus = equipment.EQUIPMENT_STATUS;

                if (result)
                {
                    string newStatus = MapOperationToStatus(
                        dto.Operation,
                        equipment.EQUIPMENT_STATUS,
                        equipment.EQUIPMENT_TYPE
                    );

                    if (equipment.EQUIPMENT_STATUS != newStatus)
                    {
                        equipment.EQUIPMENT_STATUS = newStatus;
                        _context.LogOperation(dto.EquipmentID,dto.OperatorID, dto.Operation,true, originalStatus, newStatus);
                        _logger.LogInformation($"设备{dto.EquipmentID}状态变更:{originalStatus} -> {newStatus}");
                    }
                    _logger.LogInformation("操作成功:设备={EquipmentId}, 操作员={Operator}, 操作={Operation}",
                        dto.EquipmentID, dto.OperatorID, dto.Operation);
                }
                else
                {
                    _context.LogOperation(dto.EquipmentID, dto.OperatorID, dto.Operation,false, originalStatus,originalStatus);
                    _logger.LogWarning("操作失败:设备={EquipmentId},操作员={Operator},操作={Operation}",
                        dto.EquipmentID, dto.OperatorID, dto.Operation);
                }
                await _context.SaveChangesAsync();

                return result ?
                    Ok(new
                    {
                        status = equipment.EQUIPMENT_STATUS,
                        result = "操作成功",
                        statusChanged = equipment.EQUIPMENT_STATUS != originalStatus
                    }) :
                    BadRequest("指令发送失败，请重试");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"操作设备 {dto.EquipmentID} 失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        private string MapOperationToStatus(string operation, string currentStatus, string equipmentType)
        {
            operation = operation.ToLower();
            equipmentType = equipmentType.ToLower();

            switch (operation)
            {
                case "开机" when equipmentType == "空调":
                case "开灯" when equipmentType == "照明":
                case "启动" when equipmentType == "电梯":
                    return EquipmentStatus.Running;

                case "关机" when equipmentType == "空调":
                case "关灯" when equipmentType == "照明":
                case "停止" when equipmentType == "电梯":
                    return EquipmentStatus.Standby;

                case "紧急停止" when equipmentType == "电梯":
                    return EquipmentStatus.Faulted;
            }

            return currentStatus;
        }

        private bool IsOperationValidForStatus(string operation, string currentStatus, string equipmentType)
        {
            if (currentStatus == EquipmentStatus.Offline ||
                     currentStatus == EquipmentStatus.UnderMaintenance ||
                     currentStatus == EquipmentStatus.Faulted)
                return false;

            operation = operation.ToLower();

            if (currentStatus == EquipmentStatus.Running)
            {
                if (operation == "开机" || operation == "开灯" || operation == "启动")
                    return false;
            }
            else if (currentStatus == EquipmentStatus.Standby)
            {
                if (operation == "关机" || operation == "关灯" || operation == "停止")
                    return false;
            }
            return true;
        }

        //简单模拟，操作成功概率是90%
        private bool SimulateDeviceOperation()
        {
            Random rand = new Random();
            return rand.Next(1, 101) <= 90;
        }
        public class RepairOrderDto
        {
            public string OperatorID { get; set; }
            public int EquipmentID { get; set; }
            public bool inProgressOnly { get; set; }
        }
        public class RepairOrderDetailDto
        {
            public int EQUIPMENT_ID { get; set; }
            //员工ID
            public int STAFF_ID { get; set; }
            //维修开始时间
            public DateTime REPAIR_START { get; set; }
            //维修结束时间
            public DateTime REPAIR_END { get; set; }
            //维修花费
            public double REPAIR_COST { get; set; }
        }

        [HttpGet("RepairList")]
        public async Task<ActionResult<IEnumerable<RepairOrderDetailDto>>> GetRepairList([FromQuery] RepairOrderDto dto)
        {
            _logger.LogInformation("正在查询工单列表");
            try
            {
                var operatorAccount = await _accountContext.FindAccount(dto.OperatorID);
                if (operatorAccount == null)
                    return BadRequest("操作员账号不存在");
                if (operatorAccount.AUTHORITY > 2)
                    return BadRequest("权限不足，需要设备管理权限");

                var equipment = await _context.Equipments.FindAsync(dto.EquipmentID);
                if (equipment == null)
                    return NotFound($"设备ID={dto.EquipmentID} 不存在");

                var repairOrders = _context.RepairOrders
                    .Where(r => r.EQUIPMENT_ID == dto.EquipmentID);

                if (dto.inProgressOnly)
                {
                    repairOrders = repairOrders.Where(r => r.REPAIR_END == default(DateTime));
                }

                var resultList = await repairOrders
                    .Select(r => new RepairOrderDetailDto
                    {
                        EQUIPMENT_ID = r.EQUIPMENT_ID,
                        STAFF_ID = r.STAFF_ID,
                        REPAIR_START = r.REPAIR_START,
                        REPAIR_END = r.REPAIR_END,
                        REPAIR_COST = r.REPAIR_COST
                    })
                    .ToListAsync();

                if (!resultList.Any())
                    return NotFound("不存在任何工单列表");

                return Ok(resultList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取工单列表失败");
                return StatusCode(500, "服务器内部错误");
            }
        }
        //2.9.3 创建工单
        //DTO类
        public class CreateOrderDto
        {
            public string OperatorID { get; set; }
            public int EquipmentId { get; set; }
            public string FaultDescription { get; set; } //故障描述
        }

        public class CompleteRepairDto
        {
            public int EquipmentId { get; set; }
            public int StaffId { get; set; }
            public DateTime RepairStart { get; set; }
            public double Cost { get; set; }
            public bool Success { get; set; }
        }

        public class OrderKeyDto
        {
            public string OperatorID { get; set; }
            public int EquipmentId { get; set; }
            public int StaffId { get; set; }
            public DateTime RepairStart { get; set; }
        }

        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            _logger.LogInformation("正在创建工单");
            try
            {
                var operatorAccount = await _accountContext.FindAccount(dto.OperatorID);
                if (operatorAccount == null)
                    return BadRequest("操作员账号不存在");

                if (operatorAccount.AUTHORITY > 2)
                    return BadRequest("权限不足，需要设备管理权限");

                var equipment = await _context.Equipments.FindAsync(dto.EquipmentId);
                if (equipment == null)
                    return NotFound("设备不存在");

                if (equipment.EQUIPMENT_STATUS != EquipmentStatus.Faulted)
                    return BadRequest("该设备正常或正在维修中");

                equipment.EQUIPMENT_STATUS = EquipmentStatus.UnderMaintenance;
                var STAFF_ID = await GetRepairStaff();
                if (STAFF_ID == 0)
                    return BadRequest("暂无维修人员，无法维修");
                var repairStart = DateTime.Now;
                repairStart = new DateTime(repairStart.Year, repairStart.Month, repairStart.Day,
                     repairStart.Hour, repairStart.Minute, repairStart.Second);
                var newOrder = new RepairOrder
                {
                    EQUIPMENT_ID = dto.EquipmentId,
                    STAFF_ID = STAFF_ID,
                    REPAIR_START = repairStart,
                    REPAIR_END = default,
                    REPAIR_COST = 0
                };

                _context.RepairOrders.Add(newOrder);
                await _context.SaveChangesAsync();
                _logger.LogInformation("设备账号 {EquipmentId}, 因 {FaultDescription} 处于维修中", dto.EquipmentId, dto.FaultDescription);
                return Ok(new
                {
                    message = "工单创建成功",
                    compositeKey = new
                    {
                        equipmentId = newOrder.EQUIPMENT_ID,
                        staffId = newOrder.STAFF_ID,
                        repairStart = newOrder.REPAIR_START
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "工单创建失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        [HttpPut("CompleteRepair")]
        public async Task<IActionResult> CompleteRepair([FromBody] CompleteRepairDto dto)
        {
            _logger.LogInformation("正在更新工单");
            try
            {
                var order = await _context.RepairOrders.FindAsync(
                    dto.EquipmentId, dto.StaffId, dto.RepairStart);

                if (order == null)
                    return NotFound("工单不存在");

                if (order.REPAIR_END != default)
                    return BadRequest("工单已完成，不可修改");
                if (dto.Cost < 0)
                    return BadRequest("维修费用不可为负数");

                order.REPAIR_END = DateTime.Now;
                order.REPAIR_COST = dto.Success ? Math.Abs(dto.Cost) : -Math.Abs(dto.Cost);//根据正负判断维修成功或失败
                await _context.SaveChangesAsync();
                return Ok(dto.Success ? "维修成功结果已提交" : "维修失败结果已提交");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成维修工单失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        [HttpPost("confirm-repair")]
        public async Task<IActionResult> ConfirmRepair([FromBody] OrderKeyDto dto)
        {
            try
            {
                var confirmer = await _accountContext.FindAccount(dto.OperatorID);
                if (confirmer == null)
                    return BadRequest("操作员账号不存在");

                if (confirmer.AUTHORITY > 3)
                    return BadRequest("权限不足，需要设备管理权限");

                var order = await _context.RepairOrders.FindAsync(
                    dto.EquipmentId, dto.StaffId, dto.RepairStart);

                if (order == null)
                    return NotFound("工单不存在");
                if (order.REPAIR_END == default)
                    return BadRequest("工单尚未完成，无法确认");
                var equipment = await _context.Equipments.FindAsync(dto.EquipmentId);
                if (equipment == null)
                    return NotFound("设备不存在");
                //正常维修花销大于0,
                if (order.REPAIR_COST > 0)
                {
                    equipment.EQUIPMENT_STATUS = EquipmentStatus.Running;
                    await _context.SaveChangesAsync();
                    return Ok("设备状态已更新为正常运行");
                }
                else
                {
                    equipment.EQUIPMENT_STATUS = EquipmentStatus.Faulted;
                    await _context.SaveChangesAsync();
                    return Ok("维修失败，等待再次分配工单");
                }
                //同步现金流？
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认维修失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        private async Task<int> GetRepairStaff()
        {
            try
            {
                //获取维修部在职员工ID列表
                var repairStaffIds = await _context.Staffs
                    .Where(s => s.STAFF_APARTMENT == "维修部")
                    .Select(s => s.STAFF_ID)
                    .ToListAsync();

                if (repairStaffIds.Count == 0)
                {
                    _logger.LogWarning("无可用维修人员");
                    throw new Exception("无可用维修人员");
                }

                //随机选择一名员工
                var random = new Random();
                int index = random.Next(repairStaffIds.Count);
                return repairStaffIds[index];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "随机选择维修人员失败");
                throw;
            }
        }
        //新增添加、删除设备功能

        [HttpPost("AddEquipment")]
        public async Task<ActionResult<Equipment>> AddEquipment([FromBody] Equipment newEquipment, string OperatorID)
        {
            _logger.LogInformation("正在添加设备");
            try
            {
                var operatorAccount = await _accountContext.FindAccount(OperatorID);
                if (operatorAccount == null)
                    return BadRequest("操作员账号不存在");
                if (operatorAccount.AUTHORITY > 2)
                    return BadRequest("权限不足，需要设备管理权限");

                var equipment = await _context.Equipments.FindAsync(newEquipment.EQUIPMENT_ID);
                if (equipment != null)
                    return BadRequest("该设备ID已存在，无法新增设备");
                await _context.Equipments.AddAsync(newEquipment);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"设备添加成功，ID: {newEquipment.EQUIPMENT_ID}");
                return CreatedAtAction(
                    nameof(GetEquipmentDetail),
                    new { equipmentID = newEquipment.EQUIPMENT_ID },
                    new
                    {
                        message = "设备添加成功",
                        equipment = newEquipment
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设备添加失败");
                return StatusCode(500, "服务器内部错误");
            }
        }
        [HttpPost("AddEquipmentLocation")]
        public async Task<ActionResult<EquipmentLocation>> AddEquipmentLocation(int equipmentID, int areaID, string OperatorID)
        {
            _logger.LogInformation("正在添加设备位置信息");
            try
            {
                var operatorAccount = await _accountContext.FindAccount(OperatorID);
                if (operatorAccount == null)
                    return BadRequest("操作员账号不存在");
                if (operatorAccount.AUTHORITY > 2)
                    return BadRequest("权限不足，需要设备管理权限");

                var equipment = await _context.Equipments.FindAsync(equipmentID);
                if (equipment == null)
                    return BadRequest("该设备ID不存在，无法添加设备位置信息");

                //检查设备位置是否已经存在
                var locationExists = await _context.EquipmentLocations
                    .CountAsync(el => el.EQUIPMENT_ID == equipmentID)>0;
                if (locationExists)
                    return BadRequest("该设备已经存在位置信息，无法重复添加");

                var count = await _context.Areas
                     .Where(a => a.AREA_ID == areaID)
                     .CountAsync();
                if (count<=0)
                    return BadRequest($"区域ID {areaID} 不存在，无法添加设备位置信息");


                //添加设备位置
                var newLocation = new EquipmentLocation
                {
                    EQUIPMENT_ID = equipmentID,
                    AREA_ID = areaID
                };

                await _context.EquipmentLocations.AddAsync(newLocation);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"设备位置添加成功，设备ID: {equipmentID}, 区域ID: {areaID}");
                return Created("GetEquipmentLocation", new
                {
                    message = "设备位置添加成功",
                    equipmentID = newLocation.EQUIPMENT_ID,
                    areaID = newLocation.AREA_ID
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设备位置信息添加失败");
                return StatusCode(500, "服务器内部错误");
            }
        }


        [HttpDelete("DeleteEquipment")]
        public async Task<IActionResult> DeleteEquipment(int equipmentID, string OperatorID)
        {
            _logger.LogInformation($"正在尝试删除设备ID={equipmentID}");
            try
            {
                var operatorAccount = await _accountContext.FindAccount(OperatorID);
                if (operatorAccount == null)
                    return BadRequest("操作员账号不存在");
                if (operatorAccount.AUTHORITY > 2)
                    return BadRequest("权限不足，需要设备管理权限");

                var equipment = await _context.Equipments.FindAsync(equipmentID);
                if (equipment == null)
                    return NotFound("设备不存在");

                var location = await _context.EquipmentLocations.FirstOrDefaultAsync(el => el.EQUIPMENT_ID == equipmentID);
                if (location != null)
                {
                    _context.EquipmentLocations.Remove(location);
                    _logger.LogInformation($"删除关联位置记录，设备ID: {equipmentID}");
                }

                _context.Equipments.Remove(equipment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"设备删除成功，ID: {equipmentID}");
                return Ok(new { message = $"设备删除成功，ID: {equipmentID}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除设备失败");
                return StatusCode(500, "服务器内部错误");
            }
        }
    }
}