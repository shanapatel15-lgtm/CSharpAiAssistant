using System.ComponentModel.DataAnnotations;

namespace CSharpAiAssistant.Models;

public sealed class ChatRequest
{
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;

    public List<ChatMessage> History { get; set; } = [];

    public Guid? ConversationId { get; set; }
}

public sealed class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public sealed class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public bool DemoMode { get; set; }
    public bool ToolUsed { get; set; }
    public bool KnowledgeBaseUsed { get; set; }
    public Guid ConversationId { get; set; }
}
