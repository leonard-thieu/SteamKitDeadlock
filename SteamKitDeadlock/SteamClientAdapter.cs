using System;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using static SteamKit2.SteamClient;
using static SteamKit2.SteamUser;

namespace SteamKitDeadlock
{
    sealed class SteamClientAdapter : IDisposable
    {
        public SteamClientAdapter(SteamClient steamClient, CallbackManager manager)
        {
            this.steamClient = steamClient;
            this.manager = manager;
            MessageLoop = new Thread(() =>
            {
                while (isRunning)
                {
                    this.manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                }
            });
            MessageLoop.IsBackground = true;
            MessageLoop.Name = "MessageLoop";
        }

        readonly SteamClient steamClient;
        readonly CallbackManager manager;
        bool isRunning;

        internal Thread MessageLoop { get; }

        public Task<ConnectedCallback> ConnectAsync()
        {
            var tcs = new TaskCompletionSource<ConnectedCallback>();

            IDisposable onConnected = null;
            IDisposable onDisconnected = null;
            onConnected = manager.Subscribe<ConnectedCallback>(response =>
            {
                Console.WriteLine("Connected to Steam.");
                tcs.SetResult(response);

                onConnected.Dispose();
                onDisconnected.Dispose();

                onDisconnected = manager.Subscribe<DisconnectedCallback>(_ =>
                {
                    StopMessageLoop();
                    Console.WriteLine("Disconnected from Steam.");
                    onDisconnected.Dispose();
                });
            });
            onDisconnected = manager.Subscribe<DisconnectedCallback>(response =>
            {
                tcs.SetException(new Exception("Unable to connect to Steam."));
                onConnected.Dispose();
                onDisconnected.Dispose();
            });

            StartMessageLoop();
            steamClient.Connect();

            return tcs.Task;
        }

        public Task<LoggedOnCallback> LogOnAsync(LogOnDetails details)
        {
            var tcs = new TaskCompletionSource<LoggedOnCallback>();

            IDisposable onLoggedOn = null;
            IDisposable onDisconnected = null;
            onLoggedOn = manager.Subscribe<LoggedOnCallback>(response =>
            {
                switch (response.Result)
                {
                    case EResult.OK:
                        {
                            Console.WriteLine("Logged on to Steam.");
                            tcs.SetResult(response);
                            break;
                        }
                    default:
                        {
                            var ex = new Exception($"Unable to logon to Steam. {response.Result}");
                            tcs.SetException(ex);
                            break;
                        }
                }

                onLoggedOn.Dispose();
                onDisconnected.Dispose();
            });
            onDisconnected = manager.Subscribe<DisconnectedCallback>(response =>
            {
                tcs.SetException(new Exception("Unable to connect to Steam."));
                onLoggedOn.Dispose();
                onDisconnected.Dispose();
            });

            steamClient.GetHandler<SteamUser>().LogOn(details);

            return tcs.Task;
        }

        public SteamUserStats GetSteamUserStats() => steamClient.GetHandler<SteamUserStats>();

        public bool IsConnected => steamClient.IsConnected;

        public void Disconnect() => steamClient.Disconnect();

        private void StartMessageLoop()
        {
            isRunning = true;
            MessageLoop.Start();
        }

        private void StopMessageLoop()
        {
            isRunning = false;
        }

        #region IDisposable Implementation

        bool disposed;

        public void Dispose()
        {
            if (disposed) { return; }

            if (IsConnected)
            {
                Disconnect();
            }
            else
            {
                StopMessageLoop();
            }

            disposed = true;
        }

        #endregion
    }
}
