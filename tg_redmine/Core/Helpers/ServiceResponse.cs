namespace tg_redmine.Core.Helpers;

public class ServiceResponse<T> : IServiceResponse<T>
{
	public bool IsSuccess { get; set; }
	public bool HasErrors { get; set; } = false;
	public T Data { get; set; }
	public string? Message { get; set; }

	public static ServiceResponse<T> Success(T data, string message = null)
	{
		return new ServiceResponse<T> { IsSuccess = true, Data = data, Message = message };
	}
	public static ServiceResponse<T> Failure(string message)
	{
		return new ServiceResponse<T> { IsSuccess = false, Message = message };
	}
	public static ServiceResponse<T> Error(string message)
	{
		return new ServiceResponse<T> { HasErrors = true, IsSuccess = false, Message = message };
	}
}