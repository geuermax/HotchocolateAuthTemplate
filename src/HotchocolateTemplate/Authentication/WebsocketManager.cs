using HotChocolate.AspNetCore.Subscriptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;

namespace HotchocolateTemplate.Authentication
{
    public class WebsocketManager
    {

        private const string Message = "Socket closed by server";
        private readonly ConcurrentDictionary<string, ISocketConnection> _connections = new ConcurrentDictionary<string, ISocketConnection>();
        private readonly ConcurrentDictionary<string, System.Timers.Timer> _timers = new ConcurrentDictionary<string, System.Timers.Timer>();

        private static WebsocketManager? instance = null;
        private static readonly object padlock = new object();


        private WebsocketManager() { }

        public static WebsocketManager Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                        instance = new WebsocketManager();

                    return instance;
                }
            }
        }

        public ConcurrentDictionary<string, ISocketConnection> GetAll()
        {
            return _connections;
        }

        public void AddSocketConnection(ISocketConnection connection)
        {
            string id = connection.HttpContext.Connection.Id;
            _connections.TryAdd(id, connection);
            Debug.WriteLine("Websocket added to manager. " + id);
            //var timer = new System.Timers.Timer(15 * 1000 * 60);
            var timer = new System.Timers.Timer(30 * 1000); // for testing
            timer.Elapsed += async (sender, e) =>
            {
                Debug.WriteLine("Expiration of websocket exceeded. Closing websocket with id: " + id);
                await RemoveSocketConnection(id);
            };
            timer.AutoReset = false;
            _timers.TryAdd(id, timer);
            timer.Start();
        }

        public async Task RemoveSocketConnection(string id)
        {
            bool deleted = _connections.TryRemove(id, out ISocketConnection? connection);
            if (deleted)
            {
                _timers.TryRemove(id, out System.Timers.Timer? timer);
                timer?.Stop();

                Debug.WriteLine("Websocket closed and removed from manager.");
                if (connection != null)
                {
                    await connection.CloseAsync(Message, SocketCloseStatus.NormalClosure, CancellationToken.None);
                }
            }
        }

        public bool ExtendSocketConnection(string id)
        {
            // System.Timers.Timer? timer;
            _timers.TryGetValue(id, out var timer);
            if (timer != null)
            {
                timer.Stop();
                timer.Start();
                return true;
            }

            return false;
        }
    }
}
