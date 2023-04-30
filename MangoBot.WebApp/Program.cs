using MangoBot.Infrastructure.Contexts;
using MangoBot.Infrastructure.DiscordMessaging;
using MangoBot.Infrastructure.Holidays;
using MangoBot.Infrastructure.Models;
using MangoBot.WebApp.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

log.Information("Starting");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.Configure<DiscordSettings>(builder.Configuration.GetSection("Discord"));
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddDbContext<SchedulesContext>(contextOptions =>
    {
        var connectionString = builder.Configuration.GetConnectionString("Cosmos");
        if (connectionString != null)
        {
            contextOptions.UseCosmos(connectionString, databaseName: "MangoBot");
        }
    });
    builder.Services.AddSingleton<IPublicHolidaysProvider, PublicHolidaysProvider>();
    builder.Services.AddSingleton<IDiscordClientProvider, DiscordClientProvider>();
    builder.Services.AddSingleton<IMessageSender, DiscordMessageSender>();
    
    builder.Services.AddHostedService<SchedulingService>();
    builder.Host.UseSerilog(log);
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(log);
    
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    // if (app.Environment.IsDevelopment())
    // {
    //     app.UseSwagger();
    //     app.UseSwaggerUI();
    // }

    // app.UseHttpsRedirection();

    // app.UseAuthorization();

    // app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    log.Fatal(ex, "Application Crash!");
}
finally
{
    Log.CloseAndFlush();
}