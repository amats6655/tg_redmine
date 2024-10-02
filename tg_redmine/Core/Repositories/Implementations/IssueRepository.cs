using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using tg_redmine.Core.Helpers;
using tg_redmine.Core.Models;
using tg_redmine.Core.Repositories.Interfaces;


namespace tg_redmine.Core.Repositories.Implementations;

public class IssueRepository : IIssueRepository
{
	private readonly string _connectionString;
	public IssueRepository(string connectionString)
	{
		_connectionString = connectionString;
	}

	public async Task<IServiceResponse<List<Issue>>> GetIssuesAsync()
	{
		var connection = new MySqlConnection(_connectionString);
		connection.Open();
		
		var command = new MySqlCommand("SELECT * FROM osp_issues_view", connection);
		try
		{
			await using var result = await command.ExecuteReaderAsync();
			var issues = new List<Issue>();
			while (await result.ReadAsync())
			{
				var issue = new Issue
				{
					Id = result.GetInt32("id"),
					Tracker = !result.IsDBNull(result.GetOrdinal("tracker"))
								? result.GetString("tracker")
								: string.Empty,
					RoomNumber = !result.IsDBNull(result.GetOrdinal("room_number"))
								   ? result.GetString("room_number")
								   : string.Empty,
					Corpus = !result.IsDBNull(result.GetOrdinal("corpus"))
							   ? result.GetString("corpus")
							   : string.Empty,
					Subject = !result.IsDBNull(result.GetOrdinal("subject"))
								? result.GetString("subject")
								: string.Empty,
					Status = !result.IsDBNull(result.GetOrdinal("status"))
							   ? result.GetString("status")
							   : string.Empty,
					CreatedOn = !result.IsDBNull(result.GetOrdinal("created_on"))
								  ? result.GetDateTime("created_on")
								  : DateTime.MinValue,
					UpdatedOn = !result.IsDBNull(result.GetOrdinal("updated_on"))
								  ? result.GetDateTime("updated_on")
								  : DateTime.MinValue,
					Author = !result.IsDBNull(result.GetOrdinal("author"))
							  ? result.GetString("author")
							  : string.Empty,
					Telegram = !result.IsDBNull(result.GetOrdinal("Telegram"))
								? result.GetString("Telegram").Split(' ').ToList()
								: new List<string>(),
					Comment = !result.IsDBNull(result.GetOrdinal("comment"))
							   ? result.GetString("comment")
							   : string.Empty,
					Commentator = !result.IsDBNull(result.GetOrdinal("commentator"))
								   ? result.GetString("commentator")
								   : string.Empty,
					Priority = !result.IsDBNull(result.GetOrdinal("priority"))
								? result.GetString("priority")
								: string.Empty
				};

				issues.Add(issue);
			}

			return ServiceResponse<List<Issue>>.Success(issues);
		}
		catch (Exception e)
		{
			return ServiceResponse<List<Issue>>.Error(e.Message);
		}
	}
}