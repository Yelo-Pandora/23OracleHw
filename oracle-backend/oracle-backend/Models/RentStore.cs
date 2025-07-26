//对应于数据库租用店面(RENT_STORE)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("RENT_STORE")]
    [PrimaryKey(nameof(STORE_ID), nameof(AREA_ID))]
    public class RentStore
    {
        public int STORE_ID { get; set; }
        public int AREA_ID { get; set; }

        [ForeignKey("STORE_ID")]
        public Store storeNavigation { get; set; }

        [ForeignKey("AREA_ID")]
        public RetailArea retailAreaNavigation { get; set; }
    }
}
