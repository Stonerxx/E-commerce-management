using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryTreeDto>> GetTreeAsync(bool includeDisabled, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(includeDisabled, cancellationToken);
        return BuildTree(categories);
    }

    public async Task<int> CreateAsync(CategoryRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var category = new Category
        {
            ParentId = request.ParentId,
            Name = request.Name,
            TreeLevel = request.TreeLevel,
            SortOrder = request.SortOrder,
            Status = request.Status,
            IconUrl = request.IconUrl,
            CreatedAt = DateTime.Now
        };

        if (request.ParentId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent == null)
            {
                throw new BusinessException("CATEGORY_NOT_FOUND", "父分类不存在");
            }
            if (parent.TreeLevel >= 5)
            {
                throw new BusinessException("CATEGORY_LEVEL_EXCEEDED", "分类层级不能超过5级");
            }
            if (request.TreeLevel != parent.TreeLevel + 1)
            {
                throw new BusinessException("CATEGORY_LEVEL_INVALID", "分类层级必须为父分类层级+1");
            }
        }
        else
        {
            if (request.TreeLevel != 1)
            {
                throw new BusinessException("CATEGORY_LEVEL_INVALID", "根分类层级必须为1");
            }
        }

        return await _categoryRepository.CreateAsync(category, cancellationToken);
    }

    public async Task UpdateAsync(int categoryId, CategoryRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var existing = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (existing == null)
        {
            throw new BusinessException("CATEGORY_NOT_FOUND", "分类不存在");
        }

        if (request.ParentId.HasValue && request.ParentId.Value == categoryId)
        {
            throw new BusinessException("CATEGORY_PARENT_SELF", "不能将分类设置为自己的父分类");
        }

        if (request.ParentId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent == null)
            {
                throw new BusinessException("CATEGORY_NOT_FOUND", "父分类不存在");
            }
            if (parent.TreeLevel >= 5)
            {
                throw new BusinessException("CATEGORY_LEVEL_EXCEEDED", "分类层级不能超过5级");
            }
            if (request.TreeLevel != parent.TreeLevel + 1)
            {
                throw new BusinessException("CATEGORY_LEVEL_INVALID", "分类层级必须为父分类层级+1");
            }
        }
        else
        {
            if (request.TreeLevel != 1)
            {
                throw new BusinessException("CATEGORY_LEVEL_INVALID", "根分类层级必须为1");
            }
        }

        existing.ParentId = request.ParentId;
        existing.Name = request.Name;
        existing.TreeLevel = request.TreeLevel;
        existing.SortOrder = request.SortOrder;
        existing.Status = request.Status;
        existing.IconUrl = request.IconUrl;
        existing.CreatedAt = DateTime.Now;

        await _categoryRepository.UpdateAsync(existing, cancellationToken);
    }

    public async Task DeleteOrDisableAsync(int categoryId, long operatorId, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category == null)
        {
            throw new BusinessException("CATEGORY_NOT_FOUND", "分类不存在");
        }

        var hasChildren = await _categoryRepository.HasChildrenAsync(categoryId, cancellationToken);
        if (hasChildren)
        {
            throw new BusinessException("CATEGORY_HAS_CHILDREN", "该分类下存在子分类，无法删除");
        }

        var hasProducts = await _categoryRepository.HasProductsAsync(categoryId, cancellationToken);
        if (hasProducts)
        {
            throw new BusinessException("CATEGORY_HAS_PRODUCTS", "该分类下存在商品，无法删除");
        }

        await _categoryRepository.DeleteAsync(categoryId, cancellationToken);
    }

    private static void ValidateRequest(CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BusinessException("CATEGORY_NAME_EMPTY", "分类名称不能为空");
        }
        if (request.Name.Length > 100)
        {
            throw new BusinessException("CATEGORY_NAME_TOO_LONG", "分类名称不能超过100个字符");
        }
        if (request.TreeLevel < 1 || request.TreeLevel > 5)
        {
            throw new BusinessException("CATEGORY_LEVEL_INVALID", "分类层级必须在1-5之间");
        }
        if (request.Status != 0 && request.Status != 1)
        {
            throw new BusinessException("CATEGORY_STATUS_INVALID", "分类状态只能是0（禁用）或1（启用）");
        }
    }

    private static IReadOnlyList<CategoryTreeDto> BuildTree(IReadOnlyList<Category> categories)
    {
        var rootCategories = new List<CategoryTreeDto>();

        var nodes = categories.Select(c => MapToTreeDto(c)).ToList();
        var nodeDict = nodes.ToDictionary(n => n.CategoryId);

        foreach (var node in nodes)
        {
            if (node.ParentId.HasValue && nodeDict.TryGetValue(node.ParentId.Value, out var parentNode))
            {
                ((List<CategoryTreeDto>)parentNode.Children).Add(node);
            }
            else
            {
                rootCategories.Add(node);
            }
        }

        return rootCategories;
    }

    private static CategoryTreeDto MapToTreeDto(Category category)
    {
        return new CategoryTreeDto(
            CategoryId: category.Id,
            ParentId: category.ParentId,
            Name: category.Name,
            TreeLevel: category.TreeLevel,
            SortOrder: category.SortOrder,
            Status: category.Status,
            IconUrl: category.IconUrl,
            Children: new List<CategoryTreeDto>()
        );
    }
}