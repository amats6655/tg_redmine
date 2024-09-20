using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using tg_redmine.Core.Repositories.Interfaces;
using tg_redmine.Core.Services.Interfaces;

namespace tg_redmine.Core.Services.Implementations;

public class CommandHandler : ICommandHandler
{
	private readonly TelegramBotClient _botClient;
	private readonly ILogger<CommandHandler> _logger;
	private readonly IAdminRepository _adminRepository;
	private readonly Dictionary<long, string> _userStates;
	private readonly Dictionary<string, ICommand> _commands;
	
	private const string AwaitingShowUsersState = "awaiting_show_users";
	private const string AwaitingAddUserState = "awaiting_add_user";
	private const string AwaitingUpdateUserState = "awaiting_update_user";
	private const string AwaitingDeleteUserState = "awaiting_delete_user";

	public CommandHandler(TelegramBotClient botClient, ILogger<CommandHandler> logger, IAdminRepository adminRepository,
		Dictionary<long, string> userStates)
	{
		_botClient = botClient;
		_logger = logger;
		_adminRepository = adminRepository;
		_userStates = userStates;

		_commands = new Dictionary<string, ICommand>
		{
			{ "/start", new StartCommand(_botClient) },
			{ "/admin", new AdminCommand(_botClient, _adminRepository, _logger) }
			// Добавьте другие команды здесь
		};
	}
	
	public async Task HandleMessageAsync(Message message)
	{
		if (_userStates.TryGetValue(message.Chat.Id, out var state))
		{
			await HandleStateAsync(message, state);
		}
		else if (_commands.TryGetValue(message.Text ?? string.Empty, out var command))
		{
			await command.ExecuteAsync(message);
		}
		else
		{
			await _botClient.SendTextMessageAsync(message.Chat.Id, "Неизвестная команда.");
		}
	}
	
	private Task HandleStateAsync(Message message, string state) => state switch
	{
		AwaitingShowUsersState => HandleAwaitingShowUsersStateAsync(message),
		AwaitingAddUserState => HandleAwaitingAddUserStateAsync(message),
		AwaitingUpdateUserState => HandleAwaitingUpdateUserStateAsync(message),
		AwaitingDeleteUserState => HandleAwaitingDeleteUserStateAsync(message),
		_ => Task.CompletedTask
	};
	
	
	private async Task HandleAwaitingShowUsersStateAsync(Message message)
	{
		try
		{
			if (!await CheckIfAdminAndRespondAsync(message.Chat.Id, message.From?.Username ?? " ", message.From!.Id))
				return;

			var users = await _adminRepository.GetAllUsersAsync();
			if (users.IsSuccess)
			{
				var sb = new StringBuilder();
				sb.AppendLine("Список пользователей:");
				foreach (var user in users.Data)
				{
					sb.AppendLine($"{user.TelegramLogin} ; {user.ChatId} ; {user.TelegramUserId} ; {user.ThreadId} ; {user.IsAdmin}");
				}

				await _botClient.SendTextMessageAsync(message.Chat.Id, sb.ToString(), parseMode: ParseMode.Html);
			}
			else
			{
				await _botClient.SendTextMessageAsync(message.Chat.Id, users.Message);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при обработке состояния awaiting_show_users");
			await _botClient.SendTextMessageAsync(message.Chat.Id, "Произошла ошибка при обработке запроса.");
		}
		finally
		{
			_userStates.Remove(message.Chat.Id);
		}
	}

    private async Task HandleAwaitingAddUserStateAsync(Message msg)
    {
        if (!await CheckIfAdminAndRespondAsync(msg.Chat.Id, msg.From.Username, msg.From.Id))
	        return;

        var userDetails = msg.Text.Split(' ');
        if (userDetails.Length == 5)
        {
            var response = await _adminRepository.AddUserAsync(
                login: userDetails[0],
                chatId: long.Parse(userDetails[1]),
                userId: long.Parse(userDetails[2]),
                threadId: int.Parse(userDetails[3]),
                isAdmin: int.Parse(userDetails[4])
            );
            await _botClient.SendTextMessageAsync(msg.Chat.Id, response.Message);
            _logger.LogInformation(
                $"Пользователь {msg.From.Username}, {msg.From.Id} добавил пользователя {msg.Text}");
        }
        else
        {
            await _botClient.SendTextMessageAsync(msg.Chat.Id, "Неверный формат данных. Попробуйте снова.");
        }

        _userStates.Remove(msg.Chat.Id);
    }

    private async Task HandleAwaitingUpdateUserStateAsync(Message msg)
    {
        if (!await CheckIfAdminAndRespondAsync(msg.Chat.Id, msg.From.Username, msg.From.Id))
	        return;

        var updateDetails = msg.Text!.Split(' ');
        if (updateDetails.Length == 5)
        {
            var response = await _adminRepository.UpdateUserAsync(
                login: updateDetails[0],
                chatId: long.Parse(updateDetails[1]),
                userId: long.Parse(updateDetails[2]),
                threadId: int.Parse(updateDetails[3]),
                isAdmin: int.Parse(updateDetails[4])
            );
            await _botClient.SendTextMessageAsync(msg.Chat.Id, response.Message);
            _logger.LogInformation(
                $"Пользователь {msg.From.Username}, {msg.From.Id} изменил пользователя {msg.Text}");
        }
        else
        {
            await _botClient.SendTextMessageAsync(msg.Chat.Id, "Неверный формат данных. Попробуйте снова.");
        }

        _userStates.Remove(msg.Chat.Id);
    }

    private async Task HandleAwaitingDeleteUserStateAsync(Message msg)
    {
	    if (!await CheckIfAdminAndRespondAsync(msg.Chat.Id, msg.From?.Username ?? " ", msg.From.Id))
		    return;

        var responseDelete = await _adminRepository.DeleteUserAsync(msg.Text?? " ");
        await _botClient.SendTextMessageAsync(msg.Chat.Id, responseDelete.Message);
        if(responseDelete.IsSuccess)
			_logger.LogInformation($"Пользователь {msg.From.Username}, {msg.From.Id} удалил пользователя {msg.Text}");
        _userStates.Remove(msg.Chat.Id);
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
}