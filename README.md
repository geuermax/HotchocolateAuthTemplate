# HotchocolateAuthTemplate
A template for an ASP.Net Core GraphQL server with hotchocolate and authentication via JWT. 

This project is a small template that can be used to setup hotchocolate v11 with websockets and JWT authentication. It uses the apollo implemention for authentication over websockets. 
## How it works
When a websocket connection is established, the first message conatins the token. The socketinterceptor retrives that token and authenticates the user. It then adds the user to the http context for further use. It also adds the connection to a manager that keeps track of each connection. The manager terminates the connection after a cretain time. The amount of time should be the same as the duration of the token. There is also a mutation defined wich can be used to refresh the token so that the websocketconnection wont be terminated. This mutation can only be used over websocket. If you are using hybridmode you must get track of the connectionID so the timer can reset if the token is refreshed.
