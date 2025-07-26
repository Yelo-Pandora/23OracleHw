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
        //[Key]
        //public int AREA_ID { get; set; }
        public int? CAPACITY { get; set; }
        public string AREA_FEE { get; set; }

        //[ForeignKey("AREA_ID")]
        //public Area area { get; set; }

        //导航属性：一个活动区域可能举办多次活动，与多个合作方相关联
        public ICollection<VenueEventDetail> venueEventDetailNavigations { get; set; }
    }
}
