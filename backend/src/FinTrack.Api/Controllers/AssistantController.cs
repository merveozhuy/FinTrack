using FinTrack.Api;
using FinTrack.Application.Features.Assistant;
using FinTrack.Application.Features.Assistant.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/assistant")]
[Authorize]
[EnableRateLimiting(RateLimitingPolicies.Assistant)]
public class AssistantController : ControllerBase
{
    private readonly IAssistantService _assistantService;

    public AssistantController(IAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    /// <summary>Answers a question about the user's own finances, grounded in backend-computed data.</summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatResponse>> Chat(ChatRequest request, CancellationToken cancellationToken)
    {
        var response = await _assistantService.ChatAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>Lists the current user's conversations.</summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryDto>>> GetConversations(CancellationToken cancellationToken)
    {
        var conversations = await _assistantService.GetConversationsAsync(cancellationToken);
        return Ok(conversations);
    }

    /// <summary>Returns a conversation with its messages.</summary>
    [HttpGet("conversations/{id:guid}")]
    [ProducesResponseType(typeof(ConversationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationDetailDto>> GetConversation(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _assistantService.GetConversationAsync(id, cancellationToken);
        return Ok(conversation);
    }

    /// <summary>Deletes a conversation owned by the current user.</summary>
    [HttpDelete("conversations/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken cancellationToken)
    {
        await _assistantService.DeleteConversationAsync(id, cancellationToken);
        return NoContent();
    }
}
