//对应于数据库员工账号(STAFF_ACCOUNT)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("STAFF_ACCOUNT")]
    [PrimaryKey(nameof(ACCOUNT), nameof(STAFF_ID))]
    public class StaffAccount
    {
        //账号
        public string ACCOUNT { get; set; }
        //员工ID
        public int STAFF_ID { get; set; }

        //外键约束，账号和员工ID是一对一关系
        [ForeignKey("ACCOUNT")]
        public Account accountNavigation { get; set; }

        [ForeignKey("STAFF_ID")]
        public Staff staffNavigation { get; set; }
    }
}
