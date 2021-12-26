﻿using DotNurse.Injector;
using Microsoft.Extensions.DependencyInjection;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class CommonServiceExtension
{
    public static IServiceCollection AddEntityFrameworkMigrations(this IServiceCollection services)
    {
        services.AddServicesFrom("WinTenDev.Zizi.DbMigrations.EfMigrations", ServiceLifetime.Scoped);

        return services;
    }

    public static IServiceCollection AddCommonService(this IServiceCollection services)
    {
        services.AddServicesFrom("WinTenDev.Zizi.Services.Externals", ServiceLifetime.Scoped);
        services.AddServicesFrom("WinTenDev.Zizi.Services.Internals", ServiceLifetime.Scoped);
        services.AddServicesFrom("WinTenDev.Zizi.Services.Google", ServiceLifetime.Scoped);
        services.AddServicesFrom("WinTenDev.Zizi.Services.Telegram", ServiceLifetime.Scoped);

        return services;
    }
}