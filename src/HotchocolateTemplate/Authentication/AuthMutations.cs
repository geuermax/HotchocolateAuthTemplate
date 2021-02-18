using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotchocolateTemplate.Authentication
{
    [Authorize]
    [ExtendObjectType(Name = "Mutation")]
    public class AuthMutations
    {
        /// <summary>
        /// Refresh the authentication. It is meant to be used for auth over websockets. If the Authentication would not be refreshed the socket has to be closed
        /// </summary>
        /// <param name="token">The token that will be send as part of the mutation.</param>
        /// <param name="contextAccessor">To get access to the current httpcontext</param>
        /// <returns>A status about the authentication.</returns>
        public async Task<RenewAuthStatus> RenewWsAuthAsync(string token, [Service] IHttpContextAccessor contextAccessor)
        {
            // Mutation is only for websockets
            if (contextAccessor.HttpContext.WebSockets.IsWebSocketRequest)
            {
                WebsocketManager manager = WebsocketManager.Instance;

                var context = contextAccessor.HttpContext;

                context.Items.TryAdd(AuthSocketInterceptor.HTTP_CONTEXT_WEBSOCKET_AUTH_KEY, token);
                context.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
                {
                    OriginalPath = context.Request.Path,
                    OriginalPathBase = context.Request.PathBase
                });

                var authResult = await context.AuthenticateAsync(IdentityServerAuthenticationDefaults.AuthenticationScheme);
                if (authResult != null)
                {
                    Console.WriteLine("Renew of authentication was successfull");
                    var extended = manager.ExtendSocketConnection(context.Connection.Id);
                    if (extended)
                    {
                        return RenewAuthStatus.RENEW_AUTH_SUCCESS;
                    }
                }
                return RenewAuthStatus.RENEW_AUTH_FAILED;
            }
            else
            {
                return RenewAuthStatus.NO_WEBSOCKET;
            }

        }

        public enum RenewAuthStatus
        {
            NO_WEBSOCKET,
            RENEW_AUTH_SUCCESS,
            RENEW_AUTH_FAILED
        }
    }
}
