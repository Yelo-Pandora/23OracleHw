//对应于数据库促销目标商铺(PART_STORE)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("PART_STORE")]
    [PrimaryKey(nameof(EVENT_ID), nameof(STORE_ID))]
    public class PartStore
    {
        //促销活动ID
        public int EVENT_ID { get; set; }
        //参与的商铺ID
        public int STORE_ID { get; set; }

        //外键约束，促销活动和参与活动的商铺是多对多关系
        [ForeignKey("EVENT_ID")]
        public SaleEvent saleEventNavigation { get; set; }

        [ForeignKey("STORE_ID")]
        public Store storeNavigation { get; set; }
    }
}
