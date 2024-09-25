using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using tg_redmine.Core.Helpers;
using tg_redmine.Core.Models;
using tg_redmine.Core.Repositories.Interfaces;
using tg_redmine.Data;

namespace tg_redmine.Core.Repositories.Implementations;

public class MessageRepository : IMessageRepository
{
	private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
	private readonly IMemoryCache _memoryCache;
	private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

	public MessageRepository(IDbContextFactory<ApplicationDbContext> contextFactory, IMemoryCache memoryCache)
	{
		_contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
		_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
	}
	
	public MessageRepository(){}

	public async Task<IServiceResponse<List<Message>>> GetMessagesByIssueIdAsync(
		int issueId,
		CancellationToken stoppingToken)
	{
		await using var context = await _contextFactory.CreateDbContextAsync(stoppingToken);
		try
		{
			if (_memoryCache.TryGetValue(issueId, out List<Message>? cachedMessage))
				return ServiceResponse<List<Message>>.Success(cachedMessage!);
			var result = await context.Messages
				.Where(i => i.IssueId == issueId)
				.ToListAsync(stoppingToken);

			_memoryCache.Set(issueId, result, CacheDuration);
			return result.Count == 0
				? ServiceResponse<List<Message>>.Failure("Сообщения не найдены")
				: ServiceResponse<List<Message>>.Success(result);
		}
		catch (Exception ex)
		{
			return ServiceResponse<List<Message>>.Error($"Ошибка при получении сообщений: {ex.Message}");
		}
	}

	public async Task<IServiceResponse<bool>> AddMessageAsync(
		Telegram.Bot.Types.Message message, 
		Issue issue,
		CancellationToken stoppingToken)
	{
		await using var context = await _contextFactory.CreateDbContextAsync(stoppingToken);
		try
		{
			var newMessage = new Message
			{
				ChatId = message.Chat.Id,
				IssueId = issue.Id,
				UpdatedOn = issue.UpdatedOn,
				MessageId = message.MessageId,
				CreatedOn = message.Date
			};
			await context.Messages.AddAsync(newMessage, stoppingToken);
			await context.SaveChangesAsync(stoppingToken);
			_memoryCache.Set(newMessage.IssueId, newMessage, CacheDuration);
			return ServiceResponse<bool>.Success(true);
		}
		catch (Exception ex)
		{
			return ServiceResponse<bool>.Error($"Ошибка при добавлении сообщения: {ex.Message}");
		}
	}

	public async Task<IServiceResponse<List<Message>>> GetMessagesExcludingIdsAsync(
		HashSet<int> ids,
		CancellationToken stoppingToken)
	{
		await using var context = await _contextFactory.CreateDbContextAsync(stoppingToken);
		try
		{
			var result = await context.Messages
				.Where(message => !ids.Contains(message.IssueId))
				.ToListAsync(stoppingToken);

			return result.Count == 0
				? ServiceResponse<List<Message>>.Failure("Сообщения не найдены")
				: ServiceResponse<List<Message>>.Success(result);
		}
		catch (Exception ex)
		{
			return ServiceResponse<List<Message>>.Error($"Ошибка при получении сообщений: {ex.Message}");
		}
	}

	public async Task<IServiceResponse<bool>> DeleteMessageAsync(
		Message message,
		CancellationToken stoppingToken)
	{
		await using var context = await _contextFactory.CreateDbContextAsync(stoppingToken);
		try
		{
			context.Messages.Remove(message);
			await context.SaveChangesAsync(stoppingToken);
			_memoryCache.Remove(message.IssueId);
			return ServiceResponse<bool>.Success(true);
		}
		catch (Exception ex)
		{
			return ServiceResponse<bool>.Error($"Ошибка при удалении сообщения: {ex.Message}");
		}
	}

	public async Task<IServiceResponse<List<Message>>> GetOldMessagesAsync(CancellationToken stoppingToken)
	{
		await using var context = await _contextFactory.CreateDbContextAsync(stoppingToken);
		try
		{
			var actualDate = DateTime.UtcNow.AddHours(-45);
			var oldMessagess = context.Messages.Where(i => i.CreatedOn < actualDate).ToList();
			return ServiceResponse<List<Message>>.Success(oldMessagess);
		}
		catch
		{
			return ServiceResponse<List<Message>>.Error("Сообщения не получены");
		}
	}

	public async Task<IServiceResponse<bool>> UpdateMessage(Telegram.Bot.Types.Message message, Issue issue, CancellationToken stoppingToken)
	{
		await using var context = await _contextFactory.CreateDbContextAsync(stoppingToken);
		try
		{
			var currentMessage = await context.Messages.FirstOrDefaultAsync(m => m.MessageId == message.MessageId, cancellationToken: stoppingToken);
			if (currentMessage is null)
				return ServiceResponse<bool>.Failure("Не найдено сообщение для изменения");
			currentMessage.UpdatedOn = issue.UpdatedOn;
			context.Messages.Update(currentMessage);
			await context.SaveChangesAsync(stoppingToken);
			_memoryCache.Set(currentMessage.IssueId, currentMessage, CacheDuration);
			return ServiceResponse<bool>.Success(true);
		}
		catch (Exception ex)
		{
			return ServiceResponse<bool>.Error($"Ошибка при обновлении сообщения: {ex.Message}");
		}
	}
}