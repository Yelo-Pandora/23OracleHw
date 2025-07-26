//对应于数据库其他区域(OTHER_AREA)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("OTHER_AREA")]
    public class OtherArea : Area
    {
        //[Key]
        //public int AREA_ID { get; set; }
        public string TYPE {  get; set; }

        //[ForeignKey("AREA_ID")]
        //public Area area { get; set; }
    }
}
