using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace HotchocolateTemplate.Authentication
{
    public static class AuthenticationExtension
    {
        public static IServiceCollection AddCustomJWTAuth(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
            })
                .AddJwtBearer("websocket", ctx => { }) // Websockets must first be allowed, as they are not authenticated until the onConnect message is sent. 
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = configuration["Authentication:Authority"];
                    options.ApiName = configuration["Authentication:Client"];

                    string secret = configuration["Authentication:Secret"];
                    if (secret != string.Empty)
                    {
                        Console.WriteLine("Secret");
                        options.ApiSecret = secret;
                    }

                    options.RequireHttpsMetadata = false;

                    // check the kind of request and decide how the user should be authenticate
                    options.ForwardDefaultSelector = context =>
                    {
                        if (
                            !context.Items.ContainsKey(AuthSocketInterceptor.HTTP_CONTEXT_WEBSOCKET_AUTH_KEY) &&
                            context.Request.Headers.TryGetValue("Upgrade", out var value)
                        )
                        {
                            if (value.Count > 0 && value[0] is string stringValue && stringValue == "websocket")
                            {
                                return "websocket";
                            }
                        }
                        return JwtBearerDefaults.AuthenticationScheme;
                    };

                    // Extract the token either from context item or from the request header
                    options.TokenRetriever = new Func<Microsoft.AspNetCore.Http.HttpRequest, string>(req =>
                    {
                        if (req.HttpContext.Items.TryGetValue(AuthSocketInterceptor.HTTP_CONTEXT_WEBSOCKET_AUTH_KEY, out object? token) && token is string stringToken)
                        {
                            return stringToken;
                        }
                        var fromHeader = TokenRetrieval.FromAuthorizationHeader();
                        var tokenFromHeader = fromHeader(req);

                        return tokenFromHeader;
                    });


                    // Set error if something is wrong
                    // - Maybe the auth-server is not reachable
                    // - Token expired / not valid
                    options.JwtBearerEvents = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = c =>
                        {
                            c.NoResult();
                            if (!c.HttpContext.WebSockets.IsWebSocketRequest)
                            {
                                c.Response.StatusCode = 500;
                                c.Response.ContentType = "text/plain";
                            }
                            return c.Response.WriteAsync("Error with the authentication");
                        }
                    };


                });

            return services;
        }



        public static IServiceCollection AddAuthorization(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Administrator", policy => policy.RequireRole(configuration["Authentication:AdminRoleName"]));
            });

            return services;
        }


    }
}
