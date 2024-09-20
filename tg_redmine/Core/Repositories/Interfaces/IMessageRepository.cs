using tg_redmine.Core.Helpers;
using tg_redmine.Core.Models;

namespace tg_redmine.Core.Repositories.Interfaces;

public interface IMessageRepository
{
	/// <summary>
	/// Асинхронно получает список сообщений, связанных с указанной задачей.
	/// </summary>
	/// <param name="issueId">Идентификатор задачи.</param>
	/// <param name="stoppingToken">Токен отмены операции.</param>
	/// <returns>
	/// Ответ сервиса, содержащий список сообщений, если они найдены.
	/// В случае отсутствия сообщений или ошибки возвращает соответствующее сообщение.
	/// </returns>
	Task <IServiceResponse<List<Message>>> GetMessagesByIssueIdAsync(int issueId, CancellationToken stoppingToken);

	/// <summary>
	/// Асинхронно добавляет новое сообщение в базу данных.
	/// </summary>
	/// <param name="message">Сообщение Telegram для добавления.</param>
	/// <param name="issueId">Идентификатор связанной задачи.</param>
	/// <param name="issue"></param>
	/// <param name="stoppingToken">Токен отмены операции.</param>
	/// <returns>
	/// Ответ сервиса, указывающий на успешность операции добавления.
	/// В случае ошибки возвращает сообщение с описанием проблемы.
	/// </returns>
	Task <IServiceResponse<bool>> AddMessageAsync(Telegram.Bot.Types.Message message, Issue issue, CancellationToken stoppingToken);
	/// <summary>
	/// Асинхронно получает список сообщений, исключая сообщения с указанными идентификаторами задач.
	/// </summary>
	/// <param name="ids">Набор идентификаторов задач для исключения.</param>
	/// <param name="stoppingToken">Токен отмены операции.</param>
	/// <returns>
	/// Ответ сервиса, содержащий список сообщений, за исключением тех, чьи идентификаторы задач были указаны.
	/// В случае отсутствия сообщений или ошибки возвращает соответствующее сообщение.
	/// </returns>
	Task <IServiceResponse<List<Message>>> GetMessagesExcludingIdsAsync(HashSet<int> ids, CancellationToken stoppingToken);
	/// <summary>
	/// Асинхронно удаляет указанное сообщение из базы данных.
	/// </summary>
	/// <param name="message">Сообщение для удаления.</param>
	/// <param name="stoppingToken">Токен отмены операции.</param>
	/// <returns>
	/// Ответ сервиса, указывающий на успешность операции удаления.
	/// В случае ошибки возвращает сообщение с описанием проблемы.
	/// </returns>
	Task <IServiceResponse<bool>> DeleteMessageAsync(Message message, CancellationToken stoppingToken);
	/// <summary>
	/// Асинхронно получает список устаревающих сообщений старше 45 часов. 
	/// </summary>
	/// <param name="stoppingToken"></param>
	/// <returns>
	/// Ответ сервиса, содержащий список сообщений, отправленных в чат позже чем 45 часов назад.
	/// </returns>
	Task<IServiceResponse<List<Message>>> GetOldMessagesAsync(CancellationToken stoppingToken);
	/// <summary>
	/// Обновляет информацию о сообщении в базе данных
	/// </summary>
	/// <param name="message"></param>
	/// <param name="issue"></param>
	/// <param name="stoppingToken"></param>
	/// <returns>
	/// Возвращает ответ сервиса, указывающий на успешность операции обновления. В случае ошибки возвращает сообщение с описанием проблемы
	/// </returns>
	Task<IServiceResponse<bool>> UpdateMessage(Telegram.Bot.Types.Message message, Issue issue, CancellationToken stoppingToken);
}