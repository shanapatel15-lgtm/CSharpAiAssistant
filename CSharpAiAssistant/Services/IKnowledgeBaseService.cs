namespace CSharpAiAssistant.Services;

public sealed record KnowledgeDocument(string FileName, long SizeBytes, DateTime UploadedAt);
public sealed record KnowledgeChunk(string FileName, string Text, int ChunkNumber);

public interface IKnowledgeBaseService
{
    Task<KnowledgeDocument> AddDocumentAsync(string fileName, Stream content, long sizeBytes, CancellationToken cancellationToken);
    IReadOnlyList<KnowledgeDocument> GetDocuments();
    bool DeleteDocument(string fileName);
    IReadOnlyList<KnowledgeChunk> Search(string query, int maxResults = 3);
}
