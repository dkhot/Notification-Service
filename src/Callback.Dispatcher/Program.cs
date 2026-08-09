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

var connectionString = builder.Configuration.GetConnectionString("NotificationDatabase") ?? "Server=localhost,1433;Database=NotificationService;User Id=sa;******;";

builder.Services.AddNotificationApplication();
builder.Services.AddNotificationInfrastructure(connectionString);

builder.Services.AddHostedService<CallbackDispatcherWorker>();

var host = builder.Build();

host.EnsureNotificationDatabase();

host.Run();
