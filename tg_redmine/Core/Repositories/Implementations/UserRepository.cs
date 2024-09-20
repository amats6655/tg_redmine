using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using tg_redmine.Core.Helpers;
using tg_redmine.Core.Models;
using tg_redmine.Core.Repositories.Interfaces;
using tg_redmine.Data;

namespace tg_redmine.Core.Repositories.Implementations;

public class UserRepository : IUserRepository
{
	private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
	private readonly IMemoryCache _cache;
	private const string UserCacheKeyPrefix = "User_";
	private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

	public UserRepository(IMemoryCache cache, IDbContextFactory<ApplicationDbContext> contextFactory)
	{
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
	}

	public async Task<IServiceResponse<User>> GetUserByLogin(
		string login, 
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(login))
		{
			return ServiceResponse<User>.Failure("Логин не может быть пустым");
		}

		var cacheKey = $"{UserCacheKeyPrefix}{login}";
		if (_cache.TryGetValue(cacheKey, out User? cachedUser))
		{
			return ServiceResponse<User>.Success(cachedUser!);
		}
		await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
		
		try
		{
			var user = await context.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.TelegramLogin == login, cancellationToken);

			if (user != null)
			{
				_cache.Set(cacheKey, user, CacheDuration);
			}

			return user != null 
				? ServiceResponse<User>.Success(user)
				: ServiceResponse<User>.Failure("Пользователь не найден");
		}
		catch (Exception ex)
		{
			return ServiceResponse<User>.Error($"Ошибка при получении пользователя: {ex.Message}");
		}
	}

	public async Task<IServiceResponse<User>> AddUser(
		string login,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(login))
		{
			return ServiceResponse<User>.Failure("Логин не может быть пустым");
		}
		await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);


		try
		{
			var newUser = new User
			{
				TelegramLogin = login
			};

			var result = await context.Users.AddAsync(newUser, cancellationToken);
			await context.SaveChangesAsync(cancellationToken);

			var cacheKey = $"{UserCacheKeyPrefix}{login}";
			_cache.Set(cacheKey, result.Entity, CacheDuration);

			return ServiceResponse<User>.Success(result.Entity);
		}
		catch (Exception ex)
		{
			return ServiceResponse<User>.Error($"Ошибка при добавлении пользователя: {ex.Message}");
		}
	}
}