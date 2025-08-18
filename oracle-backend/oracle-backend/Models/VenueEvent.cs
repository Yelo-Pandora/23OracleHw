//对应于数据库场地活动(VENUE_EVENT)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
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

    }
}
