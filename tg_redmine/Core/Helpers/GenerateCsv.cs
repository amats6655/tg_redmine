using System.Reflection;
using System.Text;

namespace tg_redmine.Core.Helpers;

public class GenerateCsv<T>
{
	public static MemoryStream GenerateCsvFromList(List<T> items)
	{
		var sb = new StringBuilder();
		var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
		sb.AppendLine(string.Join(";", properties.Select(p => p.Name)));
		foreach (var item in items)
		{
			var values = properties.Select(p => p.GetValue(item)?.ToString() ?? string.Empty);
			sb.AppendLine(string.Join(";", values));
		}

		var memoryStream = new MemoryStream();
		var writer = new StreamWriter(memoryStream);
		writer.Write(sb.ToString());
		writer.Flush();
		memoryStream.Position = 0; 
		return memoryStream;
	}
}