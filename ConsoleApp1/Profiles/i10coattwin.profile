# ==============================================================
# DeviceScanner machine profile
# EXAMPLE built from the SD1X block in FactoryReset - edit it to match the real machine.
# To test another machine, copy this file and change the values. Nothing else needs editing.
#
# Format:  key = value        '#' starts a comment
# Repeatable keys: home.programs, ip.expect, com.expect, io
# ==============================================================

name = I10-TWIN-SHARED

# ---------- controller ----------
zmc.ip        = 192.168.0.11
zmc.init      = INITSYSTEM
home.programs = GHOME           # started together, in this order
home.axes     = 6, 7, 8, 9, 10, 11, 12, 13       # axes to wait on; leave empty to auto-discover

# ---------- devices that must be present ----------
# ip.expect  = <ip>   | <device>    device: CL3 (Keyence) | OPT light | Camera GigE | ZMC
# com.expect = <port> | <device>    device: ITV regulator | MT balance | OPT light | LC18 light
#                                           Temp TK | Temp TX | Temp TM2 | Temp TM4
# The ZMC at zmc.ip is checked automatically.
com.slaves = 1, 2, 3, 4                  # temperature controller slave addresses to try

ip.expect  = 192.168.0.101 | Camera GigE
ip.expect  = 192.168.0.102 | Camera GigE
ip.expect  = 192.168.0.20 | OPT light
ip.expect  = 192.168.0.30 | OPT light
ip.expect  = 192.168.1.10  | CL3 (Keyence)
ip.expect  = 192.168.1.17  | CL3 (Keyence)

com.expect = COM3 | ITV regulator
com.expect = COM4 | ITV regulator
com.expect = COM5 | ITV regulator
com.expect = COM6 | ITV regulator
com.expect = COM7 | ViscoTec EcoSpray
com.expect = COM10 | ViscoTec EcoSpray
# com.expect = COM3 | MT balance         # weigh is StartUpEnable=false in FactoryReset

# ---------- discrete IO loopback ----------
# io = <name> | <output> | <expected input> | <settle ms> | optional
#   expected input blank or '-' : do not check a pair, just report which inputs react
#   settle ms blank             : use IoSettleMs from ScanConfig
#   optional                    : a failure is shown as WARN and does not fail the run
# Outputs really switch while this runs.

# I/O map: I_SERIES_TWIN / TWIN_DL / TWIN_SHARED_STATION (ELabelDO / ELabelDI)
# Each valve output is paired with the sensor that should follow it. Up runs before down, so a lifter
# left up by a double-acting valve is brought back down by the next test.

# process station, lane 1
io = PRO A1 stopper up        | 36 | 38 | 1000
io = PRO A1 lifter up         | 37 | 39 | 1000
io = PRO 1 lifter down        | 38 | 40 | 1000
# process station, lane 2
io = PRO A2 stopper up        | 39 | 44 | 1000
io = PRO A2 lifter up         | 40 | 45 | 1000
io = PRO 2 lifter down        | 41 | 46 | 1000

# pre station, lane 1
io = PRE A1 stopper up        | 54 | 49 | 1000
io = PRE A1 lifter up         | 55 | 50 | 1000
io = PRE 1 lifter down        | 56 | 51 | 1000
# pre station, lane 2
io = PRE A2 stopper up        | 57 | 55 | 1000
io = PRE A2 lifter up         | 58 | 56 | 1000
io = PRE 2 lifter down        | 59 | 57 | 1000

# precisors: the input pairing is read from the label names (EXT_1/EXT_2, EXT_PRO_A_2/EXT_PRE_A_2)
io = PRO A1 precisor          | 60 | 52 | 1000 | optional
io = PRE A1 precisor          | 61 | 53 | 1000 | optional
io = PRO A2 precisor          | 62 | 68 | 1000 | optional
io = PRE A2 precisor          | 63 | 69 | 1000 | optional

# second PRE bank (OUT 64-70 / IN 65-73 carry the same labels as OUT 54-59): only one bank is wired per variant
io = PRE A1 stopper up (bank 2) | 64 | 65 | 1000 | optional
io = PRE A1 lifter up (bank 2)  | 65 | 66 | 1000 | optional
io = PRE 1 lifter down (bank 2) | 66 | 67 | 1000 | optional
io = PRE A2 stopper up (bank 2) | 68 | 71 | 1000 | optional
io = PRE A2 lifter up (bank 2)  | 69 | 72 | 1000 | optional
io = PRE 2 lifter down (bank 2) | 70 | 73 | 1000 | optional

# post station
io = POST A1 stopper up       | 72 | 77 | 1000
io = POST A1 lifter up        | 73 | 78 | 1000
io = POST 1 lifter down       | 74 | 79 | 1000
io = POST A2 stopper up       | 76 | 81 | 1000
io = POST A2 lifter up        | 77 | 82 | 1000
io = POST 2 lifter down       | 78 | 83 | 1000

# chuck vacuum: the switch only trips with a board or plate on the chuck
io = Chuck vac PRE A1         | 80 | 86 | 1000 | optional
io = Chuck vac PRE A2         | 81 | 87 | 1000 | optional
io = Chuck vac POST A1        | 82 | 88 | 1000 | optional
io = Chuck vac POST A2        | 83 | 89 | 1000 | optional
io = Chuck vac 1              | 84 | 90 | 1000 | optional
io = Chuck vac 2              | 85 | 91 | 1000 | optional
io = Chuck vac 3              | 86 | 92 | 1000 | optional
io = Chuck vac 4              | 87 | 93 | 1000 | optional

# door lock: only locks with the door closed
io = Door lock                | 11 | 15 | 1000 | optional

# ---------- conveyor burn run (lite: asked after homing, runs until Stop) ----------
# Port of OneDispenser I10_TWIN_DL.LoadBoardDryRun_C1 / _C2. Each cycle per lane:
#   return to board-in -> (PRE_PRO: load PRE) -> load PRO A -> load out.  Lane 1 then lane 2, repeated.
# A test board must be on each lane before starting.
# burn.mode    = PRO | PRE_PRO
# burn.speed   = fast, slow, slow after ms        (controller units/s; slow after 0 = never)
# burn.ratio   = % of the original programmed speeds (SPEED_RATIO on lane + home.axes; default 100)
# burn.loadin  = delay ms, reverse distance       (after board present)
# burn.timeout = sensor ms, io ms
# burn.lane    = name | conveyor axes | board-in IN | board-out IN | tension roller OP or -
# burn.station = lane | PRE or PRO | stopper | lifter | precisor | vacuum | board present IN
# burn.programs = controller programs RUN when the burn run starts, STOPped (and RAPIDSTOP(2)) when it stops
#                each module: out/in, out (no feedback) or - (not fitted)
#
# Numbers from OneDispenser TC_SYSTEM.FactoryReset, I_SERIES twin block (I_SERIES_TWIN_SHARED_STATION):
#   Conveyor_1 = axis 0, Conveyor_2 = axis 1. Shared station is not "dual lane", so TF_CONVEYOR jogs
#   both conveyor axes for either lane, at the Conveyor_1 speed.
#   IN 36/42 SENS_BOARD_IN_1/2, IN 41/47 SENS_BOARD_OUT_1/2,
#   IN 37/43 SENS_BOARD_PRESENT_PRO_A_1/2, IN 48/54 SENS_BOARD_PRESENT_PRE_A_1/2 (first bank; GetInput takes the first match)
#   precisor feedback: SENS_PRECISOR_EXT_PRO_A_1 / PRE_A_1 are not mapped (52/53 are EXT_1/EXT_2),
#     so lane 1 precisors run without feedback; lane 2 uses 68 / 69
#   vacuum: OP 45 SV_CHUCK_VAC_PRO_A_1 (no VAC_CHUCK_ON_PRO_A_1 input), PRO A2 not mapped,
#     PRE A1 80/86, PRE A2 81/87
#   tension rollers (SV_TENSION_ROLLER_LIFTER_1/2) and kickers are not mapped on this model
# Not in FactoryReset (OneDispenser process settings): check against the machine before relying on them
#   burn.mode    <- StationMode (0 = PRO, 1 = PRE_PRO)
#   burn.speed   <- Conveyor_1 FastVelocity, StartVelocity, BoardLoadSlow_Delay (fast = CSPEED from the .bas)
#   burn.loadin  <- BoardLoadINLeft_Delay, BoardInRevDist
#   burn.timeout <- BoardSensIn / BoardSensOut timeout, IO module timeout (5000 in TF_CONVEYOR)

# From the controller project D:\ssvbas.zip  I10\i-TWIN_ZMC432_(GantryMode)_R00_Oct2025 (FW 1.0.0, 2025-09-30):
#   axes  GX1 6, GX2 7, GY1 8, GZ1 9 | GX3 10, GX4 11, GY2 12, GZ2 13 (gantry pairs 6/7 and 10/11), UNITS 2000
#   RunXY.bas  XY burn-in: GX1/GX3/GY1/GY2 SPEED 300, ACCEL 10000, 7-point loop, never ends on its own
#   RunZ.bas   Z burn-in:  GZ1/GZ2 SPEED 50, ACCEL 2000, Z 0 .. -55 mm, never ends on its own
#   MotionStop.bas  STOP "RunXY.bas" / STOP "RunZ.bas", then CANCEL(2) on axes 6-13
#   conveyors  CONV1_AXIS 0, CONV2_AXIS 1, CUNITS 22.15 (6 mm roller, so mm), CSPEED 100, CACCEL 500 (set by InitSystem)
burn.programs = RunXY, RunZ       # RUN "RunXY.bas", RUN "RunZ.bas" when the burn run starts
burn.stop     = MotionStop        # RUN "MotionStop.bas" on Stop (after STOP of each program)
burn.mode    = PRO
burn.speed   = 100, 20, 0         # CSPEED 100 mm/s from Variables.bas; slow speed unused while slow-after is 0
burn.ratio   = 100               # 100 % of the original speeds (RunXY/RunZ SPEED, CSPEED)
burn.loadin  = 0, 0
burn.timeout = 10000, 5000
burn.lane    = lane 1 | 0, 1 | 36 | 41 | -
burn.station = lane 1 | PRE | 54/49 | 55/50 | 61    | 80/86 | 48
burn.station = lane 1 | PRO | 36/38 | 37/39 | 60    | 45    | 37
burn.lane    = lane 2 | 0, 1 | 42 | 47 | -
burn.station = lane 2 | PRE | 57/55 | 58/56 | 63/69 | 81/87 | 54
burn.station = lane 2 | PRO | 39/44 | 40/45 | 62/68 | -     | 43
