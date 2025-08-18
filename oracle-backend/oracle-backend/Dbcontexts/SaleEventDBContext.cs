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
        }
    }
}