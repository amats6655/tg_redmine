using System.IO.Compression;

namespace tg_redmine.Core.Helpers;

public class GenerateZip
{
	public static string GenerateZipFile()
	{
		var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}");
		Directory.CreateDirectory(tempDirectory);

		foreach (var filePath in Directory.GetFiles(logsDirectory))
		{
			try
			{
				var fileInfo = new FileInfo(filePath);
				var destinationPath = Path.Combine(tempDirectory, fileInfo.Name);

				File.Copy(filePath, destinationPath);
			}
			catch (IOException)
			{
				// Если файл заблокирован (например, текущий лог), игнорируем его
			}
		}

		var zipFilePath = Path.Combine(Path.GetTempPath(), $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
		ZipFile.CreateFromDirectory(tempDirectory, zipFilePath);
		Directory.Delete(tempDirectory, true);

		return zipFilePath;
	}
}