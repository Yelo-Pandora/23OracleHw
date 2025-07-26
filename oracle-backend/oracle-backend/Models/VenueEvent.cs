//对应于数据库场地活动(VENUE_EVENT)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    public class VenueEvent : Event
    {
        //[Key]
        //public int EVENT_ID { get; set; }
        public int? HEADCOUNT { get; set; }
        public double FEE { get; set; }
        public int CAPACITY { get; set; }
        public int EXPENSE { get; set; }

        //[ForeignKey("EVENT_ID")]
        //public Event evenT {  get; set; }

        //导航属性：一个场地活动可能和多个有临时权限的员工关联
        public ICollection<TempAuthority> tempAuthorityNavigation { get; set; }

        //导航属性：一个场地活动设定成可能同时在多个活动区域内都举办，同时可能有多个合作方
        public ICollection<VenueEventDetail> venueEventDetailNavigations { get; set; }
    }
}
