using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using tg_redmine.Core.Host;
using tg_redmine.Core.Models;
using tg_redmine.Core.Repositories.Implementations;
using tg_redmine.Core.Repositories.Interfaces;
using tg_redmine.Core.Services.Implementations;

namespace tg_redmine.Tests;

[TestFixture]
public class NotificationServiceTests
{
	private Mock<IMessageRepository> _messageRepositoryMock;
	private Mock<ITelegramBotService> _telegramBotClientMock;
	private NotificationService _notificationService;
	private ILogger<NotificationService> _logger;

	[SetUp]
	public void Setup()
	{
		_logger = new Mock<ILogger<NotificationService>>().Object;
		_messageRepositoryMock = new Mock<IMessageRepository>();
		_telegramBotClientMock = new Mock<ITelegramBotService>();
		_notificationService =
			new NotificationService(_telegramBotClientMock.Object, _logger, _messageRepositoryMock.Object);
	}

	[Test]
	public async Task NotifyUser_ShouldSendCorrectNotification_WhenCalledWithValidIssue()
	{
		// Arrange
		var issue = new Issue
		{
			Id = 1,
			Subject = "Test Issue",
			Author = "Test User",
			Corpus = "Test Corpus",
			Priority = "High",
			CreatedOn = DateTime.UtcNow.AddDays(-1),
			UpdatedOn = DateTime.UtcNow,
			RoomNumber = "101",
			Status = "Open",
			Tracker = "Bug",
			Telegram = new List<string> { "@testuser" }
		};

		var user = new User
		{
			Id = 1,
			TelegramLogin = "@testuser",
			ChatId = 123456789,
			IsAdmin = 0,
			ThreadId = 0,
			TelegramUserId = 987654321
		};

		var users = new List<User> { user };

		var expectedMessage =
			$"New Issue Assigned:\n\n*Subject*: Test Issue\n*Author*: Test User\n*Corpus*: Test Corpus\n*Priority*: High\n*Room*: 101\n*Status*: Open";

		// Act
		await _notificationService.NotifyUser(issue, users, CancellationToken.None);

		// Assert
		_telegramBotClientMock.Verify(bot => bot.SendMessageAsync(
			It.Is<long>(chatId => chatId == user.ChatId),
			It.IsAny<string>(),
			ParseMode.Html,
			It.IsAny<CancellationToken>(),
			It.Is<int>(issueId => issueId == issue.Id),
			It.Is<string>(status => status == issue.Status)), Times.Once);
	}
}
