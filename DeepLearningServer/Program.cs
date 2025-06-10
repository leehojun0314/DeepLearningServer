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

// 시딩 스크립트 실행 메서드


public class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        //Console.WriteLine("Checking license..");
        //bool hasLicense = Easy.CheckLicense(Euresys.Open_eVision.LicenseFeatures.Features.EasyClassify);
        //Console.WriteLine($"Has license: {hasLicense}");
        //if (!hasLicense) throw new Exception("No license found");

        var builder = WebApplication.CreateBuilder(args);

        // ȯ�� ���� �߰� �� ����
        builder.Configuration.AddEnvironmentVariables();
        builder.Services.Configure<ServerSettings>(builder.Configuration.GetSection("ServerSettings"));
        //builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("DatabaseSettings"));
        builder.Services.Configure<SqlDbSettings>(
            builder.Configuration.GetSection("ConnectionStrings")
        );

        var connectionString = builder.Configuration.GetSection("ConnectionStrings:DefaultConnection").Value;

        // MSSQL �����ͺ��̽� ���� ����
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
            //options.SchemaFilter<EnumSchemaFilter>();

            // XML 주석 파일 경로 설정
            var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        });
        builder.WebHost.ConfigureKestrel(options =>
        {
            int port = builder.Configuration.GetValue<int>("ServerSettings:PORT");
            Console.WriteLine("PORT" + port);
            options.ListenAnyIP(port, listenOptions =>
            {
                // �ִ� ��û ũ�⸦ 1000MB�� ���� (���ϴ� ũ��� ���� ����)
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
                Console.WriteLine("Starting database migration...");
                dbContext.Database.Migrate(); // 자동 마이그레이션 실행
                Console.WriteLine("Database migration completed successfully.");

                // 시딩 작업 실행
                Console.WriteLine("Starting database seeding...");
                await ExecuteSeedScript(dbContext);
                Console.WriteLine("Database seeding completed successfully.");
            }
            catch (Exception error)
            {
                Console.WriteLine($"Error during database initialization: {error.Message}");
            }
        }
        //app.MapOpenApi();
        app.MapGet("/", () =>
        {
            string version = "Unknown";
            try
            {
                version = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "version.txt")).Trim();
            }
            catch (Exception)
            {
                // If version.txt is not found or cannot be read, keep default "Unknown"
            }

            return new
            {
                Status = "OK",
                Message = "ADMS DeepLearning Server is running",
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Version = version,
                Environment = app.Environment.EnvironmentName
            };
        });
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
    static async Task ExecuteSeedScript(DlServerContext dbContext)
    {
        try
        {
            string seedFilePath = Path.Combine(AppContext.BaseDirectory, "seed.sql");

            if (!File.Exists(seedFilePath))
            {
                Console.WriteLine("seed.sql file not found. Skipping seeding.");
                return;
            }

            string seedScript = await File.ReadAllTextAsync(seedFilePath);

            if (string.IsNullOrWhiteSpace(seedScript))
            {
                Console.WriteLine("seed.sql file is empty. Skipping seeding.");
                return;
            }

            // SQL 스크립트를 배치로 나누어 실행 (GO 구분자 기준)
            var batches = seedScript.Split(new[] { "\nGO\n", "\nGO\r\n", "\rGO\r", "\rGO\n" },
                                          StringSplitOptions.RemoveEmptyEntries);

            foreach (var batch in batches)
            {
                var trimmedBatch = batch.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedBatch))
                {
                    await dbContext.Database.ExecuteSqlRawAsync(trimmedBatch);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing seed script: {ex.Message}");
            throw;
        }
    }
}
