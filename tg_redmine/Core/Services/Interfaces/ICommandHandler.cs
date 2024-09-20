using Telegram.Bot.Types;

namespace tg_redmine.Core.Services.Interfaces;

public interface ICommandHandler
{
	public Task HandleMessageAsync(Message message);
}