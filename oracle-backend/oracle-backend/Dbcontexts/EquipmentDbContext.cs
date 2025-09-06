using Microsoft.EntityFrameworkCore;
using oracle_backend.Controllers;
using oracle_backend.Models;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;

namespace oracle_backend.Dbcontexts
{
    public class EquipmentDbContext : DbContext
    {
        //构造函数
        public EquipmentDbContext(DbContextOptions<EquipmentDbContext> options) : base(options) { }

        //数据库实体集
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<EquipmentLocation> EquipmentLocations { get; set; }
        public DbSet<RepairOrder> RepairOrders { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Area> Areas { get; set; }
        
        //操作日志记录类
        private class DeviceOperationRecord
        {
            public Guid LogID { get; } = Guid.NewGuid();
            public int EquipmentID { get; set; }
            public string OperatorID { get; set; }
            public string Operation { get; set; }
            public DateTime Timestamp { get; }= DateTime.UtcNow;
            public bool IsSuccess { get; set; }
            public string PreviousState { get; set; }
            public string CurrentState { get; set; }
        }

        private static readonly ConcurrentDictionary<Guid, DeviceOperationRecord> _operationLogs =
                new ConcurrentDictionary<Guid, DeviceOperationRecord>();
        public void LogOperation(int EquipmentID, string OperatorID, string operationType,bool isSuccess,string prevState, string currState)
        {
            var record = new DeviceOperationRecord
            {
                EquipmentID = EquipmentID,
                OperatorID = OperatorID,
                Operation = operationType,
                IsSuccess = isSuccess,
                PreviousState = prevState,
                CurrentState = currState
            };

            _operationLogs.TryAdd(record.LogID, record);
        }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //删除设备时，级联删除设备位置
            modelBuilder.Entity<EquipmentLocation>()
                .HasOne(el => el.equipmentNavigation) //一个设备只有一个位置信息
                .WithOne()             
                .HasForeignKey<EquipmentLocation>(el => el.EQUIPMENT_ID)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<EquipmentLocation>()
                .HasOne(el => el.areaNavigation)
                .WithMany()
                .HasForeignKey(el => el.AREA_ID)
                .OnDelete(DeleteBehavior.Cascade);

            //维修工单配置
            modelBuilder.Entity<RepairOrder>().HasKey(ro => new { ro.EQUIPMENT_ID, ro.STAFF_ID, ro.REPAIR_START });
            modelBuilder.Entity<RepairOrder>()
             .HasOne(ro => ro.equipmentNavigation)
             .WithMany()
             .HasForeignKey(ro => ro.EQUIPMENT_ID)
             .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RepairOrder>()
                .HasOne(ro => ro.staffNavigation)
                .WithMany()
                .HasForeignKey(ro => ro.STAFF_ID)
                .OnDelete(DeleteBehavior.Restrict);

            //员工实体配置
            modelBuilder.Entity<Staff>(entity =>
            {
                entity.ToTable("STAFF");
                //仅配置查询需要的字段
                entity.Property(s => s.STAFF_ID).HasColumnName("STAFF_ID");
                entity.Property(s => s.STAFF_APARTMENT).HasColumnName("STAFF_APARTMENT");
                entity.Ignore(s => s.STAFF_NAME);
                entity.Ignore(s => s.STAFF_SEX);
                entity.Ignore(s => s.STAFF_POSITION);
                entity.Ignore(s => s.STAFF_SALARY);
            });

        modelBuilder.Entity<Equipment>()
                .HasIndex(e => e.EQUIPMENT_STATUS);

            modelBuilder.Entity<RepairOrder>()
                .HasIndex(ro => ro.REPAIR_START);
        }

        public async Task<List<Equipment>> GetFaultedEquipment()
        {
            return await Equipments
                .Where(e => e.EQUIPMENT_STATUS == EquipmentController.EquipmentStatus.Faulted)
                .ToListAsync();
        }

        public async Task<List<Equipment>> GetEquipmentForMaintenance()
        {
            return await Equipments
                .Where(e => e.EQUIPMENT_STATUS == EquipmentController.EquipmentStatus.Faulted ||
                            e.EQUIPMENT_STATUS == EquipmentController.EquipmentStatus.UnderMaintenance)
                .ToListAsync();
        }
    }
}