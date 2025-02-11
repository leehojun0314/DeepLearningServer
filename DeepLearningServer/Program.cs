using DeepLearningServer.Classes;
using DeepLearningServer.Services;
using DeepLearningServer.Settings;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 환경 변수 추가 등 설정
        builder.Configuration.AddEnvironmentVariables();
        builder.Services.Configure<ServerSettings>(builder.Configuration.GetSection("ServerSettings"));
        builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("DatabaseSettings"));

        // MSSQL 데이터베이스 연결 설정
        builder.Services.AddDbContext<DbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
        );
        builder.Services.AddSingleton<MongoDbService>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>();
            return new MongoDbService(settings);
        });
        builder.Services.AddAutoMapper(typeof(TrainingMappingProfile));
        builder.Services.AddControllers();
        //builder.Services.AddOpenApi();
        //builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SupportNonNullableReferenceTypes();
            options.UseAllOfToExtendReferenceSchemas();
        });
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8080);
        });

        var app = builder.Build();

        //app.MapOpenApi();
        app.MapGet("/", () =>
            "Greetings from deep learning server " + DateTime.Now.ToLongTimeString());

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors("AllowAll");
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
