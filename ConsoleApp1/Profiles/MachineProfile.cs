using System.Collections.Generic;
using System.Linq;

namespace DeviceScanner.Profiles
{
    /// <summary>
    /// Everything that differs from one machine to the next. Loaded from a .profile text file,
    /// so testing a new machine means copying a file, not changing code.
    /// </summary>
    internal sealed class MachineProfile
    {
        public string Name { get; set; } = "unnamed";
        public string SourcePath { get; set; }

        // ----- controller -----
        public string ZmcIp { get; set; }
        public string InitProgram { get; set; } = "INITSYSTEM";
        public List<string> HomePrograms { get; } = new List<string>();
        public int[] HomeAxes { get; set; } = new int[0];          // empty = auto-discover

        // ----- devices that must be present -----
        public int[] TempSlaves { get; set; } = { 1, 2, 3, 4 };
        public List<ExpectedDevice> ExpectedIp { get; } = new List<ExpectedDevice>();
        public List<ExpectedDevice> ExpectedCom { get; } = new List<ExpectedDevice>();

        // ----- discrete IO loopback -----
        public List<IoTestCase> IoTests { get; } = new List<IoTestCase>();

        // ----- conveyor burn run (lanes empty = not configured) -----
        public BurnRunSetup Burn { get; } = new BurnRunSetup();
    }

    /// <summary>
    /// Conveyor dry run per lane, as OneDispenser's I10_TWIN_DL.LoadBoardDryRun_C1/_C2:
    /// return to board-in, (load PRE), load PRO A, load out, again until the operator stops it.
    /// </summary>
    internal sealed class BurnRunSetup
    {
        public bool UsePre { get; set; }                   // StationMode 1 (PRE_PRO): stop at PRE before PRO
        public double Speed { get; set; } = 100;           // conveyor jog speed, controller units/s
        public double SlowSpeed { get; set; } = 20;
        public int SlowAfterMs { get; set; }               // 0 = never slow down
        public double SpeedPercent { get; set; } = 100;    // SPEED_RATIO on the burn axes: 100 = the original programmed speeds
        public int LoadInDelayMs { get; set; }             // keep running this long after board present
        public double ReverseDist { get; set; }            // then back the conveyor off by this much (0 = no)
        public int SensorTimeoutMs { get; set; } = 10000;  // board-in / present / out sensors
        public int IoTimeoutMs { get; set; } = 5000;       // stopper / lifter / precisor / vacuum feedback
        public List<BurnLane> Lanes { get; } = new List<BurnLane>();
        public List<string> Programs { get; } = new List<string>();   // controller programs RUN alongside, STOPped with it
        public List<string> StopPrograms { get; } = new List<string>(); // RUN on stop instead of RAPIDSTOP(2), e.g. MotionStop
        public bool Configured => Lanes.Count > 0 || Programs.Count > 0;
    }

    internal sealed class BurnLane
    {
        public string Name { get; set; }
        public int[] Axes { get; set; } = new int[0];      // conveyor axes jogged together
        public int BoardIn { get; set; }                   // IN numbers
        public int BoardOut { get; set; }
        public int? Tension { get; set; }                  // tension roller OP, null = not fitted
        public BurnStation Pre { get; set; }
        public BurnStation Pro { get; set; }
        public IEnumerable<BurnStation> Stations => new[] { Pre, Pro }.Where(s => s != null);
    }

    /// <summary>One working station on a lane; each module is an output with an optional feedback input.</summary>
    internal sealed class BurnStation
    {
        public string Name { get; set; }
        public IoPair Stopper { get; set; } = IoPair.None;
        public IoPair Lifter { get; set; } = IoPair.None;
        public IoPair Precisor { get; set; } = IoPair.None;
        public IoPair Vacuum { get; set; } = IoPair.None;
        public int? Present { get; set; }                  // board present IN
    }

    /// <summary>"out/in", "out" (no feedback) or "-" (not fitted, skipped).</summary>
    internal sealed class IoPair
    {
        public static readonly IoPair None = new IoPair(null, null);
        public int? Out { get; }
        public int? In { get; }
        public IoPair(int? outNo, int? inNo) { Out = outNo; In = inNo; }
        public override string ToString() => Out == null ? "-" : In == null ? $"OP({Out})" : $"OP({Out})/IN({In})";
    }

    /// <summary>A device the profile says must answer at an IP address or COM port.</summary>
    internal sealed class ExpectedDevice
    {
        public string Address { get; }     // IP or COM port
        public string Name { get; }        // must match a DeviceProbe name

        public ExpectedDevice(string address, string name) { Address = address; Name = name; }
    }

    /// <summary>One output that should make one input follow.</summary>
    internal sealed class IoTestCase
    {
        public string Name { get; }
        public int Out { get; }
        public int? In { get; }            // null = just report which inputs react
        public int SettleMs { get; }       // 0 = use ScanConfig.IoSettleMs
        public bool Optional { get; }      // failure is a warning only

        public IoTestCase(string name, int outNo, int? inNo, int settleMs, bool optional)
        {
            Name = name; Out = outNo; In = inNo; SettleMs = settleMs; Optional = optional;
        }
    }
}
