using Callback.Dispatcher;
using Notification.Application.DependencyInjection;
using Notification.Infrastructure.Extensions;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

var connectionString = builder.Configuration.GetConnectionString("NotificationDatabase")
    ?? throw new InvalidOperationException("Connection string 'NotificationDatabase' is required.");

builder.Services.AddNotificationApplication();
builder.Services.AddNotificationInfrastructure(connectionString);

builder.Services.AddHostedService<CallbackDispatcherWorker>();

var host = builder.Build();

host.EnsureNotificationDatabase();

host.Run();
