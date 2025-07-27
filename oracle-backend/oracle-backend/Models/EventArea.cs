//对应于数据库活动区域(EVENT_AREA)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("EVENT_AREA")]
    public class EventArea : Area
    {
        //活动区域容量
        public int? CAPACITY { get; set; }
        //区域租金
        public string AREA_FEE { get; set; }

    }
}
