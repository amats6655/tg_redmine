using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tg_redmine.Core.Models;

namespace tg_redmine.Data;

public class ApplicationDbContext : DbContext
{
	public DbSet<User> Users { get; set; }
	public DbSet<Message> Messages { get; set; }

	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
		: base(options)
	{

	}
	
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		if (!Database.GetPendingMigrations().Any()) return;
		Database.Migrate();
	}
}