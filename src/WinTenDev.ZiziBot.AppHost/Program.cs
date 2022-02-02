using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Extensions;

namespace WinTenDev.ZiziBot.AppHost;

public static class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            await CreateHostBuilder(args)
                .Build()
                .RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(@"Error Start {0}", ex.Demystify());

            if (ex.StackTrace.Contains("Hangfire"))
            {
                Log.Warning("Seem error about Hangfire!");
            }

            Log.Fatal(ex.Demystify(), "Host terminated unexpectedly");

            await ex.SaveErrorToText();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args)
            .UseSerilog
            (
                (
                    hostBuilderContext,
                    serviceProvider,
                    loggerConfiguration
                ) => {
                    loggerConfiguration.AddSerilogBootstrapper(serviceProvider);
                }
            )
            .ConfigureAppConfiguration
            (
                (
                    context,
                    builder
                ) => {
                    builder.AddJsonFile("appsettings.json", true, true);
                    builder.AddAppSettingsJson();
                }
            )
            .ConfigureServices
            (
                (
                    context,
                    services
                ) => {
                    services.MappingAppSettings();
                }
            )
            .ConfigureWebHostDefaults
            (
                webBuilder => {
                    webBuilder.UseStartup<Startup>();
                }
            )
            .ConfigureLogging
            (
                (
                    context,
                    builder
                ) => {
                    builder.AddSerilog();
                }
            );

        // if (Directory.Exists("wwwroot")) hostBuilder.UseContentRoot("wwwroot");

        return hostBuilder;
    }

    [Obsolete("WebHostBuilder will replaced with CreateHostBuilder")]
    private static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        return WebHost.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration
            (
                (
                    webHostBuilder,
                    configBuilder
                ) => {
                    configBuilder
                        .AddJsonFile("appsettings.json", true, true)
                        // .AddJsonFile($"appsettings.{hostBuilder.HostingEnvironment.EnvironmentName}.json", true, true)
                        // .AddJsonFile("Storage/Config/security-base.json", true, true)
                        .AddJsonEnvVar("QUICKSTART_SETTINGS", true);
                }
            ).UseStartup<Startup>()
            .UseSerilog();
    }
}