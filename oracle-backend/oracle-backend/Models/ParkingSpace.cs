//对应于数据库车位(PARKING_SPACE)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("PARKING_SPACE")]
    public class ParkingSpace
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PARKING_SPACE_ID { get; set; }
        public bool OCCUPIED { get; set; }

        //导航属性：表明一个停车位上可以多次停车
        public ICollection<Park> parkNavigations { get; set; }
        //导航属性：表明一个停车位只可能位于某一个停车场
        public ParkingSpaceDistribution parkingSpaceDistributionNavigation { get; set; }
    }
}
