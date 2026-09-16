[CmdletBinding()]
param(
    [ValidateSet('Host', 'Client')][string]$MeasuredRole = 'Host',
    [ValidateSet(1, 2, 4, 6)][int]$PlayerCount = 4,
    [ValidateRange(30, 10000)][int]$Frames = 2000,
    [ValidateRange(0, 120)][double]$WarmupSeconds = 2,
    [string]$PlayerPath = '',
    [string]$OutputRoot = '',
    [string]$Tag = '',
    [ValidateRange(1024, 65535)][int]$Port = 17900
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$resolvedPlayer = if ($PlayerPath) { [IO.Path]::GetFullPath($PlayerPath) } else { Join-Path $projectRoot 'Builds\PerformanceAuditInstrumented2\TCC.exe' }
if (-not (Test-Path -LiteralPath $resolvedPlayer -PathType Leaf)) { throw "Player not found: $resolvedPlayer" }
if ($MeasuredRole -eq 'Client' -and $PlayerCount -lt 2) { throw 'A measured client requires at least two player processes.' }

$existing = @(Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'TCC.exe' })
if ($existing.Count -gt 0) { throw "Refusing to start with orphaned TCC.exe processes: $($existing.ProcessId -join ', ')" }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$safeTag = ($Tag -replace '[^A-Za-z0-9_-]', '-').Trim('-')
$runName = if ($safeTag) { "$stamp-$safeTag" } else { $stamp }
$root = if ($OutputRoot) { [IO.Path]::GetFullPath($OutputRoot) } else { Join-Path $projectRoot 'Logs\PerformanceProfiling\Multiplayer' }
$runDirectory = Join-Path $root $runName
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null

$processes = [Collections.Generic.List[object]]::new()

function Start-AuditProcess {
    param([string]$Role, [int]$ClientIndex, [bool]$Measured)
    $name = if ($Role -eq 'host') { 'host' } else { "client-$ClientIndex" }
    $output = Join-Path $runDirectory $name
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    $log = Join-Path $output 'Player.log'
    $arguments = [Collections.Generic.List[string]]::new()
    if (-not $Measured) {
        $arguments.Add('-batchmode')
        $arguments.Add('-nographics')
    } else {
        $arguments.Add('-force-d3d12')
        $arguments.Add('-screen-width'); $arguments.Add('1920')
        $arguments.Add('-screen-height'); $arguments.Add('1080')
        $arguments.Add('-screen-fullscreen'); $arguments.Add('1')
    }
    $arguments.Add('-logFile'); $arguments.Add(('"{0}"' -f $log))
    $arguments.Add("--performance-audit-scene=MN_Run")
    $arguments.Add("--performance-audit-output=$output")
    $arguments.Add("--performance-audit-frames=$Frames")
    $arguments.Add("--performance-audit-warmup=$WarmupSeconds")
    $arguments.Add('--performance-audit-quality=Ultra')
    $arguments.Add('--performance-audit-width=1920')
    $arguments.Add('--performance-audit-height=1080')
    $arguments.Add('--performance-audit-fullscreen=true')
    $arguments.Add('--performance-audit-render-scale=1')
    $arguments.Add("--performance-audit-role=$Role")
    $arguments.Add("--performance-audit-client-index=$ClientIndex")
    $arguments.Add("--performance-audit-player-count=$PlayerCount")
    $arguments.Add("--performance-audit-port=$Port")
    $arguments.Add("--performance-audit-measured=$($Measured.ToString().ToLowerInvariant())")
    $windowStyle = if ($Measured) { 'Normal' } else { 'Hidden' }
    $process = Start-Process -FilePath $resolvedPlayer -ArgumentList $arguments -PassThru -WindowStyle $windowStyle
    $item = [pscustomobject]@{ name = $name; role = $Role; clientIndex = $ClientIndex; measured = $Measured; process = $process; output = $output; log = $log }
    $processes.Add($item)
    return $item
}

try {
    $measureHost = $MeasuredRole -eq 'Host'
    $hostProcess = Start-AuditProcess -Role 'host' -ClientIndex 0 -Measured $measureHost
    $hostReady = Join-Path $hostProcess.output 'network-ready.txt'
    $hostDeadline = [DateTime]::UtcNow.AddSeconds(60)
    while (-not (Test-Path -LiteralPath $hostReady -PathType Leaf) -and [DateTime]::UtcNow -lt $hostDeadline) {
        if ($hostProcess.process.HasExited) { throw "Host exited before opening KCP with code $($hostProcess.process.ExitCode). See $($hostProcess.log)" }
        Start-Sleep -Milliseconds 250
    }
    if (-not (Test-Path -LiteralPath $hostReady -PathType Leaf)) { throw 'Host did not publish its KCP-ready marker.' }

    for ($index = 1; $index -lt $PlayerCount; $index++) {
        $isMeasuredClient = $MeasuredRole -eq 'Client' -and $index -eq 1
        Start-AuditProcess -Role 'client' -ClientIndex $index -Measured $isMeasuredClient | Out-Null
    }

    $measured = $processes | Where-Object measured | Select-Object -First 1
    $deadline = [DateTime]::UtcNow.AddSeconds(180)
    $completed = Join-Path $measured.output 'completed.txt'
    $failed = Join-Path $measured.output 'failed.txt'
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-Path -LiteralPath $completed -PathType Leaf) { break }
        if (Test-Path -LiteralPath $failed -PathType Leaf) { throw (Get-Content -Raw -LiteralPath $failed) }
        if ($measured.process.HasExited) { throw "Measured process exited early with code $($measured.process.ExitCode). See $($measured.log)" }
        Start-Sleep -Milliseconds 250
    }
    if (-not (Test-Path -LiteralPath $completed -PathType Leaf)) { throw 'Timed out waiting for the measured process capture.' }

    $summaryPath = Join-Path $measured.output 'summary.json'
    $summary = Get-Content -Raw -LiteralPath $summaryPath | ConvertFrom-Json
    $manifest = [ordered]@{
        schema = 'tcc-multiplayer-performance-run/v1'
        utc = [DateTime]::UtcNow.ToString('O')
        playerCount = $PlayerCount
        measuredRole = $MeasuredRole.ToLowerInvariant()
        measuredClientIndex = $measured.clientIndex
        port = $Port
        frames = $Frames
        warmupSeconds = $WarmupSeconds
        quality = 'Ultra'
        resolution = '1920x1080'
        renderScale = 1
        graphicsApi = 'd3d12'
        playerPath = $resolvedPlayer
        processes = @($processes | ForEach-Object { [ordered]@{ name = $_.name; role = $_.role; clientIndex = $_.clientIndex; measured = $_.measured; processId = $_.process.Id; headless = -not $_.measured } })
        measuredSummary = $summaryPath
        networkBytesIn = $summary.networkBytesIn
        networkBytesOut = $summary.networkBytesOut
        networkMessagesIn = $summary.networkMessagesIn
        networkMessagesOut = $summary.networkMessagesOut
    }
    $manifestPath = Join-Path $runDirectory 'multiplayer-run.json'
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
    Write-Host "Capture complete: $manifestPath" -ForegroundColor Green
} finally {
    foreach ($item in $processes) {
        if (-not $item.process.HasExited) { Stop-Process -Id $item.process.Id -Force -ErrorAction SilentlyContinue }
        try { $item.process.WaitForExit(10000) | Out-Null } catch { }
    }
    $remaining = @(Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'TCC.exe' })
    if ($remaining.Count -gt 0) { throw "TCC.exe processes remained after cleanup: $($remaining.ProcessId -join ', ')" }
}
