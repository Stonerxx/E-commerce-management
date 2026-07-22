using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Tests.Helpers;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services;

public class ProductServiceTests : ServiceTestBase
{
    [Fact]
    public async Task CreateAsync_ShouldCommitProductAggregateTogether()
    {
        var unitOfWork = CreateUnitOfWorkMock();
        var productRepository = new Mock<IProductRepository>();
        productRepository.Setup(x => x.CategoryExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        productRepository.Setup(x => x.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);

        var service = CreateService(unitOfWork, productRepository, new Mock<IProductImageRepository>(), new Mock<IProductSpecRepository>());

        var productId = await service.CreateAsync(CreateRequest(), operatorId: 1);

        Assert.Equal(100, productId);
        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenImageSaveFails_ShouldRollbackProductAggregate()
    {
        var unitOfWork = CreateUnitOfWorkMock();
        var productRepository = new Mock<IProductRepository>();
        productRepository.Setup(x => x.CategoryExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        productRepository.Setup(x => x.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);

        var imageRepository = new Mock<IProductImageRepository>();
        imageRepository.Setup(x => x.CreateAsync(It.IsAny<ProductImage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("image write failed"));

        var service = CreateService(unitOfWork, productRepository, imageRepository, new Mock<IProductSpecRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            CreateRequest(images: new[] { new ProductImageRequest("/images/product.png", 1) }),
            operatorId: 1));

        unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRecommendationsAsync_ClampsRequestedLimitToTwenty()
    {
        var unitOfWork = CreateUnitOfWorkMock();
        var productRepository = new Mock<IProductRepository>();
        productRepository.Setup(x => x.GetRecommendationsAsync(100, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProductListItemDto>());
        var service = CreateService(
            unitOfWork,
            productRepository,
            new Mock<IProductImageRepository>(),
            new Mock<IProductSpecRepository>());

        var result = await service.GetRecommendationsAsync(100, 999);

        Assert.Empty(result);
        productRepository.Verify(
            x => x.GetRecommendationsAsync(100, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ProductService CreateService(
        Mock<IUnitOfWork> unitOfWork,
        Mock<IProductRepository> productRepository,
        Mock<IProductImageRepository> imageRepository,
        Mock<IProductSpecRepository> specRepository)
    {
        return new ProductService(
            productRepository.Object,
            imageRepository.Object,
            specRepository.Object,
            new Mock<ISkuService>().Object,
            unitOfWork.Object);
    }

    private static ProductSaveRequest CreateRequest(IReadOnlyList<ProductImageRequest>? images = null) => new(
        CategoryId: 1,
        Name: "测试商品",
        Description: null,
        MainImage: "/images/main.png",
        Status: 1,
        Images: images ?? Array.Empty<ProductImageRequest>(),
        Specs: Array.Empty<ProductSpecRequest>(),
        Skus: Array.Empty<SkuSaveRequest>());
}
