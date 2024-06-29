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
    private class ClientPrincipal
    {
        public string IdentityProvider { get; set; }
        public string UserId { get; set; }
        public string UserDetails { get; set; }
        public IEnumerable<string> UserRoles { get; set; }
    }

    public static ClaimsPrincipal Parse(HttpRequestData req)
    {
        var principal = new ClientPrincipal();
        var data = req.Headers.GetValues("x-ms-client-principal").First();
        var decoded = Convert.FromBase64String(data);
        var json = Encoding.UTF8.GetString(decoded);
        principal = JsonSerializer.Deserialize<ClientPrincipal>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (principal is null)
        {
            return new ClaimsPrincipal();
        }

        principal.UserRoles = principal.UserRoles.Except(new string[] { "anonymous" }, StringComparer.CurrentCultureIgnoreCase);

        if (!principal.UserRoles?.Any() ?? true)
        {
            return new ClaimsPrincipal();
        }

        var identity = new ClaimsIdentity(principal.IdentityProvider);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principal.UserId));
        identity.AddClaim(new Claim(ClaimTypes.Name, principal.UserDetails));
        identity.AddClaims(principal.UserRoles.Select(r => new Claim(ClaimTypes.Role, r)));

        return new ClaimsPrincipal(identity);
    }
}