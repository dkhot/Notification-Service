using Microsoft.AspNetCore.Http.Json;
using Notification.Api.Endpoints;
using Notification.Application.DependencyInjection;
using Notification.Infrastructure.Extensions;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

var connectionString = builder.Configuration.GetConnectionString("NotificationDatabase")
    ?? throw new InvalidOperationException("Connection string 'NotificationDatabase' is required.");

builder.Services.AddNotificationApplication();
builder.Services.AddNotificationInfrastructure(connectionString);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

app.EnsureNotificationDatabase();

app.MapHealthChecks("/health");
app.MapNotificationEndpoints();

app.Run();
