//对应于数据库员工(STAFF)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("STAFF")]
    public class Staff
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int STAFF_ID{ get; set; }
        public string STAFF_NAME { get; set; }
        public string? STAFF_SEX { get; set; }
        public string STAFF_APARTMENT { get; set; }
        public string STAFF_POSITION { get; set; }
        public double STAFF_SALARY {  get; set; }

        //导航属性：维修工单联系中，一个负责维修的员工可能同时负责多个设备的维修
        public ICollection<RepairOrder> repairOrderNavigations { get; set; }

        //导航属性：一个员工会上多个月的班，会和多个月的工资总支出产生关系
        public ICollection<SalarySlip> salarySlipNavigations { get; set; }

        //导航属性：一个员工账号和员工一一对应
        public StaffAccount staffAccountNavigation { get; set; }

    }
}
