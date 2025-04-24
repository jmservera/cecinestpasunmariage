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
        req.Headers.TryGetValues("x-ms-client-principal", out var values);
        if (values == null || !values.Any())
        {
            var referer = req.Headers.GetValues("referer").FirstOrDefault();
            if (referer?.Contains("fotoup", StringComparison.CurrentCultureIgnoreCase) ?? false)
            {
                var cookie = req.Headers.GetValues("cookie").FirstOrDefault();
                // read cookie as json
                var cookieValue = cookie?.Split(';')
                    .Select(c => c.Trim())
                    .FirstOrDefault(c => c.StartsWith("nameRequest="))?["nameRequest=".Length..];
                // decode cookie value from base64
                if (!string.IsNullOrEmpty(cookieValue))
                {
                    var name = Convert.FromBase64String(cookieValue);
                    var nameString = Encoding.UTF8.GetString(name);
                    nameString = nameString.Replace('"', ' ').Trim();
                    var cookieIdentity = new ClaimsIdentity("cookie");
                    cookieIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, nameString));
                    cookieIdentity.AddClaim(new Claim(ClaimTypes.Name, nameString));
                    return new ClaimsPrincipal(cookieIdentity);
                }
            }
            return new ClaimsPrincipal();
        }
        var data = values.First();
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