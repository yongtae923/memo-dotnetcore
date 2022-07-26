using System.Reflection;
using AblyAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Service를 container에 추가합니다.
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers();

// SwaggerUI를 통해 API를 읽고 테스트하기 쉽게 만듭니다.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Ably API",
        Description = "회원가입 및 비밀번호 재설정을 할 수 있는 에이블리 과제 API written by 김용태"
    });

    // using System.Reflection;
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// InMemory Database : 실제 데이터베이스를 사용하려면 이 부분을 교체하면 됩니다.
builder.Services.AddDbContext<DatabaseContext>(options =>
{
    options.UseInMemoryDatabase("InMemoryDB");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();