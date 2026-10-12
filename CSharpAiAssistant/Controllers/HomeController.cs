using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CSharpAiAssistant.Models;

namespace CSharpAiAssistant.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}
