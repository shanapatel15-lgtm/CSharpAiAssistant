using System.Text;
using System.Text.RegularExpressions;
using CSharpAiAssistant.Data;
using Microsoft.EntityFrameworkCore;

namespace CSharpAiAssistant.Services;

/// <summary>SQLite-backed text knowledge base. Search currently uses keyword overlap, not vector embeddings.</summary>
public sealed class KnowledgeBaseService(AppDbContext db, ILogger<KnowledgeBaseService> logger) : IKnowledgeBaseService
{
    private const long MaxDocumentBytes = 1_000_000;
    private static readonly string[] AllowedExtensions = [".txt", ".md"];

    public async Task<KnowledgeDocument> AddDocumentAsync(string fileName, Stream content, long sizeBytes, CancellationToken cancellationToken)
    {
        if (sizeBytes <= 0 || sizeBytes > MaxDocumentBytes)
            throw new ArgumentException("Documents must be between 1 byte and 1 MB.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Only .txt and .md files are supported in this version.");

        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) throw new ArgumentException("A valid file name is required.");

        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        var actualSize = Encoding.UTF8.GetByteCount(text);
        if (actualSize == 0 || actualSize > MaxDocumentBytes)
            throw new ArgumentException("The document must contain text and be no larger than 1 MB.");

        var existing = await db.KnowledgeDocuments.FirstOrDefaultAsync(d => d.FileName == safeName, cancellationToken);
        if (existing is null)
        {
            db.KnowledgeDocuments.Add(new KnowledgeDocumentRecord
            {
                FileName = safeName, Content = text, SizeBytes = actualSize, UploadedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.Content = text;
            existing.SizeBytes = actualSize;
            existing.UploadedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Knowledge document {FileName} was stored in SQLite ({SizeBytes} bytes).", safeName, actualSize);
        return new KnowledgeDocument(safeName, actualSize, existing?.UploadedAtUtc ?? DateTime.UtcNow);
    }

    public IReadOnlyList<KnowledgeDocument> GetDocuments() => db.KnowledgeDocuments.AsNoTracking()
        .OrderByDescending(d => d.UploadedAtUtc)
        .Select(d => new KnowledgeDocument(d.FileName, d.SizeBytes, d.UploadedAtUtc))
        .ToList();

    public bool DeleteDocument(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        var document = db.KnowledgeDocuments.FirstOrDefault(d => d.FileName == safeName);
        if (document is null) return false;
        db.KnowledgeDocuments.Remove(document);
        db.SaveChanges();
        logger.LogInformation("Knowledge document {FileName} was deleted from SQLite.", safeName);
        return true;
    }

    public IReadOnlyList<KnowledgeChunk> Search(string query, int maxResults = 3)
    {
        var terms = Tokenize(query).Where(t => t.Length > 2).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (terms.Count == 0) return [];
        var matches = new List<(KnowledgeChunk Chunk, int Score)>();
        foreach (var document in db.KnowledgeDocuments.AsNoTracking().OrderBy(d => d.FileName).ToList())
        {
            var chunks = SplitIntoChunks(document.Content, 1100);
            for (var i = 0; i < chunks.Count; i++)
            {
                var chunkTerms = Tokenize(chunks[i]);
                var score = terms.Sum(term => chunkTerms.Contains(term, StringComparer.OrdinalIgnoreCase) ? 1 : 0);
                if (score > 0) matches.Add((new KnowledgeChunk(document.FileName, chunks[i], i + 1), score));
            }
        }
        return matches.OrderByDescending(m => m.Score).ThenBy(m => m.Chunk.FileName)
            .Take(Math.Clamp(maxResults, 1, 5)).Select(m => m.Chunk).ToList();
    }

    private static List<string> SplitIntoChunks(string text, int maxChars)
    {
        var result = new List<string>();
        var paragraphs = Regex.Split(text, @"\r?\n\s*\r?\n");
        var current = new StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            var cleaned = paragraph.Trim();
            if (cleaned.Length == 0) continue;
            if (current.Length > 0 && current.Length + cleaned.Length + 2 > maxChars) { result.Add(current.ToString()); current.Clear(); }
            while (cleaned.Length > maxChars) { if (current.Length > 0) { result.Add(current.ToString()); current.Clear(); } result.Add(cleaned[..maxChars]); cleaned = cleaned[maxChars..]; }
            if (current.Length > 0) current.AppendLine().AppendLine();
            current.Append(cleaned);
        }
        if (current.Length > 0) result.Add(current.ToString());
        return result;
    }

    private static string[] Tokenize(string text) => Regex.Matches(text.ToLowerInvariant(), @"[a-z0-9][a-z0-9+#.-]*")
        .Cast<Match>().Select(m => m.Value.Trim('.', '-', '+', '#')).Where(t => t.Length > 0).ToArray();
}
