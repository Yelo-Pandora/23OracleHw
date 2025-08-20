//对应于数据库工资单(SALARY_SLIP)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("SALARY_SLIP")]
    [PrimaryKey(nameof(STAFF_ID), nameof(MONTH_TIME))]
    public class SalarySlip
    {
        //员工ID
        public int STAFF_ID { get; set; }
        //月度时间
        public DateTime MONTH_TIME { get; set; }
        //出勤次数
        public int ATD_COUNT { get; set; }
        //奖金
        public double BONUS { get; set; }
        //罚金
        public double FINE { get; set; }

        //外键约束，员工和每月工资总支出是多对多关系
        [ForeignKey("STAFF_ID")]
        public Staff staffNavigation { get; set; }

        [ForeignKey("MONTH_TIME")]
        public MonthSalaryCost monthSalaryCostNavigation { get; set; }
    }
}
