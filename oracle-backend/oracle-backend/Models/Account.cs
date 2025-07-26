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
        public int AUTHORITY { get; set; }

        //导航属性：一个员工账号和员工一一对应
        public StaffAccount staffAccountNavigation { get; set; }

        //导航属性：一个店铺账号和店铺一一对应
        public StoreAccount storeAccountNavigation { get; set; }

        //导航属性：一个可能账号有多项临时权限
        public ICollection<TempAuthority> tempAuthoritieNavigations { get; set; }

    }
}
