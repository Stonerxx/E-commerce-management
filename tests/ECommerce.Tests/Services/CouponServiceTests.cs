using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services;

public class CouponServiceTests
{
    private readonly Mock<ICouponRepository> _mockRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly CouponService _sut;

    public CouponServiceTests()
    {
        _mockRepo = new Mock<ICouponRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _sut = new CouponService(_mockRepo.Object, _mockUow.Object);
    }

    [Fact]
    public async Task SearchTemplatesAsync_ShouldReturnMappedDtos()
    {
        // Arrange
        var query = new CouponTemplateQuery { PageIndex = 1, PageSize = 10 };
        var templates = new List<CouponTemplate>
        {
            new CouponTemplate { Id = 1, Name = "Test1", Amount = 20m, MinAmount = 100m },
            new CouponTemplate { Id = 2, Name = "Test2", Amount = 30m, MinAmount = 200m }
        };
        var pagedResult = new PagedResult<CouponTemplate>(templates, 1, 10, 2);

        _mockRepo.Setup(x => x.GetTemplatesAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.SearchTemplatesAsync(query);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Test1", result.Items[0].Name);
        Assert.Equal(20m, result.Items[0].Amount);
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldCallInsertAndReturnId()
    {
        // Arrange
        var request = new CouponTemplateRequest("New Coupon", 1, 20m, 100m, 1000, DateTime.Now, DateTime.Now.AddDays(7), 1);
        _mockRepo.Setup(x => x.InsertTemplateAsync(It.IsAny<CouponTemplate>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(99);

        // Act
        var id = await _sut.CreateTemplateAsync(request, 1);

        // Assert
        Assert.Equal(99, id);
        _mockRepo.Verify(x => x.InsertTemplateAsync(It.Is<CouponTemplate>(t => t.Name == "New Coupon" && t.Amount == 20m), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_ShouldThrowWhenNotFound()
    {
        // Arrange
        _mockRepo.Setup(x => x.GetTemplateByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((CouponTemplate?)null);
        var request = new CouponTemplateRequest("Updated", 1, 30m, 100m, 1000, DateTime.Now, DateTime.Now.AddDays(7), 1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateTemplateAsync(1, request, 1));
        Assert.Equal("NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task UpdateTemplateAsync_ShouldCallUpdateWhenFound()
    {
        // Arrange
        var existingTemplate = new CouponTemplate { Id = 1, Name = "Old", Amount = 10m };
        _mockRepo.Setup(x => x.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existingTemplate);
        _mockRepo.Setup(x => x.UpdateTemplateAsync(It.IsAny<CouponTemplate>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var request = new CouponTemplateRequest("Updated", 1, 30m, 100m, 1000, DateTime.Now, DateTime.Now.AddDays(7), 1);

        // Act
        await _sut.UpdateTemplateAsync(1, request, 1);

        // Assert
        Assert.Equal("Updated", existingTemplate.Name);
        Assert.Equal(30m, existingTemplate.Amount);
        _mockRepo.Verify(x => x.UpdateTemplateAsync(existingTemplate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetTemplateStatusAsync_ShouldThrowWhenNotFound()
    {
        // Arrange
        _mockRepo.Setup(x => x.UpdateTemplateStatusAsync(1, 0, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.SetTemplateStatusAsync(1, 0, 1));
        Assert.Equal("NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task SetTemplateStatusAsync_ShouldSucceedWhenFound()
    {
        // Arrange
        _mockRepo.Setup(x => x.UpdateTemplateStatusAsync(1, 0, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        // Act
        var exception = await Record.ExceptionAsync(() => _sut.SetTemplateStatusAsync(1, 0, 1));

        // Assert
        Assert.Null(exception);
        _mockRepo.Verify(x => x.UpdateTemplateStatusAsync(1, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldThrowIfTemplateNotFound()
    {
        _mockRepo.Setup(x => x.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((CouponTemplate?)null);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.ReceiveAsync(1, 1));
        Assert.Equal("NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldSucceedAndCommitTransaction()
    {
        var template = new CouponTemplate { Id = 1, Status = 1, StartTime = DateTime.Now.AddDays(-1), EndTime = DateTime.Now.AddDays(1) };
        _mockRepo.Setup(x => x.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(template);
        _mockRepo.Setup(x => x.IncrementTemplateReceivedCountAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockRepo.Setup(x => x.InsertUserCouponAsync(It.IsAny<UserCoupon>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.ReceiveAsync(1, 1);

        _mockUow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(x => x.IncrementTemplateReceivedCountAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(x => x.InsertUserCouponAsync(It.Is<UserCoupon>(u => u.UserId == 1 && u.CouponTemplateId == 1 && u.Status == 0), It.IsAny<CancellationToken>()), Times.Once);
        _mockUow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_ShouldCalculateDiscountCorrectly()
    {
        // Test discount calculation (Type 2, 85%)
        var uc = new UserCoupon { Id = 1, UserId = 1, CouponTemplateId = 1, Status = 0 };
        var template = new CouponTemplate { Id = 1, Type = 2, Amount = 0.85m, MinAmount = 100m, StartTime = DateTime.Now.AddDays(-1), EndTime = DateTime.Now.AddDays(1) };
        
        _mockRepo.Setup(x => x.GetUserCouponByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(uc);
        _mockRepo.Setup(x => x.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(template);

        var result = await _sut.ValidateAsync(1, 1, 200m);

        Assert.True(result.Available);
        Assert.Equal(30m, result.DiscountAmount); // 200 * (1 - 0.85) = 30
    }
}
