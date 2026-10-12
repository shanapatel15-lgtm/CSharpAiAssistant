using CSharpAiAssistant.Data;
using CSharpAiAssistant.Models;
using CSharpAiAssistant.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSharpAiAssistant.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ChatController(IAiChatService aiChatService, AppDbContext db, ILogger<ChatController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var now = DateTime.UtcNow;
            var conversation = request.ConversationId is Guid id
                ? await db.ChatConversations.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                : null;
            if (conversation is null)
            {
                conversation = new ChatConversationRecord
                {
                    Id = Guid.NewGuid(),
                    Title = MakeTitle(request.Message),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                db.ChatConversations.Add(conversation);
            }

            var response = await aiChatService.GetReplyAsync(request, cancellationToken);
            conversation.UpdatedAtUtc = now;
            conversation.Messages.Add(new ChatMessageRecord { Role = "user", Content = request.Message.Trim(), CreatedAtUtc = now });
            conversation.Messages.Add(new ChatMessageRecord { Role = "assistant", Content = response.Reply, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync(cancellationToken);
            response.ConversationId = conversation.Id;
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(title: "AI configuration problem", detail: ex.Message, statusCode: 503);
        }
        catch (HttpRequestException ex)
        {
            return Problem(title: "AI provider request failed", detail: ex.Message, statusCode: 502);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(title: "AI provider timed out", detail: "The request took too long. Please try again.", statusCode: 504);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while generating an AI reply.");
            return Problem(title: "Unexpected error", detail: "Something went wrong. Please try again.", statusCode: 500);
        }
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var conversations = await db.ChatConversations.AsNoTracking()
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Select(c => new { c.Id, c.Title, c.CreatedAtUtc, c.UpdatedAtUtc, MessageCount = c.Messages.Count })
            .Take(50).ToListAsync(cancellationToken);
        return Ok(conversations);
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await db.ChatConversations.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { c.Id, c.Title, c.CreatedAtUtc, c.UpdatedAtUtc,
                Messages = c.Messages.OrderBy(m => m.CreatedAtUtc).Select(m => new { m.Role, m.Content, m.CreatedAtUtc }) })
            .FirstOrDefaultAsync(cancellationToken);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    private static string MakeTitle(string message)
    {
        var clean = message.Trim();
        return clean.Length <= 60 ? clean : clean[..57] + "...";
    }
}
