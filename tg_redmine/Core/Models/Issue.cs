namespace tg_redmine.Core.Models;

public class Issue
{
	public int Id { get; set; }
	public string Tracker { get; set; } 
	public string Corpus { get; set; }
	public string RoomNumber { get; set; }
	public string Priority { get; set; }
	public string Subject { get; set; }
	public string Status { get; set; }
	public DateTime CreatedOn { get; set; }
	public DateTime UpdatedOn { get; set; }
	public string Author { get; set; }
	public List<string> Telegram { get; set; }
	public string? Comment { get; set; }
	public string? Commentator { get; set; }
}