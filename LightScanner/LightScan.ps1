# LightScan.ps1 - finds lights only: OPT / LC18 on every COM port, OPT on 192.168.<subnet>.1-254.
# Prints where each light is and exits. No build needed: loads NSW_Components.dll from this folder.
#   LightScan.bat            -> COM ports + 192.168.0.x and 192.168.1.x
#   LightScan.bat 0 1 2      -> choose the 192.168.<n>.x subnets
# Exit code: 0 = light(s) found, 1 = none, 2 = error
param([int[]]$Subnets = @(0, 1))

$ErrorActionPreference = 'Stop'
$dir = $PSScriptRoot
try {
    if (-not [Environment]::Is64BitProcess) { throw 'run with 64-bit PowerShell (the light DLLs are x64)' }

    # files from a downloaded zip are blocked; native DLLs are found through PATH / current dir
    Get-ChildItem -Path $dir -Recurse -File | Unblock-File -ErrorAction SilentlyContinue
    $env:PATH = "$dir;$dir\External;$env:PATH"
    [Environment]::CurrentDirectory = $dir
    $nsw = Join-Path $dir 'NSW_Components.dll'
    if (-not (Test-Path $nsw)) { $nsw = Join-Path $dir 'External\NSW_Components.dll' }
    if (-not (Test-Path $nsw)) { throw "NSW_Components.dll not found next to the script" }
    [void][Reflection.Assembly]::LoadFrom($nsw)
} catch { Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red; exit 2 }

# Opens a device, always closes it. 'OK' / 'BUSY' / 'FAIL: reason'
function Probe([scriptblock]$open, [scriptblock]$close) {
    try { if (& $open) { 'OK' } else { 'FAIL' } }
    catch {
        $e = $_.Exception; while ($e.InnerException) { $e = $e.InnerException }
        $m = "$($e.Message)".ToLower()
        if ($e -is [UnauthorizedAccessException] -or $m -match 'denied|in use|already|no permission|busy') { 'BUSY' }
        else { "FAIL: $($e.Message)" }
    }
    finally { try { & $close } catch { } }
}

function PortBusy([string]$com) {
    $p = New-Object System.IO.Ports.SerialPort $com
    try { $p.Open(); $false }
    catch { $e = $_.Exception; while ($e.InnerException) { $e = $e.InnerException }; $e -is [UnauthorizedAccessException] }
    finally { $p.Dispose() }
}

$found = New-Object System.Collections.Generic.List[string]

# ---------- COM ----------
$ports = [System.IO.Ports.SerialPort]::GetPortNames() | Where-Object { $_ -match '^COM\d+$' } |
         Sort-Object { [int]$_.Substring(3) } -Unique
Write-Host "--- COM scan: $(if ($ports) { $ports -join ', ' } else { 'no ports' }) ---"
foreach ($com in $ports) {
    if (PortBusy $com) { Write-Host ("{0,-6} in use (access denied)" -f $com); continue }

    $opt = New-Object NSW_Components.OPT
    $r = Probe { $opt.Open($com) } { $opt.Close() }
    Write-Host ("{0,-6} {1,-11} -> {2}" -f $com, 'OPT light', $r)
    if ($r -eq 'OK') { $found.Add(("{0,-11} {1}" -f 'OPT light', $com)); continue }

    $lc = New-Object NSW_Components.LC18
    $r = Probe { if ($lc.Open($com, 115200)) { $true } else { throw 'no reply' } } { $lc.Close() }
    Write-Host ("{0,-6} {1,-11} -> {2}" -f $com, 'LC18 light', $r)
    if ($r -eq 'OK') { $found.Add(("{0,-11} {1}" -f 'LC18 light', $com)) }
}

# ---------- IP ----------
Write-Host ""
Write-Host "--- IP scan: $(($Subnets | ForEach-Object { "192.168.$_.1-254" }) -join ', ') ---"
$self = [System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces() |
        ForEach-Object { $_.GetIPProperties().UnicastAddresses } | ForEach-Object { $_.Address.ToString() }
$pings = foreach ($s in $Subnets) { foreach ($h in 1..254) {
    $ip = "192.168.$s.$h"
    [pscustomobject]@{ Ip = $ip; Task = (New-Object System.Net.NetworkInformation.Ping).SendPingAsync($ip, 500) }
} }
try { [void][Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($pings.Task)) } catch { }
$alive = $pings | Where-Object { $_.Task.Status -eq 'RanToCompletion' -and $_.Task.Result.Status -eq 'Success' -and $self -notcontains $_.Ip } |
         ForEach-Object { $_.Ip } | Sort-Object { [version]$_ }
Write-Host "$(@($alive).Count) hosts replied$(if ($alive) { ': ' + ($alive -join ', ') })"

foreach ($ip in $alive) {
    $opt = New-Object NSW_Components.OPT
    $r = Probe { $opt.Open($ip) } { $opt.Close() }
    Write-Host ("{0,-15} {1,-11} -> {2}" -f $ip, 'OPT light', $r)
    if ($r -eq 'OK')   { $found.Add(("{0,-11} {1}" -f 'OPT light', $ip)) }
    if ($r -eq 'BUSY') { $found.Add(("{0,-11} {1} (busy)" -f 'OPT light', $ip)) }
}

# ---------- result ----------
Write-Host ""
Write-Host "=== Lights found ===" -ForegroundColor Cyan
if ($found.Count -eq 0) { Write-Host "(none)" } else { $found | ForEach-Object { Write-Host $_ -ForegroundColor Green } }
if ($found.Count -gt 0) { exit 0 } else { exit 1 }
