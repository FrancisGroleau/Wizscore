using Microsoft.EntityFrameworkCore;
using Wizscore.Persistence;

namespace Wizscore.Extensions
{
    public static class EntityFrameworkExtensions
    {
        public static WebApplicationBuilder SetupPersistence(this WebApplicationBuilder builder)
        {
            builder.Services.SetupPersistenceServices(builder.Configuration);
            return builder;
        }

        public static IServiceCollection SetupPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<WizscoreContext>((options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("wizcoreDb"),
                    sqlServerOption => sqlServerOption.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: System.TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null)
                        .CommandTimeout(configuration.GetSection("Settings").GetValue<int>("CommandTimeoutInSeconds")));

            });

            return services;
        }

        public static WebApplication ApplyMigrations(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            try
            {
                var context = scope.ServiceProvider.GetRequiredService<WizscoreContext>();
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
                var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("ApplyMigration");
                ApplyMigrationExceptionLoggingMessage(logger, ex.Message, ex);
                scope.Dispose();
            }

            return app;
        }

        private static readonly Action<ILogger, string, Exception> ApplyMigrationExceptionLoggingMessage =
            LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1,
                    nameof(ApplyMigrationExceptionLoggingMessage)),
                "Error while applying Migration on startup: {message}");

    }

}
