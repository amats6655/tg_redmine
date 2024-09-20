using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace tg_redmine.Data;

public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
	public ApplicationDbContext CreateDbContext(string[] args)
	{
		var configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json")
			.Build();

		var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
		var connectionString = configuration.GetConnectionString("DefaultConnection");

		optionsBuilder.UseSqlite(connectionString);

		return new ApplicationDbContext(optionsBuilder.Options);
	}
}