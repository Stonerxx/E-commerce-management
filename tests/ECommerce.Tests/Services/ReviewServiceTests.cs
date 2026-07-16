using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services;

public class ReviewServiceTests
{
    private readonly Mock<IReviewRepository> _mockRepo;
    private readonly ReviewService _sut;

    public ReviewServiceTests()
    {
        _mockRepo = new Mock<IReviewRepository>();
        _sut = new ReviewService(_mockRepo.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowIfAlreadyReviewed()
    {
        // Arrange
        _mockRepo.Setup(x => x.HasReviewedAsync(1, 2, 3, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var request = new ReviewRequest(1, 2, 5, "Good", new List<string>(), false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateAsync(3, request));
        Assert.Equal("ALREADY_REVIEWED", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldInsertAndReturnId()
    {
        // Arrange
        _mockRepo.Setup(x => x.HasReviewedAsync(1, 2, 3, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _mockRepo.Setup(x => x.InsertAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(99L);
        var request = new ReviewRequest(1, 2, 5, "Good", new List<string> { "url1" }, true);

        // Act
        var result = await _sut.CreateAsync(3, request);

        // Assert
        Assert.Equal(99L, result);
        _mockRepo.Verify(x => x.InsertAsync(It.Is<Review>(r => r.Rating == 5 && r.IsAnonymous == 1 && r.Images != null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchByProductAsync_ShouldReturnMappedDtos()
    {
        // Arrange
        var pagedResult = new PagedResult<Review>(new List<Review>
        {
            new Review { Id = 1, Content = "A", Images = "[\"url1\"]" }
        }, 1, 10, 1);
        
        _mockRepo.Setup(x => x.GetByProductAsync(2, 1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.SearchByProductAsync(2, new PageQuery { PageIndex = 1, PageSize = 10 });

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("A", result.Items[0].Content);
        Assert.Single(result.Items[0].Images);
        Assert.Equal("url1", result.Items[0].Images[0]);
    }

    [Fact]
    public async Task SetStatusAsync_ShouldThrowWhenNotFound()
    {
        // Arrange
        _mockRepo.Setup(x => x.UpdateStatusAsync(1, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.SetStatusAsync(1, 1, 99));
        Assert.Equal("NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task SetStatusAsync_ShouldSucceedWhenFound()
    {
        // Arrange
        _mockRepo.Setup(x => x.UpdateStatusAsync(1, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        // Act
        var exception = await Record.ExceptionAsync(() => _sut.SetStatusAsync(1, 1, 99));

        // Assert
        Assert.Null(exception);
        _mockRepo.Verify(x => x.UpdateStatusAsync(1, 1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
