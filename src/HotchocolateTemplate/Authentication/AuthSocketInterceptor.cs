using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
namespace HotchocolateTemplate.Authentication
{
    public class AuthSocketInterceptor : ISocketSessionInterceptor
    {
        private readonly WebsocketManager _webSocketManager;

        public static readonly string HTTP_CONTEXT_WEBSOCKET_AUTH_KEY = "websocket-auth-token";
        public static readonly string WEBOCKET_PAYLOAD_AUTH_KEY = "Authorization";

        public AuthSocketInterceptor()
        {
            _webSocketManager = WebsocketManager.Instance;
        }

        public async ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketConnection connection,
            InitializeConnectionMessage message,
            CancellationToken cancellationToken)
        {
            // Auth
            object? token = null;
            var payload = message.Payload ?? throw new Exception("Payload is missing. Can not authenticate user.");
            payload?.TryGetValue(WEBOCKET_PAYLOAD_AUTH_KEY, out token);

            if (token != null && token is string stringToken)
            {
                // Do auth
                var context = connection.HttpContext;

                context.Items.TryAdd(HTTP_CONTEXT_WEBSOCKET_AUTH_KEY, stringToken);
                context.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
                {
                    OriginalPath = context.Request.Path,
                    OriginalPathBase = context.Request.PathBase
                });


                var authResult = await context.AuthenticateAsync(IdentityServerAuthenticationDefaults.AuthenticationScheme);
                if (authResult?.Principal != null)
                {
                    context.User = authResult.Principal;
                    // add connection to manager
                    _webSocketManager.AddSocketConnection(connection);
                    return ConnectionStatus.Accept();
                }
                else
                {
                    // reject websocket because user is not authenticated
                    return ConnectionStatus.Reject();
                }
            }
            else
            {
                return ConnectionStatus.Reject();
            }
        }

        public ValueTask OnRequestAsync(ISocketConnection connection, IQueryRequestBuilder requestBuilder, CancellationToken cancellationToken)
        {
            HttpContext context = connection.HttpContext;
            requestBuilder.TrySetServices(connection.RequestServices);
            requestBuilder.TryAddProperty(nameof(CancellationToken), connection.RequestAborted);
            requestBuilder.TryAddProperty(nameof(HttpContext), context);
            requestBuilder.TryAddProperty(nameof(ClaimsPrincipal), context.User);

            return default;
        }

        public async ValueTask OnCloseAsync(ISocketConnection connection, CancellationToken cancellationToken)
        {
            await _webSocketManager.RemoveSocketConnection(connection.HttpContext.Connection.Id);
        }

    }
}
