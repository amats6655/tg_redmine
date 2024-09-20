using tg_redmine.Core.Models;

namespace tg_redmine.Core.Services.Interfaces;

/// <summary>
/// Интерфейс для сервиса уведомлений, отвечающего за отправку, обновление и удаление уведомлений о задачах.
/// </summary>
public interface INotificationService
{
	/// <summary>
	/// Отправляет уведомление о задаче группе пользователей.
	/// </summary>
	/// <param name="issue">Задача, о которой нужно уведомить.</param>
	/// <param name="users">Список пользователей для уведомления.</param>
	/// <param name="stoppingToken">Токен отмены операции.</param>
	/// <returns>Задача, представляющая асинхронную операцию.</returns>
	Task NotifyUser(Issue issue, List<User> users, CancellationToken stoppingToken);

	/// <summary>
	/// Обновляет существующее сообщение о задаче для группы пользователей.
	/// </summary>
	/// <param name="issue">Обновленная информация о задаче.</param>
	/// <param name="users">Список пользователей, для которых нужно обновить сообщение.</param>
	/// <param name="lastComment">Последний комментарий к задаче (если есть).</param>
	/// <param name="lastCommentator">Автор последнего комментария (если есть).</param>
	/// <param name="stoppingToken">Токен отмены операции.</param>
	/// <returns>Задача, представляющая асинхронную операцию.</returns>
	Task UpdateMessage(Issue issue, List<User> users, CancellationToken stoppingToken);

	/// <summary>
	/// Удаляет сообщение о задаче.
	/// </summary>
	/// <param name="message">Сообщение для удаления.</param>
	/// <param name="stoppingToken">Токен отмены операции.</param>
	/// <returns>Задача, представляющая асинхронную операцию.</returns>
	Task DeleteMessage(Message message, CancellationToken stoppingToken);
}