using CSharpAiAssistant.Services;
using Microsoft.AspNetCore.Mvc;

namespace CSharpAiAssistant.Controllers;

[ApiController]
[Route("api/knowledge")]
public sealed class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeBaseService _knowledgeBase;
    public KnowledgeController(IKnowledgeBaseService knowledgeBase) => _knowledgeBase = knowledgeBase;

    [HttpGet]
    public ActionResult<IReadOnlyList<KnowledgeDocument>> GetDocuments() => Ok(_knowledgeBase.GetDocuments());

    [HttpPost("upload")]
    [RequestSizeLimit(1_100_000)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = "Choose a non-empty .txt or .md file." });
        try
        {
            await using var stream = file.OpenReadStream();
            var document = await _knowledgeBase.AddDocumentAsync(file.FileName, stream, file.Length, cancellationToken);
            return Ok(document);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{fileName}")]
    public IActionResult Delete(string fileName) => _knowledgeBase.DeleteDocument(fileName) ? NoContent() : NotFound(new { error = "Document not found." });
}
