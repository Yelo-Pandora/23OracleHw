//对应于数据库区域(AREA)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("AREA")]
    public class Area
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        //区域ID
        public int AREA_ID { get; set; }
        //是否为空 (0-否，1-是)
        public int ISEMPTY { get; set; }
        //区域面积
        public int? AREA_SIZE { get; set; }

    }
}
