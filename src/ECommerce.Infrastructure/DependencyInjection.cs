using ECommerce.Application.Services;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
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

        // Member 5
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ILogisticsRepository, LogisticsRepository>();
        services.AddScoped<ILogisticsService, LogisticsService>();

        return services;
    }
}
