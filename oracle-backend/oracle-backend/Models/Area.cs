//对应于数据库区域(AREA)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("AREA")]
    public class Area
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AREA_ID { get; set; }
        public bool ISEMPTY { get; set; }
        public int? AREA_SIZE { get; set; }

        // 导航属性：表示一个区域内可以有多个设备
        public ICollection<EquipmentLocation> equipmentLocationNavigations { get; set; }
    }
}
