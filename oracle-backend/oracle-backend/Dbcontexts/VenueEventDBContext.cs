using Microsoft.EntityFrameworkCore;
using oracle_backend.Models;
using oracle_backend.Models.venueEvent;

namespace oracle_backend.Dbcontexts
{
    public class VenueEventDbContext : DbContext
    {
        public VenueEventDbContext(DbContextOptions<VenueEventDbContext> options) : base(options) { }

        // 场地活动数据集
        public DbSet<VenueEvent> VenueEvents { get; set; }
        
        // 场地活动详情数据集
        public DbSet<VenueEventDetail> VenueEventDetails { get; set; }
        
        // 场地活动日志数据集
        public DbSet<VenueEventLog> VenueEventLogs { get; set; }
        
        // 场地活动结算数据集
        public DbSet<VenueEventBilling> VenueEventBillings { get; set; }
        
        // 场地费用配置数据集
        public DbSet<VenueFeeConfig> VenueFeeConfigs { get; set; }
        
        // 附加服务数据集
        public DbSet<AdditionalService> AdditionalServices { get; set; }
        
        // 结算服务明细数据集
        public DbSet<BillingServiceDetail> BillingServiceDetails { get; set; }
        
        // 统计报表数据集
        public DbSet<VenueEventReport> VenueEventReports { get; set; }
        
        // 活动区域数据集
        public DbSet<EventArea> EventAreas { get; set; }
        
        // 合作方数据集
        public DbSet<Collaboration> Collaborations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置VenueEvent继承自Event
            modelBuilder.Entity<VenueEvent>()
                .ToTable("VENUE_EVENT");

            // 配置VenueEventDetail复合主键
            modelBuilder.Entity<VenueEventDetail>()
                .HasKey(v => new { v.EVENT_ID, v.AREA_ID, v.COLLABORATION_ID });

            // 配置VenueEventDetail外键关系
            modelBuilder.Entity<VenueEventDetail>()
                .HasOne(v => v.venueEventNavigation)
                .WithMany()
                .HasForeignKey(v => v.EVENT_ID);

            modelBuilder.Entity<VenueEventDetail>()
                .HasOne(v => v.eventAreaNavigation)
                .WithMany()
                .HasForeignKey(v => v.AREA_ID);

            modelBuilder.Entity<VenueEventDetail>()
                .HasOne(v => v.collaborationNavigation)
                .WithMany()
                .HasForeignKey(v => v.COLLABORATION_ID);

            // 配置STATUS字段长度
            modelBuilder.Entity<VenueEventDetail>()
                .Property(v => v.STATUS)
                .HasMaxLength(20);

            // 配置VenueEventLog外键关系
            modelBuilder.Entity<VenueEventLog>()
                .HasOne(l => l.VenueEventNavigation)
                .WithMany()
                .HasForeignKey(l => l.EVENT_ID);

            // 配置日志字段为CLOB类型（用于存储JSON）
            modelBuilder.Entity<VenueEventLog>()
                .Property(l => l.OLD_VALUES)
                .HasColumnType("CLOB");

            modelBuilder.Entity<VenueEventLog>()
                .Property(l => l.NEW_VALUES)
                .HasColumnType("CLOB");

            // 配置VenueEventBilling外键关系
            modelBuilder.Entity<VenueEventBilling>()
                .HasOne(b => b.VenueEventNavigation)
                .WithMany()
                .HasForeignKey(b => b.EVENT_ID);

            modelBuilder.Entity<VenueEventBilling>()
                .HasOne(b => b.EventAreaNavigation)
                .WithMany()
                .HasForeignKey(b => b.AREA_ID);

            modelBuilder.Entity<VenueEventBilling>()
                .HasOne(b => b.CollaborationNavigation)
                .WithMany()
                .HasForeignKey(b => b.COLLABORATION_ID);

            // 配置开票信息字段为CLOB类型（用于存储JSON）
            modelBuilder.Entity<VenueEventBilling>()
                .Property(b => b.INVOICE_INFO)
                .HasColumnType("CLOB");

            // 配置VenueFeeConfig外键关系
            modelBuilder.Entity<VenueFeeConfig>()
                .HasOne(f => f.EventAreaNavigation)
                .WithMany()
                .HasForeignKey(f => f.AREA_ID);

            // 配置BillingServiceDetail外键关系
            modelBuilder.Entity<BillingServiceDetail>()
                .HasOne(d => d.BillingNavigation)
                .WithMany()
                .HasForeignKey(d => d.BILLING_ID);

            modelBuilder.Entity<BillingServiceDetail>()
                .HasOne(d => d.ServiceNavigation)
                .WithMany()
                .HasForeignKey(d => d.SERVICE_ID);

            // 配置VenueEventReport外键关系
            modelBuilder.Entity<VenueEventReport>()
                .HasOne(r => r.EventAreaNavigation)
                .WithMany()
                .HasForeignKey(r => r.AREA_ID);

            modelBuilder.Entity<VenueEventReport>()
                .HasOne(r => r.CollaborationNavigation)
                .WithMany()
                .HasForeignKey(r => r.COLLABORATION_ID);
        }
    }
}
