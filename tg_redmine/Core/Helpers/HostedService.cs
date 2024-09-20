using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Text.RegularExpressions;
using tg_redmine.Core.Models;
using tg_redmine.Core.Repositories.Interfaces;
using tg_redmine.Core.Services.Interfaces;
using Models_User = tg_redmine.Core.Models.User;

namespace tg_redmine.Core.Helpers;

public partial class HostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<HostedService> _logger;
    private readonly int _requestFrequency;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly IHostApplicationLifetime _appLifetime;

    private const int MaxRetryAttempts = 10;
    private const int InitialRetryDelay = 2;

    public HostedService(
        IServiceScopeFactory serviceScopeFactory, 
        ILogger<HostedService> logger, 
        int requestFrequency,
        IHostApplicationLifetime appLifetime)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestFrequency = requestFrequency;
        _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
        _retryPolicy = CreateRetryPolicy();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RegisterApplicationLifetimeEvents();
        _logger.LogInformation("Запуск приложения...");

        try
        {
            await RunMainLoop(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Критическая ошибка при выполнении основного цикла приложения.");
            _appLifetime.StopApplication();
        }
    }

    private async Task RunMainLoop(CancellationToken stoppingToken)
    {
        var isSuccess = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var services = GetRequiredServices(scope);
                var issues = await _retryPolicy.ExecuteAsync(() => services.IssueRepository.GetIssuesAsync());
                if (!isSuccess)
                {
                    _logger.LogInformation("Соединение установлено, список задач получен");
                    isSuccess = true;
                }

                await ClearChatAndDb(services, issues.Data, stoppingToken);
                await ProcessIssues(services, issues.Data, stoppingToken);

                if (stoppingToken.IsCancellationRequested) return;

                await Task.Delay(TimeSpan.FromSeconds(_requestFrequency), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке задач");
                isSuccess = false;
            }
        }
    }

    private async Task ProcessIssues(RequiredServices services, List<Issue> issues, CancellationToken stoppingToken)
    {
        var tasks = issues.Select(issue => ProcessOpenIssue(services, issue, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessOpenIssue(RequiredServices services, Issue issue, CancellationToken stoppingToken)
    {
        try
        {
            stoppingToken.ThrowIfCancellationRequested();
            var users = await GetUsers(issue.Telegram, services.UserRepository, stoppingToken);
            var currentMessage = await services.MessageRepository.GetMessagesByIssueIdAsync(issue.Id, stoppingToken);
            if (!currentMessage.IsSuccess)
            {
                await HandleNewMessage(services, issue, users, stoppingToken);
            }
            else if (currentMessage.Data.Any(i => i.UpdatedOn < issue.UpdatedOn))
            {
                await HandleUpdateMessage(services, issue, users, stoppingToken);
            }
            else if (currentMessage.HasErrors)
            {
                throw new Exception();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке задачи {IssueId}", issue.Id);
        }
    }

    private static async Task HandleNewMessage(RequiredServices services, Issue issue, List<Models_User> users, CancellationToken stoppingToken)
    {
        if(!string.IsNullOrWhiteSpace(issue.Comment))
            issue.Comment = ClearHTML(issue.Comment!);
        await services.NotificationService.NotifyUser(issue, users, stoppingToken);
    }

    private static async Task HandleUpdateMessage(RequiredServices services, Issue issue, List<Models_User> users, CancellationToken stoppingToken)
    {
        if(!string.IsNullOrWhiteSpace(issue.Comment))
            issue.Comment = ClearHTML(issue.Comment!);
        await services.NotificationService.UpdateMessage(issue, users, stoppingToken);
    }

    private static async Task ClearChatAndDb(RequiredServices services, List<Issue> issues, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
    
        var openIssueIds = issues.Select(i => i.Id).ToHashSet();
        await ClearMessages(services.MessageRepository, services.NotificationService, openIssueIds, stoppingToken);
    }

    private static async Task ClearMessages(IMessageRepository messageRepository, INotificationService notificationService, HashSet<int> openIssueIds, CancellationToken stoppingToken)
    {
        // Все сообщения, которые не связаны с переданными openIssueIds, считаются закрытыми и удаляются
        var closedMessages = await messageRepository.GetMessagesExcludingIdsAsync(openIssueIds, stoppingToken);
        if (closedMessages.IsSuccess)
        {
            foreach (var message in closedMessages.Data)
            {
                await notificationService.DeleteMessage(message, stoppingToken);
                await messageRepository.DeleteMessageAsync(message, stoppingToken);
            }
        }
        // удаляем все сообщения старше 45 часов
        var oldMessages = await messageRepository.GetOldMessagesAsync(stoppingToken);
        if (oldMessages.IsSuccess)
        {
            foreach (var message in oldMessages.Data)
            {
                await notificationService.DeleteMessage(message, stoppingToken);
                await messageRepository.DeleteMessageAsync(message, stoppingToken);
            }
        }
    }
    
    private static string ClearHTML(string html)
    {
        return Regex.Replace(html,"<.*?>", string.Empty);
    }

    private static async Task<List<Models_User>> GetUsers(IEnumerable<string> telegramLogins, IUserRepository userRepository, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var users = new List<Models_User>();

        foreach (var telegramLogin in telegramLogins)
        {
            var user = await GetOrCreateUserAsync(userRepository, telegramLogin, stoppingToken);
            if(user.ChatId != 0)
                users.Add(user);
        }

        return users;
    }

    private static async Task<Models_User> GetOrCreateUserAsync(IUserRepository userRepository, string telegramLogin, CancellationToken stoppingToken)
    {
        var user = await userRepository.GetUserByLogin(telegramLogin, stoppingToken);
        if (user.IsSuccess)
            return user.Data;
        user = await userRepository.AddUser(telegramLogin, stoppingToken);
        
        return user.HasErrors ? new Models_User() : user.Data;
    }

    private AsyncRetryPolicy CreateRetryPolicy()
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(InitialRetryDelay, retryAttempt)),
                OnRetry);
    }

    private void OnRetry(Exception exception, TimeSpan timeSpan, int retryCount, Context context)
    {
        _logger.LogWarning(exception, "Ошибка при попытке {RetryCount} получения задач. Повторная попытка через {TimeSpan}...", retryCount, timeSpan);
    }

    private void RegisterApplicationLifetimeEvents()
    {
        _appLifetime.ApplicationStarted.Register(() => _logger.LogInformation("Приложение запущено."));
        _appLifetime.ApplicationStopping.Register(() => _logger.LogInformation("Приложение останавливается..."));
        _appLifetime.ApplicationStopped.Register(() => _logger.LogInformation("Приложение остановлено."));
    }

    private RequiredServices GetRequiredServices(IServiceScope scope)
    {
        return new RequiredServices
        {
            IssueRepository = scope.ServiceProvider.GetRequiredService<IIssueRepository>(),
            NotificationService = scope.ServiceProvider.GetRequiredService<INotificationService>(),
            MessageRepository = scope.ServiceProvider.GetRequiredService<IMessageRepository>(),
            UserRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>(),
        };
    }

    private class RequiredServices
    {
        public required IIssueRepository IssueRepository { get; init; }
        public required INotificationService NotificationService { get; init; }
        public required IMessageRepository MessageRepository { get; init; }
        public required IUserRepository UserRepository { get; init; }
    }
}
