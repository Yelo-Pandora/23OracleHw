using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oracle_backend.Models.venueEvent
{
    [Table("VENUE_EVENT_LOG")]
    public class VenueEventLog
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LOG_ID { get; set; }

        // 关联的活动ID
        [Required]
        public int EVENT_ID { get; set; }

        // 操作类型：CREATE, UPDATE, CANCEL, STATUS_CHANGE
        [Required]
        [StringLength(20)]
        public string OPERATION_TYPE { get; set; }

        // 变更前的值（JSON格式）
        public string OLD_VALUES { get; set; }

        // 变更后的值（JSON格式）
        public string NEW_VALUES { get; set; }

        // 操作描述
        [StringLength(500)]
        public string DESCRIPTION { get; set; }

        // 操作人员ID（可选，根据实际权限系统调整）
        [StringLength(50)]
        public string OPERATOR_ID { get; set; }

        // 操作时间
        [Required]
        public DateTime OPERATION_TIME { get; set; } = DateTime.Now;

        // 外键关联
        [ForeignKey("EVENT_ID")]
        public VenueEvent VenueEventNavigation { get; set; }
    }
}