using FinTrack.Domain.Enums;

namespace FinTrack.Application.Features.Transactions.Dtos;

public enum TransactionSortBy
{
    Date = 1,
    Amount = 2
}

public enum SortDirection
{
    Asc = 1,
    Desc = 2
}

/// <summary>Filter, sort and pagination parameters bound from the query string.</summary>
public class TransactionQuery
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public Guid? CategoryId { get; set; }
    public TransactionType? Type { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? Search { get; set; }

    public TransactionSortBy SortBy { get; set; } = TransactionSortBy.Date;
    public SortDirection SortDir { get; set; } = SortDirection.Desc;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
