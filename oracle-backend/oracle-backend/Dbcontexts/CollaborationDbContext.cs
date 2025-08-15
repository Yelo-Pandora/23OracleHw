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
        public DbSet<SalarySlip> SalarySlips { get; set; }
        public DbSet<MonthSalaryCost> MonthSalaryCosts { get; set; }
        // public DbSet<TempAuthority> TempAuthorities { get; set; }

        // 用于检查依赖关系的实体
        public DbSet<VenueEventDetail> VenueEventDetails { get; set; }


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

        // 根据员工ID和monthTime返回唯一的SalarySlip记录
        public async Task<SalarySlip?> GetSalarySlipByStaffId(int staffId, DateTime monthTime)
        {
            // 提取年月份
            var year = monthTime.Year;
            var month = monthTime.Month;

            return await SalarySlips.FirstOrDefaultAsync(ss => ss.STAFF_ID == staffId && ss.MONTH_TIME.Year == year && ss.MONTH_TIME.Month == month);
        }

        // 根据monthTime返回MonthSalaryCost
        public async Task<MonthSalaryCost?> GetMonthSalaryCostByStaffId(DateTime monthTime)
        {
            // 提取年月份
            var year = monthTime.Year;
            var month = monthTime.Month;

            return await MonthSalaryCosts.FirstOrDefaultAsync(msc => msc.MONTH_TIME.Year == year && msc.MONTH_TIME.Month == month);
        }
    }
}