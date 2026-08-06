using Microsoft.Extensions.Logging;
using FinancialApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using Microsoft.Maui.Storage;

namespace FinancialApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // Load configuration from appsettings.json and environment-specific file.
            var envName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                          Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                          "Production";

            builder.Configuration
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: true);

            // If a connection string is present in configuration, register the DbContext.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                builder.Services.AddDbContext<FinancialDbContext>(options =>
                {
                    options.UseSqlite(connectionString);
                });
            }
            else
            {
                // No connection string provided; DbContext must be configured externally.
            }

            // NOTE: DbContext registration can be provided via configuration (appsettings.json)
            // above. If no connection string is present the DbContext must be configured externally.

            // Register repository pattern implementations
            builder.Services.AddScoped(typeof(Data.Repositories.IRepository<>), typeof(Data.Repositories.Repository<>));
            builder.Services.AddScoped<Data.Repositories.IAccountRepository, Data.Repositories.AccountRepository>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Manual migrations only: do NOT call Database.Migrate() here.
            // Run migrations locally using the EF tools and the DesignTimeDbContextFactory.

            return app;
        }
    }
}
