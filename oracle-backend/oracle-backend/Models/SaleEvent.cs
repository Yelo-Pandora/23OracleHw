//对应于数据库促销活动(SALE_EVENT)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("SALE_EVENT")]
    public class SaleEvent :Event
    {
        //促销活动花费
        public double COST { get; set; }
        //促销活动的描述
        public string DESCRIPTION { get; set; }

    }
}
