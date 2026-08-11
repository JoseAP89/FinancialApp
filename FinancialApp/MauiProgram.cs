using Microsoft.Extensions.Logging;
using FinancialApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using Microsoft.Maui.Storage;
using FinancialApp.Infrastructure.Services;
using FinancialApp.Core.Services;

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
                          Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

#if DEBUG
            // When debugging locally (including Android emulator) prefer Development if no env var set
            if (string.IsNullOrWhiteSpace(envName))
            {
                envName = "Development";
            }
            System.Diagnostics.Debug.WriteLine("Environment detected: " + envName); 
            System.Diagnostics.Debug.WriteLine("Raw config value: " + builder.Configuration["ConnectionStrings:DefaultConnection"]);
#else
            envName ??= "Production";
#endif

            // Helpful output when debugging launch/environment issues
            System.Diagnostics.Debug.WriteLine($"Environment detected: {envName}");

            // Try the normal file provider first (works on desktop).
            builder.Configuration
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: true);

            // On some platforms the file provider may not find appsettings files
            // when packaged. Instead of synchronously trying to open package files
            // (which can throw on some platforms), prefer a safe fallback: if the
            // configuration key is missing, use an application-local DB path.

            // If a connection string is present in configuration, register the DbContext.
            // Ensure a packaged DB is copied to the app data folder on first run.
            // This will throw if the packaged DB is missing or cannot be copied,
            // stopping startup (per user preference).
            const string packagedDbFileName = "PersonalFinanceDB.db";

            // Ensure the packaged DB is copied to the app data folder on first run.
            EnsureDatabaseCopiedAsync(packagedDbFileName).GetAwaiter().GetResult();

            // If a connection string is present in configuration, register the DbContext.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // If configuration did not provide a connection string (common on mobile),
            // use the copied app data file so the app can run in Development.
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var dbFile = Path.Combine(FileSystem.AppDataDirectory, packagedDbFileName);
                connectionString = $"Data Source={dbFile}";
                System.Diagnostics.Debug.WriteLine($"Using app-local DB: {dbFile}");
            }

            builder.Services.AddDbContext<FinancialDbContext>(options =>
            {
                options.UseSqlite(connectionString);
            });

            // NOTE: DbContext registration can be provided via configuration (appsettings.json)
            // above. If no connection string is present the DbContext must be configured externally.

            // Register repository pattern implementations
            builder.Services.AddScoped(typeof(Data.Repositories.IRepository<>), typeof(Data.Repositories.Repository<>));
            // Toast service acts as a global in-process event hub; register as singleton so
            // all components receive the same instance and events propagate as expected.
            builder.Services.AddSingleton<IToastService, ToastService>();
            builder.Services.AddScoped<ITransactionBalancingService, TransactionBalancingService>();
            builder.Services.AddScoped<Data.Repositories.IAccountRepository, Data.Repositories.AccountRepository>();
            builder.Services.AddScoped<Data.Repositories.ITransactionRepository, Data.Repositories.TransactionRepository>();
            builder.Services.AddScoped<Data.Repositories.ITransactionLineRepository, Data.Repositories.TransactionLineRepository>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Manual migrations only: do NOT call Database.Migrate() here.
            // Run migrations locally using the EF tools and the DesignTimeDbContextFactory.

            return app;
        }

        static async System.Threading.Tasks.Task EnsureDatabaseCopiedAsync(string dbFileName)
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, dbFileName);
            try
            {
                // If DB does not exist on device, copy the packaged DB as a baseline.
                if (!File.Exists(dbPath))
                {
                    using var src = await FileSystem.OpenAppPackageFileAsync(dbFileName);
                    if (src == null)
                        throw new FileNotFoundException($"Packaged database '{dbFileName}' not found in app package.");

                    // Ensure directory exists
                    var dir = Path.GetDirectoryName(dbPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir!);

                    using var dest = File.Create(dbPath);
                    await src.CopyToAsync(dest);
                    System.Diagnostics.Debug.WriteLine($"Copied DB to {dbPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy DB to app data: {ex.Message}");
                // Stop startup by rethrowing with context
                throw new InvalidOperationException($"Unable to prepare embedded database '{dbFileName}'", ex);
            }
        }
    }
}
