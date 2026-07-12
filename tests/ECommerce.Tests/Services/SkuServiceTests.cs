using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Exceptions;
using Moq;

namespace ECommerce.Tests.Services;

public sealed class SkuServiceTests
{
    [Fact]
    public async Task CreateAsync_RequiresEveryDefinedSpecificationGroup()
    {
        var skuRepository = new Mock<ISkuRepository>(MockBehavior.Strict);
        var productSpecRepository = CreateProductSpecRepository(
            new ProductSpec { ProductId = 1, SpecName = "颜色", SpecValue = "红色" },
            new ProductSpec { ProductId = 1, SpecName = "尺寸", SpecValue = "M" });
        var service = new SkuService(skuRepository.Object, productSpecRepository.Object);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(1, CreateRequest(("颜色", "红色")), 1));

        Assert.Equal("SKU_SPEC_INCOMPLETE", exception.Code);
        skuRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_RejectsAnExistingSpecificationCombination()
    {
        var skuRepository = new Mock<ISkuRepository>();
        skuRepository.Setup(repository => repository.GetByProductAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new SkuDto(10, 1, "{\"尺寸\":\"M\",\"颜色\":\"红色\"}", 100, null, 10, 0, 1, null, 1)
            });
        var productSpecRepository = CreateProductSpecRepository(
            new ProductSpec { ProductId = 1, SpecName = "颜色", SpecValue = "红色" },
            new ProductSpec { ProductId = 1, SpecName = "尺寸", SpecValue = "M" });
        var service = new SkuService(skuRepository.Object, productSpecRepository.Object);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(1, CreateRequest(("颜色", "红色"), ("尺寸", "M")), 1));

        Assert.Equal("SKU_SPEC_COMBINATION_DUPLICATE", exception.Code);
        skuRepository.Verify(repository => repository.CreateAsync(It.IsAny<Sku>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IProductSpecRepository> CreateProductSpecRepository(params ProductSpec[] specs)
    {
        var repository = new Mock<IProductSpecRepository>();
        repository.Setup(item => item.GetByProductAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(specs);
        return repository;
    }

    private static SkuSaveRequest CreateRequest(params (string Name, string Value)[] selections) => new(
        selections.Select(item => new SkuSpecSelection(item.Name, item.Value)).ToArray(),
        Price: 100,
        OriginalPrice: null,
        Stock: 10,
        WarningStock: 1,
        SkuImage: null,
        Status: 1);
}
