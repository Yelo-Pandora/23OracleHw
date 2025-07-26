//对应于数据库合作方(COLLABORATION)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("COLLABORATION")]
    public class Collaboration
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int COLLABORATION_ID { get; set; }
        public required string COLLABORATION_NAME { get; set; }
        public string? CONTACTOR { get; set; }
        public string? PHONE_NUMBER { get; set; }
        public string? EMAIL { get; set; }

        //导航属性：一个合作方可能参与赞助了多次的场地活动
        public ICollection<VenueEventDetail> venueEventDetailNavigations { get; set; }
    }
}
