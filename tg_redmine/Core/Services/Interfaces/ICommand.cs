using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace tg_redmine.Core.Services.Interfaces;

public interface ICommand
{
    Task ExecuteAsync(Message message);
}