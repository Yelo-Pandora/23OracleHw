//对应于数据库活动临时权限(TEMP_AUTHORITY)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace oracle_backend.Models
{
    [PrimaryKey(nameof(ACCOUNT), nameof(EVENT_ID))]
    public class TempAuthority
    {
        public string ACCOUNT {  get; set; }
        public int EVENT_ID { get; set; }
        public int? TEMP_AUTHORITY {  get; set; }

        [ForeignKey("ACCOUNT")]
        public Account accountNavigation { get; set; }

        [ForeignKey("EVENT_ID")]
        public VenueEvent venueEventNavigation { get; set; }
    }
}  
