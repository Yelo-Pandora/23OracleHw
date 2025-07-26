//对应于数据库场地活动详情(VENUE_EVENT_DETAIL)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [PrimaryKey(nameof(EVENT_ID), nameof(AREA_ID),nameof(COLLABORATION_ID))]
    public class VenueEventDetail
    {
        public int EVENT_ID { get; set; }
        public int AREA_ID { get; set; }
        public int COLLABORATION_ID { get; set; }
        public DateTime RENT_START { get; set; }
        public DateTime RENT_END { get; set; }
        public string STATUS { get; set; }
        public double FUNDING {  get; set; }

        [ForeignKey("EVENT_ID")]
        public VenueEvent venueEventNavigation {  get; set; }

        [ForeignKey("AREA_ID")]
        public EventArea eventAreaNavigation { get; set; }

        [ForeignKey("COLLABORATION_ID")]
        public Collaboration collaborationNavigation { get; set; }
    }
}
