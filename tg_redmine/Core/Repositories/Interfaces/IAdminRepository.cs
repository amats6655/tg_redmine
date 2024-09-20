using tg_redmine.Core.Helpers;
using tg_redmine.Core.Models;

namespace tg_redmine.Core.Repositories.Interfaces;

public interface IAdminRepository
{
	/// <summary>
	/// Асинхронно добавляет нового пользователя в систему.
	/// </summary>
	/// <param name="login">Логин пользователя.</param>
	/// <param name="chatId">Идентификатор чата.</param>
	/// <param name="userId">Идентификатор пользователя в Telegram.</param>
	/// <param name="threadId">Идентификатор треда.</param>
	/// <param name="isAdmin">Флаг, указывающий, является ли пользователь администратором.</param>
	/// <returns>Ответ сервиса с результатом операции добавления пользователя.</returns>
	Task<IServiceResponse<bool>> AddUserAsync(string login, long chatId, long userId, int threadId, int isAdmin);

	/// <summary>
	/// Асинхронно обновляет информацию о существующем пользователе.
	/// </summary>
	/// <param name="login">Логин пользователя.</param>
	/// <param name="chatId">Идентификатор чата.</param>
	/// <param name="userId">Идентификатор пользователя в Telegram.</param>
	/// <param name="threadId">Идентификатор треда.</param>
	/// <param name="isAdmin">Флаг, указывающий, является ли пользователь администратором.</param>
	/// <returns>Ответ сервиса с результатом операции обновления пользователя.</returns>
	Task<IServiceResponse<bool>> UpdateUserAsync(string login, long chatId, long userId, int threadId, int isAdmin);

	/// <summary>
	/// Асинхронно удаляет пользователя из системы.
	/// </summary>
	/// <param name="login">Логин пользователя для удаления.</param>
	/// <returns>Ответ сервиса с результатом операции удаления пользователя.</returns>
	Task<IServiceResponse<bool>> DeleteUserAsync(string login);

	/// <summary>
	/// Асинхронно получает список всех пользователей.
	/// </summary>
	/// <returns>Ответ сервиса со списком всех пользователей.</returns>
	Task<IServiceResponse<List<User>>> GetAllUsersAsync();

	/// <summary>
	/// Получает логи системы.
	/// </summary>
	/// <returns>Ответ сервиса с логами в виде строки.</returns>
	IServiceResponse<string> GetLogsAsync();

	/// <summary>
	/// Асинхронно получает список всех сообщений.
	/// </summary>
	/// <returns>Ответ сервиса со списком всех сообщений.</returns>
	Task<IServiceResponse<List<Message>>> GetAllMessagesAsync();

	/// <summary>
	/// Асинхронно проверяет, является ли пользователь администратором.
	/// </summary>
	/// <param name="userId">Идентификатор пользователя в Telegram для проверки.</param>
	/// <returns>True, если пользователь является администратором, иначе false.</returns>
	Task<bool> IsAdminAsync(long userId);
}