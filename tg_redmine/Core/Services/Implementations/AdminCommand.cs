using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using tg_redmine.Core.Repositories.Interfaces;
using tg_redmine.Core.Services.Interfaces;

namespace tg_redmine.Core.Services.Implementations;
public class AdminCommand : ICommand
{
    private readonly TelegramBotClient _botClient;
    private readonly IAdminRepository _adminRepository;
    private readonly ILogger<CommandHandler> _logger;

    public AdminCommand(TelegramBotClient botClient, IAdminRepository adminRepository, ILogger<CommandHandler> logger)
    {
        _botClient = botClient;
        _adminRepository = adminRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(Message msg)
    {		
        var isAdmin = await _adminRepository.IsAdminAsync(msg.From!.Id);
		if (isAdmin && msg.From.Id == msg.Chat.Id)
		{
			var inlineMarkup = new InlineKeyboardMarkup()
				.AddButton("Пользователи", callbackData: "users")
				.AddButton("Cообщения", callbackData: "messages")
				.AddNewRow()
				.AddButton("Логи", callbackData: "logs");
			await _botClient.SendTextMessageAsync(msg.Chat.Id, "Админ панель", replyMarkup: inlineMarkup);
		}
		else
		{
			_logger.LogWarning($"Неудачная аворизация пользователем {msg.From.Username}, {msg.From.Id}");
			await _botClient.SendTextMessageAsync(msg.Chat.Id, "Нет доступа. Обратитесь к администратору");
		}
    }
}
