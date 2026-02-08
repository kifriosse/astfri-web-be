
using KIFRIOSSE.ASTFRI.SDK;

namespace KIFRIOSSE.ASTFRI.Web.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

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
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();

                // CORS support for development
                app.UseCors(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
