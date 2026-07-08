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

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryService, CategoryService>();

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<IProductSpecRepository, ProductSpecRepository>();
        services.AddScoped<IProductService, ProductService>();

        services.AddScoped<ISkuRepository, SkuRepository>();
        services.AddScoped<ISkuService, SkuService>();

        services.AddScoped<IInventoryLogRepository, InventoryLogRepository>();
        services.AddScoped<IInventoryService, InventoryService>();

        return services;
    }
}