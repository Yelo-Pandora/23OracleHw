// Program.cs

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using oracle_backend.Dbcontexts;
using oracle_backend.Services;


namespace oracle_backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("OracleConnection");
            builder.Services.AddDbContext<AccountDbContext>(options =>
            {
                options.UseOracle(connectionString); // 指定使用 Oracle 提供程序和连接字符串
            });
            // 添加对 ComplexDbContext 的依赖注入
            builder.Services.AddDbContext<ComplexDbContext>(options =>
            {
                options.UseOracle(connectionString);
            });

            // 添加对 ComplexDbContext 的依赖注入
            builder.Services.AddDbContext<ComplexDbContext>(options =>
            {
                options.UseOracle(connectionString);
            });

            builder.Services.AddDbContext<EquipmentDbContext>(options =>
            {
                options.UseOracle(connectionString);
            });

            builder.Services.AddDbContext<StoreDbContext>(options =>
            {
                options.UseOracle(connectionString); // 添加商店相关的数据库上下文
            });

            builder.Services.AddDbContext<ParkingContext>(options =>
            {
                options.UseOracle(connectionString); // 添加停车场相关的数据库上下文
            });

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // 保持属性名原样，不进行驼峰转换
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });
          
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // 添加数据库上下文
            builder.Services.AddDbContext<SaleEventDbContext>(options => options.UseOracle(connectionString));

            // 注册服务
            builder.Services.AddScoped<SaleEventService>();
            builder.Services.AddScoped<ISaleEventService, SaleEventService>();

            // 添加CORS配置
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // 启用CORS
            app.UseCors("AllowAll");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();

        }
    }
}