using Microsoft.EntityFrameworkCore;
using oracle_backend.Models;

namespace oracle_backend.Dbcontexts
{
    // 与商店和区域相关的数据库上下文
    public class StoreDbContext : DbContext
    {
        // 构造函数
        public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options)
        {
        }

        // 导入与商店相关的表
        public DbSet<Store> STORE { get; set; }
        public DbSet<Area> AREA { get; set; }
        public DbSet<RetailArea> RETAIL_AREA { get; set; }
        public DbSet<RentStore> RENT_STORE { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置继承关系，RetailArea继承自Area
            modelBuilder.Entity<RetailArea>()
                .HasBaseType<Area>();

            // 配置RentStore的外键关系
            modelBuilder.Entity<RentStore>()
                .HasOne(rs => rs.storeNavigation)
                .WithMany()
                .HasForeignKey(rs => rs.STORE_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RentStore>()
                .HasOne(rs => rs.areaNavigation)
                .WithMany()
                .HasForeignKey(rs => rs.AREA_ID)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // 检查指定区域是否可用（空置）
        public async Task<bool> IsAreaAvailable(int areaId)
        {
            // 需要检查AREA表的ISEMPTY字段和RETAIL_AREA表的RENT_STATUS字段
            var query = from area in AREA
                       join retailArea in RETAIL_AREA on area.AREA_ID equals retailArea.AREA_ID
                       where area.AREA_ID == areaId
                       select new { area.ISEMPTY, retailArea.RENT_STATUS };
            
            var result = await query.FirstOrDefaultAsync();
            return result != null && result.ISEMPTY == 1 && result.RENT_STATUS == "空置";
        }

        // 检查租户是否已在综合体有店铺
        public async Task<bool> TenantExists(string tenantName, string contactInfo)
        {
            return await STORE.AnyAsync(s => s.TENANT_NAME == tenantName || s.CONTACT_INFO == contactInfo);
        }

        // 获取下一个可用的店铺ID
        public async Task<int> GetNextStoreId()
        {
            var maxId = await STORE.MaxAsync(s => (int?)s.STORE_ID) ?? 0;
            return maxId + 1;
        }

        // 更新区域状态为已租用
        public async Task UpdateAreaStatus(int areaId, bool isEmpty, string rentStatus)
        {
            // 更新AREA表的ISEMPTY字段
            var area = await AREA.FirstOrDefaultAsync(a => a.AREA_ID == areaId);
            if (area != null)
            {
                area.ISEMPTY = isEmpty ? 1 : 0; // Oracle中用1/0表示布尔值
            }
            
            // 更新RETAIL_AREA表的RENT_STATUS字段
            var retailArea = await RETAIL_AREA.FirstOrDefaultAsync(a => a.AREA_ID == areaId);
            if (retailArea != null)
            {
                retailArea.RENT_STATUS = rentStatus;
            }
        }

        // 根据店铺ID获取店铺信息
        public async Task<Store?> GetStoreById(int storeId)
        {
            return await STORE.FirstOrDefaultAsync(s => s.STORE_ID == storeId);
        }

        // 获取所有空置的零售区域
        public async Task<List<object>> GetAvailableAreas()
        {
            var query = from area in AREA
                       join retailArea in RETAIL_AREA on area.AREA_ID equals retailArea.AREA_ID
                       where area.ISEMPTY == 1 && retailArea.RENT_STATUS == "空置"
                       select new
                       {
                           areaId = area.AREA_ID,
                           areaSize = area.AREA_SIZE,
                           baseRent = retailArea.BASE_RENT,
                           rentStatus = retailArea.RENT_STATUS,
                           isEmpty = area.ISEMPTY == 1
                       };
            
            return await query.Cast<object>().ToListAsync();
        }
    }
}
