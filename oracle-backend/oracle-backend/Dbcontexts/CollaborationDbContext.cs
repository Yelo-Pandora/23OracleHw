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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
    }
}