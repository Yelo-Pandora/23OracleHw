//对应于数据库车辆(CAR)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("CAR")]
    [PrimaryKey(nameof(LICENSE), nameof(PARK_START))]
    public class Car
    {
        public required int LICENSE { get; set; }
        public DateTime PARK_START { get; set; }
        public DateTime? PARK_END { get; set; }

        //导航属性：表明一辆车可以在一个车位上停多次
        public ICollection<Park> parkNavigations { get; set; }
    }
}
