using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace BibliotecaEscolar.Api.Security;

// Supabase publica JWKS. Não pressupõe um endpoint de descoberta OpenID Connect.
public sealed class SupabaseConfigurationRetriever(string issuer)
    : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        var json = await retriever.GetDocumentAsync(address, cancel);
        var configuration = new OpenIdConnectConfiguration { Issuer = issuer, JwksUri = address };
        foreach (var key in new JsonWebKeySet(json).GetSigningKeys())
            configuration.SigningKeys.Add(key);
        return configuration;
    }
}
