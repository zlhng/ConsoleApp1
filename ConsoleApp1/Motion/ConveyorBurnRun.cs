using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using DeviceScanner.Native;
using DeviceScanner.Profiles;
using DeviceScanner.Reporting;

namespace DeviceScanner.Motion
{
    /// <summary>
    /// Conveyor dry run, ported from OneDispenser TF_CONVEYOR / I10_TWIN_DL.LoadBoardDryRun_C1 and _C2:
    /// per lane, return the board to board-in, (load PRE), load PRO A, load out. Lane 1 then lane 2, over
    /// and over until the set run time is up (the cycle in progress is finished first) or Stop() is called.
    /// burn.conveyor = continuous: the conveyor only jogs forward until Stop (no stations, IO or sensors).
    /// Board-free by default (burn.sensors = off): no test board and no board-in / present / out sensor is
    /// needed; each conveyor move runs for burn.jog ms instead of until a sensor, and vacuum feedback is not
    /// waited for. With burn.sensors = on it is the original sensor-driven sequence.
    /// A sensor or feedback timeout stops it as a failure. The conveyor and gantry axes run at burn.ratio
    /// (default 100 %) of their original programmed speeds.
    /// Every controller call goes through one lock, so Stop() can halt the conveyors from the UI thread.
    /// </summary>
    internal sealed class ConveyorBurnRun
    {
        private readonly ScanConfig _cfg;
        private readonly MachineProfile _profile;
        private readonly BurnRunSetup _b;
        private readonly Action<string> _log;
        private readonly TimeSpan _duration;                // zero = until Stop()
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly object _gate = new object();
        private ZmcConnection _zmc;
        private volatile bool _stop;
        private string _failure = "";

        private sealed class StopRequested : Exception { }

        /// <param name="duration">How long to run; TimeSpan.Zero = until Stop().</param>
        public ConveyorBurnRun(ScanConfig cfg, MachineProfile profile, TimeSpan duration, Action<string> log)
        {
            _cfg = cfg; _profile = profile; _b = profile.Burn; _log = log;
            _duration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
        }

        private bool TimeUp => _duration > TimeSpan.Zero && _clock.Elapsed >= _duration;

        /// <summary>Operator stop: ends the run after halting the conveyors now. Safe from any thread.</summary>
        public void Stop()
        {
            _stop = true;
            lock (_gate) { if (_zmc != null && _zmc.IsOpen) HaltAll(); }
        }

        public BurnRunResult Run()
        {
            _clock.Restart();
            var result = new BurnRunResult { Planned = _duration, SpeedPercent = _b.SpeedPercent };
            result.Lanes.AddRange(_b.Lanes.Select(l => new BurnLaneResult { Name = l.Name }));

            using (var zmc = new ZmcConnection())
            {
                lock (_gate) _zmc = zmc;
                try
                {
                    if (!zmc.Open(_profile.ZmcIp)) Fail($"cannot open ZMC at {_profile.ZmcIp}");
                    else if (!new ZmcInit(_cfg, _profile.InitProgram, _log).EnsureReady(zmc))
                        Fail($"{_profile.InitProgram} did not bring the controller to sysstate=1");
                    else
                    {
                        _clock.Restart();                 // the set run time counts from here, not from controller init
                        _log(_duration > TimeSpan.Zero
                            ? $"  run time {BurnRunResult.Hms(_duration)}, until about {DateTime.Now.Add(_duration):yyyy-MM-dd HH:mm:ss}"
                            : "  run time: until Stop");
                        if (_b.Lanes.Count > 0 && _b.ContinuousConveyor)
                            _log("  conveyor: runs forward continuously, no stations, no board sensors");
                        else if (_b.Lanes.Count > 0)
                            _log(_b.UseBoardSensors ? "  board sensors: on (board-in / present / out)"
                                                    : $"  board sensors: off, no board needed; each conveyor move runs {_b.JogMs} ms");
                        SetSpeedRatio();
                        if (StartPrograms(result)) Loop(result);
                    }
                }
                catch (StopRequested) { }
                finally
                {
                    lock (_gate)
                    {
                        if (zmc.IsOpen) HaltAll();
                        _zmc = null;
                    }
                }
            }

            result.Failed = _failure.Length > 0;
            result.Completed = !result.Failed && TimeUp;
            result.StoppedByOperator = _stop && !result.Completed;
            result.Detail = _failure;
            result.Elapsed = _clock.Elapsed;
            _log(result.Failed ? $"  burn run FAILED: {_failure}"
                 : result.Completed ? $"  burn run finished: set run time {BurnRunResult.Hms(_duration)} reached"
                 : "  burn run stopped by the operator");
            return result;
        }

        /// <summary>
        /// SPEED_RATIO = burn.ratio on every conveyor axis and every gantry axis (home.axes), so the burn programs
        /// and the conveyor jog run at that share of their own SPEED (100 % = the original parameters), whatever
        /// override the machine software left on the controller.
        /// </summary>
        private void SetSpeedRatio()
        {
            string r = (_b.SpeedPercent / 100.0).ToString("0.###", CultureInfo.InvariantCulture);
            var axes = _b.Lanes.SelectMany(l => l.Axes).Concat(_profile.HomeAxes).Distinct().OrderBy(a => a).ToList();
            if (axes.Count == 0) return;
            _log($"  speed {_b.SpeedPercent:0.#} % of the original parameters (SPEED_RATIO={r}) on axes {string.Join(", ", axes)}");
            lock (_gate)
                foreach (int a in axes)
                {
                    int rc = _zmc.Execute($"SPEED_RATIO AXIS({a})={r}", out string resp);
                    if (rc != 0 || resp.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
                        _log($"  ! SPEED_RATIO AXIS({a}) not set (rc={rc}, reply \"{resp}\"); the axis keeps its current ratio");
                }
        }

        /// <summary>RUN each burn.programs entry (e.g. RUNXY, RUNZ) so the gantry moves while the conveyors cycle.</summary>
        private bool StartPrograms(BurnRunResult result)
        {
            foreach (string program in _b.Programs)
            {
                int rc;
                string resp;
                lock (_gate) rc = _zmc.Execute($"RUN {BasFile(program)}", out resp);
                _log($"  > RUN {BasFile(program)}  rc={rc}  resp=\"{resp}\"");
                // the controller reports errors as reply text with rc=0
                if (rc != 0 || resp.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Fail($"RUN {program} failed (rc={rc}, reply \"{resp}\")");
                result.Programs.Add(program);
            }
            return true;
        }

        private void Loop(BurnRunResult result)
        {
            if (_b.Lanes.Count == 0)                      // programs only: keep them running until the time is up or Stop
            {
                while (!_stop && !TimeUp) Pause(200);
                return;
            }

            if (_b.ContinuousConveyor)                    // conveyor jogs forward non-stop next to the gantry programs
            {
                foreach (var lane in _b.Lanes) Jog(lane, _b.Speed);
                result.Continuous = true;
                while (!_stop && !TimeUp) Pause(200);
                foreach (var lane in _b.Lanes) StopConv(lane);
                return;
            }

            while (!_stop && !TimeUp)
            {
                for (int i = 0; i < _b.Lanes.Count && !_stop && !TimeUp; i++)
                {
                    var lane = _b.Lanes[i];
                    var lr = result.Lanes[i];
                    _log($"[{lane.Name}] cycle {lr.Cycles + 1}");
                    if (!Cycle(lane)) { lr.Failure = _failure; return; }
                    lr.Cycles++;
                }
            }
        }

        // ---------- one lane cycle (LoadBoardDryRun_C1 / _C2 body) ----------

        private bool Cycle(BurnLane lane) =>
            ReturnToBoardIn(lane)
            && (!_b.UsePre || LoadPre(lane))
            && LoadPro(lane)
            && LoadOut(lane);

        private bool ReturnToBoardIn(BurnLane lane)
        {
            _log("  return to board-in");
            foreach (var s in lane.Stations) if (!Unlock(s)) return false;
            Jog(lane, -_b.Speed);
            try { return WaitBoard(lane.BoardIn, "board-in sensor"); }
            finally { StopConv(lane); }
        }

        private bool LoadPre(BurnLane lane)
        {
            _log("  load PRE");
            var pre = lane.Pre;
            if (!On(pre.Stopper, pre.Name + " stopper")) return false;
            Tension(lane, true);
            if (!Off(pre.Lifter, pre.Name + " lifter")) return false;
            try { if (_b.UseBoardSensors && !WaitIn(lane.BoardIn, true, _b.SensorTimeoutMs, "board-in sensor", null)) return false; }
            finally { StopConv(lane); Tension(lane, false); }

            if (!RunToPresent(lane, pre)) return false;
            return Lock(pre);
        }

        private bool LoadPro(BurnLane lane)
        {
            _log("  load PRO A");
            var pre = lane.Pre;
            var pro = lane.Pro;
            try
            {
                Tension(lane, true);
                if (pre != null && !Unlock(pre)) return false;
                if (!On(pro.Stopper, pro.Name + " stopper")) return false;
                if (pre != null && !Off(pre.Stopper, pre.Name + " stopper")) return false;
                if (!Off(pro.Lifter, pro.Name + " lifter")) return false;
                if (_b.UseBoardSensors)
                {
                    bool alreadyIn = (pre != null && IsOn(pre.Present)) || IsOn(pro.Present);
                    if (!alreadyIn && !WaitIn(lane.BoardIn, true, _b.SensorTimeoutMs, "board-in sensor", null)) return false;
                }
            }
            finally { StopConv(lane); Tension(lane, false); }

            if (!RunToPresent(lane, pro)) return false;
            return Lock(pro);
        }

        private bool LoadOut(BurnLane lane)
        {
            _log("  load out");
            var pro = lane.Pro;
            try
            {
                Tension(lane, true);
                if (!Unlock(pro)) return false;
                Pause(500);
                Jog(lane, _b.Speed);
                return WaitBoard(lane.BoardOut, "board-out sensor");
            }
            finally { Tension(lane, false); StopConv(lane); }
        }

        /// <summary>Jogs until the station's board-present sensor (slow after SlowAfterMs), then delay, stop and back off.</summary>
        private bool RunToPresent(BurnLane lane, BurnStation station)
        {
            if (!_b.UseBoardSensors)                      // board-free: run the conveyor for burn.jog, no sensor
            {
                Jog(lane, _b.Speed);
                try { Pause(_b.JogMs); Pause(_b.LoadInDelayMs); }
                finally { StopConv(lane); }
                if (_b.ReverseDist != 0) MoveRel(lane, -_b.ReverseDist);
                return true;
            }
            if (station.Present == null) return Fail($"{station.Name}: no board present input in the profile");
            bool slow = false;
            Jog(lane, _b.Speed);
            try
            {
                bool ok = WaitIn(station.Present.Value, true, _b.SensorTimeoutMs, station.Name + " board present", elapsed =>
                {
                    if (slow || _b.SlowAfterMs <= 0 || elapsed < _b.SlowAfterMs) return;
                    slow = true;
                    Jog(lane, _b.SlowSpeed);
                });
                if (!ok) return false;
                Pause(_b.LoadInDelayMs);
            }
            finally { StopConv(lane); }

            if (_b.ReverseDist != 0) MoveRel(lane, -_b.ReverseDist);
            return true;
        }

        // ---------- station lock / unlock (TF_WorkingStation) ----------

        // board-free: vacuum cannot build up without a board, so its feedback is not waited for
        private bool Lock(BurnStation s) =>
            On(s.Precisor, s.Name + " precisor")
            && Set(s.Vacuum, true, s.Name + " vacuum", _b.UseBoardSensors)
            && Off(s.Precisor, s.Name + " precisor")
            && On(s.Lifter, s.Name + " lifter");

        private bool Unlock(BurnStation s) =>
            Off(s.Precisor, s.Name + " precisor")
            && Set(s.Vacuum, false, s.Name + " vacuum", _b.UseBoardSensors)
            && Off(s.Lifter, s.Name + " lifter")
            && Off(s.Stopper, s.Name + " stopper");

        // ---------- IO module: set the output, wait for its feedback (IO_Module.On / Off) ----------

        private bool On(IoPair p, string what) => Set(p, true, what);
        private bool Off(IoPair p, string what) => Set(p, false, what);

        private bool Set(IoPair p, bool on, string what, bool feedback = true)
        {
            if (p.Out == null) return true;                     // not fitted
            Op(p.Out.Value, on);
            if (!feedback) { Pause(200); return true; }
            return p.In == null || WaitIn(p.In.Value, on, _b.IoTimeoutMs, what, null);
        }

        /// <summary>Conveyor is jogging: wait for the board sensor, or (board-free) just let it run burn.jog ms.</summary>
        private bool WaitBoard(int input, string what)
        {
            if (_b.UseBoardSensors) return WaitIn(input, true, _b.SensorTimeoutMs, what, null);
            Pause(_b.JogMs);
            return true;
        }

        private bool WaitIn(int input, bool level, int timeoutMs, string what, Action<long> tick)
        {
            var sw = Stopwatch.StartNew();
            while (In(input) != level)
            {
                if (_stop) throw new StopRequested();
                if (timeoutMs > 0 && sw.ElapsedMilliseconds > timeoutMs)
                    return Fail($"{what}: IN({input}) did not turn {(level ? "ON" : "OFF")} within {timeoutMs} ms");
                tick?.Invoke(sw.ElapsedMilliseconds);
                Thread.Sleep(5);
            }
            return true;
        }

        private bool IsOn(int? input) => input != null && In(input.Value);

        private void Pause(int ms)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < ms)
            {
                if (_stop) throw new StopRequested();
                Thread.Sleep(5);
            }
        }

        private bool Fail(string why)
        {
            _failure = why;
            return false;
        }

        // ---------- controller calls ----------

        private bool In(int n)
        {
            lock (_gate) return _zmc.QueryInt($"IN({n})") == 1;
        }

        private void Op(int n, bool on)
        {
            lock (_gate) _zmc.Execute($"OP({n},{(on ? 1 : 0)})");
        }

        private void Tension(BurnLane lane, bool on)
        {
            if (lane.Tension != null) Op(lane.Tension.Value, on);
        }

        private void Jog(BurnLane lane, double speed)
        {
            if (_stop) throw new StopRequested();
            string v = Math.Abs(speed).ToString("0.###", CultureInfo.InvariantCulture);
            lock (_gate)
                foreach (int a in lane.Axes)
                {
                    _zmc.Execute($"SPEED AXIS({a})={v}");
                    _zmc.Execute($"{(speed > 0 ? "FORWARD" : "REVERSE")} AXIS({a})");
                }
        }

        private void StopConv(BurnLane lane)
        {
            lock (_gate) foreach (int a in lane.Axes) _zmc.Execute($"BASE({a}) CANCEL(2)");
        }

        private void MoveRel(BurnLane lane, double dist)
        {
            string d = dist.ToString("0.######", CultureInfo.InvariantCulture);
            lock (_gate) foreach (int a in lane.Axes) _zmc.Execute($"BASE({a}) MOVE({d})");
            foreach (int a in lane.Axes)
                while (true)
                {
                    bool idle;
                    lock (_gate) { if (!_zmc.TryGetIdle(a, out idle)) idle = true; }
                    if (idle) break;
                    if (_stop) throw new StopRequested();
                    Thread.Sleep(5);
                }
        }

        /// <summary>RunXY -> "RunXY.bas" (quoted), the form the ZMC projects use themselves (RUN "Revision.bas", STOP "RunZ.bas").</summary>
        private static string BasFile(string program) =>
            "\"" + (program.IndexOf('.') >= 0 ? program : program + ".bas") + "\"";

        /// <summary>Caller holds _gate. Burn programs stopped, every axis halted, tension rollers off; stations are left as they are.</summary>
        private void HaltAll()
        {
            foreach (string program in _b.Programs) _zmc.Execute($"STOP {BasFile(program)}");
            if (_b.StopPrograms.Count > 0)
                foreach (string program in _b.StopPrograms) _zmc.Execute($"RUN {BasFile(program)}");   // the project's own stop, e.g. MotionStop
            else if (_b.Programs.Count > 0) _zmc.Execute("RAPIDSTOP(2)");   // the gantry moves those programs started
            foreach (var lane in _b.Lanes)
            {
                foreach (int a in lane.Axes) _zmc.Execute($"BASE({a}) CANCEL(2)");
                if (lane.Tension != null) _zmc.Execute($"OP({lane.Tension.Value},0)");
            }
        }
    }
}
