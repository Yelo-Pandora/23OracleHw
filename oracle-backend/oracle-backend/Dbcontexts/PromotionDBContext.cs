using Microsoft.EntityFrameworkCore;
using oracle_backend.Models.Promotion;

namespace oracle_backend.Dbcontexts
{
    public class PromotionDbContext : DbContext
    {
        public PromotionDbContext(DbContextOptions<PromotionDbContext> options) : base(options) { }

        public DbSet<Promotion> Promotions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Promotion>()
                .Property(p => p.PromotionRules)
                .HasColumnType("CLOB"); // 对于Oracle的大文本存储

            modelBuilder.Entity<Promotion>()
                .Property(p => p.ShopIds)
                .HasColumnType("CLOB");
        }
    }
}