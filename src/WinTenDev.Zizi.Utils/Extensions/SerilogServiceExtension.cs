using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BotFramework.Config;
using Exceptionless;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Configuration;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.Datadog.Logs;
using Serilog.Sinks.Grafana.Loki;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Sinks.TelegramBot;
using WinTenDev.Zizi.Models.Configs;

namespace WinTenDev.Zizi.Utils.Extensions;

/// <summary>
/// Extension methods for Serilog Service
/// </summary>
public static class SerilogServiceExtension
{
    private const string LogPath = "Storage/Logs/ZiziBot-.log";
    private const string DataDogHost = "intake.logs.datadoghq.com";
    private const RollingInterval RollingInterval = Serilog.RollingInterval.Day;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);
    private const string TemplateBase = $"[{{Level:u3}}]{{MemoryUsage}}{{ThreadId}} {{Message:lj}}{{NewLine}}{{Exception}}";
    private const string OutputTemplate = $"{{Timestamp:HH:mm:ss.fff}} {TemplateBase}";

    /// <summary>
    /// Setup the serilog using the specified app
    /// </summary>
    /// <param name="app">The app</param>
    [Obsolete("This method will be removed, please use AddSerilogBootstrapper")]
    public static void SetupSerilog(this IApplicationBuilder app)
    {
        // const string logPath = "Storage/Logs/ZiziBot-.log";
        // const RollingInterval rollingInterval = RollingInterval.Day;
        // var flushInterval = TimeSpan.FromSeconds(1);
        //
        // var templateBase = $"[{{Level:u3}}] {{MemoryUsage}}{{ThreadId}}| {{Message:lj}}{{NewLine}}{{Exception}}";
        // var outputTemplate = $"{{Timestamp:HH:mm:ss.fff}} {templateBase}";

        // var appConfig = app.GetServiceProvider().GetRequiredService<AppConfig>();
        // var envConfig = appConfig.EnvironmentConfig;
        // var logger = appConfig.DataDogConfig;

        var serviceProvider = app.GetServiceProvider();
        var envConfig = serviceProvider.GetRequiredService<EnvironmentConfig>();
        var datadogConfig = serviceProvider.GetRequiredService<IOptionsSnapshot<DataDogConfig>>().Value;
        var datadogKey = datadogConfig.ApiKey;

        var serilogConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            // .Enrich.WithThreadId()
            // .Enrich.WithPrettiedMemoryUsage()
            .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
            .MinimumLevel.Override("MySqlConnector", LogEventLevel.Warning)
            // .Filter.ByExcluding("{@m} not like '%pinged server%'")
            .WriteTo.Async
            (
                a =>
                    a.File(LogPath, rollingInterval: RollingInterval, flushToDiskInterval: FlushInterval, shared: true, outputTemplate: OutputTemplate)
            )
            .WriteTo.Async
            (
                a =>
                    a.Console(theme: SystemConsoleTheme.Colored, outputTemplate: OutputTemplate)
            );
        // .WriteTo.Async(logger => logger.Sentry(options =>
        // {
        //     options.MinimumBreadcrumbLevel = LogEventLevel.Debug;
        //     options.MinimumEventLevel = LogEventLevel.Warning;
        // }));

        if (envConfig.IsProduction)
        {
            serilogConfig.MinimumLevel.Information();
        }
        else
        {
            serilogConfig.MinimumLevel.Debug();
        }

        // if (datadogKey != "YOUR_API_KEY" || datadogKey.IsNotNullOrEmpty())
        // {
        //     var dataDogHost = "intake.logs.datadoghq.com";
        //     var logger = new DatadogConfiguration(url: dataDogHost, port: 10516, useSSL: true, useTCP: true);
        //
        //     serilogConfig.WriteTo.DatadogLogs(
        //         apiKey: datadogKey,
        //         service: "TelegramBot",
        //         source: logger.Source,
        //         host: logger.Host,
        //         tags: logger.Tags.ToArray(),
        //         logger: logger);
        // }

        Log.Logger = serilogConfig.CreateLogger();
    }

    /// <summary>
    /// Add Serilog bootstrap
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static LoggerConfiguration AddSerilogBootstrapper(
        this LoggerConfiguration configuration,
        IServiceProvider serviceProvider
    )
    {
        SelfLog.Enable(msg => Debug.WriteLine(msg));

        var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
        var botConfig = serviceProvider.GetRequiredService<IOptions<BotConfig>>().Value;
        var datadogConfig = serviceProvider.GetRequiredService<IOptions<DataDogConfig>>().Value;
        var eventLogConfig = serviceProvider.GetRequiredService<IOptions<EventLogConfig>>().Value;
        var exceptionlessConfig = serviceProvider.GetRequiredService<IOptions<ExceptionlessConfig>>().Value;
        var grafanaConfig = serviceProvider.GetRequiredService<IOptions<GrafanaConfig>>().Value;
        var sentryConfig = serviceProvider.GetRequiredService<IOptions<SentryConfig>>().Value;

        eventLogConfig.BotConfig = botConfig;

        configuration
            .Enrich.FromLogContext()
            // .Enrich.WithThreadId()
            // .Enrich.WithPrettiedMemoryUsage()
            .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
            .MinimumLevel.Override("MySqlConnector", LogEventLevel.Information)
            // .Filter.ByExcluding("{@m} not like '%pinged server%'")
            .WriteTo.Async
            (
                a =>
                    a.File(LogPath, rollingInterval: RollingInterval, flushToDiskInterval: FlushInterval, shared: true, outputTemplate: OutputTemplate)
            )
            .WriteTo.Async
            (
                a =>
                    a.Console(theme: SystemConsoleTheme.Colored, outputTemplate: OutputTemplate)
            );

        if (hostEnvironment.IsProduction())
        {
            configuration.MinimumLevel.Information();
        }
        else
        {
            configuration.MinimumLevel.Debug();
        }

        configuration.AddDatadog(datadogConfig);
        configuration.AddGrafana(grafanaConfig);
        configuration.AddSentry(sentryConfig);
        configuration.AddTelegramBot4EventLog(eventLogConfig);
        configuration.AddExceptionless(exceptionlessConfig);

        return configuration;
    }

    /// <summary>
    /// Adds the grafana using the specified logger for Serilog
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="config">The grafana logger</param>
    /// <returns>The logger</returns>
    private static LoggerConfiguration AddGrafana(
        this LoggerConfiguration logger,
        GrafanaConfig config
    )
    {
        if (!config.IsEnabled) return logger;
        if (!config.LokiUrl.IsNotNullOrEmpty()) return logger;

        logger.WriteTo.GrafanaLoki
        (
            uri: config.LokiUrl,
            queueLimit: 20,
            labels: new List<LokiLabel>()
            {
                new LokiLabel()
                {
                    Key = "app-name",
                    Value = "Zizi Beta"
                }
            },
            batchPostingLimit: 20,
            createLevelLabel: true
        );

        return logger;
    }

    /// <summary>
    /// Adds the datadog using the specified logger for Serilog
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="config">The datadog logger</param>
    /// <returns>The logger</returns>
    private static LoggerConfiguration AddDatadog(
        this LoggerConfiguration logger,
        DataDogConfig config
    )
    {
        var datadogKey = config.ApiKey;

        if (!config.IsEnabled) return logger;
        if (datadogKey == "YOUR_API_KEY" && !datadogKey.IsNotNullOrEmpty()) return logger;

        var datadogConfiguration = new DatadogConfiguration(url: DataDogHost, port: 10516, useSSL: true, useTCP: true);

        logger.WriteTo.DatadogLogs
        (
            apiKey: datadogKey,
            service: "TelegramBot",
            source: config.Source,
            host: config.Host,
            tags: config.Tags.ToArray(),
            configuration: datadogConfiguration
        );

        return logger;
    }

    /// <summary>
    /// Adds the sentry using the specified logger for Serilog
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="config">The sentry logger</param>
    /// <returns>The logger</returns>
    private static LoggerConfiguration AddSentry(
        this LoggerConfiguration logger,
        SentryConfig config
    )
    {
        if (!config.IsEnabled) return logger;

        logger.WriteTo.Sentry
        (
            options => {
                // options.AutoSessionTracking = true;
                // options.InitializeSdk = true;
                // options.DeduplicateMode = DeduplicateMode.All;
                // options.AttachStacktrace = true;
                options.Dsn = config.Dsn;
            }
        );

        return logger;
    }

    /// <summary>
    /// Adds the telegram bot 4 event log using the specified logger for Serilog
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="config">The event log logger</param>
    /// <returns>The logger</returns>
    private static LoggerConfiguration AddTelegramBot4EventLog(
        this LoggerConfiguration logger,
        EventLogConfig config
    )
    {
        if (!config.IsEnabled) return logger;
        var botToken = config.BotToken;

        if (botToken.IsNullOrEmpty()) botToken = config.BotConfig.Token;

        logger.WriteTo.TelegramBot
        (
            token: botToken,
            chatId: config.ChannelId.ToString(),
            restrictedToMinimumLevel: LogEventLevel.Error,
            parseMode: ParseMode.Markdown
        );

        return logger;
    }

    private static LoggerConfiguration AddExceptionless(
        this LoggerConfiguration logger,
        ExceptionlessConfig config
    )
    {
        if (!config.IsEnabled) return logger;

        ExceptionlessClient.Default.Startup(config.ApiKey);

        logger.WriteTo.Exceptionless(builder => builder.AddTags(config.Tags));

        return logger;
    }

    /// <summary>
    /// Add ThreadId to Logging logger for Serilog
    /// </summary>
    /// <param name="enrich"></param>
    /// <returns></returns>
    public static LoggerConfiguration WithThreadId(this LoggerEnrichmentConfiguration enrich)
    {
        return enrich.WithDynamicProperty
        (
            "ThreadId", () => {
                var threadId = Thread.CurrentThread.ManagedThreadId.ToString();
                return $"ThreadId: {threadId} ";
            }
        );
    }

    /// <summary>
    /// Add MemoryUsage to Logging logger for Serilog
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static LoggerConfiguration WithPrettiedMemoryUsage(
        this LoggerEnrichmentConfiguration configuration
    )
    {
        return configuration.WithDynamicProperty
        (
            "MemoryUsage", () => {
                var proc = Process.GetCurrentProcess();
                var mem = proc.PrivateMemorySize64.SizeFormat();

                return $"{mem} ";
            }
        );
    }
}