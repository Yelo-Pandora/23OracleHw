using Microsoft.EntityFrameworkCore;
using oracle_backend.Models;

namespace oracle_backend.Dbcontexts
{
    public class CollaborationDbContext : DbContext
    {
        public CollaborationDbContext(DbContextOptions<CollaborationDbContext> options) : base(options)
        {
        }
        // 合作方和员工相关实体
        public DbSet<Collaboration> Collaborations { get; set; }
        public DbSet<Staff> Staff { get; set; }

        // 用于检查依赖关系的实体
        public DbSet<VenueEventDetail> VenueEventDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VenueEventDetail>()
                .ToTable("VENUE_EVENT_DETAIL")
                .HasKey(ved => new { ved.EVENT_ID, ved.AREA_ID, ved.COLLABORATION_ID });
        }
    }
}