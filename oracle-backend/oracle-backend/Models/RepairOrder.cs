//对应于数据库维修单(REPAIR_ORDER)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("REPAIR_ORDER")]
    [PrimaryKey(nameof(EQUIPMENT_ID), nameof(STAFF_ID),nameof(REPAIR_START))]
    public class RepairOrder
    {
        //设备ID
        public int EQUIPMENT_ID { get; set; }
        //员工ID
        public int STAFF_ID { get; set; }
        //维修开始时间
        public DateTime REPAIR_START { get; set; }
        //维修结束时间
        public DateTime REPAIR_END { get; set; }
        //维修花费
        public double REPAIR_COST { get; set; }


        //外键约束，设备和员工是多对多关系
        [ForeignKey("EQUIPMENT_ID")]
        public Equipment equipmentNavigation { get; set; }

        [ForeignKey("STAFF_ID")]
        public Staff staffNavigation { get; set; }
    }
}
