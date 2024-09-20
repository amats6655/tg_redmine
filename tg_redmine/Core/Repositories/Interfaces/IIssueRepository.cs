using tg_redmine.Core.Helpers;
using tg_redmine.Core.Models;

namespace tg_redmine.Core.Repositories.Interfaces;

public interface IIssueRepository
{
	Task<IServiceResponse<List<Issue>>> GetIssuesAsync();
}