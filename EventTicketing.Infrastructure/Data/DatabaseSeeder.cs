using EventTicketing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public static class DatabaseSeeder
{
    public static void SeedFromSql(EventTicketingDbContext context, string sqlFilePath)
    {
        if (!File.Exists(sqlFilePath))
            throw new FileNotFoundException("Seed SQL file not found.", sqlFilePath);

        string sqlScript = File.ReadAllText(sqlFilePath);

        // Execute SQL script
        context.Database.ExecuteSqlRaw(sqlScript);
    }
}
