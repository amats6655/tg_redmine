using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Interfaces_IUpdateHandler = tg_redmine.Core.Services.Interfaces.IUpdateHandler;

namespace tg_redmine.Core.Host
{
    /// <summary>
    /// Сервис для работы с Telegram ботом.
    /// </summary>
    public class TelegramBotService : ITelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ReceiverOptions _receiverOptions;

        private const int MaxRetryAttempts = 5;
        private const int InitialRetryDelay = 2;

        /// <summary>
        /// Инициализирует новый экземпляр класса TelegramBotService.
        /// </summary>
        public TelegramBotService(TelegramBotClient botClient, ILogger<TelegramBotService> logger,
            Interfaces_IUpdateHandler updateHandler)
        {
            _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _retryPolicy = CreateRetryPolicy();
            _receiverOptions = CreateReceiverOptions();

            InitializeBot(updateHandler);
        }
        
        public TelegramBotService(){}

        /// <summary>
        /// Отправляет сообщение в указанный чат.
        /// </summary>
        public async Task<Message> SendMessageAsync(long chatId, string message, ParseMode parseMode,
            CancellationToken stoppingToken, int issueId, string status)
        {
            var inlineMarkup = new InlineKeyboardMarkup();
            switch (status)
            {
                case "В работе":
                    inlineMarkup.AddButton("Решить заявку", callbackData: $"closeIssue:{issueId}");
                    break;
                default:
                    inlineMarkup.AddButton("Взять в работу", callbackData: $"InWorkIssue:{issueId}");
                    break;
            }
            return await ExecuteWithRetryAsync(() => 
                _botClient.SendTextMessageAsync(chatId, message, parseMode: parseMode, cancellationToken: stoppingToken, replyMarkup: inlineMarkup));
        }

        /// <summary>
        /// Редактирует существующее сообщение в указанном чате.
        /// </summary>
        public async Task<Message> EditMessageAsync(long chatId, int messageId, string message, ParseMode parseMode,
            CancellationToken stoppingToken, int issueId, string status)
        {
            var inlineMarkup = new InlineKeyboardMarkup();
            switch (status)
            {
                case "В работе":
                    inlineMarkup.AddButton("Решить заявку", callbackData: $"closeIssue:{issueId}");
                    break;
                default:
                    inlineMarkup.AddButton("Взять в работу", callbackData: $"InWorkIssue:{issueId}");
                    break;
            }
            return await ExecuteWithRetryAsync(() => 
                _botClient.EditMessageTextAsync(chatId: chatId, messageId: messageId, text: message,
                    parseMode: parseMode, cancellationToken: stoppingToken, replyMarkup: inlineMarkup));
        }

        /// <summary>
        /// Отправляет сообщение в указанный тред супер-чата.
        /// </summary>
        public async Task<Message> SendMessageThreadAsync(long chatId, int threadId, string message,
            ParseMode parseMode, CancellationToken stoppingToken, int issueId, string status)
        {
            var inlineMarkup = new InlineKeyboardMarkup();
            switch (status)
            {
                case "В работе":
                    inlineMarkup.AddButton("Решить заявку", callbackData: $"closeIssue:{issueId}");
                    break;
                default:
                    inlineMarkup.AddButton("Взять в работу", callbackData: $"InWorkIssue:{issueId}");
                    break;
            }
            
            return await ExecuteWithRetryAsync(() =>
                _botClient.SendTextMessageAsync(chatId: chatId, messageThreadId: threadId, text: message,
                    parseMode: parseMode, cancellationToken: stoppingToken, replyMarkup: inlineMarkup));
        }

        /// <summary>
        /// Удаляет сообщение из указанного чата.
        /// </summary>
        public async Task DeleteMessageAsync(long chatId, int messageId, CancellationToken stoppingToken)
        {
            // await ExecuteWithRetryAsync(() => 
               await _botClient.DeleteMessageAsync(chatId, messageId, stoppingToken);
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
            _logger.LogWarning(exception, "Ошибка при попытке {RetryCount} обращения к API Telegram. Повторная попытка через {TimeSpan}...", retryCount, timeSpan);
        }

        private static ReceiverOptions CreateReceiverOptions()
        {
            return new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };
        }

        private void InitializeBot(Interfaces_IUpdateHandler updateHandler)
        {
            _botClient.StartReceiving(
                updateHandler: async (_, update, token) => await updateHandler.HandleUpdateAsync(update, token),
                errorHandler: HandleErrorAsync,
                receiverOptions: _receiverOptions,
                cancellationToken: default
            );
        }

        private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError("HandleError: {ErrorMessage}", errorMessage);

            if (exception is ApiRequestException { ErrorCode: 409 })
            {
                _logger.LogCritical("Запущено более одного экземпляра бота");
            }

            await Task.CompletedTask;
        }

        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
        {
            return await _retryPolicy.ExecuteAsync(action);
        }

        private async Task ExecuteWithRetryAsync(Func<Task> action)
        {
            await _retryPolicy.ExecuteAsync(action);
        }
    }
}