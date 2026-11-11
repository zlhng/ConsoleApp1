# ==============================================================
# DeviceScanner machine profile: I10 DUAL Z (OneDispenser I_SERIES + I_SERIES_DUAL_Z)
# Built from OneDispenser TC_SYSTEM.FactoryReset (I_SERIES, non-TWIN branch), TC_GANTRY
# (DUAL_Z is handled like N4: one gantry X / Y / Z / Z2) and TC_GANTRY.I10 (conveyor, IO).
# Lines marked CHECK are not fixed by that code: confirm them on the machine.
#
# Format:  key = value        '#' starts a comment
# Repeatable keys: home.programs, ip.expect, com.expect, io
# ==============================================================

name = I10-DUAL-Z

# ---------- controller ----------
# FactoryReset I_SERIES: X axis 6, Y axis 8, Z axis 9, Z encoder 4, conveyor axis 0, UNITS (PPU) 2000.
# Axis 7 is the X gantry partner (as GX1 6 / GX2 7 on the i-TWIN project).
# Z2: FactoryReset never sets its axis number for DUAL_Z (only TWIN, as 13). 10 is the N4 value,
# which DUAL_Z follows everywhere else. CHECK. An axis that does not exist is idle at once, so
# listing it does not hold up homing.
zmc.ip        = 192.168.0.11
zmc.init      = INITSYSTEM
home.programs = GHOME                       # CHECK: home program name in this machine's ZMC project
home.axes     = 6, 7, 8, 9, 10              # X, X gantry partner, Y, Z, Z2 (CHECK Z2)

# ---------- devices that must be present ----------
# ip.expect  = <ip>   | <device>    device: CL3 (Keyence) | OPT light | Camera GigE | ZMC
# com.expect = <port> | <device>    device: ITV regulator | MT balance | OPT light | LC18 light
#                                           Temp TK | Temp TX | Temp TM2 | Temp TM4
# The ZMC at zmc.ip is checked automatically.
# FactoryReset enables only the first camera, light and height sensor on startup.
ip.expect  = 192.168.0.100 | Camera GigE        # VisionCam[0], MVC GenTL
ip.expect  = 192.168.1.16  | OPT light          # OPT 8-channel over IP
ip.expect  = 192.168.1.10  | CL3 (Keyence)      # HSensor[0]

# ITV regulators (ITV_RS), all StartUpEnable = true
com.expect = COM1  | ITV regulator              # FLUID (1)
com.expect = COM3  | ITV regulator              # PULSE (1)
com.expect = COM2  | ITV regulator              # VAC (1)
com.expect = COM5  | ITV regulator              # FLUID (2)
com.expect = COM10 | ITV regulator              # PULSE (2)
com.expect = COM6  | ITV regulator              # VAC (2)

# ---------- discrete IO loopback ----------
# io = <name> | <output> | <expected input> | <settle ms> | optional
#   expected input blank or '-' : do not check a pair, just report which inputs react
#   optional                    : a failure is shown as WARN and does not fail the run
# Outputs really switch while this runs.
#
# I/O map: I_SERIES (not TWIN), ELabelDO / ELabelDI
#   OUT 36 stopper 1, 37 lifter up 1, 38 lifter down 1, 39 stopper 2, 40 lifter up 2, 41 lifter down 2,
#       42 kicker, 44/45 chuck vac 1/2, 46 clean press, 47 clean vac, 54/55 precisor 1/2, 11 door lock
#   IN  36 board in, 37 board present 1, 38 stopper up 1, 39 lifter up 1, 40 lifter down 1, 41 board out,
#       43 board present 2, 44 stopper up 2, 45 lifter up 2, 46 lifter down 2, 48 kicker,
#       49/50 precisor ext 1/2, 52/53 chuck vac on 1/2, 19 clean vac on, 15 door locked

# station 1 (PRO)
io = Station 1 stopper up     | 36 | 38 | 1000
io = Station 1 lifter up      | 37 | 39 | 1000
io = Station 1 lifter down    | 38 | 40 | 1000
# station 2 (PRE)
io = Station 2 stopper up     | 39 | 44 | 1000
io = Station 2 lifter up      | 40 | 45 | 1000
io = Station 2 lifter down    | 41 | 46 | 1000

io = Kicker                   | 42 | 48 | 1000 | optional
io = Precisor 1               | 54 | 49 | 1000 | optional
io = Precisor 2               | 55 | 50 | 1000 | optional
# chuck vacuum: the switch only trips with a board or plate on the chuck
io = Chuck vac 1              | 44 | 52 | 1000 | optional
io = Chuck vac 2              | 45 | 53 | 1000 | optional
io = Clean vac                | 47 | 19 | 1000 | optional
# door lock: only locks with the door closed
io = Door lock                | 11 | 15 | 1000 | optional

# ---------- burn run (lite: asked after homing, runs until Stop burn run) ----------
# Gantry: the controller burn programs run non-stop. Conveyor: jogs forward non-stop at burn.speed.
# No station sequence, no stopper / lifter / precisor / vacuum, no board or board sensors, no run time:
# everything keeps going until the operator presses Stop burn run, then the programs are stopped
# (burn.stop) and the conveyor is cancelled.
# burn.conveyor = cycle | continuous   continuous: conveyor axes only jog forward, nothing else
# burn.time     = ask | stop           stop: no hours/minutes/seconds prompt, runs until Stop
# burn.speed    = conveyor jog speed (controller units/s)
# burn.ratio    = % of the original programmed speeds (SPEED_RATIO on conveyor + home.axes; default 100)
# burn.lane     = name | conveyor axes | board-in IN | board-out IN | tension roller OP or -
#                 (continuous: only the axes are used; the sensor inputs are ignored)
# burn.programs = controller programs RUN when the burn run starts, STOPped when it stops
burn.programs = RunXY, RunZ       # CHECK: gantry burn programs in this machine's ZMC project (as i-TWIN)
burn.stop     = MotionStop        # CHECK: RUN on Stop (after STOP of each program)
burn.conveyor = continuous
burn.time     = stop
burn.speed    = 100               # CSPEED 100 mm/s as the other I-series
burn.ratio    = 100               # 100 % of the original speeds
burn.lane     = conveyor | 0 | 36 | 41 | -     # conveyor axis 0 (I10 Conveyor_1)
