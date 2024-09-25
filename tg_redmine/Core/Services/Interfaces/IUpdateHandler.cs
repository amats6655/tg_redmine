using Telegram.Bot.Types;

namespace tg_redmine.Core.Services.Interfaces;

public interface IUpdateHandler
{
	public Task HandleUpdateAsync(Update update, CancellationToken cancellationToken);
}