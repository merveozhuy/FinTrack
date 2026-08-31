using System.Globalization;
using FinTrack.Application.Features.Auth;
using FinTrack.Application.Features.Budgets;
using FinTrack.Application.Features.Categories;
using FinTrack.Application.Features.Dashboard;
using FinTrack.Application.Features.RecurringTransactions;
using FinTrack.Application.Features.RecurringTransactions.Processing;
using FinTrack.Application.Features.Transactions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Force English validation messages regardless of the host OS culture, so API
        // responses are consistent (the default would follow the machine locale, e.g. tr-TR).
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("en-US");

        // Registers all AbstractValidator<T> implementations in this assembly.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
        services.AddScoped<IRecurringTransactionProcessor, RecurringTransactionProcessor>();

        return services;
    }
}
