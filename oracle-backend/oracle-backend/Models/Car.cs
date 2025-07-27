//对应于数据库车辆(CAR)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("CAR")]
    [PrimaryKey(nameof(LICENSE_PLATE_NUMBER), nameof(PARK_START))]
    public class Car
    {
        //车牌号
        public required string LICENSE_PLATE_NUMBER { get; set; }
        //停车开始时间
        public DateTime PARK_START { get; set; }
        //停车结束时间
        public DateTime? PARK_END { get; set; }

    }
}
