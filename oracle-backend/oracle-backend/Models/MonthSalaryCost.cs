//对应于数据库每月工资总支出(MONTH_SALARY_COST)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("MONTH_SALARY_COST")]
    public class MonthSalaryCost
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        //月度时间
        public DateTime MONTH_TIME {  get; set; }
        //每月工资总支出
        public int TOTAL_COST { get; set; }

    }
}
