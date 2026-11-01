using LogiTrack.Data.Seeding;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Data
{
    public static class DataExtensions
    {
        public static async Task MigrateDbAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LogiTrackContext>();

            // Ensure the directory for the SQLite database exists
            var dataSource = new SqliteConnectionStringBuilder(context.Database.GetConnectionString()).DataSource;
            var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await context.Database.MigrateAsync();
        }

        public static async Task SeedDataAsync(this WebApplication app)
        {   
            using var scope = app.Services.CreateScope();
            var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
            await seedService.SeedDataAsync();
        }

        public static async Task SeedIdentityAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var seedService = scope.ServiceProvider.GetRequiredService<IdentitySeedService>();
            await seedService.SeedIdentityAsync();
        }
    }
}