using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Exceptions;
using ECommerce.Tests.Helpers;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services;

public class CategoryServiceTests : ServiceTestBase
{
    [Fact]
    public async Task UpdateAsync_MoveToDescendant_ShouldRejectCycle()
    {
        var categories = new[]
        {
            CreateCategory(1, null, 1),
            CreateCategory(2, 1, 2),
            CreateCategory(3, 2, 3)
        };
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(categories[1]);
        repository.Setup(x => x.GetAllAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(categories);

        var service = new CategoryService(repository.Object, CreateUnitOfWorkMock().Object);
        var request = new CategoryRequest(ParentId: 3, Name: "分类2", TreeLevel: 4, SortOrder: 1, Status: 1, IconUrl: null);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.UpdateAsync(2, request, 1));

        Assert.Equal("CATEGORY_PARENT_CYCLE", exception.Code);
        repository.Verify(x => x.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MoveCategory_ShouldUpdateDescendantLevelsWithoutChangingCreatedAt()
    {
        var originalCreatedAt = DateTime.Now.AddDays(-10);
        var categories = new[]
        {
            CreateCategory(1, null, 1),
            CreateCategory(2, 1, 2, createdAt: originalCreatedAt),
            CreateCategory(3, 2, 3),
            CreateCategory(4, 1, 2)
        };
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(categories[1]);
        repository.Setup(x => x.GetAllAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(categories);
        repository.Setup(x => x.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var unitOfWork = CreateUnitOfWorkMock();
        var service = new CategoryService(repository.Object, unitOfWork.Object);
        var request = new CategoryRequest(ParentId: 4, Name: "已移动分类", TreeLevel: 3, SortOrder: 2, Status: 1, IconUrl: null);

        await service.UpdateAsync(2, request, 1);

        Assert.Equal(3, categories[1].TreeLevel);
        Assert.Equal(4, categories[2].TreeLevel);
        Assert.Equal(originalCreatedAt, categories[1].CreatedAt);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Category CreateCategory(int id, int? parentId, int treeLevel, DateTime? createdAt = null) => new()
    {
        Id = id,
        ParentId = parentId,
        Name = $"分类{id}",
        TreeLevel = treeLevel,
        SortOrder = id,
        Status = 1,
        CreatedAt = createdAt ?? DateTime.Now.AddDays(-1)
    };
}
