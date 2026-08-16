using FinTrack.Application.Common;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Common.Validation;
using FinTrack.Application.Features.Categories.Dtos;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Application.Features.Categories;

public class CategoryService : ICategoryService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateCategoryRequest> _createValidator;
    private readonly IValidator<UpdateCategoryRequest> _updateValidator;

    public CategoryService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IValidator<CreateCategoryRequest> createValidator,
        IValidator<UpdateCategoryRequest> updateValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeArchived, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var query = _db.Categories.AsNoTracking().Where(c => c.UserId == userId);
        if (!includeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        return await query
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type, c.IsDefault, c.IsArchived))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var userId = _currentUser.RequireUserId();
        var name = request.Name.Trim();

        await EnsureNameIsUniqueAsync(userId, name, request.Type, excludeId: null, cancellationToken);

        var category = new Category
        {
            UserId = userId,
            Name = name,
            Type = request.Type,
            IsDefault = false,
            IsArchived = false
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);

        var userId = _currentUser.RequireUserId();

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), id);

        var name = request.Name.Trim();
        await EnsureNameIsUniqueAsync(userId, name, category.Type, excludeId: category.Id, cancellationToken);

        category.Name = name;
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsArchived, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), id);

        // Soft delete: archive instead of removing, so historical transactions stay intact.
        category.IsArchived = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNameIsUniqueAsync(
        Guid userId, string name, CategoryType type, Guid? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _db.Categories.AnyAsync(
            c => c.UserId == userId
                 && !c.IsArchived
                 && c.Type == type
                 && c.Name == name
                 && (excludeId == null || c.Id != excludeId),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A {type} category named '{name}' already exists.");
        }
    }

    private static CategoryDto ToDto(Category c) =>
        new(c.Id, c.Name, c.Type, c.IsDefault, c.IsArchived);
}
