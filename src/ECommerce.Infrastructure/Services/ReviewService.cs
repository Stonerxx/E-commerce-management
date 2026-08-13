using System.Text.Json;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Services;

public sealed class ReviewService : IReviewService
{
    private const int MaximumImageCount = 9;
    private const int MaximumImageUrlLength = 500;
    private const int MaximumImagesJsonLength = 2000;

    private readonly IReviewRepository _reviewRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReviewService(
        IReviewRepository reviewRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _reviewRepository = reviewRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<long> CreateAsync(
        long userId,
        ReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var order = await _orderRepository.GetOrderByIdAsync(request.OrderId, cancellationToken)
            ?? throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");
        if (order.UserId != userId)
        {
            throw new BusinessException("FORBIDDEN", "不能评价其他用户的订单");
        }

        if (order.Status != (int)OrderStatus.Completed)
        {
            throw new BusinessException("ORDER_NOT_COMPLETED", "订单完成后才能评价商品");
        }

        if (!await _reviewRepository.OrderContainsProductAsync(request.OrderId, request.ProductId, cancellationToken))
        {
            throw new BusinessException("PRODUCT_NOT_IN_ORDER", "订单中不包含该商品");
        }

        if (await _reviewRepository.HasReviewedAsync(request.OrderId, request.ProductId, userId, cancellationToken))
        {
            throw new BusinessException("ALREADY_REVIEWED", "该订单商品已经评价");
        }

        var imagesJson = request.Images.Count == 0
            ? null
            : JsonSerializer.Serialize(request.Images);
        var review = new Review
        {
            OrderId = request.OrderId,
            ProductId = request.ProductId,
            UserId = userId,
            Rating = request.Rating,
            Content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content.Trim(),
            Images = imagesJson,
            IsAnonymous = request.IsAnonymous,
            Status = (int)ReviewStatus.Pending,
            CreatedAt = DateTime.Now
        };

        try
        {
            return await _reviewRepository.InsertAsync(review, cancellationToken);
        }
        catch (OracleException exception) when (exception.Number == 1)
        {
            throw new BusinessException("ALREADY_REVIEWED", "该订单商品已经评价");
        }
    }

    public async Task<PagedResult<ReviewDto>> SearchByProductAsync(
        long productId,
        PageQuery query,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
        {
            throw new BusinessException("VALIDATION_ERROR", "商品 ID 无效");
        }

        var result = await _reviewRepository.SearchByProductAsync(
            productId,
            query.SafePageIndex,
            query.SafePageSize,
            cancellationToken);
        return MapPage(result, hideAnonymousUser: true);
    }

    public async Task<PagedResult<ReviewDto>> SearchAdminAsync(
        ReviewQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Status.HasValue && !Enum.IsDefined(typeof(ReviewStatus), query.Status.Value))
        {
            throw new BusinessException("VALIDATION_ERROR", "评价状态无效");
        }

        var safeQuery = query with
        {
            PageIndex = query.SafePageIndex,
            PageSize = query.SafePageSize
        };
        var result = await _reviewRepository.SearchAdminAsync(safeQuery, cancellationToken);
        return MapPage(result, hideAnonymousUser: false);
    }

    public async Task SetStatusAsync(
        long reviewId,
        int status,
        long operatorId,
        CancellationToken cancellationToken = default)
    {
        if (status is not ((int)ReviewStatus.Pending) and not ((int)ReviewStatus.Published) and not ((int)ReviewStatus.Blocked))
        {
            throw new BusinessException("VALIDATION_ERROR", "评价状态无效");
        }

        var review = await _reviewRepository.GetByIdAsync(reviewId, cancellationToken)
            ?? throw new BusinessException("REVIEW_NOT_FOUND", "评价不存在");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await _reviewRepository.UpdateStatusAsync(reviewId, status, cancellationToken))
            {
                throw new BusinessException("REVIEW_NOT_FOUND", "评价不存在");
            }

            await _reviewRepository.RefreshProductAverageRatingAsync(review.ProductId, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void ValidateRequest(ReviewRequest request)
    {
        if (request.OrderId <= 0 || request.ProductId <= 0)
        {
            throw new BusinessException("VALIDATION_ERROR", "订单 ID 和商品 ID 必须有效");
        }

        if (request.Rating is < 1 or > 5)
        {
            throw new BusinessException("VALIDATION_ERROR", "评分必须在 1 到 5 之间");
        }

        if (request.Images is null)
        {
            throw new BusinessException("VALIDATION_ERROR", "评价图片列表不能为空");
        }

        if (request.Images.Count > MaximumImageCount)
        {
            throw new BusinessException("VALIDATION_ERROR", $"评价图片不能超过 {MaximumImageCount} 张");
        }

        if (request.Images.Any(image => string.IsNullOrWhiteSpace(image) || image.Length > MaximumImageUrlLength))
        {
            throw new BusinessException("VALIDATION_ERROR", $"评价图片 URL 不能为空且不能超过 {MaximumImageUrlLength} 个字符");
        }

        var imagesJson = JsonSerializer.Serialize(request.Images);
        if (imagesJson.Length > MaximumImagesJsonLength)
        {
            throw new BusinessException("VALIDATION_ERROR", "评价图片数据过长");
        }
    }

    private static PagedResult<ReviewDto> MapPage(PagedResult<Review> result, bool hideAnonymousUser)
    {
        var items = result.Items.Select(review => new ReviewDto(
            review.Id,
            review.OrderId,
            review.ProductId,
            hideAnonymousUser && review.IsAnonymous ? null : review.UserId,
            review.Rating,
            review.Content,
            ParseImages(review.Images),
            review.IsAnonymous,
            review.Status,
            review.CreatedAt)).ToList();

        return new PagedResult<ReviewDto>(items, result.PageIndex, result.PageSize, result.TotalCount);
    }

    private static IReadOnlyList<string> ParseImages(string? images)
    {
        if (string.IsNullOrWhiteSpace(images))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(images) ?? new List<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
