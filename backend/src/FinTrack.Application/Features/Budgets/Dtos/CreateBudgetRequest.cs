namespace FinTrack.Application.Features.Budgets.Dtos;

public class CreateBudgetRequest
{
    public Guid CategoryId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal MonthlyLimit { get; set; }
}
