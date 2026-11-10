using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DeviceScanner.Profiles
{
    /// <summary>Reads a .profile text file ("key = value" lines, '#' comments) into a MachineProfile.</summary>
    internal static class ProfileLoader
    {
        public static MachineProfile Load(string path)
        {
            string file = Path.GetFileName(path);
            var p = new MachineProfile { SourcePath = path, Name = Path.GetFileNameWithoutExtension(path) };

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = StripComment(lines[i]).Trim();
                if (line.Length == 0) continue;

                int eq = line.IndexOf('=');
                if (eq <= 0) throw new FormatException($"{file} line {i + 1}: expected \"key = value\"");

                string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                string val = line.Substring(eq + 1).Trim();

                try { Apply(p, key, val); }
                catch (FormatException ex) { throw new FormatException($"{file} line {i + 1}: {ex.Message}"); }
            }

            if (string.IsNullOrWhiteSpace(p.ZmcIp)) throw new FormatException($"{file}: zmc.ip is required");
            foreach (var lane in p.Burn.Lanes)
            {
                if (lane.Pro == null) throw new FormatException($"{file}: burn lane \"{lane.Name}\" has no PRO station");
                if (p.Burn.UsePre && lane.Pre == null) throw new FormatException($"{file}: burn.mode = PRE_PRO but lane \"{lane.Name}\" has no PRE station");
            }
            return p;
        }

        private static void Apply(MachineProfile p, string key, string val)
        {
            switch (key)
            {
                case "name": p.Name = val; break;
                case "zmc.ip": p.ZmcIp = val; break;
                case "zmc.init": p.InitProgram = val; break;
                case "home.programs": p.HomePrograms.AddRange(SplitList(val)); break;
                case "home.axes": p.HomeAxes = SplitList(val).Select(s => ParseInt(s, "axis")).ToArray(); break;
                case "com.slaves": p.TempSlaves = SplitList(val).Select(s => ParseInt(s, "slave address")).ToArray(); break;
                case "ip.expect": p.ExpectedIp.Add(ParseDevice(val)); break;
                case "com.expect": p.ExpectedCom.Add(ParseDevice(val)); break;
                case "io": p.IoTests.Add(ParseIo(val)); break;
                case "burn.mode": p.Burn.UsePre = ParseMode(val); break;
                case "burn.speed": ParseSpeed(p.Burn, val); break;
                case "burn.ratio": ParseRatio(p.Burn, val); break;
                case "burn.loadin": ParseLoadIn(p.Burn, val); break;
                case "burn.timeout": ParseTimeout(p.Burn, val); break;
                case "burn.lane": p.Burn.Lanes.Add(ParseLane(val)); break;
                case "burn.station": ParseStation(p.Burn, val); break;
                case "burn.programs": p.Burn.Programs.AddRange(SplitList(val)); break;
                case "burn.stop": p.Burn.StopPrograms.AddRange(SplitList(val)); break;
                default: throw new FormatException($"unknown key \"{key}\"");
            }
        }

        // ---------- conveyor burn run ----------

        private static bool ParseMode(string val)
        {
            switch (val.ToUpperInvariant())
            {
                case "PRO": return false;
                case "PRE_PRO": return true;
                default: throw new FormatException($"burn.mode must be PRO or PRE_PRO, got \"{val}\"");
            }
        }

        // burn.speed = fast, slow, slow after ms
        private static void ParseSpeed(BurnRunSetup b, string val)
        {
            string[] f = SplitList(val);
            if (f.Length < 1 || f.Length > 3) throw new FormatException("expected \"burn.speed = fast, slow, slow after ms\"");
            b.Speed = ParseDouble(f[0], "speed");
            if (f.Length > 1) b.SlowSpeed = ParseDouble(f[1], "slow speed");
            if (f.Length > 2) b.SlowAfterMs = ParseInt(f[2], "slow after ms");
        }

        // burn.ratio = percent of the original programmed speed (SPEED_RATIO), 1 .. 100
        private static void ParseRatio(BurnRunSetup b, string val)
        {
            double pct = ParseDouble(val.TrimEnd('%', ' '), "speed ratio %");
            if (pct <= 0 || pct > 100) throw new FormatException($"burn.ratio must be 1 .. 100 (%), got \"{val}\"");
            b.SpeedPercent = pct;
        }

        // burn.loadin = delay ms, reverse distance
        private static void ParseLoadIn(BurnRunSetup b, string val)
        {
            string[] f = SplitList(val);
            if (f.Length < 1 || f.Length > 2) throw new FormatException("expected \"burn.loadin = delay ms, reverse distance\"");
            b.LoadInDelayMs = ParseInt(f[0], "load-in delay ms");
            if (f.Length > 1) b.ReverseDist = ParseDouble(f[1], "reverse distance");
        }

        // burn.timeout = sensor ms, io ms
        private static void ParseTimeout(BurnRunSetup b, string val)
        {
            string[] f = SplitList(val);
            if (f.Length < 1 || f.Length > 2) throw new FormatException("expected \"burn.timeout = sensor ms, io ms\"");
            b.SensorTimeoutMs = ParseInt(f[0], "sensor timeout ms");
            if (f.Length > 1) b.IoTimeoutMs = ParseInt(f[1], "io timeout ms");
        }

        // burn.lane = name | conveyor axes | board-in IN | board-out IN | tension roller OP or -
        private static BurnLane ParseLane(string val)
        {
            string[] f = SplitFields(val);
            if (f.Length < 4 || f.Length > 5 || f[0].Length == 0)
                throw new FormatException("expected \"name | conveyor axes | board-in IN | board-out IN | tension roller OP\"");
            return new BurnLane
            {
                Name = f[0],
                Axes = SplitList(f[1]).Select(s => ParseInt(s, "conveyor axis")).ToArray(),
                BoardIn = ParseInt(f[2], "board-in input"),
                BoardOut = ParseInt(f[3], "board-out input"),
                Tension = f.Length > 4 && f[4].Length > 0 && f[4] != "-" ? ParseInt(f[4], "tension roller output") : (int?)null,
            };
        }

        // burn.station = lane | PRE or PRO | stopper | lifter | precisor | vacuum | board present IN
        private static void ParseStation(BurnRunSetup b, string val)
        {
            string[] f = SplitFields(val);
            if (f.Length != 7)
                throw new FormatException("expected \"lane | PRE or PRO | stopper | lifter | precisor | vacuum | board present IN\"");

            var lane = b.Lanes.FirstOrDefault(l => l.Name.Equals(f[0], StringComparison.OrdinalIgnoreCase))
                       ?? throw new FormatException($"burn.station for unknown lane \"{f[0]}\" (put its burn.lane line first)");
            var station = new BurnStation
            {
                Name = $"{lane.Name} {f[1].ToUpperInvariant()}",
                Stopper = ParsePair(f[2], "stopper"),
                Lifter = ParsePair(f[3], "lifter"),
                Precisor = ParsePair(f[4], "precisor"),
                Vacuum = ParsePair(f[5], "vacuum"),
                Present = f[6].Length > 0 && f[6] != "-" ? ParseInt(f[6], "board present input") : (int?)null,
            };
            switch (f[1].ToUpperInvariant())
            {
                case "PRE": lane.Pre = station; break;
                case "PRO": lane.Pro = station; break;
                default: throw new FormatException($"station must be PRE or PRO, got \"{f[1]}\"");
            }
        }

        private static IoPair ParsePair(string s, string what)
        {
            if (s.Length == 0 || s == "-") return IoPair.None;
            string[] f = s.Split('/').Select(x => x.Trim()).ToArray();
            if (f.Length > 2) throw new FormatException($"{what} must be \"out/in\", \"out\" or \"-\", got \"{s}\"");
            int? inNo = f.Length == 2 && f[1].Length > 0 && f[1] != "-" ? ParseInt(f[1], what + " input") : (int?)null;
            return new IoPair(ParseInt(f[0], what + " output"), inNo);
        }

        private static ExpectedDevice ParseDevice(string val)
        {
            string[] f = SplitFields(val);
            if (f.Length != 2 || f[0].Length == 0 || f[1].Length == 0)
                throw new FormatException("expected \"address | device name\"");
            return new ExpectedDevice(f[0], f[1]);
        }

        private static IoTestCase ParseIo(string val)
        {
            string[] f = SplitFields(val);
            if (f.Length < 2 || f.Length > 5 || f[0].Length == 0)
                throw new FormatException("expected \"name | output | input | settle ms | optional\"");

            int outNo = ParseInt(f[1], "output");
            int? inNo = f.Length > 2 && f[2].Length > 0 && f[2] != "-" ? ParseInt(f[2], "input") : (int?)null;
            int settle = f.Length > 3 && f[3].Length > 0 ? ParseInt(f[3], "settle ms") : 0;

            bool optional = false;
            if (f.Length > 4 && f[4].Length > 0)
            {
                if (!f[4].Equals("optional", StringComparison.OrdinalIgnoreCase))
                    throw new FormatException($"last field must be \"optional\" or empty, got \"{f[4]}\"");
                optional = true;
            }

            return new IoTestCase(f[0], outNo, inNo, settle, optional);
        }

        // ---------- small helpers ----------

        private static string StripComment(string s)
        {
            int hash = s.IndexOf('#');
            return hash < 0 ? s : s.Substring(0, hash);
        }

        private static string[] SplitList(string s) =>
            s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
             .Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();

        private static string[] SplitFields(string s) =>
            s.Split('|').Select(x => x.Trim()).ToArray();

        private static int ParseInt(string s, string what) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
                ? v : throw new FormatException($"{what} must be a whole number, got \"{s}\"");

        private static double ParseDouble(string s, string what) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
                ? v : throw new FormatException($"{what} must be a number, got \"{s}\"");
    }
}
