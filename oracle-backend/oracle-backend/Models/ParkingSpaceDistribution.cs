//对应于数据库车位分布(PARKING_SPACE_DISTRIBUTION)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("PARKING_SPACE_DISTRIBUTION")]
    [PrimaryKey(nameof(AREA_ID), nameof(PARKING_SPACE_ID))]
    public class ParkingSpaceDistribution
    {
        //停车场ID
        public int AREA_ID { get; set; }
        //车位ID
        public int PARKING_SPACE_ID { get; set; }

        //外键约束，停车场和车位是一对多关系
        [ForeignKey("AREA_ID")]
        public ParkingLot parkingLotNavigation { get; set; }

        [ForeignKey("PARKING_SPACE_ID")]
        public ParkingSpace parkingSpaceNavigation { get; set; }
    }
}