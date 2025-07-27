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


    }
}
