
using KIFRIOSSE.ASTFRI.SDK;
using KIFRIOSSE.ASTFRI.Web.API.Services;
using System.Threading.RateLimiting;

namespace KIFRIOSSE.ASTFRI.Web.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Configure max request size limit (5 MB)
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Limits.MaxRequestBodySize = 5_242_880; // 5 MB
            });

            // Configure rate limiting (60 requests per minute)
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 60,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            builder.Services.AddControllers();
            builder.Services.AddSingleton<IContentValidationService, ContentValidationService>();

            builder.Services.AddSingleton<AstfriCLI>(sp => {
                var config = builder.Configuration.GetSection("AstfriCLI").Get<AstfriCLI.Configuration>();
                if (config == null)
                {
                    throw new InvalidOperationException("AstfriCLI configuration is missing or invalid. Check appsettings.json config file.");
                }

                return new AstfriCLI(config);
            });
            
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            
            // swagger support
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Validate AstfriCLI configuration on startup
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    scope.ServiceProvider.GetRequiredService<AstfriCLI>();
                }
            }
            catch (InvalidOperationException ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogCritical(ex, "AstfriCLI configuration error: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogCritical(ex, "Unexpected error during AstfriCLI initialization: {Message}", ex.Message);
                throw;
            }

            // Configure the HTTP request pipeline.

            // CORS must be before rate limiting to allow preflight requests
            app.UseCors();

            // Enable rate limiting (after CORS)
            app.UseRateLimiter();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
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
