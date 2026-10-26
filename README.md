# Orbit AI — C# / ASP.NET Core AI Assistant

A starter full-stack AI chat application built with **C#**, **ASP.NET Core MVC**, a JSON API, and a responsive browser interface. Open the solution in Visual Studio.

## Features

- Responsive chat interface with suggested prompts and new-conversation control
- ASP.NET Core API endpoint at `POST /api/chat`
- AI service abstraction (`IAiChatService`) for maintainable design
- OpenAI-compatible Chat Completions integration using `HttpClient`
- Demo mode works without an API key, so you can run the project immediately
- Configuration via `appsettings.json` and environment variables
- Input validation, cancellation tokens, logging, timeout handling, and API error responses
- Keeps recent messages in browser memory for conversational context and also saves each successful exchange to SQLite
- Free local SQLite database managed with Entity Framework Core (no database server or subscription required)
- SQLite-backed knowledge base: uploaded `.txt` / `.md` document contents and metadata survive application restarts
- Persistent conversation and message records, with endpoints to list recent conversations and retrieve a conversation
- Database relationships, unique document filenames, and indexes for conversation history queries
- Retrieval-augmented generation (RAG) baseline: retrieves relevant document chunks and adds them to the LLM context
- Source-aware system instructions so the model can cite filenames and avoid claiming unsupported document facts
- Built-in C# calculator tool using the `calculate:` command, which works without an LLM API key
- Document size/type validation, upload size limits and database-backed storage

## Database

The project uses **SQLite**, a free embedded relational database, through Entity Framework Core. On first launch the application creates `CSharpAiAssistant/App_Data/orbit-ai.db` and its tables automatically. You do not need to install SQL Server, create an account, or configure a connection string.

The database stores:

- `KnowledgeDocuments` — filename, text content, file size, upload timestamp. Re-uploading a filename replaces its saved content.
- `ChatConversations` — conversation ID, title, creation and update timestamps.
- `ChatMessages` — user and assistant messages, timestamps, and a foreign key to the conversation.

The database stays on the machine running the application. Back up `App_Data/orbit-ai.db` if you want to keep the saved documents and chats. Do not commit the database file or sensitive conversations to source control. The current starter creates the schema automatically on first run; for a production application, introduce EF Core migrations before evolving the schema.

### Conversation API

- `GET /api/chat/conversations` — list the 50 most recently updated conversations.
- `GET /api/chat/conversations/{id}` — retrieve a conversation and its messages.
- `POST /api/chat` — starts a conversation when `conversationId` is omitted, or appends to an existing conversation when its ID is supplied. The response includes `conversationId`.

## Requirements

- Visual Studio 2022 with the ASP.NET and web development workload
- .NET 8 SDK
- An internet connection to restore/build the project

## Run it in Visual Studio

1. Extract the ZIP file.
2. Open `CSharpAiAssistant.sln` in Visual Studio.
3. If prompted, allow Visual Studio to restore/build the project. If you see `project.assets.json not found`, open **View → Terminal** in the solution folder and run `dotnet restore .\CSharpAiAssistant\CSharpAiAssistant.csproj`.
4. Set **CSharpAiAssistant** as the startup project.
5. Press **F5** (or **Ctrl+F5**).
6. On first run, the app creates the SQLite database automatically under `App_Data`.

The site should open at `https://localhost:7194`. If Visual Studio asks you to trust the local development HTTPS certificate, follow its prompt.

## Enable real AI responses

The app starts in **demo mode** if no API key is set. To connect an OpenAI API key, set the `OPENAI_API_KEY` environment variable before launching Visual Studio.

### Option A — Visual Studio terminal (recommended)

Open **View → Terminal** in Visual Studio and run:

PowerShell:
```powershell
$env:OPENAI_API_KEY="your-api-key-here"
dotnet run --project .\CSharpAiAssistant\CSharpAiAssistant.csproj
```

This starts the app from that terminal with the environment variable available to the process. Open the local URL shown in the terminal.

### Option B — Visual Studio launch profile

You can add `OPENAI_API_KEY` under `environmentVariables` in `CSharpAiAssistant/Properties/launchSettings.json`. Do not commit a real key to source control. If you use this option, keep the file private or remove the key before sharing the project.

The configured default model is `gpt-4o-mini`. You can change `AI:Model` and `AI:BaseUrl` in `appsettings.json` to a model/provider that supports the OpenAI Chat Completions API.

**API costs:** Live AI requests may incur charges under your AI provider's account. Demo mode does not call the provider.

## Project structure

```text
CSharpAiAssistant/
├── Data/
│   └── AppDbContext.cs        # EF Core entities and relationships
├── Controllers/
│   ├── ChatController.cs       # API endpoint and HTTP error handling
│   └── HomeController.cs       # Serves the web UI
├── Models/
│   ├── ChatRequest.cs          # Request/response DTOs
│   └── ErrorViewModel.cs
├── Services/
│   ├── AiOptions.cs            # AI configuration
│   ├── IAiChatService.cs       # Service contract
│   └── AiChatService.cs        # Demo and live provider logic
├── Views/
│   ├── Home/Index.cshtml       # Main chat interface
│   └── Shared/_Layout.cshtml
├── wwwroot/
│   ├── css/site.css
│   └── js/chat.js
├── App_Data/orbit-ai.db       # Created automatically at runtime (not included in ZIP)
├── appsettings.json
└── Program.cs
```

## New feature walkthrough

### Upload documents and ask questions

1. Run the app and use **Add document** in the left sidebar.
2. Upload a `.txt` or `.md` file (maximum 1 MB). PDF and Word files are not supported by this version.
3. Ask a question that relates to the uploaded material. The server splits documents into small sections and scores them by keyword overlap with your question. With a live API key, the best matching sections are sent to the LLM as reference context; without a key, the app shows the matching passages in preview mode.
4. The system prompt asks the LLM to cite the source filename and admit when the supplied text does not answer the question. Retrieval context is treated as untrusted data.
5. Remove documents using the **×** beside a filename. Document content is stored in the SQLite database, not as separate uploaded files.

This is a simple RAG implementation. For a next iteration, replace keyword matching with embeddings and a vector database, add document parsing for PDFs/DOCX, add user authentication and per-user data isolation, and introduce EF Core migrations for schema changes.

### Try the calculator tool

Send a message such as `calculate: (125 + 75) * 1.2`. The ASP.NET Core service detects the explicit command and evaluates the arithmetic in C# before making any LLM call. This demonstrates a deterministic application tool: the result is computed by your backend, not guessed by the model.

## Useful next steps

- Add unit and integration tests for database operations and retrieval
- Introduce EF Core migrations for controlled schema changes
- Replace keyword retrieval with embeddings/vector search
- Add PDF and Word extraction
- Add authentication and per-user data isolation before multi-user deployment
- Add rate limiting, usage monitoring and request quotas
- Add streaming responses and Azure deployment configuration

## Security notes

- Never put API keys in client-side JavaScript; use environment variables or a secret store.
- The SQLite database is local to the server running the app. It contains uploaded document text and conversation messages; do not commit it to source control.
- The app has no authentication or rate limiting. It is intended for local development and learning, not direct public deployment.
- Before public or multi-user deployment, add authentication, authorization, per-user data isolation, content scanning and request quotas.
- Retrieval currently uses keyword overlap rather than embeddings/vector search; this is a learning baseline, not production semantic retrieval.
- Avoid entering sensitive, confidential, or personal information.

## Architecture and engineering overview

### Application type

This project is a .NET 8 ASP.NET Core MVC application with server-rendered Razor views, JSON API controllers, a vanilla JavaScript frontend, Entity Framework Core, and SQLite.

It is important to distinguish between **Razor views** and **Razor Pages**:

- The project uses Razor syntax in `.cshtml` files.
- The project uses the MVC structure of `Controllers/`, `Views/`, and `Models/`.
- It does not use Razor Pages because it has no `Pages/` directory or `PageModel` classes.
- It does not use Blazor because the interactive browser code is ordinary JavaScript in `wwwroot/js/chat.js`.

The application is a small monolith. The web UI, API endpoints, application services, database access, and AI integration are deployed together in one ASP.NET Core process.

### High-level architecture

The main request path is:

```text
Browser UI
	|
	| HTML, CSS, JavaScript and fetch()
	v
ASP.NET Core controllers
	|
	| Request validation and HTTP responses
	v
Application services
	|
	+--> AI chat service --> OpenAI-compatible provider
	|
	+--> Knowledge-base service --> Entity Framework Core
	|
	+--> Conversation persistence --> Entity Framework Core
									  |
									  v
									SQLite
```

The project is organized into presentation, application-service, and data/infrastructure responsibilities, although it is not a formal Clean Architecture or Domain-Driven Design implementation.

### Project structure and responsibilities

```text
CSharpAiAssistant/
├── Controllers/
│   ├── HomeController.cs       MVC page controller
│   ├── ChatController.cs       Chat and conversation JSON API
│   └── KnowledgeController.cs  Knowledge-base JSON API
├── Data/
│   └── AppDbContext.cs         EF Core context, entities and relationships
├── Models/
│   ├── ChatRequest.cs           API request and response DTOs
│   └── ErrorViewModel.cs        MVC error-page model
├── Services/
│   ├── IAiChatService.cs        AI service abstraction
│   ├── AiChatService.cs         Demo mode, tools, RAG and provider calls
│   ├── IKnowledgeBaseService.cs Knowledge-base abstraction and records
│   ├── KnowledgeBaseService.cs  Document storage and keyword retrieval
│   └── AiOptions.cs             Strongly typed AI configuration
├── Views/
│   ├── Home/Index.cshtml        Main server-rendered UI
│   ├── Home/Error.cshtml        Error page
│   └── Shared/_Layout.cshtml    Shared HTML layout and static assets
├── wwwroot/
│   ├── css/site.css             Browser styling
│   └── js/chat.js               Browser chat and upload behavior
├── Program.cs                   Application startup and dependency injection
├── appsettings.json             Non-secret configuration
└── App_Data/orbit-ai.db         Runtime-created SQLite database
```

### Startup and dependency injection

`Program.cs` uses the modern .NET hosting model. It registers MVC with `AddControllersWithViews()`, configures SQLite, binds the `AI` configuration section to `AiOptions`, and registers application services.

The main registrations are:

```csharp
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(...));
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("AI"));
builder.Services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
builder.Services.AddHttpClient<IAiChatService, AiChatService>();
```

This is dependency injection and dependency inversion in practice. Controllers depend on abstractions such as `IAiChatService` and `IKnowledgeBaseService` rather than constructing their concrete implementations. That makes the services replaceable and easier to test.

The database context is scoped to an HTTP request. The knowledge-base service is also scoped. The AI service is registered as a typed `HttpClient`, which gives the application a managed HTTP client and a configured 60-second timeout.

### MVC request flow

The root page uses the conventional MVC route:

```text
/{controller=Home}/{action=Index}/{id?}
```

The request flow is:

1. A browser requests the application root.
2. The default route invokes `HomeController.Index()`.
3. `HomeController` returns `Views/Home/Index.cshtml`.
4. `_Layout.cshtml` adds the CSS and JavaScript files from `wwwroot`.
5. After the page loads, `chat.js` communicates with the JSON APIs using `fetch()`.

The initial page is rendered by the server, but chat messages and document operations are asynchronous client-to-server API calls. This makes the application a server-rendered MVC application with client-side JavaScript behavior, not a single-page framework application.

### API controllers

`ChatController` is an API controller at `/api/chat`. Its `POST` action:

1. Validates the incoming `ChatRequest`.
2. Finds an existing conversation or creates a new one.
3. Calls `IAiChatService` to generate a response.
4. Saves the user and assistant messages to SQLite.
5. Returns a `ChatResponse` containing the reply and conversation ID.

It also exposes:

```text
GET /api/chat/conversations
GET /api/chat/conversations/{id}
```

`KnowledgeController` is an API controller at `/api/knowledge`. It supports listing, uploading, and deleting `.txt` and `.md` documents:

```text
GET    /api/knowledge
POST   /api/knowledge/upload
DELETE /api/knowledge/{fileName}
```

The controllers handle HTTP concerns such as routing, model binding, validation, status codes, and error responses. The service classes contain the main application behavior.

### AI processing pipeline

`AiChatService` processes each request in this order:

```text
1. Detect an explicit calculator command.
2. Search the local knowledge base.
3. Resolve OPENAI_API_KEY or the configured fallback key.
4. Use demo mode when no key is available and demo mode is enabled.
5. Build the system prompt, including retrieved context.
6. Add up to the last 12 valid user/assistant messages.
7. Send an OpenAI-compatible chat-completions request.
8. Parse the provider response.
9. Return ChatResponse to the controller.
```

The external provider is accessed through `HttpClient`, not a provider-specific SDK. The configured base URL and model can therefore be changed to another provider that supports the same chat-completions request format.

The API key is resolved from the `OPENAI_API_KEY` environment variable first and then from `AI:ApiKey`. Environment variables are preferred because secrets should not be committed to source control.

### Demo mode and deterministic tools

The application can run without an API key. In demo mode it returns built-in responses, or displays matching knowledge-base passages as a retrieval preview.

The calculator is a deterministic application tool. Messages beginning with `calculate:` are handled by C# before any external AI request is made. For example:

```text
calculate: (125 + 75) * 1.2
```

The expression is length-limited and restricted to arithmetic characters. The result is calculated by the application, not guessed by the language model. This illustrates an important AI engineering principle: deterministic operations should be implemented as normal application logic when possible.

### Knowledge base and RAG baseline

The knowledge base is implemented by `KnowledgeBaseService` and stored in SQLite. It accepts `.txt` and `.md` files up to one megabyte. File names are normalized with `Path.GetFileName()` to remove directory components, and a unique database index prevents duplicate filenames.

The retrieval implementation is a simple lexical search rather than vector search:

1. Documents are divided into approximately 1,100-character chunks.
2. The user query is tokenized.
3. Query terms longer than two characters are retained.
4. Each chunk receives a score based on query-term overlap.
5. The highest-scoring chunks are returned.
6. The chunks are appended to the AI system prompt as reference context.

This is a basic Retrieval-Augmented Generation, or RAG, pipeline. It has no embeddings or vector database. The prompt describes uploaded material as untrusted reference data and instructs the model not to follow instructions found inside documents. It also asks the model to cite the source filename when relevant.

### Database design

`AppDbContext` defines three main records:

| Record | Purpose |
|---|---|
| `KnowledgeDocumentRecord` | Uploaded filename, text, size and timestamp |
| `ChatConversationRecord` | Conversation ID, title and timestamps |
| `ChatMessageRecord` | User/assistant message content and timestamps |

The relationship between conversations and messages is one-to-many. Messages have a foreign key to their conversation, and cascade delete removes messages when their conversation is deleted.

The database has indexes for unique document filenames and conversation-message lookup by conversation ID and creation time.

The application calls `EnsureCreatedAsync()` on startup. This is convenient for a local starter project because the database and tables are created automatically. For production schema evolution, EF Core migrations should be used instead.

### Browser state and persisted state

The browser keeps the current conversation history in memory in `wwwroot/js/chat.js`. It sends the most recent messages to the server so the AI service has conversational context.

The server separately persists successful exchanges in SQLite. The database therefore provides durable history, while the browser provides the short-term context used during the current page session.

Refreshing the page currently clears the browser state. The API already provides conversation retrieval endpoints, so a future UI enhancement could load a saved conversation and reconstruct the chat history after a refresh.

### Engineering practices used

The project demonstrates several useful .NET engineering practices:

- Dependency injection through the built-in ASP.NET Core service container.
- Interfaces for AI and knowledge-base services.
- Strongly typed configuration through the Options pattern.
- Nullable reference types enabled in the project file.
- Asynchronous database, file, and HTTP operations.
- Cancellation tokens passed through controller and service operations.
- Structured logging with `ILogger`.
- Data-annotation validation for chat messages.
- File extension, file size, and content validation.
- Appropriate HTTP status codes for provider failures and timeouts.
- `AsNoTracking()` for read-only EF Core queries.
- Static-file versioning with `asp-append-version`.
- Safe text rendering with JavaScript `textContent` rather than injecting response HTML.
- Environment-variable support for secrets.

### Current architectural limitations

This project is designed for learning and local development, so several production concerns remain:

- There is no authentication or authorization.
- Conversations and documents are not associated with individual users.
- There is no rate limiting or usage quota system.
- There are no automated unit or integration tests in the repository.
- EF Core migrations have not been added.
- Chat persistence is handled directly in `ChatController`; a dedicated conversation service could improve separation and testability.
- Knowledge-base search loads and scans stored documents for every query, which will not scale well for large datasets.
- The RAG implementation uses keyword overlap rather than semantic embeddings.
- Provider response parsing assumes the expected JSON structure.
- AI responses are not streamed to the browser.
- Uploaded content is not malware-scanned or moderated.

### Recommended next steps

For a production-oriented version, the logical progression would be:

1. Add unit tests for calculator logic, chunking, retrieval, and service error handling.
2. Add integration tests for the API controllers and database behavior.
3. Introduce EF Core migrations.
4. Add authentication, authorization, and per-user data ownership.
5. Move conversation persistence into a dedicated service.
6. Add rate limiting, quotas, monitoring, and correlation IDs.
7. Precompute document chunks during upload instead of rebuilding them for every search.
8. Replace keyword retrieval with full-text search or embeddings and vector search.
9. Add streaming responses using server-sent events or another streaming mechanism.
10. Use a managed secret store and production database when deploying publicly.

Overall, the repository is a compact MVC-based AI web application that combines server-rendered Razor UI, JSON APIs, dependency-injected application services, SQLite persistence, deterministic backend tools, and a basic RAG pipeline.
