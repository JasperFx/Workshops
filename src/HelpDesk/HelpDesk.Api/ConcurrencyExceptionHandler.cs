using JasperFx;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api;

#region sample_concurrency_exception_handler
/// <summary>
/// Optimistic concurrency is enforced down in Marten, which throws. Somebody
/// has to decide what that means at the edge, and "409 Conflict, go re-read and
/// try again" is the honest answer for a REST API.
///
/// Returning a 500 here instead - the default - would tell the caller the
/// server is broken, when in fact the caller's assumption was stale.
/// </summary>
public class ConcurrencyExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ConcurrencyException) return false;

        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = "This record was modified by somebody else. Re-read it and try again."
        }, cancellationToken);

        return true;
    }
}
#endregion
