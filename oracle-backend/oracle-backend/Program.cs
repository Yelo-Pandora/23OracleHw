// Program.cs

// 1. 引入必要的命名空间
using Microsoft.EntityFrameworkCore;
// using YourApp.Data; // 假设你的DbContext在这个命名空间下

var builder = WebApplication.CreateBuilder(args);

// --- 服务注册区域 (Add services to the container) ---

// 2. 配置CORS策略
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:8080") // 允许你的Vue前端地址
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// 3. 注册数据库上下文
// builder.Services.AddDbContext<YourDbContext>(options =>
//    options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- 中间件配置区域 (Configure the HTTP request pipeline) ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 4. 启用CORS策略
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthorization();

app.MapControllers();

app.Run();