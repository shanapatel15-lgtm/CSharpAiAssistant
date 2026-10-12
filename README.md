# CSharp-AI-Assistant
AI chatbot built with C#, ASP.NET Core, RAG and database integration.
C# AI Assistant (Razor Pages) - README

Overview

This repository contains a Razor Pages (.NET 8) web application that demonstrates an AI assistant integration (LLM). This README explains how to run the app locally, add a database (EF Core), securely store and use LLM API keys, and recommended improvements for LLM usage and production deployment.

Requirements

- .NET 8 SDK
- Visual Studio 2026 (or Visual Studio / VS Code with C# support)
- (Optional) SQL Server / LocalDB / SQLite for local database

Quick start (local)

1. Clone the repository
   - git clone <repo-url>
2. Open the solution
   - Open CSharpAiAssistant.sln in Visual Studio or use dotnet CLI
3. Restore and run
   - dotnet restore
   - dotnet build
   - dotnet run --project ./CSharp_AI_Assistant/CSharpAiAssistant.csproj (adjust path if different)
4. Open browser: https://localhost:5001 or the https endpoint shown by the run output

Database (EF Core) - Local development

You can choose SQLite for quick local dev or SQL Server for production parity.

1) Add EF Core packages (dotnet CLI):

- For SQL Server
  - dotnet add package Microsoft.EntityFrameworkCore.SqlServer
  - dotnet add package Microsoft.EntityFrameworkCore.Design
  - dotnet tool install --global dotnet-ef (if not installed)

- For SQLite
  - dotnet add package Microsoft.EntityFrameworkCore.Sqlite
  - dotnet add package Microsoft.EntityFrameworkCore.Design

2) Create a DbContext and models

- Create a folder Data/ and add ApplicationDbContext.cs:

  public class ApplicationDbContext : DbContext
  {
	  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
		  : base(options) { }

	  public DbSet<MyEntity> MyEntities { get; set; }
  }

- Create a model in Models/MyEntity.cs:

  public class MyEntity
  {
	  public int Id { get; set; }
	  public string Name { get; set; }
  }

3) Register DbContext in Program.cs (Razor Pages startup)

  var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
  builder.Services.AddDbContext<ApplicationDbContext>(options =>
	  options.UseSqlServer(connectionString)); // or UseSqlite(connectionString)

4) Configure connection string

- appsettings.Development.json (example for LocalDB):

  {
	"ConnectionStrings": {
	  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CSharpAiAssistantDb;Trusted_Connection=True;MultipleActiveResultSets=true"
	}
  }

- or for SQLite:
  "DefaultConnection": "Data Source=local.db"

5) Add migration and update DB

- Using dotnet CLI (from project folder containing csproj):
  - dotnet ef migrations add InitialCreate
  - dotnet ef database update

- Using Visual Studio Package Manager Console (Default project = your web project):
  - Add-Migration InitialCreate
  - Update-Database

Notes
- If you get errors about dotnet-ef, install the global tool: dotnet tool install --global dotnet-ef
- For CI, run dotnet ef database update during deployment or use migrations scripts.

Storing LLM API keys securely

Do NOT store API keys directly in source control. Use one of the following secure options.

1) Development: dotnet user-secrets

- Initialize user secrets (if not already):
  - dotnet user-secrets init
- Set the key:
  - dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
- Read from configuration in Program.cs:
  var openAiKey = builder.Configuration["OpenAI:ApiKey"];

2) Environment variables (recommended for containers and CI)

- Windows (PowerShell):
  - $env:OpenAI__ApiKey = "sk-..."
- Linux / macOS:
  - export OpenAI__ApiKey="sk-..."
- In code, use builder.Configuration["OpenAI:ApiKey"] or IConfiguration.GetSection("OpenAI").Get<OpenAiOptions>()

3) Azure Key Vault (production)

- Store secrets in Key Vault and use Managed Identity or service principal to access.
- In Azure App Service or Azure Functions, assign a managed identity and grant Key Vault access policies.
- In Program.cs use Azure.Extensions.AspNetCore.Configuration.Secrets to add Key Vault configuration provider.

Example configuration section (appsettings.json):

  "OpenAI": {
	"ApiKey": "",
	"BaseUrl": "https://api.openai.com/v1",
	"Model": "gpt-4o-mini",
	"Temperature": 0.3
  }

Reading configuration in code (example service registration):

  builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
  builder.Services.AddSingleton<IOpenAiClient>(sp => {
	  var opts = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
	  return new OpenAiClient(opts.ApiKey, opts.BaseUrl); // adapt to your client
  });

Improving LLM usage, reliability and cost

- Prompt engineering
  - Use clear system prompts and concise context. Keep examples minimal and structured.
- Temperature and tokens
  - Use low temperature (0.0-0.4) for deterministic outputs. Control max tokens to limit cost.
- Caching
  - Cache responses for identical prompts using MemoryCache or Redis. Use a hash of prompt + model settings as the cache key.
  - Example: builder.Services.AddMemoryCache(); use IMemoryCache in your LLM service.
- Rate limiting and retries
  - Implement exponential backoff and idempotent retries (Polly is recommended). Configure limits per API key.
  - dotnet add package Polly
- Streaming and chunking
  - For large outputs, prefer streaming to reduce latency and memory usage.
- Cost control
  - Monitor usage, set quotas, and prefer smaller models for trivial tasks.
- Retrieval augmented generation (RAG)
  - Use embeddings + vector DB (e.g., Azure Cognitive Search, Weaviate, Pinecone) to fetch relevant docs and pass them as context.
- Local model fallback
  - For offline or privacy-sensitive tasks, consider running LLMs locally (Llama-based runtimes) and fall back when API unavailable.

Example caching + retry strategy (conceptual)

- Use IMemoryCache to store LLM responses for N minutes.
- Wrap API calls with Polly retry and timeout policies.

Security and production deployment

- Never commit secrets. Add patterns to .gitignore (appsettings.Development.json may be ok, but do not commit secrets).
- Use HTTPS, HSTS in production.
- Use Content Security Policy and secure cookies.
- Use Key Vault and Managed Identity for secrets in Azure.
- For containerized deployments, pass secrets as environment variables or use secret stores (Azure Key Vault, AWS Secrets Manager).

Monitoring and observability

- Log request metadata (not secrets) and model name, token usage, latency to Application Insights.
- Track errors, successes, retries, and tail latencies.
- Expose metrics for cost (tokens per request) and latency.

CI/CD and GitHub Actions snippets

- Use secrets in GitHub (Settings > Secrets) and reference them in workflows as env: OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
- During deployment set environment variables or configure secrets in the target environment.

Where to add code in this Razor Pages project

- Pages/ - add or edit Razor Pages that present UI.
- Data/ - add ApplicationDbContext and migrations.
- Services/ - add an LLM service (e.g., Services/OpenAiService.cs) that encapsulates API calls, throttling, caching, and retries.
- Models/ - data transfer objects for prompts, responses, and DB entities.
- appsettings.Development.json - local connection strings and non-secret defaults.

Minimal example: a simple LLM service interface

  public interface IOpenAiService
  {
	  Task<string> GetCompletionAsync(string prompt, CancellationToken ct = default);
  }

  public class OpenAiService : IOpenAiService
  {
	  private readonly HttpClient _http;
	  private readonly OpenAiOptions _opts;
	  private readonly IMemoryCache _cache;

	  public OpenAiService(HttpClient http, IOptions<OpenAiOptions> opts, IMemoryCache cache) { /* ... */ }

	  public async Task<string> GetCompletionAsync(string prompt, CancellationToken ct = default)
	  {
		  // check cache, call API with retries (Polly), return text
	  }
  }

Useful packages

- Microsoft.EntityFrameworkCore.*, Microsoft.EntityFrameworkCore.Tools
- Polly
- Microsoft.Extensions.Caching.Memory (or StackExchange.Redis)
- Azure.Extensions.AspNetCore.Configuration.Secrets (for Key Vault)

Additional resources

- EF Core docs: https://learn.microsoft.com/ef/core/
- ASP.NET Core configuration: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/
- Secret management (user-secrets): https://learn.microsoft.com/aspnet/core/security/app-secrets
- Polly resilience: https://github.com/App-vNext/Polly
- OpenAI API docs (or your provider's docs)
