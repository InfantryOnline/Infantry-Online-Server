using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountWeb.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public int ErrorStatusCode { get; private set; }

    public bool IsNotFound => ErrorStatusCode == StatusCodes.Status404NotFound;

    public string Heading => IsNotFound ? "Page not found" : "Something went wrong";

    public string Message => IsNotFound
        ? "The link may be expired, invalid, or no longer available."
        : "The request could not be completed. Try again, or check the server logs if this keeps happening.";

    public void OnGet(int? statusCode = null)
    {
        ErrorStatusCode = statusCode ?? HttpContext.Response.StatusCode;
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}

