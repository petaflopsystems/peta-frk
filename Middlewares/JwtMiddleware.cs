using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Petaframework.Interfaces;
using PetaframeworkStd.Interfaces;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Petaframework.Middlewares
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly OIdCSettings _appSettings;

        public JwtMiddleware(RequestDelegate next, IOptions<OIdCSettings> appSettings)
        {
            _next = next;
            _appSettings = appSettings.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
                AttachUserToContext(context, token, _appSettings, CookieAuthenticationDefaults.AuthenticationScheme);

            await _next(context);
        }

        /// <summary>
        /// Realize the account challenge based on AccessToken
        /// </summary>
        /// <param name="context">HttpContext</param>
        /// <param name="appSettings">Settings to Jwt Validation</param>
        /// <param name="authenticationDefaultScheme">Authentication Default Scheme. Default Value: Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme</param>
        public static void Challenge(HttpContext context, OIdCSettings appSettings, String authenticationDefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme, Action<IPtfkSession> OnAuthenthicatedSession = null)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            AttachUserToContext(context, token, appSettings, authenticationDefaultScheme, OnAuthenthicatedSession);
        }

        private static void AttachUserToContext(HttpContext context, string token, OIdCSettings _appSettings, String authenticationDefaultScheme, Action<IPtfkSession> onAuthenthicatedSession=null)
        {
            try
            {
                var client = new HttpClient();
                var disco = client.GetDiscoveryDocumentAsync(
                    _appSettings.Authority
                ).Result;
                var valid = JwtValidation(disco, token, _appSettings);
                var response = client.GetUserInfoAsync(new UserInfoRequest
                {
                    Address = disco.UserInfoEndpoint,
                    Token = token
                }).Result;

                var claimsIdentity = new ClaimsIdentity(response.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
   
                };
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                context.User = claimsPrincipal;
                context.SignInAsync(authenticationDefaultScheme, claimsPrincipal, authProperties).Wait();

                if (!context.User.Identity.IsAuthenticated)
                    throw new Exception();
            }
            catch (Exception ex)
            {
                // do nothing if jwt validation fails
                // user is not attached to context so request won't have access to secure routes
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
        }

        private static bool JwtValidation(DiscoveryDocumentResponse disco, string token, OIdCSettings _appSettings)
        {
            var keys = disco.KeySet
                .Keys
                .Select(key =>
                {
                    var e = Base64Url.Decode(key.E);
                    var n = Base64Url.Decode(key.N);
                    var rsaParameters = new RSAParameters
                    {
                        Exponent = e,
                        Modulus = n
                    };

                    return new RsaSecurityKey(rsaParameters)
                    {
                        KeyId = key.Kid
                    };
                })
                .ToList();
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonAccessToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            bool validateAudience = jsonAccessToken != null && jsonAccessToken.Audiences.Any();
            var tokenValidationParams = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKeys = keys,
                RequireAudience = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateAudience = validateAudience,
                ValidAudience = jsonAccessToken.Audiences.FirstOrDefault(),
                ValidateIssuer = true,
                ValidIssuer = _appSettings.Authority ?? disco.Issuer,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };

            tokenHandler.InboundClaimTypeMap
                .Clear();

            return tokenHandler.ValidateToken(token, tokenValidationParams, out _)
                .Claims
                .Any();
        }

        public class OIdCSettings
        {
            public string Authority { get; set; }

            public string UserLoginClaim { get; set; }
        }

    }
}
