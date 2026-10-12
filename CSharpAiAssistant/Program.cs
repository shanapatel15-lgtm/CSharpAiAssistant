using CSharpAiAssistant.Data;
using CSharpAiAssistant.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var databaseFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(databaseFolder);
var databasePath = Path.Combine(databaseFolder, "orbit-ai.db");

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("AI"));
builder.Services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
builder.Services.AddHttpClient<IAiChatService, AiChatService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

var app = builder.Build();

// Creates the local database and tables on first run. Keep the DB file out of source control.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
