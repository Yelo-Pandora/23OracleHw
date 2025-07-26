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
        public int EVENT_ID { get; set; }
        public string EVENT_NAME { get; set; }
        public DateTime EVENT_START { get; set; }
        public DateTime? EVENT_END { get; set; }
    }
}
