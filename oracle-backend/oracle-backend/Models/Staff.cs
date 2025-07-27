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
        //员工ID
        public int STAFF_ID{ get; set; }
        //员工名称
        public string STAFF_NAME { get; set; }
        //员工性别
        public string? STAFF_SEX { get; set; }
        //员工部门
        public string STAFF_APARTMENT { get; set; }
        //员工位置（普通员工/经理）
        public string STAFF_POSITION { get; set; }
        //员工基础薪资
        public double STAFF_SALARY {  get; set; }

    }
}
