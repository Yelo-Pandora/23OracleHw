//对应于数据库店铺(STORE)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("STORE")]
    public class Store
    {
        [Key]
        //商铺ID
        public int STORE_ID { get; set; }
        //商铺名
        public string STORE_NAME { get; set; }
        //商铺状态（正常营业/歇业中/翻新中）
        public string STORE_STATUS { get; set; }
        //商铺种类（个人/企业连锁）
        public string STORE_TYPE { get; set; }
        //租户名
        public string TENANT_NAME { get; set; }
        //联系方式
        public string CONTACT_INFO { get; set; }
        //租赁起始时间
        public DateTime RENT_START { get; set; }
        //租赁结束时间
        public DateTime RENT_END { get; set; }
    }
}
