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
        //车位ID
        public int PARKING_SPACE_ID { get; set; }
        //是否有车
        public bool OCCUPIED { get; set; }
    }
}
