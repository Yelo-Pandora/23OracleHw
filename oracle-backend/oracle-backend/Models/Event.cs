//对应于数据库活动(EVENT)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("EVENT")]
    public class Event
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        //活动ID
        public int EVENT_ID { get; set; }
        //活动名称
        public string EVENT_NAME { get; set; }
        //活动开始时间
        public DateTime EVENT_START { get; set; }
        //活动结束时间
        public DateTime? EVENT_END { get; set; }
    }
}
