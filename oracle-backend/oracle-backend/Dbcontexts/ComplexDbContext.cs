using Microsoft.EntityFrameworkCore;
using oracle_backend.Models;

namespace oracle_backend.Dbcontexts
{
    public class ComplexDbContext : DbContext
    {
        public ComplexDbContext(DbContextOptions<ComplexDbContext> options) : base(options)
        {
        }

        // 区域相关实体
        public DbSet<Area> Areas { get; set; }
        public DbSet<RetailArea> RetailAreas { get; set; }
        public DbSet<EventArea> EventAreas { get; set; }
        public DbSet<ParkingLot> ParkingLots { get; set; }
        public DbSet<OtherArea> OtherAreas { get; set; }

        // 用于检查依赖关系的实体
        public DbSet<RentStore> RentStores { get; set; }
        public DbSet<VenueEventDetail> VenueEventDetails { get; set; }
        public DbSet<ParkingSpaceDistribution> ParkingSpaceDistributions { get; set; }

        // 合作方和员工相关实体
        public DbSet<Collaboration> Collaborations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 定义Area表的主键
            modelBuilder.Entity<Area>().HasKey(a => a.AREA_ID);
            // 定义子类表的主键并配置TPH继承策略
            // 注意：EF Core默认使用TPH，我们在这里明确指定以防万一
            // 实际上，由于您的SQL已经将它们分表，这更像是TPT。
            // 但为了EF Core能正确工作，我们仍需定义关系。
            modelBuilder.Entity<RetailArea>().HasBaseType<Area>().ToTable("RETAIL_AREA");
            modelBuilder.Entity<EventArea>().HasBaseType<Area>().ToTable("EVENT_AREA");
            modelBuilder.Entity<ParkingLot>().HasBaseType<Area>().ToTable("PARKING_LOT");
            modelBuilder.Entity<OtherArea>().HasBaseType<Area>().ToTable("OTHER_AREA");
            // 其他实体的主键配置
            modelBuilder.Entity<RentStore>().HasKey(rs => new { rs.STORE_ID, rs.AREA_ID });
            modelBuilder.Entity<VenueEventDetail>()
                .ToTable("VENUE_EVENT_DETAIL")
                .HasKey(ved => new { ved.EVENT_ID, ved.AREA_ID, ved.COLLABORATION_ID });
            modelBuilder.Entity<ParkingSpaceDistribution>().HasKey(rs => new { rs.PARKING_SPACE_ID, rs.AREA_ID });
        }
    }
}