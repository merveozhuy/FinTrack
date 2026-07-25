using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Common;

/// <summary>
/// The starter category set created for every new user so the app is usable immediately.
/// </summary>
public static class DefaultCategories
{
    private static readonly (string Name, CategoryType Type)[] Definitions =
    {
        ("Salary", CategoryType.Income),
        ("Freelance", CategoryType.Income),
        ("Food", CategoryType.Expense),
        ("Transportation", CategoryType.Expense),
        ("Rent", CategoryType.Expense),
        ("Bills", CategoryType.Expense),
        ("Shopping", CategoryType.Expense),
        ("Health", CategoryType.Expense),
        ("Entertainment", CategoryType.Expense)
    };

    public static IEnumerable<Category> CreateFor(Guid userId) =>
        Definitions.Select(definition => new Category
        {
            UserId = userId,
            Name = definition.Name,
            Type = definition.Type,
            IsDefault = true,
            IsArchived = false
        });
}
