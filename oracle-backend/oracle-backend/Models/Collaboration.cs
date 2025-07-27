//对应于数据库合作方(COLLABORATION)表的类

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace oracle_backend.Models
{
    [Table("COLLABORATION")]
    public class Collaboration
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        //合作方ID
        public int COLLABORATION_ID { get; set; }
        //合作方名字
        public required string COLLABORATION_NAME { get; set; }
        //合作方的负责人
        public string? CONTACTOR { get; set; }
        //合作方的电话号码
        public string? PHONE_NUMBER { get; set; }
        //合作方的邮箱
        public string? EMAIL { get; set; }

    }
}
