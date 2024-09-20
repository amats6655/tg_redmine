using tg_redmine.Core.Helpers;
using tg_redmine.Core.Models;

namespace tg_redmine.Core.Repositories.Interfaces;

public interface IUserRepository
{
	/// <summary>
	/// Получает пользователя по логину из кэша или базы данных.
	/// </summary>
	/// <param name="login">Логин пользователя в Telegram.</param>
	/// <param name="cancellationToken">Токен отмены операции.</param>
	/// <returns>
	/// Ответ сервиса, содержащий пользователя, если он найден.
	/// В случае ошибки или отсутствия пользователя возвращает соответствующее сообщение.
	/// </returns>
	Task<IServiceResponse<User>> GetUserByLogin(string login, CancellationToken cancellationToken);
	
	/// <summary>
	/// Добавляет нового пользователя в базу данных и кэш.
	/// </summary>
	/// <param name="login">Логин пользователя в Telegram.</param>
	/// <param name="cancellationToken">Токен отмены операции.</param>
	/// <returns>
	/// Ответ сервиса, содержащий добавленного пользователя.
	/// В случае ошибки возвращает соответствующее сообщение.
	/// </returns>
	Task<IServiceResponse<User>> AddUser(string login, CancellationToken cancellationToken);
}