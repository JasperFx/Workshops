using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api;

#region sample_development_authentication
/// <summary>
/// Turns a "user-id" request header into a claim so the workshop's endpoints
/// can be driven from curl or the .http file without standing up an identity
/// provider.
///
/// This is a development affordance and nothing more. It trusts a header, which
/// in production would mean anybody can be anybody. It is registered only when
/// the environment is Development, and the integration tests replace it with
/// Alba's own stub.
/// </summary>
public class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Development";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>();

        if (Request.Headers.TryGetValue("user-id", out var userId) && userId.Count > 0)
        {
            claims.Add(new Claim("user-id", userId[0]!));
        }

        if (Request.Headers.TryGetValue("tenant-id", out var tenantId) && tenantId.Count > 0)
        {
            claims.Add(new Claim("tenant.id", tenantId[0]!));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));

        return Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
#endregion
