using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Http;

namespace functions.Claims;
public static class ClaimsPrincipalParser
{
    private struct ClientPrincipal
    {
        public string IdentityProvider { get; set; }
        public string UserId { get; set; }
        public string UserDetails { get; set; }
        public IEnumerable<string> UserRoles { get; set; }
    }

    static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ClaimsPrincipal Parse(HttpRequestData req)
    {
        var data = req.Headers.GetValues("x-ms-client-principal").First();
        var decoded = Convert.FromBase64String(data);
        var json = Encoding.UTF8.GetString(decoded);
        var principal = JsonSerializer.Deserialize<ClientPrincipal>(json, jsonOptions);

        principal.UserRoles = principal.UserRoles.Except(["anonymous"], StringComparer.CurrentCultureIgnoreCase);

        if (!principal.UserRoles?.Any() ?? true)
        {
            return new ClaimsPrincipal();
        }

        var identity = new ClaimsIdentity(principal.IdentityProvider);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principal.UserId));
        identity.AddClaim(new Claim(ClaimTypes.Name, principal.UserDetails));

        var roles = principal.UserRoles?.Select(r => new Claim(ClaimTypes.Role, r));
        if (roles != null) identity.AddClaims(roles);

        return new ClaimsPrincipal(identity);
    }
}