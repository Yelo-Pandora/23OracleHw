//对应于数据库商家账号(STORE_ACCOUNT)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("STORE_ACCOUNT")]
    [PrimaryKey(nameof(ACCOUNT), nameof(STORE_ID))]
    public class StoreAccount
    {
        //账号
        public string ACCOUNT {  get; set; }
        //店铺ID
        public int STORE_ID { get; set; }

        //外键约束，账号和店铺是一对一关系
        [ForeignKey("ACCOUNT")]
        public Account accountNavigation { get; set; }

        [ForeignKey("STORE_ID")]
        public Store storeNavigation { get; set; }
    }
}
