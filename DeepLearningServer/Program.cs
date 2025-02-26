using DeepLearningServer.Classes;
using DeepLearningServer.Models;
using DeepLearningServer.Services;
using DeepLearningServer.Settings;
using Euresys.Open_eVision;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        //Console.WriteLine("Checking license..");
        //bool hasLicense = Easy.CheckLicense(Euresys.Open_eVision.LicenseFeatures.Features.EasyClassify);
        //Console.WriteLine($"Has license: {hasLicense}");
        //if (!hasLicense) throw new Exception("No license found");

        var builder = WebApplication.CreateBuilder(args);

        // 환경 변수 추가 등 설정
        builder.Configuration.AddEnvironmentVariables();
        builder.Services.Configure<ServerSettings>(builder.Configuration.GetSection("ServerSettings"));
        //builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("DatabaseSettings"));
        builder.Services.Configure<SqlDbSettings>(
            builder.Configuration.GetSection("ConnectionStrings")
        );

        var connectionString = builder.Configuration.GetSection("ConnectionStrings:DefaultConnection").Value;

        // MSSQL 데이터베이스 연결 설정
        builder.Services.AddDbContext<DlServerContext>(options =>
        {
            var dbSettings = builder.Configuration.GetSection("ConnectionStrings").Get<SqlDbSettings>();
            options.UseSqlServer(connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(DlServerContext).Assembly.GetName().Name));
        });
        //builder.Services.AddSingleton<MongoDbService>(sp =>
        //{
        //    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>();
        //    return new MongoDbService(settings);
        //});
        builder.Services.AddSingleton<MssqlDbService>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<SqlDbSettings>>();
            var configuration = sp.GetRequiredService<IConfiguration>();
            return new MssqlDbService(settings, configuration);
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
            options.ListenAnyIP(8082);
        });

        var app = builder.Build();
        using (var scope = builder.Services.BuildServiceProvider().CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DlServerContext>();
            try
            {
                dbContext.Database.Migrate(); // 자동 마이그레이션 수행
            }
            catch (Exception error)
            {
                Console.WriteLine("Error on database migration");
            }
        }
        //app.MapOpenApi();
        app.MapGet("/", () =>
            "Greetings from deep learning server " + DateTime.Now.ToLongTimeString());
        app.UseMiddleware<GlobalExceptionMiddleware>();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors("AllowAll");
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
