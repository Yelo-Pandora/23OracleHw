//对应于数据库停车场(PARKING_LOT)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("PARKING_LOT")]
    public class ParkingLot : Area
    {
        //[Key]
        //public int AREA_ID { get; set; }
        public int PARKING_FEE { get; set; }

        //[ForeignKey("AREA_ID")]
        //public Area area { get; set; }

        //导航属性：表明一个停车场中有多个车位
        public ICollection<ParkingSpaceDistribution> parkingSpaceDistributionNavigations { get; set; }
    }
}
