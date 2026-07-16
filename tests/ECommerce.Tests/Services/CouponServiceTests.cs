// 连接 Controller和 Repository
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services;

public class CouponServiceTests
{
    private readonly Mock<ICouponRepository> _mockRepo;
    private readonly CouponService _sut;

    public CouponServiceTests()
    {
        _mockRepo = new Mock<ICouponRepository>();
        _sut = new CouponService(_mockRepo.Object);
    }
    
    // 测试分页查询时，Service能不能正确接收Repository的数据并转化成 DTO
    [Fact]
    public async Task SearchTemplatesAsync_ShouldReturnMappedDtos()
    {
        // Arrange
        var query = new CouponTemplateQuery { PageIndex = 1, PageSize = 10 };
        var templates = new List<CouponTemplate>
        {
            new CouponTemplate { Id = 1, Name = "Test1" },
            new CouponTemplate { Id = 2, Name = "Test2" }
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
    }
    
    // 测试创建优惠券时，参数是否被正确拼装，并成功返回新的 ID。
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
        _mockRepo.Verify(x => x.InsertTemplateAsync(It.Is<CouponTemplate>(t => t.Name == "New Coupon" && t.FaceValue == 20m), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    // 测试当试图更新一个不存在的优惠券时的情况
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
    
    // 测试如果找到了该优惠券，能否正确将新参数覆盖上去并保存
    [Fact]
    public async Task UpdateTemplateAsync_ShouldCallUpdateWhenFound()
    {
        // Arrange
        var existingTemplate = new CouponTemplate { Id = 1, Name = "Old", FaceValue = 10m };
        _mockRepo.Setup(x => x.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existingTemplate);
        _mockRepo.Setup(x => x.UpdateTemplateAsync(It.IsAny<CouponTemplate>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var request = new CouponTemplateRequest("Updated", 1, 30m, 100m, 1000, DateTime.Now, DateTime.Now.AddDays(7), 1);

        // Act
        await _sut.UpdateTemplateAsync(1, request, 1);

        // Assert
        Assert.Equal("Updated", existingTemplate.Name);
        Assert.Equal(30m, existingTemplate.FaceValue);
        _mockRepo.Verify(x => x.UpdateTemplateAsync(existingTemplate, It.IsAny<CancellationToken>()), Times.Once);
    }
    
    // 测试修改状态时，如果找不到目标数据会不会报错
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
    
    // 测试正确的启停状态切换流程
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
}
