using FinTrack.Application.Features.Budgets;
using FinTrack.Application.Features.Budgets.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/budgets")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;

    public BudgetsController(IBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    /// <summary>Lists budgets for a given month with computed spending, remaining and status.</summary>
    [HttpGet("{year:int}/{month:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<BudgetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BudgetDto>>> GetForMonth(int year, int month, CancellationToken cancellationToken)
    {
        var budgets = await _budgetService.GetForMonthAsync(year, month, cancellationToken);
        return Ok(budgets);
    }

    /// <summary>Creates a monthly budget for an expense category.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BudgetDto>> Create(CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        var budget = await _budgetService.CreateAsync(request, cancellationToken);
        return Created($"/api/budgets/{budget.Year}/{budget.Month}", budget);
    }

    /// <summary>Updates the monthly limit of a budget owned by the current user.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetDto>> Update(Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        var budget = await _budgetService.UpdateAsync(id, request, cancellationToken);
        return Ok(budget);
    }

    /// <summary>Deletes a budget owned by the current user.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _budgetService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
