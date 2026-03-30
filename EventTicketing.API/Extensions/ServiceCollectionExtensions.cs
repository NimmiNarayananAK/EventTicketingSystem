using EventTicketing.API.DTOs.Requests;
using EventTicketing.API.Validators;
using EventTicketing.Infrastructure.Data;
using EventTicketing.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.API.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the EF Core database context.</summary>
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<EventTicketingDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }

    /// <summary>Registers business logic services.</summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IReportingService, ReportingService>();

        return services;
    }

    /// <summary>
    /// Registers FluentValidation validators from this assembly.
    /// NOTE: FluentValidation.AspNetCore auto-validation is deprecated.
    /// Validators are injected and called explicitly in controllers.
    /// </summary>
    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateEventRequest>, CreateEventRequestValidator>();
        services.AddScoped<IValidator<PurchaseTicketRequest>, PurchaseTicketRequestValidator>();
        //services.AddFluentValidationAutoValidation();
        return services;
    }

    /// <summary>Registers Swagger/OpenAPI documentation.</summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Event Ticketing API",
                Version = "v1",
                Description = "A simplified event ticketing system API."
            });
        });

        return services;
    }
}