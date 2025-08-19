using Microsoft.EntityFrameworkCore;
using oracle_backend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace oracle_backend.Dbcontexts
{
    //与账号和临时权限有关的数据库上下文
    public class AccountDbContext : DbContext
    {
        //构造函数
        public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options)
        {
        }

        //将和账号和权限有关的类全部导入
        public DbSet<Account> ACCOUNT { get; set; }
        public DbSet<StaffAccount> STAFF_ACCOUNT { get; set; }
        public DbSet<StoreAccount> STORE_ACCOUNT { get; set; }
        public DbSet<TempAuthority> TEMP_AUTHORITY { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //规定账号对员工账号的级联删除规则，账号删除时，对应员工账号一并删除
            modelBuilder.Entity<StaffAccount>()
                .HasOne(o => o.accountNavigation)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
            //规定账号对商户账号的级联删除规则，账号删除时，对应商户账号一并删除
            modelBuilder.Entity<StoreAccount>()
                .HasOne(o => o.accountNavigation)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
            //规定账号对临时权限的级联删除规则，账号删除时，对应临时权限一并删除
            modelBuilder.Entity<TempAuthority>()
                .HasOne(o => o.accountNavigation)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        }

        //返回某个具体账号的信息
        public async Task<Account?> FindAccount(string goal)
        {
            //用户名唯一，找不到则返回 null
            return await this.ACCOUNT
            .FirstOrDefaultAsync(a => a.ACCOUNT.ToLower() == goal.ToLower());
        }

        //根据员工ID返回账号信息
        public async Task<Account?> AccountFromStaffID(int StaffID)
        {
            var task = this.STAFF_ACCOUNT.FirstOrDefaultAsync(a => a.STAFF_ID == StaffID);

            StaffAccount? goal = await task;
            if (goal != null)
                return await this.FindAccount(goal.ACCOUNT);
            else
                return null;
        }

        //根据店铺ID返回账号信息
        public async Task<Account?> AccountFromStoreID(int StoreID)
        {
            var task = this.STORE_ACCOUNT.FirstOrDefaultAsync(a => a.STORE_ID == StoreID);

            StoreAccount? goal = await task;
            if (goal != null)
                return await this.FindAccount(goal.ACCOUNT);
            else
                return null;
        }

        //根据账号返回相关的员工ID
        public async Task<StaffAccount?> CheckStaff(string goal)
        {
            return await STAFF_ACCOUNT.FirstOrDefaultAsync(sa => sa.ACCOUNT == goal);
        }

        //根据账号返回相关的商铺ID
        public async Task<StoreAccount?> CheckStore(string goal)
        {
            return await STORE_ACCOUNT.FirstOrDefaultAsync(sa => sa.ACCOUNT == goal);
        }

        //找到某个账号可能存在的临时权限
        public async Task<List<TempAuthority>> FindTempAuthorities(string goal)
        {
            return await TEMP_AUTHORITY.Where(ta => ta.ACCOUNT == goal)
                .ToListAsync();
        }

        //检查某个账号是否拥有足够的权限
        public async Task<bool> CheckAuthority(string goal, int auth)
        {
            //先获取账号信息,检查账号是否存在
            var acc = await this.FindAccount(goal);
            if(acc == null) return false;
            //再比较权限
            if(acc.AUTHORITY>auth) return false;
            else if(auth == 4 && acc.AUTHORITY == 3) return false;
            return true;
        }

        //根据账号返回商户账号信息（用于租金收取功能）
        public async Task<StoreAccount?> GetStoreAccountByAccount(string account)
        {
            return await STORE_ACCOUNT.FirstOrDefaultAsync(sa => sa.ACCOUNT == account);
        }
    }
}