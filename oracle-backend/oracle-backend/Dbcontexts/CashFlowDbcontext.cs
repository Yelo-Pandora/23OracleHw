using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;
using oracle_backend.Models;

namespace oracle_backend.Dbcontexts
{
    public class CashFlowDbContext : DbContext
    {
        public CashFlowDbContext(DbContextOptions<CashFlowDbContext> options) : base(options)
        {
        }
        public DbSet<VenueEventDetail> VenueEventDetails { get; set; }
        public DbSet<RepairOrder> RepairOrders { get; set; }
        public DbSet<MonthSalaryCost> MonthSalaryCosts { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Park> Parks { get; set; }
        public DbSet<ParkingLot> ParkingLots { get; set; }
        public DbSet<ParkingSpaceDistribution> ParkingSpaceDistributions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<RepairOrder>().HasKey(ro => new { ro.EQUIPMENT_ID, ro.STAFF_ID, ro.REPAIR_START });
            // 配置ParkingSpaceDistribution的外键关系
            modelBuilder.Entity<ParkingSpaceDistribution>()
                .HasOne(psd => psd.parkingLotNavigation)
                .WithMany()
                .HasForeignKey(psd => psd.AREA_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ParkingSpaceDistribution>()
                .HasOne(psd => psd.parkingSpaceNavigation)
                .WithMany()
                .HasForeignKey(psd => psd.PARKING_SPACE_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // 配置Park的外键关系 - 使用自定义映射解决类型不匹配
            modelBuilder.Entity<Park>()
                .HasOne(p => p.carNavigation)
                .WithMany()
                .HasPrincipalKey(c => new { c.LICENSE_PLATE_NUMBER, c.PARK_START })
                .HasForeignKey(p => new { p.LICENSE_PLATE_NUMBER, p.PARK_START })
                .OnDelete(DeleteBehavior.Restrict);

            // 忽略类型不匹配的导航属性，避免外键关系错误
            modelBuilder.Entity<Park>()
                .Ignore(p => p.parkingSpaceNavigation);
        }
    }
}
