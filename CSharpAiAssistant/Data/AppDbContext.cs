using Microsoft.EntityFrameworkCore;

namespace CSharpAiAssistant.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<KnowledgeDocumentRecord> KnowledgeDocuments => Set<KnowledgeDocumentRecord>();
    public DbSet<ChatConversationRecord> ChatConversations => Set<ChatConversationRecord>();
    public DbSet<ChatMessageRecord> ChatMessages => Set<ChatMessageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnowledgeDocumentRecord>()
            .HasIndex(d => d.FileName).IsUnique();
        modelBuilder.Entity<ChatConversationRecord>()
            .HasMany(c => c.Messages).WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChatMessageRecord>()
            .HasIndex(m => new { m.ConversationId, m.CreatedAtUtc });
    }
}

public sealed class KnowledgeDocumentRecord
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}

public sealed class ChatConversationRecord
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "New conversation";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<ChatMessageRecord> Messages { get; set; } = [];
}

public sealed class ChatMessageRecord
{
    public int Id { get; set; }
    public Guid ConversationId { get; set; }
    public ChatConversationRecord? Conversation { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
