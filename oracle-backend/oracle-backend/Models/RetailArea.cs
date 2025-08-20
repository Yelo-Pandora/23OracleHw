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
        //出租状态
        public string RENT_STATUS { get; set; }
        //基础租金
        public double BASE_RENT { get; set; }
    }
}
