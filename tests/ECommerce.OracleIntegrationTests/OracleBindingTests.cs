using ECommerce.Application.DTOs;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.OracleIntegrationTests;

public sealed class OracleBindingTests
{
    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Public_product_search_includes_products_from_descendant_categories()
    {
        var factory = new OracleConnectionFactory(Options.Create(new OracleOptions
        {
            ConnectionString = OracleTestEnvironment.DevConnectionString!
        }));
        await using var unitOfWork = new UnitOfWork(factory);
        var repository = new ProductRepository(unitOfWork);

        var result = await repository.SearchPublicAsync(new ProductQuery
        {
            CategoryId = 9001,
            PageIndex = 1,
            PageSize = 50
        });

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, product => Assert.Contains(product.CategoryId, new[] { 9002, 9003 }));
    }

    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Production_connection_binds_repeated_placeholders_by_name()
    {
        var factory = new OracleConnectionFactory(Options.Create(new OracleOptions
        {
            ConnectionString = OracleTestEnvironment.DevConnectionString!
        }));

        await using var connection = (OracleConnection)await factory.CreateOpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT :Value + :Value FROM DUAL";
        command.Parameters.Add(new OracleParameter("Value", OracleDbType.Int32) { Value = 2 });

        Assert.True(command.BindByName);
        Assert.Equal(4, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }
}
