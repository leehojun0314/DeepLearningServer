using DeepLearningServer.Classes;
using DeepLearningServer.Models;
using DeepLearningServer.Services;
using DeepLearningServer.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using DeepLearningServer.Swagger;
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
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            //ValidIssuer = builder.Configuration["Jwt:Issuer"],
            //ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

        builder.Services.AddAuthorization();
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
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });
            options.SchemaFilter<EnumSchemaFilter>();

        });
        builder.WebHost.ConfigureKestrel(options =>
        {
            int port = builder.Configuration.GetValue<int>("ServerSettings:PORT");
            Console.WriteLine("PORT" + port);
            options.ListenAnyIP(port, listenOptions =>
            {
                // 최대 요청 크기를 1000MB로 설정 (원하는 크기로 조정 가능)
                listenOptions.KestrelServerOptions.Limits.MaxRequestBodySize = 1000 * 1024 * 1024;
            });
            //int port = 8082
            //options.ListenAnyIP(8082);
            Console.WriteLine($"Listening port : {port}");
        });

        var app = builder.Build();
        using (var scope = builder.Services.BuildServiceProvider().CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DlServerContext>();
            try
            {
                dbContext.Database.Migrate(); // 자동 마이그레이션 수행
                bool enableAdminSeed = builder.Configuration.GetValue<bool>("ServerSettings:EnableAdminSeed");
                ServerSettings? serverSettings = builder.Configuration.GetSection("ServerSettings").Get<ServerSettings>();
                if (serverSettings == null)
                {
                    throw new Exception("Failed to load server settings");
                }
                DbInitializer.Initialize(dbContext, serverSettings); // 기본 역할(Role) 데이터 시딩
               
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
        ToolStatusManager.SetProcessRunning(false);
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors("AllowAll");
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
