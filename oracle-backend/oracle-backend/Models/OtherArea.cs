//对应于数据库其他区域(OTHER_AREA)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("OTHER_AREA")]
    public class OtherArea : Area
    {
        //区域类型（如卫生间、杂物间、电梯间等）
        public string TYPE {  get; set; }
    }
}
