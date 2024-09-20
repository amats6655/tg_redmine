using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tg_redmine.Core.Helpers;
using tg_redmine.Core.Models;
using tg_redmine.Core.Repositories.Interfaces;
using tg_redmine.Data;

namespace tg_redmine.Core.Repositories.Implementations;

public class AdminRepository : IAdminRepository
{
	private readonly ILogger<AdminRepository> _logger;
	private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
	private const int AdminRoleValue = 1;
	
	public AdminRepository(ILogger<AdminRepository> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
	{
		_logger = logger;
		_contextFactory = contextFactory;
	}
	
	public async Task<IServiceResponse<bool>> AddUserAsync(string login, long chatId, long userId, int threadId, int isAdmin)
	{
		await using var context = await _contextFactory.CreateDbContextAsync();

		var currentUser = await context.Users.FirstOrDefaultAsync(i => i.ChatId == chatId && i.TelegramUserId == userId);
		if (currentUser is not null)
			return ServiceResponse<bool>.Failure($"Пользователь {login} с chatId {currentUser.ChatId} уже существует");
		var newUser = new User
		{
			TelegramLogin = login,
			ChatId = chatId,
			ThreadId = threadId,
			TelegramUserId = userId,
			IsAdmin = isAdmin
		};
		await context.Users.AddAsync(newUser);
		await context.SaveChangesAsync();
		return ServiceResponse<bool>.Success(true, $"Пользователь {login} добавлен");
	}

	public async Task<IServiceResponse<bool>> UpdateUserAsync(string login, long chatId, long userId, int threadId, int isAdmin)
	{
		await using var context = await _contextFactory.CreateDbContextAsync();

		if (string.IsNullOrWhiteSpace(login))
		{
			return ServiceResponse<bool>.Failure("Логин не может быть пустым");
		}

		var currentUser = await context.Users.FirstOrDefaultAsync(i => i.TelegramLogin!.Equals(login));
		if (currentUser is null)
		{
			_logger.LogWarning($"Пользователь {login} не найден");
			return ServiceResponse<bool>.Failure($"Пользователь {login} не найден");
		}

		currentUser.ChatId = chatId;
		currentUser.ThreadId = threadId;
		currentUser.TelegramUserId = userId;
		currentUser.IsAdmin = isAdmin;
		currentUser.TelegramLogin = login;
		await context.SaveChangesAsync();
		return ServiceResponse<bool>.Success(true, $"Пользователь {login} изменен");
	}

	public async Task<IServiceResponse<bool>> DeleteUserAsync(string login)
	{
		await using var context = await _contextFactory.CreateDbContextAsync();

		if (string.IsNullOrWhiteSpace(login))
		{
			return ServiceResponse<bool>.Failure("Логин не может быть пустым");
		}

		var currentUser = await context.Users.FirstOrDefaultAsync(i => i.TelegramLogin == login);
		if (currentUser == null || currentUser.TelegramLogin == "@amats")
		{
			_logger.LogWarning($"Пользователь {login} не найден или не может быть удален");
			return ServiceResponse<bool>.Failure("Пользователь не найден или не может быть удален");
		}

		_logger.LogInformation($"Пользователь найден {login} {currentUser.ChatId} {currentUser.TelegramUserId}");
		context.Users.Remove(currentUser);
		await context.SaveChangesAsync();
		return ServiceResponse<bool>.Success(true, $"Пользователь {login} удален");
	}

	public async Task<IServiceResponse<List<User>>> GetAllUsersAsync()
	{
		await using var context = await _contextFactory.CreateDbContextAsync();

		var result = await context.Users.ToListAsync();
		return result.IsNullOrEmpty()
			? ServiceResponse<List<User>>.Failure("Список пользователей пуст")
			: ServiceResponse<List<User>>.Success(result);
	}
	

	public IServiceResponse<string> GetLogsAsync()
	{
		var result = GenerateZip.GenerateZipFile();
		return result.IsNullOrEmpty()
			? ServiceResponse<string>.Failure("Список логов пуст")
			: ServiceResponse<string>.Success(result);
	}

	public async Task<IServiceResponse<List<Message>>> GetAllMessagesAsync()
	{
		await using var context = await _contextFactory.CreateDbContextAsync();

		var result = await context.Messages.ToListAsync();
		return result.IsNullOrEmpty()
			? ServiceResponse<List<Message>>.Failure("Список сообщений пуст")
			: ServiceResponse<List<Message>>.Success(result);
	}

	public async Task<bool> IsAdminAsync(long userId)
	{
		await using var context = await _contextFactory.CreateDbContextAsync();

		var user = await context.Users.FirstOrDefaultAsync(i => i.TelegramUserId == userId);
		return user?.IsAdmin == AdminRoleValue;
	}
}

public static class EnumerableExtensions
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
    {
        return enumerable == null || !enumerable.Any();
    }
}