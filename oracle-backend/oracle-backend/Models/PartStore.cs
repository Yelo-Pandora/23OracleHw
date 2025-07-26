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
        public int EVENT_ID { get; set; }
        public int STORE_ID { get; set; }

        [ForeignKey("EVENT_ID")]
        public SaleEvent saleEventNavigation { get; set; }

        [ForeignKey("STORE_ID")]
        public Store storeNavigation { get; set; }
    }
}
