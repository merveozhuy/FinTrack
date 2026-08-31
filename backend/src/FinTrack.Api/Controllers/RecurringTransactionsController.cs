using FinTrack.Application.Features.RecurringTransactions;
using FinTrack.Application.Features.RecurringTransactions.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/recurring-transactions")]
[Authorize]
public class RecurringTransactionsController : ControllerBase
{
    private readonly IRecurringTransactionService _service;

    public RecurringTransactionsController(IRecurringTransactionService service)
    {
        _service = service;
    }

    /// <summary>Lists the current user's recurring rules.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RecurringTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecurringTransactionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _service.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    /// <summary>Creates a recurring rule; the first occurrence is scheduled for its start date.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RecurringTransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecurringTransactionDto>> Create(CreateRecurringTransactionRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return Created($"/api/recurring-transactions/{created.Id}", created);
    }

    /// <summary>Updates a recurring rule owned by the current user.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RecurringTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringTransactionDto>> Update(Guid id, UpdateRecurringTransactionRequest request, CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Pauses or resumes a recurring rule.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(RecurringTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringTransactionDto>> UpdateStatus(Guid id, UpdateRecurringStatusRequest request, CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Deletes a recurring rule owned by the current user.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
