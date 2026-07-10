using ECommerce.Application.Services;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Services;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services.Mocks;
using ECommerce.Shared.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OracleOptions>(configuration.GetSection(OracleOptions.SectionName));
        services.Configure<StatisticsSnapshotOptions>(configuration.GetSection(StatisticsSnapshotOptions.SectionName));
        services.AddScoped<IOracleConnectionFactory, OracleConnectionFactory>();
        services.AddScoped<IDatabaseHealthCheck, DatabaseHealthCheck>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        // Member4
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Services
        // Member4
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        // TEMP_DEMO_* mocks keep the current demo flow runnable until member2/3/5 real services are merged.
        services.AddScoped<ISkuService, MockSkuService>();
        services.AddScoped<IAddressService, MockAddressService>();
        services.AddScoped<IInventoryService, MockInventoryService>();
        services.AddScoped<ICouponService, MockCouponService>();
        services.AddScoped<IOperationLogService, MockOperationLogService>();

        // Member6
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IStatisticsSnapshotService, StatisticsSnapshotService>();
        services.AddHostedService<OrderStatisticsSnapshotHostedService>();


        return services;
    }
}
