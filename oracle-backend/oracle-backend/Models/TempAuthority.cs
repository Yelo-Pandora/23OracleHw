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
        //账号
        public string ACCOUNT {  get; set; }
        //活动ID
        public int EVENT_ID { get; set; }
        //临时权限名称
        //临时权限不设管理员
        //2：对应活动的项目经理，对活动和活动项目下的员工有完全管理功能
        //3：普通员工，能管理自身信息和活动信息
        //5：被封禁的帐号，权限和游客等同，无权限访问深层次系统，只能查看部分活动信息，被封禁的账号尝试登录时拒
        public int? TEMP_AUTHORITY {  get; set; }

        //外键约束，账号和活动ID是多对多关系
        [ForeignKey("ACCOUNT")]
        public Account accountNavigation { get; set; }

        [ForeignKey("EVENT_ID")]
        public VenueEvent venueEventNavigation { get; set; }
    }
}  
