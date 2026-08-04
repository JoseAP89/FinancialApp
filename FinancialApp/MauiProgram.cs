using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using FinancialApp.Data;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
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

            // Configure EF Core DbContext to use a SQLite file in app data directory
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "financialapp.db");
            builder.Services.AddDbContext<FinancialDbContext>(options =>
            {
                options.UseSqlite($"Data Source={dbPath}");
            });

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
