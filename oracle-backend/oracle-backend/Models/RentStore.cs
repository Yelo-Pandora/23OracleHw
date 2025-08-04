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
        //商铺ID
        public int STORE_ID { get; set; }
        //店面区域ID
        public int AREA_ID { get; set; }

        //外键约束，店铺和店面是一对多关系
        [ForeignKey("STORE_ID")]
        public Store? storeNavigation { get; set; }

        [ForeignKey("AREA_ID")]
        public Area? areaNavigation { get; set; }
    }
}
