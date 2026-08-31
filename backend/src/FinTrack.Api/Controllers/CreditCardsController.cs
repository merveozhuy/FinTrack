using FinTrack.Application.Features.CreditCards;
using FinTrack.Application.Features.CreditCards.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/credit-cards")]
[Authorize]
public class CreditCardsController : ControllerBase
{
    private readonly ICreditCardService _creditCardService;

    public CreditCardsController(ICreditCardService creditCardService)
    {
        _creditCardService = creditCardService;
    }

    /// <summary>Lists the current user's credit cards with computed debt and available credit.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CreditCardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CreditCardDto>>> GetAll(CancellationToken cancellationToken)
    {
        var cards = await _creditCardService.GetAllAsync(cancellationToken);
        return Ok(cards);
    }

    /// <summary>Adds a credit card.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreditCardDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreditCardDto>> Create(CreateCreditCardRequest request, CancellationToken cancellationToken)
    {
        var card = await _creditCardService.CreateAsync(request, cancellationToken);
        return Created($"/api/credit-cards/{card.Id}", card);
    }

    /// <summary>Updates a credit card owned by the current user.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CreditCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardDto>> Update(Guid id, UpdateCreditCardRequest request, CancellationToken cancellationToken)
    {
        var card = await _creditCardService.UpdateAsync(id, request, cancellationToken);
        return Ok(card);
    }

    /// <summary>Records a payment towards a card, reducing its debt.</summary>
    [HttpPost("{id:guid}/payments")]
    [ProducesResponseType(typeof(CreditCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardDto>> AddPayment(Guid id, CreateCardPaymentRequest request, CancellationToken cancellationToken)
    {
        var card = await _creditCardService.AddPaymentAsync(id, request, cancellationToken);
        return Ok(card);
    }

    /// <summary>Deletes a card; its transactions are unlinked and its payments removed.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _creditCardService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
