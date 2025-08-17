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
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<StaffAccount> STAFF_ACCOUNT { get; set; }

        // 用于检查依赖关系的实体
        public DbSet<VenueEventDetail> VenueEventDetails { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VenueEventDetail>()
                .ToTable("VENUE_EVENT_DETAIL")
                .HasKey(ved => new { ved.EVENT_ID, ved.AREA_ID, ved.COLLABORATION_ID });
        }

        // 根据账号查员工信息
        public async Task<Staff?> FindStaffByAccount(string account)
        {
            // 先根据STAFF_ACCOUNT查员工ID
            var staffAccount = await STAFF_ACCOUNT.FirstOrDefaultAsync(sa => sa.ACCOUNT == account);
            if (staffAccount == null) return null;
            // 再根据员工ID查员工信息
            return await Staffs.FirstOrDefaultAsync(s => s.STAFF_ID == staffAccount.STAFF_ID);
        }

        // 根据ID查找员工信息
        public async Task<Staff?> FindStaffById(int staffId)
        {
            return await Staffs.FirstOrDefaultAsync(s => s.STAFF_ID == staffId);
        }
    }
}