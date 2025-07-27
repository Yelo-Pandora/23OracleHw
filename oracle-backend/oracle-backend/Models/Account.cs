//对应于数据库中账号(ACCOUNT)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;


namespace oracle_backend.Models
{
    [Table("ACCOUNT")]
    public class Account
    {
        [Key]
        //账号
        public string ACCOUNT { get; set; }
        //密码
        public string PASSWORD { get; set; }
        //身份
        public string IDENTITY { get; set; }
        //用户名
        public string USERNAME { get; set; }
        //权限，以编码方式呈现
        //1：数据库管理员，对整个数据库有完全管理权限，可以将其它账号设为管理员
        //2：对应部门经理，对其部门下的所有员工有完全管理权限，拥有申请活动权限
        //3：普通员工，能管理自身信息和账号，维修部的员工可以管理维修单
        //4：商铺租户，能管理自身店铺信息，能申请参与促销活动
        //5：被封禁的帐号，或游客，无权限访问深层次系统，只能查看停车场、部分商铺公开信息、部分活动信息，被封禁的账号尝试登录时拒绝访问
        //除常驻权限外还有活动的临时权限，由对应部门（市场部）经理授予经理或员工以针对某个活动的临时权限，具体见TempAuthority实体
        public int AUTHORITY { get; set; }

    }
}
