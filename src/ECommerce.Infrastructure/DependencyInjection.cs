using ECommerce.Application.Services;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
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
        // Mock
        services.AddScoped<ISkuService, MockSkuService>();
        services.AddScoped<IAddressService, MockAddressService>();
        services.AddScoped<IInventoryService, MockInventoryService>();
        services.AddScoped<ICouponService, MockCouponService>();
        services.AddScoped<IOperationLogService, MockOperationLogService>();

        return services;
    }
}
