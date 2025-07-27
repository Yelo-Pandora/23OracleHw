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
        //停车费
        public int PARKING_FEE { get; set; }

    }
}
