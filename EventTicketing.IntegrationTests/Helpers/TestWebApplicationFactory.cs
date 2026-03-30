using EventTicketing.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.IntegrationTests.Helpers;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection _connection = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real SQLite DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<EventTicketingDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            //// Each factory instance gets its own isolated in-memory database
            //services.AddDbContext<EventTicketingDbContext>(options =>
            //    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));


            // ✅ Create SINGLE shared connection
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            services.AddDbContext<EventTicketingDbContext>(options =>
                options.UseSqlite(_connection));

            // ✅ Build service provider
            var sp = services.BuildServiceProvider();

            // ✅ Create DB + Tables
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EventTicketingDbContext>();

            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}