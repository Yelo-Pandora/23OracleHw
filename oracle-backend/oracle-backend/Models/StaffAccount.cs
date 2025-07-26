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
        public string ACCOUNT { get; set; }
        public int STAFF_ID { get; set; }

        [ForeignKey("ACCOUNT")]
        public Account accountNavigation { get; set; }

        [ForeignKey("STAFF_ID")]
        public Staff staffNavigation { get; set; }
    }
}
