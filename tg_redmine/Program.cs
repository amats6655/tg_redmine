using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Redmine.Net.Api;
using Serilog;
using Serilog.Settings.Configuration;
using Telegram.Bot;
using tg_redmine.Core.Helpers;
using tg_redmine.Core.Repositories.Implementations;
using tg_redmine.Core.Repositories.Interfaces;
using tg_redmine.Core.Services.Implementations;
using tg_redmine.Core.Services.Interfaces;
using tg_redmine.Data;
using tg_redmine.RedmineIntegration;

namespace tg_redmine;

public class Program
{
	public static async Task Main(string[] args)
	{
		var host = CreateHostBuilder(args).Build();

		using (var scope = host.Services.CreateScope())
		{
			var services = scope.ServiceProvider;
			try
			{
				SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
			}
			catch (Exception ex)
			{
				var logger = services.GetRequiredService<ILogger<Program>>();
				logger.LogError(ex, $"An error occurred while initializing the database.");
			}
		}

		await host.RunAsync();
	}

private static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((config) =>
        {
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddEnvironmentVariables();
        })
        .UseSerilog((context, _, configuration) => {
            var options = new ConfigurationReaderOptions(
                typeof(ConsoleLoggerConfigurationExtensions).Assembly, // Для консольного логгера
                typeof(FileLoggerConfigurationExtensions).Assembly // Для файлового логгера
            );

            configuration.ReadFrom.Configuration(context.Configuration, options)
                        .Enrich.FromLogContext();
        })
        .ConfigureServices((context, services) =>
        {
            var configuration = context.Configuration;

            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
			services.Configure<HostingSettings>(configuration.GetSection("HostingSettings"));
			services.Configure<RedmineSettings>(configuration.GetSection("RedmineSettings"));
            services.Configure<TelegramSettings>(configuration.GetSection("TelegramSettings"));
            services.Configure<IssuesSettings>(configuration.GetSection("IssuesViewConnection"));
            services.AddMemoryCache();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IIssueRepository, IssueRepository>(sp =>
            {
	            var connectionString = configuration.GetConnectionString("IssuesViewConnection");
	            return new IssueRepository(connectionString);
            });
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IAdminRepository, AdminRepository>();
            
            services.AddSingleton<RedmineService>(sp =>
            {
	            var redmineSettings = sp.GetRequiredService<IOptions<RedmineSettings>>().Value;
	            var redmineManager = new RedmineManager(new RedmineManagerOptionsBuilder()
		            .WithHost(redmineSettings.Url)
		            .WithApiKeyAuthentication(redmineSettings.ApiKey));
	            return new RedmineService(redmineManager);
            });
            
            services.AddSingleton<TelegramBotClient>(sp =>
            {
                var telegramSettings = sp.GetRequiredService<IOptions<TelegramSettings>>().Value;
                return new TelegramBotClient(telegramSettings.Token!);
            });
            services.AddSingleton<Dictionary<long, string>>();
            services.AddSingleton<IUpdateHandler, UpdateHandler>();
            services.AddSingleton<ICommandHandler, CommandHandler>();

            services.AddSingleton<TelegramBotService>(sp => new TelegramBotService(
	            sp.GetRequiredService<TelegramBotClient>(),
	            sp.GetRequiredService<ILogger<TelegramBotService>>(),
	            sp.GetRequiredService<IUpdateHandler>()));

            services.AddHostedService<HostedService>(sp =>
            {
                var hostedSettings = sp.GetRequiredService<IOptions<HostingSettings>>().Value;
                return new HostedService(
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    sp.GetRequiredService<ILogger<HostedService>>(),
                    hostedSettings.RequestFrequency,
                    sp.GetRequiredService<IHostApplicationLifetime>());
            });
        });
}