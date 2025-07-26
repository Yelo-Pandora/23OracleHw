//对应于数据库设备位置(EQUIPMENT_LOCATION)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("EQUIPMENT_LOCATION")]
    [PrimaryKey(nameof(EQUIPMENT_ID), nameof(AREA_ID))]
    public class EquipmentLocation
    {
        public int EQUIPMENT_ID { get; set; }
        public int AREA_ID { get; set; }

        [ForeignKey("EQUIPMENT_ID")]
        public Equipment equipmentNavigation { get; set; }

        [ForeignKey("AREA_ID")]
        public Area areaNavigation { get; set; }
    }
}
