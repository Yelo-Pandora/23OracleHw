using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oracle_backend.Models.venueEvent
{
    // 场地活动统计报表主表
    [Table("VENUE_EVENT_REPORT")]
    public class VenueEventReport
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int REPORT_ID { get; set; }

        // 报表类型：DAILY, WEEKLY, MONTHLY, CUSTOM
        [Required]
        [StringLength(20)]
        public string REPORT_TYPE { get; set; }

        // 统计开始时间
        [Required]
        public DateTime START_DATE { get; set; }

        // 统计结束时间
        [Required]
        public DateTime END_DATE { get; set; }

        // 报表标题
        [Required]
        [StringLength(200)]
        public string REPORT_TITLE { get; set; }

        // 统计区域ID（可选，为空表示全部区域）
        public int? AREA_ID { get; set; }

        // 统计合作方ID（可选，为空表示全部合作方）
        public int? COLLABORATION_ID { get; set; }

        // 总活动数量
        public int TOTAL_EVENTS { get; set; }

        // 总租用时长（小时）
        public decimal TOTAL_RENTAL_HOURS { get; set; }

        // 总收费金额
        public decimal TOTAL_REVENUE { get; set; }

        // 平均参与人数
        public decimal AVERAGE_ATTENDANCE { get; set; }

        // 平均上座率
        public decimal AVERAGE_OCCUPANCY_RATE { get; set; }

        // 生成时间
        [Required]
        public DateTime GENERATED_TIME { get; set; } = DateTime.Now;

        // 生成人员ID
        [StringLength(50)]
        public string GENERATED_BY { get; set; }

        // 报表状态：GENERATING, COMPLETED, FAILED
        [Required]
        [StringLength(20)]
        public string STATUS { get; set; } = "GENERATING";

        // 外键关联
        [ForeignKey("AREA_ID")]
        public EventArea EventAreaNavigation { get; set; }

        [ForeignKey("COLLABORATION_ID")]
        public Collaboration CollaborationNavigation { get; set; }
    }
}