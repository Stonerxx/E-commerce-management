using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;
using System.Text.Json;

namespace ECommerce.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;

    public ReviewService(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<long> CreateAsync(long userId, ReviewRequest request, CancellationToken cancellationToken = default)
    {
        // 理想情况下，当用户发商品评价时，应该去校验：这个订单是不是真实存在的？是不是属于这个用户的？订单状态是不是‘已完成/已签收’？以及这个订单里到底包没包含这件商品？
        // 需要Member 4的订单查询接口写好，这里先跳过了
        
        bool hasReviewed = await _reviewRepository.HasReviewedAsync(request.OrderId, request.ProductId, userId, cancellationToken);
        if (hasReviewed)
        {
            throw new BusinessException("ALREADY_REVIEWED", "You have already reviewed this product for this order.");
        }

        string? imagesJson = null;
        if (request.Images != null && request.Images.Any())
        {
            imagesJson = JsonSerializer.Serialize(request.Images);
        }

        var review = new Review
        {
            OrderId = request.OrderId,
            ProductId = request.ProductId,
            UserId = userId,
            Rating = request.Rating,
            Content = request.Content,
            Images = imagesJson,
            IsAnonymous = request.IsAnonymous ? 1 : 0,
            Status = 0 // 0 = Pending review (审核中)
        };

        return await _reviewRepository.InsertAsync(review, cancellationToken);
    }

    public async Task<PagedResult<ReviewDto>> SearchByProductAsync(long productId, PageQuery query, CancellationToken cancellationToken = default)
    {
        var result = await _reviewRepository.GetByProductAsync(productId, query.PageIndex, query.PageSize, cancellationToken);
        var dtos = result.Items.Select(MapToDto).ToList();
        return new PagedResult<ReviewDto>(dtos, result.PageIndex, result.PageSize, result.TotalCount);
    }

    public async Task<PagedResult<ReviewDto>> SearchAdminAsync(ReviewQuery query, CancellationToken cancellationToken = default)
    {
        var result = await _reviewRepository.GetForAdminAsync(query.ProductId, query.Status, query.PageIndex, query.PageSize, cancellationToken);
        var dtos = result.Items.Select(MapToDto).ToList();
        return new PagedResult<ReviewDto>(dtos, result.PageIndex, result.PageSize, result.TotalCount);
    }

    public async Task SetStatusAsync(long reviewId, int status, long operatorId, CancellationToken cancellationToken = default)
    {
        bool success = await _reviewRepository.UpdateStatusAsync(reviewId, status, cancellationToken);
        if (!success)
        {
            throw new BusinessException("NOT_FOUND", $"Review {reviewId} not found.");
        }
    }

    private static ReviewDto MapToDto(Review review)
    {
        var imagesList = new List<string>();
        if (!string.IsNullOrWhiteSpace(review.Images))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(review.Images);
                if (parsed != null)
                {
                    imagesList = parsed;
                }
            }
            catch
            {
               
            }
        }

        return new ReviewDto(
            review.Id,
            review.OrderId,
            review.ProductId,
            review.UserId,
            review.Rating,
            review.Content,
            imagesList,
            review.IsAnonymous == 1,
            review.Status,
            review.CreatedAt
        );
    }
}
