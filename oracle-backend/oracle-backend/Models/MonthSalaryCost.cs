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
        public DateTime MONTH_TIME {  get; set; }
        public int TOTAL_COST { get; set; }

        //导航属性：某个月的工资总支出包含多个员工的当月工资
        public ICollection<SalarySlip> salarySlipNavigations { get; set; }
    }
}
