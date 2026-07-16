using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
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

        var allCategories = await _categoryRepository.GetAllAsync(includeDisabled: true, cancellationToken);
        var categoryMap = allCategories.ToDictionary(category => category.Id);
        var descendants = GetDescendants(categoryId, allCategories);
        var targetLevel = 1;

        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == categoryId)
            {
                throw new BusinessException("CATEGORY_PARENT_SELF", "不能将分类设置为自己的父分类");
            }

            if (descendants.Any(category => category.Id == request.ParentId.Value))
            {
                throw new BusinessException("CATEGORY_PARENT_CYCLE", "不能将分类移动到自己的子分类下");
            }

            if (!categoryMap.TryGetValue(request.ParentId.Value, out var parent))
            {
                throw new BusinessException("CATEGORY_NOT_FOUND", "父分类不存在");
            }

            if (parent == null)
            {
                throw new BusinessException("CATEGORY_NOT_FOUND", "父分类不存在");
            }
            if (parent.TreeLevel >= 5)
            {
                throw new BusinessException("CATEGORY_LEVEL_EXCEEDED", "分类层级不能超过5级");
            }
            targetLevel = parent.TreeLevel + 1;
        }
        else
        {
            targetLevel = 1;
        }

        if (request.TreeLevel != targetLevel)
        {
            throw new BusinessException("CATEGORY_LEVEL_INVALID", "分类层级必须与父分类层级一致");
        }

        var levelOffset = targetLevel - existing.TreeLevel;
        if (descendants.Any(category => category.TreeLevel + levelOffset > 5))
        {
            throw new BusinessException("CATEGORY_LEVEL_EXCEEDED", "移动后分类层级不能超过5级");
        }

        existing.ParentId = request.ParentId;
        existing.Name = request.Name;
        existing.TreeLevel = targetLevel;
        existing.SortOrder = request.SortOrder;
        existing.Status = request.Status;
        existing.IconUrl = request.IconUrl;
        var ownsTransaction = _unitOfWork.CurrentTransaction is null;
        if (ownsTransaction)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            await _categoryRepository.UpdateAsync(existing, cancellationToken);

            foreach (var descendant in descendants)
            {
                descendant.TreeLevel += levelOffset;
                await _categoryRepository.UpdateAsync(descendant, cancellationToken);
            }

            if (ownsTransaction)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (ownsTransaction)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }

            throw;
        }
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

    private static IReadOnlyList<Category> GetDescendants(int categoryId, IReadOnlyList<Category> categories)
    {
        var childrenByParent = categories
            .Where(category => category.ParentId.HasValue)
            .GroupBy(category => category.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());
        var descendants = new List<Category>();
        var pending = new Queue<int>();
        pending.Enqueue(categoryId);

        while (pending.Count > 0)
        {
            var parentId = pending.Dequeue();
            if (!childrenByParent.TryGetValue(parentId, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                descendants.Add(child);
                pending.Enqueue(child.Id);
            }
        }

        return descendants;
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
