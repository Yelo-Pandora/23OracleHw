//对应于数据库停车(PARK)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("PARK")]
    [PrimaryKey(nameof(LICENSE_PLATE_NUMBER), nameof(PARKING_SPACE_ID),nameof(PARK_START))]
    public class Park
    {
        //车牌号
        public string LICENSE_PLATE_NUMBER { get; set; }
        //车位ID
        public int PARKING_SPACE_ID { get; set; }
        //停车开始时间
        public DateTime PARK_START {  get; set; }

        //外键约束，车辆和车位是多对多关系
        [ForeignKey("LICENSE_PLATE_NUMBER")]
        public Car carNavigation { get; set; }

        [ForeignKey("PARKING_SPACE_ID")]
        public ParkingSpace parkingSpaceNavigation {  get; set; }
    }
}
