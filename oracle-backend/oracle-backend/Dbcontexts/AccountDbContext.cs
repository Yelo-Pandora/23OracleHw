using Microsoft.EntityFrameworkCore;
using oracle_backend.Models;

namespace oracle_backend.Dbcontexts
{
    public class AccountDbContext : DbContext
    {
        // 1. 构造函数：这是让依赖注入系统配置数据库连接所必需的
        public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<StaffAccount> StaffAccounts { get; set; }
        public DbSet<StoreAccount> StoreAccounts { get; set; }
        public DbSet<TempAuthority> TempAuthoritie { get; set; }
    }
}