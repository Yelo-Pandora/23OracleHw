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
                options.UseOracle(connectionString); // ָ��ʹ�� Oracle �ṩ����������ַ���
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ������ݿ�������
            builder.Services.AddDbContext<PromotionDbContext>(options => options.UseOracle(builder.Configuration.GetConnectionString("PromotionDb")));

            // ע�����
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