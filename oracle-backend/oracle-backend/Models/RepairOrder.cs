//对应于数据库维修单(REPAIR_ORDER)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("REPAIR_ORDER")]
    [PrimaryKey(nameof(EQUIPMENT_ID), nameof(STAFF_ID))]
    public class RepairOrder
    {
        public int EQUIPMENT_ID { get; set; }
        public int STAFF_ID { get; set; }
        public DateTime REPAIR_START { get; set; }
        public DateTime REPAIR_END { get; set; }
        public double REPAIR_COST { get; set; }

        [ForeignKey("EQUIPMENT_ID")]
        public Equipment equipmentNavigation { get; set; }

        [ForeignKey("STAFF_ID")]
        public Staff staffNavigation { get; set; }
    }
}
