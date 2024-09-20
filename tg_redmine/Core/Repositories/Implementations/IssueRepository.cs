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
					Tracker = result.GetString("tracker"),
					Corpus = result.GetString("corpus"),
					RoomNumber = result.GetString("room_number"),
					Subject = result.GetString("subject"),
					Status = result.GetString("status"),
					CreatedOn = result.GetDateTime("created_on"),
					UpdatedOn = result.GetDateTime("updated_on"),
					Author = result.GetString("author"),
					Telegram = result.GetString("Telegram").Split(' ').ToList(),
					Comment = result.GetValue("comment").ToString() ?? string.Empty,
					Commentator = result.GetValue("commentator").ToString() ?? string.Empty,
					Priority = result.GetString("priority")
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