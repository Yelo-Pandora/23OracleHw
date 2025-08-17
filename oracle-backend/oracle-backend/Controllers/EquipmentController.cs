using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using oracle_backend.Dbcontexts;
using oracle_backend.Models;
using System.ComponentModel.DataAnnotations;

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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Equipment>>> GetEquipmentList(string OperatorID)
        {
            _logger.LogInformation("正在读取设备列表信息");
            try
            {
                var operatorAccount = await _accountContext.FindAccount(OperatorID);
                if (operatorAccount == null)
                    return BadRequest("操作员账号不存在");

                if (operatorAccount.AUTHORITY > 2)
                    return BadRequest("权限不足，需要设备管理权限");

                var equipment = await _context.Equipments.ToListAsync(); 
                if (!equipment.Any())
                    return NotFound("不存在任何设备");

                return Ok(equipment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取设备列表失败");
                return StatusCode(500, "服务器内部错误");
            }
        }

        //查询具体设备
        [HttpGet("{equipmentID}")]
        public async Task<ActionResult<Equipment>> GetEquipmentDetail(int equipmentID, string OperatorID)
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

                return Ok(equipment);
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
        [HttpGet("{id}/actionsList")]
        public async Task<IActionResult> GetAvailableActions(int id, string OperatorID)
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
                            "空调" => new[] { "关机", "制冷模式", "制热模式", "调节温度" },
                            "照明" => new[] { "关灯", "调亮", "调暗" },
                            "电梯" => new[] { "停止", "开门", "关门", "紧急停止" },
                            _ => Array.Empty<string>()
                        });
                        break;
                    case EquipmentStatus.Standby:
                        actions.AddRange(equipment.EQUIPMENT_TYPE.ToLower() switch
                        {
                            "空调" => new[] { "开机" },
                            "照明" => new[] { "开灯" },
                            "电梯" => new[] { "启动" },
                            _ => Array.Empty<string>()
                        });
                        break;
                    case EquipmentStatus.UnderMaintenance:
                        actions.AddRange(new[] { "维修完成", "取消维修" });
                        break;
                    case EquipmentStatus.Faulted:
                        actions.Add("重置");
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
            public string OperatorID { get; set; }  //操作员
            public string Operation { get; set; }     //操作类型
        }

        //操作设备
        [HttpPost("{id}/operate")]
        public async Task<IActionResult> OperateEquipment(int id, [FromBody] EquipmentOperationDto dto)
        {
            _logger.LogInformation($"正在操作设备 {id}: {dto.Operation}");
            try
            {
                var operatorAccount = await _accountContext.FindAccount(dto.OperatorID);
                if (operatorAccount == null)
                    return BadRequest("操作员账号不存在");

                if (operatorAccount.AUTHORITY > 2)
                    return BadRequest("权限不足，需要设备管理权限");

                var equipment = await _context.Equipments.FindAsync(id); 
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
                        _logger.LogInformation($"设备 {id} 状态变更: {originalStatus} -> {newStatus}");
                    }
                    _logger.LogInformation("操作记录: 设备={EquipmentId}, 操作员={Operator}, 操作={Operation}, 结果=成功",
                        id, dto.OperatorID, dto.Operation);
                }
                else
                {
                    _logger.LogWarning("操作失败: 设备={EquipmentId}, 操作员={Operator}, 操作={Operation}",
                        id, dto.OperatorID, dto.Operation);
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
                _logger.LogError(ex, $"操作设备 {id} 失败");
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
            if (currentStatus == EquipmentStatus.Offline)
                return false;

            if (currentStatus == EquipmentStatus.UnderMaintenance)
                return operation == "维修完成" || operation == "取消维修";

            operation = operation.ToLower();
            equipmentType = equipmentType.ToLower();

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

        //2.9.3 创建工单
        [HttpPost("create-order")]
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

        [HttpPut("complete-repair")]
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

                order.REPAIR_END = DateTime.Now;
                order.REPAIR_COST = dto.Success ? Math.Abs(dto.Cost) : -Math.Abs(dto.Cost);
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

                if (confirmer.AUTHORITY >3) 
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

                if (order.REPAIR_COST > 0) 
                {
                    equipment.EQUIPMENT_STATUS = EquipmentStatus.Running;
                    await _context.SaveChangesAsync();
                    return Ok("设备状态已更新为正常运行");
                }
                else 
                {
                    var Start = DateTime.Now;
                    Start = new DateTime(Start.Year, Start.Month, Start.Day,
                         Start.Hour, Start.Minute, Start.Second);
                    var newOrder = new RepairOrder
                    {
                        EQUIPMENT_ID = dto.EquipmentId,
                        STAFF_ID = await GetRepairStaff(), 
                        REPAIR_START = Start,
                        REPAIR_END = default,
                        REPAIR_COST = 0
                    };

                    _context.RepairOrders.Add(newOrder); 
                    await _context.SaveChangesAsync();
                    return Ok("维修失败，已创建新工单");
                }
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


        // DTO类
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
    }
}