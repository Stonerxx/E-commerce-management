using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;
using Moq;

namespace ECommerce.Tests.Services;

public sealed class CouponServiceTests
{
    private readonly Mock<ICouponRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CouponService _service;

    public CouponServiceTests()
    {
        _unitOfWork.Setup(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(item => item.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(item => item.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _service = new CouponService(_repository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task SearchTemplatesAsync_UsesSafePaginationAndMapsOracleFieldNames()
    {
        var templates = new[] { CreateTemplate() };
        _repository.Setup(item => item.GetTemplatesAsync(
                null,
                null,
                1,
                PageQuery.MaximumPageSize,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CouponTemplate>(templates, 1, PageQuery.MaximumPageSize, 1));

        var result = await _service.SearchTemplatesAsync(new CouponTemplateQuery
        {
            PageIndex = -1,
            PageSize = 1000
        });

        var dto = Assert.Single(result.Items);
        Assert.Equal(50m, dto.Amount);
        Assert.Equal(500m, dto.MinAmount);
        Assert.Equal(100, dto.TotalCount);
    }

    [Fact]
    public async Task CreateTemplateAsync_RejectsIllegalDiscountRate()
    {
        var request = new CouponTemplateRequest(
            "非法折扣券",
            (int)CouponType.Discount,
            1.2m,
            100m,
            10,
            DateTime.Now,
            DateTime.Now.AddDays(1),
            1);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _service.CreateTemplateAsync(request, 1));

        Assert.Equal("VALIDATION_ERROR", exception.Code);
    }

    [Fact]
    public async Task ReceiveAsync_AtomicallyIncrementsAndInsertsWithinTransaction()
    {
        _repository.Setup(item => item.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTemplate());
        _repository.Setup(item => item.TryIncrementReceivedCountAsync(1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.ReceiveAsync(10, 1);

        _repository.Verify(item => item.InsertUserCouponAsync(
            It.Is<UserCoupon>(coupon =>
                coupon.UserId == 10
                && coupon.CouponTemplateId == 1
                && coupon.Status == (int)UserCouponStatus.Unused),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveAsync_RollsBackWhenInventoryConditionFails()
    {
        _repository.Setup(item => item.GetTemplateByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTemplate());
        _repository.Setup(item => item.TryIncrementReceivedCountAsync(1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _service.ReceiveAsync(10, 1));

        Assert.Equal("COUPON_NOT_AVAILABLE", exception.Code);
        _unitOfWork.Verify(item => item.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(item => item.InsertUserCouponAsync(It.IsAny<UserCoupon>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_CalculatesFullReduction()
    {
        var template = CreateTemplate();
        template.ReceivedCount = template.TotalCount;
        _repository.Setup(item => item.GetUserCouponAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserCouponDetail(template));

        var result = await _service.ValidateAsync(10, 20, 600m);

        Assert.True(result.Available);
        Assert.Equal(50m, result.DiscountAmount);
    }

    [Fact]
    public async Task GetMineAsync_ShowsUnusedCouponAsExpiredWhenTemplateIsOutOfDate()
    {
        var template = CreateTemplate();
        template.EndTime = DateTime.Now.AddMinutes(-1);
        _repository.Setup(item => item.GetUserCouponsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateUserCouponDetail(template) });

        var result = await _service.GetMineAsync(10);

        var coupon = Assert.Single(result);
        Assert.Equal((int)UserCouponStatus.Expired, coupon.Status);
        Assert.Equal(template.EndTime, coupon.EndTime);
    }

    [Fact]
    public async Task ValidateAsync_CalculatesDiscountRate()
    {
        var template = CreateTemplate();
        template.Type = (int)CouponType.Discount;
        template.Amount = 0.85m;
        template.MinAmount = 100m;
        _repository.Setup(item => item.GetUserCouponAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserCouponDetail(template));

        var result = await _service.ValidateAsync(10, 20, 199.99m);

        Assert.True(result.Available);
        Assert.Equal(30m, result.DiscountAmount);
    }

    [Fact]
    public async Task ValidateAsync_RejectsCouponOwnedByAnotherUserWithoutLeakingIt()
    {
        _repository.Setup(item => item.GetUserCouponAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCouponWithTemplate?)null);

        var result = await _service.ValidateAsync(10, 20, 600m);

        Assert.False(result.Available);
        Assert.Equal(0m, result.DiscountAmount);
    }

    [Fact]
    public async Task UseForOrderAsync_PerformsConditionalAtomicUpdate()
    {
        _repository.Setup(item => item.GetUserCouponAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserCouponDetail(CreateTemplate()));
        _repository.Setup(item => item.TryUseForOrderAsync(
                10,
                20,
                30,
                600m,
                50m,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.UseForOrderAsync(10, 20, 30, 600m, 50m);

        _repository.VerifyAll();
    }

    [Fact]
    public async Task UseForOrderAsync_RejectsConcurrentSecondUse()
    {
        _repository.Setup(item => item.GetUserCouponAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserCouponDetail(CreateTemplate()));
        _repository.Setup(item => item.TryUseForOrderAsync(
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _service.UseForOrderAsync(10, 20, 30, 600m, 50m));

        Assert.Equal("COUPON_ALREADY_USED", exception.Code);
    }

    [Fact]
    public async Task RestoreForOrderAsync_RequiresMatchingUserCouponAndOrder()
    {
        _repository.Setup(item => item.TryRestoreForOrderAsync(10, 20, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.RestoreForOrderAsync(10, 20, 30);

        _repository.VerifyAll();
    }

    private static CouponTemplate CreateTemplate()
    {
        return new CouponTemplate
        {
            Id = 1,
            Name = "满 500 减 50",
            Type = (int)CouponType.FullReduction,
            Amount = 50m,
            MinAmount = 500m,
            TotalCount = 100,
            ReceivedCount = 1,
            StartTime = DateTime.Now.AddDays(-1),
            EndTime = DateTime.Now.AddDays(1),
            Status = 1
        };
    }

    private static UserCouponWithTemplate CreateUserCouponDetail(CouponTemplate template)
    {
        return new UserCouponWithTemplate(
            new UserCoupon
            {
                Id = 20,
                UserId = 10,
                CouponTemplateId = template.Id,
                Status = (int)UserCouponStatus.Unused,
                ReceivedAt = DateTime.Now.AddHours(-1)
            },
            template);
    }
}
