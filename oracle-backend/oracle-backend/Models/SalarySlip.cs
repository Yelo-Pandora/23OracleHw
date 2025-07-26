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
        public int STAFF_ID { get; set; }
        public DateTime MONTH_TIME { get; set; }
        public int ATD_COUNT { get; set; }
        public double BONUS { get; set; }
        public double FIRE {  get; set; }

        [ForeignKey("STAFF_ID")]
        public Staff staffNavigation { get; set; }

        [ForeignKey("MONTH_TIME")]
        public MonthSalaryCost monthSalaryCostNavigation { get; set; }
    }
}
