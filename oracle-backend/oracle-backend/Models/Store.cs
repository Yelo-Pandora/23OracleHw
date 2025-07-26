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
        public int STORE_ID { get; set; }
        public string STORE_NAME { get; set; }
        public string STORE_STATUS { get; set; }
        public string STORE_TYPE { get; set; }
        public string TENANT_NAME { get; set; }
        public string CONTACT_INFO { get; set; }
        public DateTime RENT_START { get; set; }
        public DateTime RENT_END { get; set; }

        //导航属性：一个店铺能同时参与多个促销活动
        public PartStore partStoreNavigation { get; set; }

        //导航属性：一个具体店铺可以租下多个店面
        public ICollection<RentStore> rentStoreNavigation { get; set; }

        //导航属性：一个店铺账号和店铺一一对应
        public StoreAccount storeAccountNavigation { get; set; }
    }
}
