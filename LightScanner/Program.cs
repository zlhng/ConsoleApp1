using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using DeviceScanner.Scanning;
using NSW_Components;

namespace LightScanner
{
    /// <summary>
    /// Command-line light finder: probes every COM port for OPT / LC18 lights and ping-sweeps the subnets
    /// for OPT lights on IP, prints what it found and exits.
    ///   usage: LightScanner.exe [subnet ...]      e.g. LightScanner.exe 0 1   (default 0 1 = 192.168.0.x and 192.168.1.x)
    ///   exit code: 0 = at least one light found, 1 = none, 2 = error
    /// </summary>
    internal static class Program
    {
        private const int PingTimeoutMs = 500;
        private const int PingParallelism = 64;

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                int[] subnets = args.Length > 0 ? args.Select(int.Parse).ToArray() : new[] { 0, 1 };
                var found = new List<string>();

                found.AddRange(ScanCom());
                found.AddRange(ScanIp(subnets));

                Console.WriteLine();
                Console.WriteLine("=== Lights found ===");
                if (found.Count == 0) Console.WriteLine("(none)");
                foreach (string f in found) Console.WriteLine(f);
                return found.Count > 0 ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                return 2;
            }
        }

        private static readonly DeviceProbe[] ComLights =
        {
            new DeviceProbe("OPT light", com =>
            {
                var d = new OPT();
                return ProbeHelpers.Run(() => d.Open(com), () => d.Close());
            }),
            new DeviceProbe("LC18 light", com =>
            {
                var d = new LC18();
                return ProbeHelpers.Run(() => d.Open(com, 115200) ? true : throw new Exception("no reply"), () => d.Close());
            }),
        };

        private static readonly DeviceProbe IpLight = new DeviceProbe("OPT light", ip =>
        {
            var d = new OPT();
            return ProbeHelpers.Run(() => d.Open(ip), () => d.Close());
        });

        private static IEnumerable<string> ScanCom()
        {
            string[] ports = SerialPort.GetPortNames()
                .Where(p => p.StartsWith("COM") && int.TryParse(p.Substring(3), out _))
                .OrderBy(p => int.Parse(p.Substring(3))).ToArray();
            Console.WriteLine($"--- COM scan: {(ports.Length == 0 ? "no ports" : string.Join(", ", ports))} ---");

            var found = new List<string>();
            foreach (string com in ports)
            {
                if (ProbeHelpers.PortBusy(com)) { Console.WriteLine($"{com,-6} in use (access denied)"); continue; }
                foreach (var probe in ComLights)
                {
                    var r = probe.Run(com);
                    Console.WriteLine($"{com,-6} {probe.Name,-11} -> {r}");
                    if (r.Status == ProbeStatus.Ok) { found.Add($"{probe.Name,-11} {com}"); break; }
                }
            }
            return found;
        }

        private static IEnumerable<string> ScanIp(int[] subnets)
        {
            Console.WriteLine();
            Console.WriteLine($"--- IP scan: {string.Join(", ", subnets.Select(s => $"192.168.{s}.1-254"))} ---");
            string[] alive = PingSweep(subnets);
            Console.WriteLine($"{alive.Length} hosts replied{(alive.Length == 0 ? "" : ": " + string.Join(", ", alive))}");

            var found = new List<string>();
            foreach (string ip in alive)
            {
                var r = IpLight.Run(ip);
                Console.WriteLine($"{ip,-15} {IpLight.Name,-11} -> {r}");
                if (r.Found) found.Add($"{IpLight.Name,-11} {ip}{(r.Status == ProbeStatus.Busy ? " (busy)" : "")}");
            }
            return found;
        }

        private static string[] PingSweep(int[] subnets)
        {
            var self = new HashSet<string>(NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Select(a => a.Address.ToString()));

            var ips = from t in subnets from h in Enumerable.Range(1, 254) select $"192.168.{t}.{h}";
            var alive = new ConcurrentBag<string>();
            Parallel.ForEach(ips, new ParallelOptions { MaxDegreeOfParallelism = PingParallelism }, ip =>
            {
                try { using (var p = new Ping()) if (p.Send(ip, PingTimeoutMs).Status == IPStatus.Success) alive.Add(ip); }
                catch { }
            });
            return alive.Where(ip => !self.Contains(ip)).OrderBy(ip => Version.Parse(ip)).ToArray();
        }
    }
}
