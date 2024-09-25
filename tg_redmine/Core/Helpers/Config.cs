namespace tg_redmine.Core.Helpers;

public class TelegramSettings
{
	public string? Token { get; set; }
}

public class IssuesSettings
{
	public string? СonnectionString { get; set; }
}

public class RedmineSettings
{
	public  string? Url { get; set; }
	public  string? ApiKey { get; set; }
}

public class HostingSettings
{
	public int RequestFrequency { get; set; }
}