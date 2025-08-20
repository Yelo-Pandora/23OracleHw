using Microsoft.EntityFrameworkCore;
using oracle_backend.Models;

namespace oracle_backend.Dbcontexts
{
    public class SaleEventDbContext : DbContext
    {
        // 构造函数
        public SaleEventDbContext(DbContextOptions<SaleEventDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<SaleEvent> SaleEvents { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<PartStore> PartStores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置继承关系
            modelBuilder.Entity<Event>().ToTable("EVENT");
            modelBuilder.Entity<SaleEvent>().ToTable("SALE_EVENT");

            // 设置默认值
            modelBuilder.Entity<Event>()
                .Property(e => e.EVENT_ID)
                .HasDefaultValueSql("'PROMO-' || TO_CHAR(SYSDATE, 'YYYYMMDDHH24MISS')");

            base.OnModelCreating(modelBuilder);

            // 配置PartStore的复合主键
            modelBuilder.Entity<PartStore>()
                .HasKey(ps => new { ps.EVENT_ID, ps.STORE_ID });

            // 配置PartStore与SaleEvent的关系
            modelBuilder.Entity<PartStore>()
                .HasOne(ps => ps.saleEventNavigation)
                .WithMany()
                .HasForeignKey(ps => ps.EVENT_ID);

            // 配置PartStore与Store的关系
            modelBuilder.Entity<PartStore>()
                .HasOne(ps => ps.storeNavigation)
                .WithMany()
                .HasForeignKey(ps => ps.STORE_ID);
        }
    }
}