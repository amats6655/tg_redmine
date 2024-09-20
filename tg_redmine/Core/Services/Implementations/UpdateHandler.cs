using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using tg_redmine.Core.Helpers;
using tg_redmine.Core.Repositories.Interfaces;
using tg_redmine.Core.Services.Interfaces;
using IUpdateHandler = tg_redmine.Core.Services.Interfaces.IUpdateHandler;
using File = System.IO.File;
using Interfaces_IUpdateHandler = tg_redmine.Core.Services.Interfaces.IUpdateHandler;

namespace tg_redmine.Core.Services.Implementations;

public class UpdateHandler : Interfaces_IUpdateHandler
{
    private readonly TelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IAdminRepository _adminRepository;
    private readonly Dictionary<long, string> _userStates;
    private readonly ICommandHandler _commandHandler;

    public UpdateHandler(TelegramBotClient botClient, ILogger<UpdateHandler> logger, 
        IAdminRepository adminRepository, Dictionary<long, string> userStates, 
        ICommandHandler commandHandler)
    {
        _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adminRepository = adminRepository ?? throw new ArgumentNullException(nameof(adminRepository));
        _userStates = userStates ?? throw new ArgumentNullException(nameof(userStates));
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
    }

    public async Task HandleUpdateAsync(Update update)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.CallbackQuery:
                    await HandleCallbackQueryAsync(update.CallbackQuery);
                    break;
                case UpdateType.Message:
                    await _commandHandler.HandleMessageAsync(update.Message);
                    break;
                default:
                    _logger.LogWarning($"Необработанный тип обновления: {update.Type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке обновления");
        }
    }
    
    private async Task HandleCallbackQueryAsync(CallbackQuery query)
    {
        if (query?.Data == null)
        {
            _logger.LogWarning("Получен пустой CallbackQuery");
            return;
        }

        switch (query.Data)
        {
            case "users":
                await HandleUsersMenuAsync(query);
                break;
            case "messages":
                await HandleGetMessagesCommandAsync(query);
                break;
            case "logs":
                await HandleGetLogsCommandAsync(query);
                break;
            case "show_users":
                await HandleGetUsersCommandAsync(query);
                break;
            case "add_user":
                await HandleAddUserCommandAsync(query);
                break;
            case "update_user":
                await HandleUpdateUserCommandAsync(query);
                break;
            case "delete_user":
                await HandleDeleteUserCommandAsync(query);
                break;
            default:
                _logger.LogWarning($"Неизвестный callback query: {query.Data}");
                break;
        }
    }

    private async Task HandleUsersMenuAsync(CallbackQuery query)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Показать", "show_users") },
            new[] { InlineKeyboardButton.WithCallbackData("Добавить", "add_user") },
            new[] { InlineKeyboardButton.WithCallbackData("Обновить", "update_user") },
            new[] { InlineKeyboardButton.WithCallbackData("Удалить", "delete_user") },
        });

        await _botClient.EditMessageTextAsync(
            chatId: query.Message.Chat.Id,
            messageId: query.Message.MessageId,
            text: "Выберите действие с пользователями:",
            replyMarkup: keyboard
        );
    }

    private async Task HandleGetMessagesCommandAsync(CallbackQuery query)
    {
        if (!await CheckIfAdminAndRespondAsync(query.Message.Chat.Id, query.From.Username, query.From.Id))
            return;

        var messages = await _adminRepository.GetAllMessagesAsync();
        await SendCsvResponseAsync(query, messages, "messages.csv", "сообщений");
    }

    private async Task HandleGetLogsCommandAsync(CallbackQuery query)
    {
        if (!await CheckIfAdminAndRespondAsync(query.Message.Chat.Id, query.From.Username, query.From.Id))
            return;

        var logs = _adminRepository.GetLogsAsync();
        if (logs.IsSuccess)
        {
            await using var stream = File.OpenRead(logs.Data);
            await _botClient.SendDocumentAsync(query.Message.Chat.Id, new InputFileStream(stream, "logs.zip"));
            _logger.LogInformation($"Пользователем {query.From.Username}, {query.From.Id} получены логи");
            File.Delete(logs.Data);
        }
        else
        {
            await _botClient.SendTextMessageAsync(query.Message.Chat.Id, logs.Message);
        }
    }

    private async Task HandleGetUsersCommandAsync(CallbackQuery query)
    {
        if (!await CheckIfAdminAndRespondAsync(query.Message.Chat.Id, query.From.Username, query.From.Id))
            return;

        var users = await _adminRepository.GetAllUsersAsync();
        await SendCsvResponseAsync(query, users, "users.csv", "пользователей");
    }

    private async Task HandleAddUserCommandAsync(CallbackQuery query)
    {
        await _botClient.SendTextMessageAsync(query.Message.Chat.Id,
            "Введите данные для добавления пользователя в формате: @логин chatId userId threadId isAdmin \n " +
            "Пример: @test 123 456 1 0 \n Получить эту информацию можно написав команду /start в чате с ботом.",
            parseMode: ParseMode.Html);
        _userStates[query.Message.Chat.Id] = "awaiting_add_user";
    }

    private async Task HandleUpdateUserCommandAsync(CallbackQuery query)
    {
        await _botClient.SendTextMessageAsync(query.Message.Chat.Id,
            "Введите данные для обновления пользователя в формате: @логин chatId userId threadId isAdmin \n " +
            "Пример: @test 123 456 1 0 \n Получить эту информацию можно написав команду /start в чате с ботом.",
            parseMode: ParseMode.Html);
        _userStates[query.Message.Chat.Id] = "awaiting_update_user";
    }

    private async Task HandleDeleteUserCommandAsync(CallbackQuery query)
    {
        await _botClient.SendTextMessageAsync(query.Message.Chat.Id, "Введите логин пользователя для удаления.");
        _userStates[query.Message.Chat.Id] = "awaiting_delete_user";
    }

    private async Task<bool> CheckIfAdminAndRespondAsync(long chatId, string username, long userId)
    {
        var isAdmin = await _adminRepository.IsAdminAsync(chatId);
        if (!isAdmin)
        {
            _logger.LogWarning($"Пользователь {username}, {userId} попытался выполнить действие без прав администратора.");
            await _botClient.SendTextMessageAsync(chatId, "Недостаточно прав.");
            _userStates.Remove(chatId);
        }
        return isAdmin;
    }

    private async Task SendCsvResponseAsync<T>(CallbackQuery query, IServiceResponse<List<T>> response, string fileName, string itemName)
    {
        if (response.IsSuccess)
        {
            await using var memoryStream = GenerateCsv<T>.GenerateCsvFromList(response.Data);
            await _botClient.SendDocumentAsync(query.Message.Chat.Id, new InputFileStream(memoryStream, fileName));
            _logger.LogWarning($"Пользователем {query.From.Username}, {query.From.Id} получен список {itemName}");
        }
        else
        {
            await _botClient.SendTextMessageAsync(query.Message.Chat.Id, response.Message);
        }
    }
}