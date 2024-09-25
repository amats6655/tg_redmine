using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace tg_redmine.Core.Host;

public interface ITelegramBotService
{
	Task<Message> SendMessageAsync(long chatId, string message, ParseMode parseMode,
		CancellationToken stoppingToken, int issueId, string status);

	Task<Message> EditMessageAsync(long chatId, int messageId, string message, ParseMode parseMode,
		CancellationToken stoppingToken, int issueId, string status);

	Task<Message> SendMessageThreadAsync(long chatId, int threadId, string message,
		ParseMode parseMode, CancellationToken stoppingToken, int issueId, string status);

	Task DeleteMessageAsync(long chatId, int messageId, CancellationToken stoppingToken);
}