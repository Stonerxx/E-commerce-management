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

public sealed class ReviewServiceTests
{
    private readonly Mock<IReviewRepository> _reviews = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ReviewService _service;

    public ReviewServiceTests()
    {
        _unitOfWork.Setup(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(item => item.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(item => item.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _service = new ReviewService(_reviews.Object, _orders.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_RejectsOrderOwnedByAnotherUser()
    {
        _orders.Setup(item => item.GetOrderByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(userId: 2));

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _service.CreateAsync(1, CreateRequest()));

        Assert.Equal("FORBIDDEN", exception.Code);
        _reviews.Verify(item => item.InsertAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RejectsOrderThatIsNotCompleted()
    {
        _orders.Setup(item => item.GetOrderByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(status: OrderStatus.Shipped));

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _service.CreateAsync(1, CreateRequest()));

        Assert.Equal("ORDER_NOT_COMPLETED", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsProductThatIsNotInOrder()
    {
        SetupCompletedOrder();
        _reviews.Setup(item => item.OrderContainsProductAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _service.CreateAsync(1, CreateRequest()));

        Assert.Equal("PRODUCT_NOT_IN_ORDER", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateReview()
    {
        SetupCompletedOrder();
        _reviews.Setup(item => item.OrderContainsProductAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _reviews.Setup(item => item.HasReviewedAsync(10, 20, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _service.CreateAsync(1, CreateRequest()));

        Assert.Equal("ALREADY_REVIEWED", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_InsertsPendingReviewAfterEligibilityChecks()
    {
        SetupCompletedOrder();
        _reviews.Setup(item => item.OrderContainsProductAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _reviews.Setup(item => item.HasReviewedAsync(10, 20, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _reviews.Setup(item => item.InsertAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(99);

        var result = await _service.CreateAsync(1, CreateRequest(isAnonymous: true));

        Assert.Equal(99, result);
        _reviews.Verify(item => item.InsertAsync(
            It.Is<Review>(review =>
                review.UserId == 1
                && review.Status == (int)ReviewStatus.Pending
                && review.IsAnonymous
                && review.Images == "[\"https://img.example/review.jpg\"]"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateAsync_RejectsRatingOutsideRange(int rating)
    {
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _service.CreateAsync(1, CreateRequest(rating: rating)));

        Assert.Equal("VALIDATION_ERROR", exception.Code);
        _orders.Verify(item => item.GetOrderByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchByProductAsync_HidesAnonymousUserAndUsesSafePagination()
    {
        var page = new PagedResult<Review>(
            new[] { CreateReview(isAnonymous: true) },
            1,
            PageQuery.MaximumPageSize,
            1);
        _reviews.Setup(item => item.SearchByProductAsync(
                20,
                1,
                PageQuery.MaximumPageSize,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _service.SearchByProductAsync(
            20,
            new PageQuery { PageIndex = -5, PageSize = 5000 });

        Assert.Null(Assert.Single(result.Items).UserId);
        Assert.Equal(1, result.PageIndex);
        Assert.Equal(PageQuery.MaximumPageSize, result.PageSize);
    }

    [Fact]
    public async Task SearchAdminAsync_KeepsAnonymousUserId()
    {
        var page = new PagedResult<Review>(new[] { CreateReview(isAnonymous: true) }, 1, 20, 1);
        _reviews.Setup(item => item.SearchAdminAsync(It.IsAny<ReviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _service.SearchAdminAsync(new ReviewQuery());

        Assert.Equal(1, Assert.Single(result.Items).UserId);
    }

    [Fact]
    public async Task SetStatusAsync_UpdatesStatusAndAverageInOneTransaction()
    {
        _reviews.Setup(item => item.GetByIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateReview());
        _reviews.Setup(item => item.UpdateStatusAsync(30, (int)ReviewStatus.Published, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.SetStatusAsync(30, (int)ReviewStatus.Published, 5);

        _unitOfWork.Verify(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _reviews.Verify(item => item.RefreshProductAverageRatingAsync(20, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(item => item.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupCompletedOrder()
    {
        _orders.Setup(item => item.GetOrderByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder());
    }

    private static ReviewRequest CreateRequest(int rating = 5, bool isAnonymous = false)
    {
        return new ReviewRequest(
            10,
            20,
            rating,
            "很好",
            new[] { "https://img.example/review.jpg" },
            isAnonymous);
    }

    private static OrderMain CreateOrder(long userId = 1, OrderStatus status = OrderStatus.Completed)
    {
        return new OrderMain
        {
            Id = 10,
            UserId = userId,
            Status = (int)status
        };
    }

    private static Review CreateReview(bool isAnonymous = false)
    {
        return new Review
        {
            Id = 30,
            OrderId = 10,
            ProductId = 20,
            UserId = 1,
            Rating = 5,
            Images = "[]",
            IsAnonymous = isAnonymous,
            Status = (int)ReviewStatus.Pending,
            CreatedAt = DateTime.Now
        };
    }
}
