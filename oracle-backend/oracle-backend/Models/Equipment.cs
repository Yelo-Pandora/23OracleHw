//对应于数据库设备(EQUIPMENT)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("EQUIPMENT")]
    public class Equipment
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public required int EQUIPMENT_ID { get; set; }
        public string EQUIPMENT_TYPE { get; set; }
        public string EQUIPMENT_STATUS { get; set; }
        public string? PORT { get; set; }
        public int? EQUIPMENT_COST { get; set; }
        public DateTime BUY_TIME { get; set; }

        // 导航属性：表示一个设备只有一个确定的位置
        public EquipmentLocation equipmentLocationNavigation { get; set; }
        
        //导航属性：在维修工单联系中，一个设备可以同时被多个人一起维修
        public ICollection<RepairOrder> repairOrderNavigations { get; set; }

    }
}
