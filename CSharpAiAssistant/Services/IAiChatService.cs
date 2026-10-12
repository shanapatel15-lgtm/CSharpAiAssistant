using CSharpAiAssistant.Models;

namespace CSharpAiAssistant.Services;

public interface IAiChatService
{
    Task<ChatResponse> GetReplyAsync(ChatRequest request, CancellationToken cancellationToken);
}
