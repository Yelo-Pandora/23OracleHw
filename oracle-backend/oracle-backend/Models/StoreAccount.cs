//对应于数据库商家账号(STORE_ACCOUNT)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [PrimaryKey(nameof(ACCOUNT), nameof(STORE_ID))]
    public class StoreAccount
    {
        public string ACCOUNT {  get; set; }
        public int STORE_ID { get; set; }

        [ForeignKey("ACCOUNT")]
        public Account accountNavigation { get; set; }

        [ForeignKey("STORE_ID")]
        public Store storeNavigation { get; set; }
    }
}
