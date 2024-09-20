namespace tg_redmine.Core.Models;

public class Message
{
	public int Id { get; set; }
	public int IssueId { get; set; }
	public int MessageId { get; set; }
	public long ChatId { get; set; }
	public DateTime UpdatedOn { get; set; }
	public DateTime CreatedOn { get; set; }
}