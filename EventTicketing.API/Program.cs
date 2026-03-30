using EventTicketing.API.Extensions;
using EventTicketing.API.Middleware;
using EventTicketing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddValidation();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

//// Auto-apply migrations on startup
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<EventTicketingDbContext>();
//    db.Database.Migrate();
//    // Ensure DB exists
//    db.Database.EnsureCreated();

//    // Run seed script
//    DatabaseSeeder.SeedFromSql(db, Path.Combine(AppContext.BaseDirectory, "Data\\Migrations\\SQL Scripts", "InitialSeed.sql"));
//}

app.Run();

public partial class Program { }