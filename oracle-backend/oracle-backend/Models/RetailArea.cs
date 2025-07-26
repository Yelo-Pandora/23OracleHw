//对应于数据库店面(RETAIL_AREA)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("RETAIL_AREA")]
    public class RetailArea : Area
    {
        //[Key]
        //public int AREA_ID { get; set; }
        public string RENT_STATUS { get; set; }
        public double BASE_RENT {  get; set; }

        //[ForeignKey("AREA_ID")]
        //public Area area { get; set; }
        //导航属性：一个店面只能同时被一个店铺持有
        public RentStore rentStoreNavigation { get; set; }
    }
}
