using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DeviceScanner.Utils
{
    /// <summary>
    /// Online = a network is up and the mail server (ScanConfig.SmtpHost) answers on its port, so the report can be sent.
    /// The lite edition does not start offline, and pauses before each module while the connection is gone.
    /// </summary>
    internal static class Online
    {
        public static bool Check(ScanConfig cfg)
        {
            if (!NetworkInterface.GetIsNetworkAvailable()) return false;
            try
            {
                using (var c = new TcpClient())
                    return c.ConnectAsync(cfg.SmtpHost, cfg.SmtpPort).Wait(cfg.OnlineTimeoutMs) && c.Connected;
            }
            catch (Exception) { return false; }      // host not found / refused
        }

        /// <summary>Asks until the PC is online again. False = the operator gave up.</summary>
        public static bool WaitFor(ScanConfig cfg, Func<string, bool> ask, Action<string> log)
        {
            while (!Check(cfg))
            {
                log(L.T("online.offline", cfg.SmtpHost, cfg.SmtpPort));
                if (!ask(L.T("online.required", cfg.SmtpHost, cfg.SmtpPort))) return false;
            }
            return true;
        }

        /// <summary>Logs every time Windows reports the network going away or coming back, also in the middle of a module.</summary>
        public static IDisposable Watch(Action<string> log)
        {
            NetworkAvailabilityChangedEventHandler h = (s, e) => log(L.T(e.IsAvailable ? "online.back" : "online.lost"));
            NetworkChange.NetworkAvailabilityChanged += h;
            return new Unsubscribe(() => NetworkChange.NetworkAvailabilityChanged -= h);
        }

        private sealed class Unsubscribe : IDisposable
        {
            private readonly Action _undo;
            public Unsubscribe(Action undo) { _undo = undo; }
            public void Dispose() => _undo();
        }
    }
}
