using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using System;
using static System.Net.WebRequestMethods;

namespace Mango.Services.Identity
{
    public static class SD
    {
        public const string Admin = "Admin";
        public static string Customer = "Customer";

        public static IEnumerable<IdentityResource> IdentityResources => new List<IdentityResource>{
            new IdentityResources.OpenId(),
            new IdentityResources.Email(),
            new IdentityResources.Profile(),
        };

        public static IEnumerable<ApiScope> ApiScopes => new List<ApiScope>
        {
            new ApiScope("mango","MangoServer"),
            new ApiScope("read","Read Your Data"),
            new ApiScope("write","Write Your Data"),
            new ApiScope("delete","Delete Your Data"),
        };
        public static IEnumerable<Client> Clients = new List<Client> { 
            new Client
            {
                ClientId="client",
                ClientSecrets = {new Secret("secret".Sha256())},
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = {"read","write","profile"}
            },
            new Client
            {
                ClientId="mango",
                ClientSecrets = {new Secret("secret".Sha256())},
                AllowedGrantTypes = GrantTypes.Code,
                RedirectUris={"https://localhost:7194/signin-oidc","https://localhost:44353/signin-oidc"},
                PostLogoutRedirectUris={"https://localhost:44353/signout-callback-oidc"},
                AllowedScopes = new List<string>
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    "mango"
                }
            }
        };
    }
}
