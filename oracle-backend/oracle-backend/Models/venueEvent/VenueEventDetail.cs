//对应于数据库场地活动详情(VENUE_EVENT_DETAIL)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models.venueEvent
{
    [PrimaryKey(nameof(EVENT_ID), nameof(AREA_ID), nameof(COLLABORATION_ID))]
    public class VenueEventDetail
    {
        //活动ID
        public int EVENT_ID { get; set; }
        //区域ID
        public int AREA_ID { get; set; }
        //合作方ID
        public int COLLABORATION_ID { get; set; }
        //场地租界开始时间
        public DateTime RENT_START { get; set; }
        //场地租借结束时间
        public DateTime RENT_END { get; set; }
        //场地租借状态
        public string STATUS { get; set; }
        //合作方投资资金
        public double FUNDING { get; set; }

        //外键约束，多对多对多关系
        [ForeignKey("EVENT_ID")]
        public VenueEvent venueEventNavigation { get; set; }

        [ForeignKey("AREA_ID")]
        public EventArea eventAreaNavigation { get; set; }

        [ForeignKey("COLLABORATION_ID")]
        public Collaboration collaborationNavigation { get; set; }
    }
}
