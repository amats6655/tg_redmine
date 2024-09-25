using System.Text;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.Enums;
using tg_redmine.Core.Host;
using tg_redmine.Core.Models;
using tg_redmine.Core.Repositories.Interfaces;
using tg_redmine.Core.Services.Interfaces;

namespace tg_redmine.Core.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly ITelegramBotService _telegramBot;
    private readonly IMessageRepository _messageRepository;

    public NotificationService(ITelegramBotService telegramBot, 
        ILogger<NotificationService> logger,
        IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telegramBot = telegramBot ?? throw new ArgumentNullException(nameof(telegramBot));
    }

    public async Task NotifyUser(Issue issue, List<User> users, CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(issue);
        if (users == null || users.Count == 0) throw new ArgumentException("Users list cannot be null or empty", nameof(users));

        stoppingToken.ThrowIfCancellationRequested();

        var userGroups = GroupUsersByChatAndThread(users);
        var notificationTasks = userGroups.Select(group => SendNotificationToGroup(issue, group, stoppingToken));

        await Task.WhenAll(notificationTasks);
    }

    public async Task UpdateMessage(Issue issue, List<User> users, CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(issue);
        if (users == null || users.Count == 0) throw new ArgumentException("Users list cannot be null or empty", nameof(users));

        stoppingToken.ThrowIfCancellationRequested();

        var currentMessagesResponse = await _messageRepository.GetMessagesByIssueIdAsync(issue.Id, stoppingToken);
        if (!currentMessagesResponse.IsSuccess)
        {
            _logger.LogWarning("Не удалось получить текущие сообщения для задачи {Id}", issue.Id);
            return;
        }

        var messageText = GenerateMessage(issue, users);

        var updateTasks = currentMessagesResponse.Data.Select(currentMessage => UpdateMessageForChat(currentMessage, messageText, issue, stoppingToken));

        await Task.WhenAll(updateTasks);
    }

    public async Task DeleteMessage(Models.Message message, CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        stoppingToken.ThrowIfCancellationRequested();

        try
        {
            await _telegramBot.DeleteMessageAsync(message.ChatId, message.MessageId, stoppingToken);
            _logger.LogInformation("Сообщение удалено из чата {ChatId}, ID сообщения {MessageId}, ID заявки {IssueID}", message.ChatId, message.MessageId, message.IssueId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Произошла ошибка при удалении сообщения в чате {ChatId}, ID сообщения {MessageId}, ID заявки {IssueID}", message.ChatId, message.MessageId, message.IssueId);
        }
    }

    private IEnumerable<IGrouping<(long ChatId, int ThreadId), User>> GroupUsersByChatAndThread(List<User> users)
    {
        return users.GroupBy(u => (u.ChatId, u.ThreadId));
    }

    private async Task SendNotificationToGroup(Issue issue, IGrouping<(long ChatId, int ThreadId), User> group, CancellationToken stoppingToken)
    {
        var messageText = GenerateMessage(issue, group.ToList());

        try
        {
            var message = group.Key.ThreadId != 0
                ? await _telegramBot.SendMessageThreadAsync(group.Key.ChatId, group.Key.ThreadId, messageText, ParseMode.Html, stoppingToken, issue.Id, issue.Status)
                : await _telegramBot.SendMessageAsync(group.Key.ChatId, messageText, ParseMode.Html, stoppingToken, issue.Id, issue.Status);

            await _messageRepository.AddMessageAsync(message, issue, stoppingToken);

            _logger.LogInformation("Уведомление отправлено в чат {Id} - {Title}, {ThreadId}. ID сообщения {MessageId}, id задачи {issueId}", 
                message.Chat.Id, message.Chat.Title, message.MessageThreadId, message.MessageId, issue.Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка при отправке уведомления в чат {ChatId}, ThreadId {ThreadId}, id задачи {IssueId}", 
                group.Key.ChatId, group.Key.ThreadId, issue.Id);
        }
    }

    private async Task UpdateMessageForChat(Models.Message currentMessage, string messageText, Issue issue, CancellationToken stoppingToken)
    {
        try
        {
            var updatedMessage = await _telegramBot.EditMessageAsync(currentMessage.ChatId, currentMessage.MessageId, messageText, ParseMode.Html, stoppingToken, issue.Id, issue.Status);
            var result = await _messageRepository.UpdateMessage(updatedMessage, issue, stoppingToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Сообщение изменено в чате {ChatId} - {Title}, {ThreadId}. ID сообщения {MessageId}, id задачи {Id}",
                    updatedMessage.Chat.Id, updatedMessage.Chat.Title, updatedMessage.MessageThreadId, updatedMessage.MessageId, issue.Id);
            }
            else
            {
                _logger.LogWarning("Не удалось изменить сообщение в чате {ChatId} - {Title}, {ThreadID}. ID сообщения {MessageID}, id задачи {id}, ошибка {error}",
                    updatedMessage.Chat.Id, updatedMessage.Chat.Title, updatedMessage.MessageThreadId, updatedMessage.MessageId, issue.Id, result.Message);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Произошла ошибка при обновлении сообщения в чате {ChatId}. ID сообщения {MessageId}, id задачи {Id}",
                currentMessage.ChatId, currentMessage.MessageId, issue.Id);
        }
    }

    private static string GenerateMessage(Issue issue, List<User> users)
    {
        var msg = new StringBuilder();

        msg.Append(GetHeader(issue))
           .Append(GetPriority(issue))
           .Append($"\n \ud83c\udfd8 <b>Корпус:</b> #{issue?.Corpus ?? "Неизвестен"}")
           .Append($"\n <b>Номер комнаты:</b> {issue?.RoomNumber ?? "Неизвестен"}")
           .Append($"\n <b>Тема:</b> {issue?.Subject ?? "Неизвестно"} \n")
           .Append(GetStatus(issue!))
           .Append($"\n \u270d\ufe0f <b>Автор:</b> {issue?.Author ?? "Неизвестно"}")
           .Append($"\n \ud83d\udcc6 <b>Создана:</b> {issue?.CreatedOn}")
           .Append($"\n \ud83d\udcc6 <b>Обновлена:</b> {issue?.UpdatedOn}");

        if (!string.IsNullOrEmpty(issue?.Comment))
        {
            msg.Append("\n\n \ud83d\udcdd <b>Последний комментарий</b>")
               .Append($"\n <b>Автор:</b> {issue.Commentator} \n {issue.Comment}");
        }

        msg.Append("\n\n");
        foreach (var user in users)
        {
            msg.Append($"{user.TelegramLogin ?? "Неизвестно"} ");
        }

        return msg.ToString();
    }

    private static string GetPriority(Issue issue) =>
        issue.Priority switch
        {
            "Критический" => "\n ‼\ufe0f <b>Приоритет:</b> Критический",
            "Высокий" => "\n \u2757 <b>Приоритет:</b> Высокий",
            _ => $"\n <b>Приоритет:</b> {issue.Priority ?? "Неизвестен"}"
        };

    private static string GetHeader(Issue issue) =>
        $"""<b>Задача:</b> <a href="https://sd.talantiuspeh.ru/issues/{issue?.Id}">#{issue?.Id}</a>""";

    private static string GetStatus(Issue issue) =>
        issue.Status switch
        {
            "Новая" => "\n \ud83d\udc49 <b>Статус:</b> Новая",
            "В работе" => "\n \ud83d\udc4d <b>Статус:</b> В работе",
            "Решена" => "\n \u2705 <b>Статус:</b> Решена",
            "Закрыта" => "\n \u2705 <b>Статус:</b> Закрыта",
            "В ожидании" => "\n \ud83e\udd14 <b>Статус:</b> В ожидании",
            _ => $"\n <b>Статус:</b> {issue.Status ?? "Неизвестно"}"
        };
}