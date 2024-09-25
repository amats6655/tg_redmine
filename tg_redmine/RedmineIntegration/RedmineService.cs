using Redmine.Net.Api;
using Redmine.Net.Api.Async;
using Redmine.Net.Api.Types;
using tg_redmine.Core.Helpers;

namespace tg_redmine.RedmineIntegration
{
	public class RedmineService
	{
		private RedmineManager _manager;
		public RedmineService(RedmineManager manager)
		{
			_manager = manager;
		}

		public async Task<IServiceResponse<Issue>> CloseIssue(string issueId)
		{
			try
			{
				var issue = await _manager.GetObjectAsync<Issue>(issueId, null);
				if (issue.Status.Id is 3 or 5)
					return ServiceResponse<Issue>.Failure("Заявку уже кто-то закрыл");
				if(issue.Status.Id is not 2)
					return ServiceResponse<Issue>.Failure("Заявка не может быть закрыта из этого статуса");

				issue.Status = IdentifiableName.Create<IdentifiableName>(3);
				await _manager.UpdateObjectAsync(issueId, issue);
				return ServiceResponse<Issue>.Success(issue);
			}
			catch
			{
				return ServiceResponse<Issue>.Failure("Возникла неизвестная ошибка при закрытии задачи. Попробуйте позднее");
			}
		}

		public async Task<IServiceResponse<Issue>> InWorkIssue(string issueId)
		{
			try
			{
				var issue = await _manager.GetObjectAsync<Issue>(issueId, null);
				if (issue.Status.Id == 2)
					return ServiceResponse<Issue>.Failure("Заявку уже кто-то взял в работу");
				if (issue.Status.Id is 3 or 5)
					return ServiceResponse<Issue>.Failure("Заявка уже закрыта");
				issue.Status = IdentifiableName.Create<IdentifiableName>(2);
				await _manager.UpdateObjectAsync(issueId, issue);
				return ServiceResponse<Issue>.Success(issue);
			}
			catch
			{
				return ServiceResponse<Issue>.Failure("Возникла неизвестная ошибка при обновлении задачи. Попробуйте позднее");
			}
		}
	}
}