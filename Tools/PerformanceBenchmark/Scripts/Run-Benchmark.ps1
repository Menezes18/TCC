[CmdletBinding()]
param(
    [ValidateSet('Preflight','Quick','Full','Batata','Transition','Quality')]
    [string]$Mode = 'Quick',
    [ValidateRange(30,10000)][int]$Frames = 2000,
    [ValidateRange(1,10)][int]$Repeats = 3,
    [ValidateRange(0,120)][double]$WarmupSeconds = 2,
    [ValidateRange(640,7680)][int]$Width = 1920,
    [ValidateRange(360,4320)][int]$Height = 1080,
    [string]$Quality = 'Ultra',
    [ValidateSet('d3d11','d3d12')][string]$GraphicsApi = 'd3d12',
    [switch]$Windowed,
    [switch]$IncludeQualityTiers,
    [switch]$SkipPreflightCheck
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot '..'))
$resultsRoot = Join-Path $packageRoot 'Results'
$releasePlayer = Join-Path $packageRoot 'Builds\Release\TCC.exe'
$diagnosticPlayer = Join-Path $packageRoot 'Builds\Diagnostic\TCC.exe'
$packageInfoPath = Join-Path $packageRoot 'package-info.json'
$preflightMarker = Join-Path $packageRoot 'preflight-approved.json'
$script:records = [Collections.Generic.List[object]]::new()
$script:transitionRows = [Collections.Generic.List[object]]::new()
$script:multiplayerRows = [Collections.Generic.List[object]]::new()
$script:failures = [Collections.Generic.List[string]]::new()
$script:allLogs = [Collections.Generic.List[string]]::new()
$script:portCounter = 0

function Write-JsonFile([string]$Path, [object]$Value, [int]$Depth = 12) {
    $Value | ConvertTo-Json -Depth $Depth | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-FileHashValue([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return 'missing' }
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash
}

function Get-HardwareInfo {
    $computer = Get-CimInstance Win32_ComputerSystem
    $cpu = @(Get-CimInstance Win32_Processor)
    $gpus = @(Get-CimInstance Win32_VideoController)
    $os = Get-CimInstance Win32_OperatingSystem
    $display = $gpus | Where-Object { $_.CurrentHorizontalResolution } | Select-Object -First 1
    [ordered]@{
        schema = 'tcc-benchmark-hardware/v1'
        capturedUtc = [DateTime]::UtcNow.ToString('O')
        machine = $env:COMPUTERNAME
        windows = $os.Caption + ' ' + $os.Version + ' build ' + $os.BuildNumber
        cpu = ($cpu.Name -join '; ').Trim()
        physicalCores = ($cpu | Measure-Object -Property NumberOfCores -Sum).Sum
        logicalThreads = ($cpu | Measure-Object -Property NumberOfLogicalProcessors -Sum).Sum
        totalRamBytes = [int64]$computer.TotalPhysicalMemory
        totalRamGB = [math]::Round([int64]$computer.TotalPhysicalMemory / 1GB, 2)
        gpu = ($gpus.Name -join '; ')
        dedicatedVramBytesReported = @($gpus | ForEach-Object { if ($_.AdapterRAM) { [uint64]$_.AdapterRAM } else { 'N/A' } })
        sharedVramBytes = 'N/A (not reliably exposed by Win32_VideoController)'
        displayResolution = if ($display) { "$($display.CurrentHorizontalResolution)x$($display.CurrentVerticalResolution)" } else { 'N/A' }
        refreshRateHz = if ($display -and $display.CurrentRefreshRate) { [int]$display.CurrentRefreshRate } else { 'N/A' }
        graphicsApiRequested = $GraphicsApi
        benchmarkResolution = "${Width}x${Height}"
        fullscreen = -not $Windowed.IsPresent
        requestedQuality = $Quality
        renderScale = 1.0
        targetFrameRate = 'recorded by Player'
        vSync = 'recorded by Player'
    }
}

function Stop-OwnedPlayers {
    $buildRoot = [IO.Path]::GetFullPath((Join-Path $packageRoot 'Builds'))
    $owned = @(Get-CimInstance Win32_Process -Filter "Name='TCC.exe'" -ErrorAction SilentlyContinue | Where-Object {
        $_.ExecutablePath -and [IO.Path]::GetFullPath($_.ExecutablePath).StartsWith($buildRoot, [StringComparison]::OrdinalIgnoreCase)
    })
    foreach ($process in $owned) { Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue }
    if ($owned.Count -gt 0) { Start-Sleep -Milliseconds 500 }
}

function Wait-ForFile([string]$Path, [object]$Process, [int]$TimeoutSeconds, [string]$FailureMessage) {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-Path -LiteralPath $Path -PathType Leaf) { return }
        if ($Process -and $Process.HasExited) { throw "$FailureMessage Process exited with code $($Process.ExitCode)." }
        Start-Sleep -Milliseconds 250
    }
    throw "$FailureMessage Timeout after $TimeoutSeconds seconds."
}

function Get-Metric([object]$Summary, [string]$Name, [string]$Field = 'p95') {
    $metric = @($Summary.metrics | Where-Object { $_.name -eq $Name } | Select-Object -First 1)
    if ($metric.Count -eq 0) { return 'N/A' }
    return $metric[0].$Field
}

function Convert-SummaryToRecord([object]$Summary, [string]$Scenario, [string]$Lane, [int]$Players, [string]$MeasuredRole) {
    [pscustomobject][ordered]@{
        machine = $env:COMPUTERNAME; lane = $Lane; scenario = $Scenario; scene = $Summary.scene
        run = $Summary.runNumber; players = $Players; role = $MeasuredRole; developmentBuild = $Summary.developmentBuild
        buildIdentifier = $Summary.buildIdentifier; packageVersion = $Summary.benchmarkVersion
        cpu = $Summary.processor; gpu = $Summary.graphicsDevice; graphicsApi = $Summary.graphicsApi
        resolution = "$($Summary.width)x$($Summary.height)"; fullscreen = $Summary.fullscreen
        quality = $Summary.quality; renderScale = $Summary.renderScale; frames = $Summary.requestedFrames
        frameMedianMs = Get-Metric $Summary 'frame' 'median'; frameP95Ms = Get-Metric $Summary 'frame' 'p95'; frameP99Ms = Get-Metric $Summary 'frame' 'p99'
        mainMedianMs = Get-Metric $Summary 'mainThread' 'median'; mainP95Ms = Get-Metric $Summary 'mainThread' 'p95'
        renderMedianMs = Get-Metric $Summary 'renderThread' 'median'; renderP95Ms = Get-Metric $Summary 'renderThread' 'p95'
        gpuMedianMs = Get-Metric $Summary 'gpuFrame' 'median'; gpuP95Ms = Get-Metric $Summary 'gpuFrame' 'p95'; gpuP99Ms = Get-Metric $Summary 'gpuFrame' 'p99'
        scriptsMedianMs = Get-Metric $Summary 'scripts' 'median'; scriptsP95Ms = Get-Metric $Summary 'scripts' 'p95'; scriptsP99Ms = Get-Metric $Summary 'scripts' 'p99'
        physicsMedianMs = Get-Metric $Summary 'physics' 'median'; physicsP95Ms = Get-Metric $Summary 'physics' 'p95'; physicsP99Ms = Get-Metric $Summary 'physics' 'p99'
        gcBytesPerFrameMedian = Get-Metric $Summary 'gcAlloc' 'median'; gcCollections = $Summary.gcCollections
        setPassMedian = Get-Metric $Summary 'setPass' 'median'; drawCallsMedian = Get-Metric $Summary 'drawCalls' 'median'; batchesMedian = Get-Metric $Summary 'batches' 'median'
        trianglesMedian = Get-Metric $Summary 'triangles' 'median'; verticesMedian = Get-Metric $Summary 'vertices' 'median'
        unityAllocatedBytes = $Summary.unityAllocatedMemoryBytes; unityReservedBytes = $Summary.unityReservedMemoryBytes
        textureMemoryMedian = Get-Metric $Summary 'textureMemory' 'median'; meshMemoryMedian = Get-Metric $Summary 'meshMemory' 'median'
        networkBytesIn = $Summary.networkBytesIn; networkBytesOut = $Summary.networkBytesOut
        unavailableCounters = ($Summary.unavailableCounters -join '; ')
    }
}

function Get-TransitionCategory([string]$Phase) {
    if ($Phase -match 'preload|async_load') { return 'disk_asset_loading' }
    if ($Phase -match 'activation|scene_activated') { return 'scene_activation' }
    if ($Phase -match 'eligible_connections|local_player_ready|player_ready_state|all_required_players_ready|disconnected') { return 'networking_readiness' }
    if ($Phase -match 'briefing_displayed|briefing_interactable') { return 'briefing_presentation' }
    if ($Phase -match 'briefing_ack|local_ready_submitted|all_briefing_acks') { return 'player_ack_waiting' }
    if ($Phase -eq 'benchmark_settle_completed') { return 'benchmark_settle_time' }
    if ($Phase -match 'match_controller|timer_started|controller_state|active_player') { return 'match_start' }
    if ($Phase -eq 'first_interactive_gameplay_frame') { return 'interactive_gameplay' }
    if ($Phase -match 'timeout|error') { return 'failure' }
    return 'transition_control'
}

function Add-TransitionFiles([string]$Directory, [string]$Scenario, [string]$Lane, [int]$Run) {
    foreach ($file in @(Get-ChildItem -LiteralPath $Directory -Filter 'transition-*.json' -File -ErrorAction SilentlyContinue)) {
        $trace = Get-Content -Raw -LiteralPath $file.FullName | ConvertFrom-Json
        $ordered = @($trace.events | Sort-Object secondsFromRequest)
        for ($index = 0; $index -lt $ordered.Count; $index++) {
            $previous = if ($index -eq 0) { 0.0 } else { [double]$ordered[$index - 1].secondsFromRequest }
            $event = $ordered[$index]
            $script:transitionRows.Add([pscustomobject][ordered]@{
                machine=$env:COMPUTERNAME; lane=$Lane; scenario=$Scenario; run=$Run; scene=$trace.scene
                transitionId=$trace.transitionId; category=Get-TransitionCategory $event.phase; phase=$event.phase; secondsFromRequest=[double]$event.secondsFromRequest
                phaseDurationSeconds=[double]$event.secondsFromRequest-$previous; connectionId=$event.connectionId; detail=$event.detail
            })
        }
        $interactive = $ordered | Where-Object { $_.phase -eq 'first_interactive_gameplay_frame' } | Select-Object -Last 1
        if ($interactive -and $WarmupSeconds -gt 0) {
            $script:transitionRows.Add([pscustomobject][ordered]@{
                machine=$env:COMPUTERNAME; lane=$Lane; scenario=$Scenario; run=$Run; scene=$trace.scene
                transitionId=$trace.transitionId; category='benchmark_settle_time'; phase='benchmark_settle_completed'
                secondsFromRequest=[double]$interactive.secondsFromRequest+$WarmupSeconds; phaseDurationSeconds=$WarmupSeconds
                connectionId=-1; detail="intentional warm-up/settle; seconds=$WarmupSeconds"
            })
        }
    }
}

function Invoke-Scenario {
    param(
        [string]$Scenario, [string]$Lane, [string]$Scene, [string[]]$Sequence,
        [int]$Players, [ValidateSet('host','client','single')][string]$MeasuredRole,
        [int]$Run, [string]$PlayerMode = 'capture', [bool]$CaptureSummary = $true,
        [string]$QualityName = $Quality, [bool]$RawCapture = $false
    )
    $player = if ($Lane -eq 'Diagnostic') { $diagnosticPlayer } else { $releasePlayer }
    if (-not (Test-Path -LiteralPath $player -PathType Leaf)) { throw "Missing $Lane Player: $player" }
    Stop-OwnedPlayers
    $script:portCounter++
    $port = 18000 + ((Get-Date).Second * 20) + $script:portCounter
    $safeScenario = ($Scenario -replace '[^A-Za-z0-9_-]','-')
    $scenarioRoot = Join-Path $runDirectory ("Runs\{0}\run-{1:00}" -f $safeScenario,$Run)
    New-Item -ItemType Directory -Force -Path $scenarioRoot | Out-Null
    $processes = [Collections.Generic.List[object]]::new()

    function Start-BenchmarkProcess([string]$Role, [int]$ClientIndex, [bool]$Measured) {
        $name = if ($Role -eq 'host' -or $Role -eq 'single') { $Role } else { "client-$ClientIndex" }
        $output = Join-Path $scenarioRoot $name
        New-Item -ItemType Directory -Force -Path $output | Out-Null
        $log = Join-Path $output 'Player.log'
        $args = [Collections.Generic.List[string]]::new()
        $buildId = if ($Lane -eq 'Diagnostic') { $packageInfo.diagnosticBuildIdentifier } else { $packageInfo.releaseBuildIdentifier }
        if (-not $Measured) { $args.Add('-batchmode'); $args.Add('-nographics') }
        else { $args.Add("-force-$GraphicsApi"); $args.Add('-screen-width'); $args.Add([string]$Width); $args.Add('-screen-height'); $args.Add([string]$Height); $args.Add('-screen-fullscreen'); $args.Add($(if($Windowed){'0'}else{'1'})) }
        $args.Add('-logFile'); $args.Add($log)
        $args.Add("--performance-audit-scene=$Scene"); $args.Add("--performance-audit-output=$output")
        $args.Add("--performance-audit-mode=$PlayerMode"); $args.Add("--performance-audit-frames=$Frames")
        $args.Add("--performance-audit-warmup=$WarmupSeconds"); $args.Add("--performance-audit-quality=$QualityName")
        $args.Add("--performance-audit-width=$Width"); $args.Add("--performance-audit-height=$Height")
        $args.Add("--performance-audit-fullscreen=$(( -not $Windowed.IsPresent).ToString().ToLowerInvariant())")
        $args.Add('--performance-audit-render-scale=1'); $args.Add("--performance-audit-role=$Role")
        $args.Add("--performance-audit-client-index=$ClientIndex"); $args.Add("--performance-audit-player-count=$Players")
        $args.Add("--performance-audit-port=$port"); $args.Add("--performance-audit-measured=$($Measured.ToString().ToLowerInvariant())")
        $args.Add("--performance-audit-run=$Run"); $args.Add('--performance-audit-seed=1337')
        $args.Add("--performance-audit-version=$($packageInfo.packageVersion)"); $args.Add("--performance-audit-build-id=$buildId")
        $args.Add("--performance-audit-lane=$($Lane.ToLowerInvariant())")
        if ($Sequence -and $Sequence.Count -gt 0) { $args.Add("--performance-audit-sequence=$($Sequence -join ',')") }
        if ($RawCapture) { $args.Add('--performance-audit-raw=true') }
        $quoted = @($args | ForEach-Object { if ($_ -match '\s') { '"' + ($_ -replace '"','\"') + '"' } else { $_ } })
        $p = Start-Process -FilePath $player -ArgumentList $quoted -PassThru -WindowStyle $(if($Measured){'Normal'}else{'Hidden'})
        $item = [pscustomobject]@{Name=$name;Role=$Role;ClientIndex=$ClientIndex;Measured=$Measured;Process=$p;Output=$output;Log=$log}
        $processes.Add($item)
        return $item
    }

    try {
        if ($Players -eq 1 -and $MeasuredRole -eq 'single') {
            $measured = Start-BenchmarkProcess 'single' 0 $true
        } else {
            $hostMeasured = $MeasuredRole -eq 'host'
            $hostItem = Start-BenchmarkProcess 'host' 0 $hostMeasured
            Wait-ForFile (Join-Path $hostItem.Output 'network-ready.txt') $hostItem.Process 120 "[$Scenario] Host did not open KCP."
            for ($index=1; $index -lt $Players; $index++) {
                $isMeasured = $MeasuredRole -eq 'client' -and $index -eq 1
                $null = Start-BenchmarkProcess 'client' $index $isMeasured
            }
            $measured = $processes | Where-Object Measured | Select-Object -First 1
        }

        $processManifest = [ordered]@{
            schema='tcc-benchmark-processes/v1'; scenario=$Scenario; lane=$Lane; run=$Run; playerCount=$Players
            measuredRole=$MeasuredRole; port=$port; scene=$Scene; sequence=$Sequence
            processes=@($processes | ForEach-Object { [ordered]@{name=$_.Name;role=$_.Role;clientIndex=$_.ClientIndex;measured=$_.Measured;headless=(-not $_.Measured);processId=$_.Process.Id} })
        }
        Write-JsonFile (Join-Path $scenarioRoot 'processes.json') $processManifest

        if ($PlayerMode -eq 'preflight') {
            foreach ($item in $processes) { Wait-ForFile (Join-Path $item.Output 'preflight-pass.txt') $item.Process 180 "[$Scenario] Network preflight failed for $($item.Name)." }
            return
        }

        $completed = Join-Path $measured.Output 'completed.txt'
        $failed = Join-Path $measured.Output 'failed.txt'
        $deadline = [DateTime]::UtcNow.AddSeconds(900)
        while ([DateTime]::UtcNow -lt $deadline) {
            if (Test-Path -LiteralPath $completed) { break }
            if (Test-Path -LiteralPath $failed) { throw (Get-Content -Raw -LiteralPath $failed) }
            if ($measured.Process.HasExited) { throw "[$Scenario] Measured Player exited early with code $($measured.Process.ExitCode)." }
            Start-Sleep -Milliseconds 300
        }
        if (-not (Test-Path -LiteralPath $completed)) { throw "[$Scenario] timed out after 900 seconds." }
        if ($CaptureSummary) {
            $summaryPath = Join-Path $measured.Output 'summary.json'
            if (-not (Test-Path -LiteralPath $summaryPath)) { throw "[$Scenario] summary.json was not created." }
            $summary = Get-Content -Raw -LiteralPath $summaryPath | ConvertFrom-Json
            $record = Convert-SummaryToRecord $summary $Scenario $Lane $Players $MeasuredRole
            $script:records.Add($record)
            if ($Players -gt 1) { $script:multiplayerRows.Add($record) }
        }
        Add-TransitionFiles $measured.Output $Scenario $Lane $Run
    } finally {
        foreach ($item in $processes) {
            if (Test-Path -LiteralPath $item.Log) { $script:allLogs.Add($item.Log) }
            if (-not $item.Process.HasExited) { Stop-Process -Id $item.Process.Id -Force -ErrorAction SilentlyContinue }
            try { $null = $item.Process.WaitForExit(10000) } catch { }
        }
        Stop-OwnedPlayers
    }
}

function Invoke-SafeScenario { param([hashtable]$Arguments)
    try { Invoke-Scenario @Arguments }
    catch { $message = "$($Arguments.Scenario): $($_.Exception.Message)"; $script:failures.Add($message); Write-Host $message -ForegroundColor Red }
}

function Write-Outputs {
    $hardwarePath = Join-Path $runDirectory 'hardware.json'
    Write-JsonFile $hardwarePath $hardware
    Write-JsonFile (Join-Path $runDirectory 'benchmark.json') @($script:records)
    if ($script:records.Count -gt 0) { $script:records | Export-Csv -NoTypeInformation -Encoding UTF8 -LiteralPath (Join-Path $runDirectory 'benchmark.csv') }
    else { 'machine,lane,scenario,scene,run' | Set-Content -LiteralPath (Join-Path $runDirectory 'benchmark.csv') -Encoding UTF8 }
    Write-JsonFile (Join-Path $runDirectory 'transitions.json') @($script:transitionRows)
    if ($script:transitionRows.Count -gt 0) { $script:transitionRows | Export-Csv -NoTypeInformation -Encoding UTF8 -LiteralPath (Join-Path $runDirectory 'transitions.csv') }
    else { 'machine,lane,scenario,run,scene,transitionId,category,phase,secondsFromRequest,phaseDurationSeconds,connectionId,detail' | Set-Content -LiteralPath (Join-Path $runDirectory 'transitions.csv') -Encoding UTF8 }
    if ($script:multiplayerRows.Count -gt 0) { $script:multiplayerRows | Export-Csv -NoTypeInformation -Encoding UTF8 -LiteralPath (Join-Path $runDirectory 'multiplayer.csv') }
    else { 'machine,lane,scenario,scene,run,players,role' | Set-Content -LiteralPath (Join-Path $runDirectory 'multiplayer.csv') -Encoding UTF8 }
    $errorLines = [Collections.Generic.List[string]]::new()
    $knownNonMeasuredLines = [Collections.Generic.List[string]]::new()
    foreach ($log in $script:allLogs) {
        foreach ($line in @(Select-String -LiteralPath $log -Pattern 'InvalidOperationException: Steamworks is not initialized' -ErrorAction SilentlyContinue)) {
            $knownNonMeasuredLines.Add("$log :: $($line.Line)")
        }
        foreach ($line in @(Select-String -LiteralPath $log -Pattern '\[PerfAudit\] FAILED|NullReferenceException|ArgumentException:|IndexOutOfRangeException:|Kcp.*(error|failed)|Connection failed' -ErrorAction SilentlyContinue)) {
            $errorLines.Add("$log :: $($line.Line)")
        }
    }
    foreach ($failure in $script:failures) { $errorLines.Add($failure) }
    $errorReport = [Collections.Generic.List[string]]::new()
    $errorReport.Add("Benchmark-relevant errors: $($errorLines.Count)")
    foreach ($line in $errorLines) { $errorReport.Add($line) }
    $errorReport.Add('')
    $errorReport.Add("Known pre-measurement KCP/Steam UI exceptions: $($knownNonMeasuredLines.Count)")
    $errorReport.Add('These remain in Player.log but are excluded from measured-window error counts because they occur while the non-Steam briefing UI is being prepared before READY.')
    foreach ($line in $knownNonMeasuredLines) { $errorReport.Add($line) }
    $errorReport | Set-Content -LiteralPath (Join-Path $runDirectory 'errors.log') -Encoding UTF8

    $lines = [Collections.Generic.List[string]]::new()
    $lines.Add('# TCC Performance Benchmark Summary'); $lines.Add('')
    $lines.Add("Machine: $($hardware.machine)"); $lines.Add("CPU: $($hardware.cpu)"); $lines.Add("GPU: $($hardware.gpu)"); $lines.Add("RAM: $($hardware.totalRamGB) GB")
    $lines.Add(''); $lines.Add('## Test configuration'); $lines.Add('')
    $lines.Add("Mode: $Mode"); $lines.Add("Resolution: ${Width}x${Height}"); $lines.Add("Quality: $Quality"); $lines.Add("Graphics API requested: $GraphicsApi"); $lines.Add("Frames per capture: $Frames")
    $lines.Add(''); $lines.Add('## Frame performance'); $lines.Add('')
    $lines.Add('| Lane | Scene | Players | Role | Repeats | Median frame p95 (ms) | Median frame p99 (ms) | Peak Unity allocated (bytes) |')
    $lines.Add('|---|---|---:|---|---:|---:|---:|---:|')
    foreach ($group in @($script:records | Group-Object lane,scene,players,role)) {
        $first=$group.Group[0]; $p95=@($group.Group.frameP95Ms | Where-Object { $_ -ne 'N/A' } | Sort-Object); $p99=@($group.Group.frameP99Ms | Where-Object { $_ -ne 'N/A' } | Sort-Object)
        $median95=if($p95.Count){$p95[[math]::Floor(($p95.Count-1)/2)]}else{'N/A'}; $median99=if($p99.Count){$p99[[math]::Floor(($p99.Count-1)/2)]}else{'N/A'}
        $peak=@($group.Group.unityAllocatedBytes | Measure-Object -Maximum).Maximum
        $lines.Add("| $($first.lane) | $($first.scene) | $($first.players) | $($first.role) | $($group.Count) | $median95 | $median99 | $peak |")
    }
    $longest = $script:transitionRows | Sort-Object phaseDurationSeconds -Descending | Select-Object -First 1
    $lines.Add(''); $lines.Add('## Transitions'); $lines.Add('')
    if ($longest) { $lines.Add("Longest measured phase: $($longest.phase) - $([math]::Round([double]$longest.phaseDurationSeconds,3)) s ($($longest.scene), run $($longest.run)).") } else { $lines.Add('No transition phase data was produced.') }
    $lines.Add(''); $lines.Add('## Batata'); $lines.Add('')
        $batataFailures=@($script:failures | Where-Object { $_ -match 'Batata' }); if($Mode -eq 'Batata' -or $Mode -eq 'Full') { if($batataFailures.Count -eq 0){$lines.Add('BATATA_REPRODUCTION: PASS')}else{$lines.Add('BATATA_REPRODUCTION: FAILED');$lines.Add('');$lines.Add('Reason:');foreach($f in $batataFailures){$lines.Add("- $f")};$lines.Add('');$lines.Add('Relevant logs: `Runs\Batata-*\run-*\*\Player.log` and `errors.log`.')} } else {$lines.Add('Not run in this mode.')}
    $lines.Add(''); $lines.Add('## Memory'); $lines.Add(''); $lines.Add('Numbers are reported without a pass/fail budget. Increased reserved memory alone is not classified as a leak.')
    $lines.Add(''); $lines.Add('## Errors'); $lines.Add(''); $lines.Add([string]$errorLines.Count)
    if($script:failures.Count){$lines.Add('');foreach($f in $script:failures){$lines.Add("- $f")}}
    $lines | Set-Content -LiteralPath (Join-Path $runDirectory 'summary.md') -Encoding UTF8
}

if (-not (Test-Path -LiteralPath $packageInfoPath)) { throw "package-info.json is missing. Build the portable package again." }
$packageInfo = Get-Content -Raw -LiteralPath $packageInfoPath | ConvertFrom-Json
New-Item -ItemType Directory -Force -Path $resultsRoot | Out-Null
$stamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$runDirectory = Join-Path $resultsRoot ("{0}_{1}_{2}" -f $stamp,$env:COMPUTERNAME,$Mode)
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null
$hardware = Get-HardwareInfo

try {
    if ($Mode -eq 'Preflight') {
        Write-Host 'If Windows asks for network access, approve it now. This stage is NOT benchmarked.' -ForegroundColor Yellow
        foreach ($lane in @('Release','Diagnostic')) {
            Invoke-Scenario -Scenario "Preflight-$lane" -Lane $lane -Scene 'RASCUNHO' -Sequence @('RASCUNHO') -Players 2 -MeasuredRole host -Run 1 -PlayerMode preflight -CaptureSummary $false
        }
        $marker = [ordered]@{ approvedUtc=[DateTime]::UtcNow.ToString('O'); releaseSha256=Get-FileHashValue $releasePlayer; diagnosticSha256=Get-FileHashValue $diagnosticPlayer }
        Write-JsonFile $preflightMarker $marker
        Write-Host 'PRE-FLIGHT PASS. Firewall/network initialization completed outside benchmark timing.' -ForegroundColor Green
    } else {
        if (-not $SkipPreflightCheck) {
            if (-not (Test-Path -LiteralPath $preflightMarker)) { throw 'Run Run_Preflight.bat first. Firewall interaction must not occur during a benchmark.' }
            $approved = Get-Content -Raw -LiteralPath $preflightMarker | ConvertFrom-Json
            if ($approved.releaseSha256 -ne (Get-FileHashValue $releasePlayer) -or $approved.diagnosticSha256 -ne (Get-FileHashValue $diagnosticPlayer)) { throw 'The benchmark executables changed after PRE-FLIGHT. Run Run_Preflight.bat again.' }
        }
        if ($Mode -eq 'Quick') {
            foreach($scene in @('MN_Run','MN_new_Rua','MN_Queda')){for($run=1;$run -le $Repeats;$run++){Invoke-SafeScenario @{Scenario="Quick-$scene";Lane='Release';Scene=$scene;Sequence=@($scene);Players=1;MeasuredRole='single';Run=$run}}}
            for($run=1;$run -le $Repeats;$run++){Invoke-SafeScenario @{Scenario='Quick-4P-MN_Run';Lane='Release';Scene='MN_Run';Sequence=@('MN_Run');Players=4;MeasuredRole='host';Run=$run}}
        }
        if ($Mode -eq 'Full') {
            $scenes=@('MN_Run','MN_new_Rua','MN_Queda','MN_BatataQ','MN_Memoria','MN_Sumo','RASCUNHO')
            foreach($scene in $scenes){for($run=1;$run -le $Repeats;$run++){Invoke-SafeScenario @{Scenario="Full-$scene";Lane='Release';Scene=$scene;Sequence=@($scene);Players=1;MeasuredRole='single';Run=$run}};Invoke-SafeScenario @{Scenario="Diagnostic-$scene";Lane='Diagnostic';Scene=$scene;Sequence=@($scene);Players=1;MeasuredRole='single';Run=1}}
            foreach($players in @(1,2,4)){for($run=1;$run -le $Repeats;$run++){if($players -eq 1){Invoke-SafeScenario @{Scenario='MP-Host-1P';Lane='Release';Scene='MN_Run';Sequence=@('MN_Run');Players=1;MeasuredRole='single';Run=$run}}else{Invoke-SafeScenario @{Scenario="MP-Host-${players}P";Lane='Release';Scene='MN_Run';Sequence=@('MN_Run');Players=$players;MeasuredRole='host';Run=$run}}}}
            foreach($players in @(2,4)){for($run=1;$run -le $Repeats;$run++){Invoke-SafeScenario @{Scenario="MP-Client-${players}P";Lane='Release';Scene='MN_Run';Sequence=@('MN_Run');Players=$players;MeasuredRole='client';Run=$run}}}
            $ModeBefore=$Mode
            foreach($scenario in @(@('Batata-A','MN_BatataQ'),@('Batata-B','MN_Run','MN_Queda','MN_new_Rua','MN_BatataQ'),@('Batata-C','MN_Run','MN_Queda','MN_new_Rua','MN_Memoria','MN_Sumo','MN_BatataQ'))){for($run=1;$run -le $Repeats;$run++){Invoke-SafeScenario @{Scenario=$scenario[0];Lane='Release';Scene=$scenario[1];Sequence=@($scenario[1..($scenario.Count-1)]);Players=4;MeasuredRole='host';Run=$run;PlayerMode='batata';CaptureSummary=$false}}}
            Invoke-SafeScenario @{Scenario='Transition-Soak-4P';Lane='Release';Scene='MN_Run';Sequence=@('MN_Run','MN_Queda','MN_new_Rua','MN_Memoria','MN_Sumo','MN_Run','MN_Queda','MN_new_Rua','MN_Memoria','MN_Sumo');Players=4;MeasuredRole='host';Run=1;PlayerMode='transition';CaptureSummary=$false}
        }
        if ($Mode -eq 'Batata') { foreach($scenario in @(@('Batata-A','MN_BatataQ'),@('Batata-B','MN_Run','MN_Queda','MN_new_Rua','MN_BatataQ'),@('Batata-C','MN_Run','MN_Queda','MN_new_Rua','MN_Memoria','MN_Sumo','MN_BatataQ'))){for($run=1;$run -le $Repeats;$run++){Invoke-SafeScenario @{Scenario=$scenario[0];Lane='Release';Scene=$scenario[1];Sequence=@($scenario[1..($scenario.Count-1)]);Players=4;MeasuredRole='host';Run=$run;PlayerMode='batata';CaptureSummary=$false}}} }
        if ($Mode -eq 'Transition') { for($run=1;$run -le $Repeats;$run++){Invoke-SafeScenario @{Scenario='Transition-4P';Lane='Release';Scene='MN_Run';Sequence=@('MN_Run','MN_Queda','MN_new_Rua','MN_Memoria','MN_Sumo');Players=4;MeasuredRole='host';Run=$run;PlayerMode='transition';CaptureSummary=$false}} }
        if ($Mode -eq 'Quality' -or $IncludeQualityTiers) { foreach($tier in @('Ultra','High Fidelity','Balanced','Very Low','Performant')){for($run=1;$run -le $Repeats;$run++){Invoke-SafeScenario @{Scenario="Quality-$tier";Lane='Release';Scene='MN_Run';Sequence=@('MN_Run');Players=1;MeasuredRole='single';Run=$run;QualityName=$tier}}} }
    }
} catch { $script:failures.Add($_.Exception.Message); Write-Host $_.Exception.Message -ForegroundColor Red }
finally {
    Stop-OwnedPlayers
    Write-Outputs
    $runDirectory | Set-Content -LiteralPath (Join-Path $resultsRoot 'LATEST.txt') -Encoding UTF8
    Write-Host "Results: $runDirectory" -ForegroundColor Cyan
}

if ($script:failures.Count -gt 0) { exit 2 }
exit 0
