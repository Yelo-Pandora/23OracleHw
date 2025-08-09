//对应于数据库场地活动(VENUE_EVENT)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models.venueEvent
{
    public class VenueEvent : Event
    {
        //活动参与人数
        public int? HEADCOUNT { get; set; }
        //活动收费，单位：人/次
        public double FEE { get; set; }
        //活动最大设定人数
        public int CAPACITY { get; set; }
        //活动花费
        public int EXPENSE { get; set; }

        //活动状态：PREPARING, ONGOING, COMPLETED, CANCELLED
        [StringLength(20)]
        public string ACTIVITY_STATUS { get; set; } = "PREPARING";

        //活动内容描述
        [StringLength(1000)]
        public string DESCRIPTION { get; set; }

    }
}
