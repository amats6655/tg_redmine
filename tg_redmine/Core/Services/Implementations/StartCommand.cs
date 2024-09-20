using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using tg_redmine.Core.Services.Interfaces;

namespace tg_redmine.Core.Services.Implementations;

public class StartCommand : ICommand
{
    private readonly TelegramBotClient _botClient;

    public StartCommand(TelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task ExecuteAsync(Message message)
    {
        await _botClient.SendTextMessageAsync(message.Chat.Id, messageThreadId: message.MessageThreadId,
            text: $"@{message.From!.Username} {message.Chat.Id} {message.From.Id} {message.MessageThreadId ?? 0} 0");
        await _botClient.SendTextMessageAsync(message.Chat.Id, messageThreadId: message.MessageThreadId,
            text: "Отправьте моё сообщение администратору");
    }
}