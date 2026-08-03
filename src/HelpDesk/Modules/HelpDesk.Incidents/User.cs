using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace HelpDesk.Incidents;

#region sample_user_detection_middleware
/// <summary>
/// A custom type rather than a bare Guid, purely so Wolverine's code generation
/// can route it around unambiguously.
/// </summary>
public record User(Guid Id);

public static class UserDetectionMiddleware
{
    public static (User, ProblemDetails) Load(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("user-id");

        if (claim is not null && Guid.TryParse(claim.Value, out var id))
        {
            return (new User(id), WolverineContinue.NoProblems);
        }

        // Stop the presses and emit a ProblemDetails response with a 400.
        return (new User(Guid.Empty), new ProblemDetails { Detail = "No valid user", Status = 400 });
    }
}
#endregion
