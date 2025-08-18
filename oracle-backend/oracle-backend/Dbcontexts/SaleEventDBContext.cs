using Microsoft.EntityFrameworkCore;
using oracle_backend.Models;

namespace oracle_backend.Dbcontexts
{
    public class SaleEventDbContext : DbContext
    {
        public SaleEventDbContext(DbContextOptions<SaleEventDbContext> options) : base(options) { }

        public DbSet<SaleEvent> SaleEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SaleEvent>()
                .Property(p => p.ShopIdsJson)
                .HasColumnType("CLOB");

            modelBuilder.Entity<SaleEvent>()
                .Property(p => p.PromotionRulesJson)
                .HasColumnType("CLOB");
        }
    }
}