//对应于数据库促销活动(SALE_EVENT)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("SALE_EVENT")]
    public class SaleEvent :Event
    {
        //[Key]
        //public int EVENT_ID { get; set; }
        public double COST { get; set; }
        public string DESCRIPTION { get; set; }

        //[ForeignKey("EVENT_ID")]
        //public Event evenT { get; set; }

        //导航属性：一次促销活动中会有多个店铺参与
        public ICollection<PartStore> partStoreNavigation { get; set; }
    }
}
