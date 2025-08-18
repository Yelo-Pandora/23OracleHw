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

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // 添加数据库上下文
            builder.Services.AddDbContext<PromotionDbContext>(options => options.UseOracle(builder.Configuration.GetConnectionString("PromotionDb")));
            
            // 添加ComplexDbContext数据库上下文
            builder.Services.AddDbContext<ComplexDbContext>(options => options.UseOracle(connectionString));

            // 注册服务
            builder.Services.AddScoped<PromotionService>();
            builder.Services.AddScoped<DiscountRuleService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}