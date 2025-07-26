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
        public string LICENSE_PLATE_NUMBER { get; set; }
        public string PARKING_SPACE_ID { get; set; }
        public DateTime PARK_START {  get; set; }

        [ForeignKey("LICENSE_PLATE_NUMBER")]
        public Car carNavigation { get; set; }

        [ForeignKey("PARKING_SPACE_ID")]
        public ParkingSpace parkingSpaceNavigation {  get; set; }
    }
}
