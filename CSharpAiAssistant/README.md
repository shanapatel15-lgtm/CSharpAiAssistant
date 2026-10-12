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
