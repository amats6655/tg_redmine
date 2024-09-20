using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace tg_redmine.Core.Models;

public class User
{
	public int Id { get; set; }
	public long TelegramUserId { get; set; }
	[MaxLength(30)] public string? TelegramLogin { get; set; } = "none";
	public long ChatId { get; set; }
	public int ThreadId { get; set; }
	public int IsAdmin { get; set; }
} 