[CmdletBinding()]
param(
    [string[]]$Scenes = @('MN_Queda'),
    [switch]$All,
    [ValidateRange(30, 10000)][int]$Frames = 2000,
    [ValidateRange(0, 120)][double]$WarmupSeconds = 2,
    [switch]$RawCapture,
    [switch]$SkipBuild,
    [switch]$CompareWithPrevious,
    [string]$Tag = '',
    [string]$UnityPath = '',
    [string]$PlayerPath = '',
    [string]$OutputRoot = '',
    [ValidateSet('Ultra', 'High Fidelity', 'Balanced', 'Very Low', 'Performant')][string]$Quality = 'Ultra',
    [ValidateRange(640, 7680)][int]$Width = 1920,
    [ValidateRange(360, 4320)][int]$Height = 1080,
    [ValidateRange(0.1, 2.0)][double]$RenderScale = 1.0,
    [ValidateSet('d3d12', 'd3d11', 'default')][string]$GraphicsApi = 'd3d12',
    [bool]$Fullscreen = $true,
    [switch]$PauseAtEnd
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$allowedScenes = @('RASCUNHO', 'MN_new_Rua', 'MN_Run', 'MN_Memoria', 'MN_BatataQ', 'MN_Queda', 'MN_Sumo')
if ($All) {
    $selectedScenes = $allowedScenes
} else {
    $selectedScenes = @($Scenes | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
}
if ($selectedScenes.Count -eq 0) { throw 'Select at least one scene.' }
foreach ($scene in $selectedScenes) {
    if ($scene -notin $allowedScenes) { throw "Unsupported scene '$scene'. Allowed: $($allowedScenes -join ', ')" }
}

$safeTag = ($Tag -replace '[^A-Za-z0-9_-]', '-').Trim('-')
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$runName = if ($safeTag) { "$stamp-$safeTag" } else { $stamp }
$runsRoot = if ($OutputRoot) { [IO.Path]::GetFullPath($OutputRoot) } else { Join-Path $projectRoot 'Temp\PerformanceAuditRuns' }
$runDirectory = Join-Path $runsRoot $runName
$resolvedPlayerPath = if ($PlayerPath) { [IO.Path]::GetFullPath($PlayerPath) } else { Join-Path $projectRoot 'Builds\PerformanceAuditPlayer\TCC.exe' }
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null

function Resolve-UnityPath {
    param([string]$ExplicitPath)
    if ($ExplicitPath) {
        $resolved = [IO.Path]::GetFullPath($ExplicitPath)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) { throw "Unity executable not found: $resolved" }
        return $resolved
    }
    $versionFile = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
    $versionLine = Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion:\s*(.+)$' | Select-Object -First 1
    if (-not $versionLine) { throw 'Could not read the Unity version from ProjectVersion.txt.' }
    $version = $versionLine.Matches[0].Groups[1].Value.Trim()
    $candidate = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "Unity $version was not found at '$candidate'. Pass -UnityPath explicitly."
    }
    return $candidate
}

function Invoke-PlayerCapture {
    param([string]$Scene)
    $sceneDirectory = Join-Path $runDirectory $Scene
    New-Item -ItemType Directory -Force -Path $sceneDirectory | Out-Null
    $playerLog = Join-Path $sceneDirectory 'Player.log'
    $arguments = @(
        '-logFile', ('"{0}"' -f $playerLog),
        '-screen-width', $Width,
        '-screen-height', $Height,
        ('--performance-audit-scene=' + $Scene),
        ('--performance-audit-output=' + $sceneDirectory),
        ('--performance-audit-frames=' + $Frames),
        ('--performance-audit-warmup=' + $WarmupSeconds.ToString([Globalization.CultureInfo]::InvariantCulture)),
        ('--performance-audit-raw=' + $RawCapture.IsPresent.ToString().ToLowerInvariant()),
        ('--performance-audit-quality=' + $Quality),
        ('--performance-audit-width=' + $Width),
        ('--performance-audit-height=' + $Height),
        ('--performance-audit-render-scale=' + $RenderScale.ToString([Globalization.CultureInfo]::InvariantCulture)),
        ('--performance-audit-fullscreen=' + $Fullscreen.ToString().ToLowerInvariant())
    )
    if ($GraphicsApi -ne 'default') { $arguments += ('-force-' + $GraphicsApi) }
    Write-Host "[$Scene] Starting $Frames-frame capture..." -ForegroundColor Cyan
    $process = Start-Process -FilePath $resolvedPlayerPath -ArgumentList $arguments -PassThru
    if (-not $process.WaitForExit(300000)) {
        $process.Kill()
        throw "[$Scene] Player exceeded the five-minute timeout. See $playerLog"
    }
    $summaryPath = Join-Path $sceneDirectory 'summary.json'
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $summaryPath -PathType Leaf)) {
        $failurePath = Join-Path $sceneDirectory 'failed.txt'
        $detail = if (Test-Path -LiteralPath $failurePath) { Get-Content -Raw -LiteralPath $failurePath } else { "exit code $($process.ExitCode)" }
        throw "[$Scene] Capture failed: $detail. See $playerLog"
    }
    Write-Host "[$Scene] Complete: $summaryPath" -ForegroundColor Green
    return (Get-Content -Raw -LiteralPath $summaryPath | ConvertFrom-Json)
}

function Find-PreviousRun {
    if (-not (Test-Path -LiteralPath $runsRoot -PathType Container)) { return $null }
    $required = @($selectedScenes)
    foreach ($directory in @(Get-ChildItem -LiteralPath $runsRoot -Directory | Where-Object FullName -ne $runDirectory | Sort-Object Name -Descending)) {
        $runSummaryPath = Join-Path $directory.FullName 'run-summary.json'
        if (-not (Test-Path -LiteralPath $runSummaryPath -PathType Leaf)) { continue }
        try { $metadata = Get-Content -Raw -LiteralPath $runSummaryPath | ConvertFrom-Json } catch { continue }
        $expectedScenes = @($selectedScenes | Sort-Object) -join '|'
        $actualScenes = @($metadata.scenes | Sort-Object) -join '|'
        if ($expectedScenes -ne $actualScenes -or
            [int]$metadata.frames -ne $Frames -or
            [double]$metadata.warmupSeconds -ne $WarmupSeconds -or
            [string]$metadata.quality -ne $Quality -or
            [string]$metadata.resolution -ne "${Width}x${Height}" -or
            [double]$metadata.renderScale -ne $RenderScale -or
            [string]$metadata.graphicsApi -ne $GraphicsApi -or
            [bool]$metadata.fullscreen -ne $Fullscreen) { continue }
        $found = $true
        foreach ($scene in $required) {
            if (-not (Test-Path -LiteralPath (Join-Path $directory.FullName "$scene\summary.json") -PathType Leaf)) {
                $found = $false
                break
            }
        }
        if ($found) { return $directory.FullName }
    }
    return $null
}

function Write-Comparison {
    param([string]$BaselineDirectory, [object[]]$CandidateSummaries)
    $lines = [Collections.Generic.List[string]]::new()
    $lines.Add('# Performance Audit Comparison')
    $lines.Add('')
    $lines.Add("Baseline: ``$BaselineDirectory``")
    $lines.Add("Candidate: ``$runDirectory``")
    $lines.Add('')
    $lines.Add('Negative delta is faster/lower. Treat changes within 5% as noise until repeated.')
    $lines.Add('')
    $lines.Add('| Scene | Metric | Baseline p99 | Candidate p99 | Delta | Signal |')
    $lines.Add('|---|---|---:|---:|---:|---|')
    $importantMetrics = @('frame', 'cpuFrame', 'mainThread', 'renderThread', 'gpuFrame', 'scripts', 'physics', 'gcAlloc', 'setPass', 'triangles', 'totalUsedMemory', 'textureMemory', 'meshMemory', 'renderTextureMemory', 'gfxUsedMemory')
    foreach ($candidate in $CandidateSummaries) {
        $baselinePath = Join-Path $BaselineDirectory "$($candidate.scene)\summary.json"
        $baseline = Get-Content -Raw -LiteralPath $baselinePath | ConvertFrom-Json
        if ([string]$baseline.processor -ne [string]$candidate.processor -or
            [string]$baseline.graphicsDevice -ne [string]$candidate.graphicsDevice) {
            $lines.Add("| ``$($candidate.scene)`` | hardware check | -- | -- | -- | skipped: different CPU/GPU |")
            continue
        }
        foreach ($metricName in $importantMetrics) {
            $before = $baseline.metrics | Where-Object name -eq $metricName | Select-Object -First 1
            $after = $candidate.metrics | Where-Object name -eq $metricName | Select-Object -First 1
            if ($null -eq $before -or $null -eq $after -or [double]$before.p99 -eq 0) { continue }
            $delta = (([double]$after.p99 - [double]$before.p99) / [double]$before.p99) * 100
            $signal = if ($delta -lt -5) { 'improved' } elseif ($delta -gt 5) { 'regressed' } else { 'within 5%' }
            $lines.Add("| ``$($candidate.scene)`` | $metricName ($($after.unit)) | $([math]::Round([double]$before.p99, 3)) | $([math]::Round([double]$after.p99, 3)) | $($delta.ToString('+0.0;-0.0;0.0'))% | $signal |")
        }
    }
    $comparisonPath = Join-Path $runDirectory 'comparison.md'
    Set-Content -LiteralPath $comparisonPath -Value $lines -Encoding UTF8
    return $comparisonPath
}

try {
    if (-not $SkipBuild) {
        $openEditor = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction SilentlyContinue |
            Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf($projectRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 } |
            Select-Object -First 1
        if ($openEditor) {
            throw 'This project is open in Unity. Use Tools > Performance Audit inside the Editor, or close Unity and rerun this command.'
        }
        $resolvedUnity = Resolve-UnityPath -ExplicitPath $UnityPath
        $buildLog = Join-Path $runDirectory 'Build.log'
        $buildArguments = @(
            '-batchmode', '-nographics', '-quit',
            '-projectPath', ('"{0}"' -f $projectRoot),
            '-executeMethod', 'PerformanceAuditBuild.BuildFromCommandLine',
            '-performanceAuditBuildPath', ('"{0}"' -f $resolvedPlayerPath),
            '-logFile', ('"{0}"' -f $buildLog)
        )
        Write-Host 'Building Windows Development Player...' -ForegroundColor Cyan
        $build = Start-Process -FilePath $resolvedUnity -ArgumentList $buildArguments -Wait -PassThru
        if ($build.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $resolvedPlayerPath -PathType Leaf)) {
            throw "Development Player build failed with exit code $($build.ExitCode). See $buildLog"
        }
    } elseif (-not (Test-Path -LiteralPath $resolvedPlayerPath -PathType Leaf)) {
        throw "Development Player not found: $resolvedPlayerPath"
    }

    $previousRun = if ($CompareWithPrevious) { Find-PreviousRun } else { $null }
    $summaries = @()
    foreach ($scene in $selectedScenes) {
        $summaries += Invoke-PlayerCapture -Scene $scene
    }
    $runSummary = [ordered]@{
        schema = 'tcc-performance-audit-run/v1'
        utc = [DateTime]::UtcNow.ToString('O')
        projectRoot = $projectRoot
        scenes = $selectedScenes
        frames = $Frames
        warmupSeconds = $WarmupSeconds
        rawCapture = $RawCapture.IsPresent
        quality = $Quality
        resolution = "${Width}x${Height}"
        renderScale = $RenderScale
        fullscreen = $Fullscreen
        graphicsApi = $GraphicsApi
        results = $summaries
    }
    $runSummaryPath = Join-Path $runDirectory 'run-summary.json'
    $runSummary | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $runSummaryPath -Encoding UTF8
    Write-Host "Run summary: $runSummaryPath" -ForegroundColor Green

    if ($previousRun) {
        $comparisonPath = Write-Comparison -BaselineDirectory $previousRun -CandidateSummaries $summaries
        Write-Host "Comparison: $comparisonPath" -ForegroundColor Green
        Start-Process -FilePath $comparisonPath
    } elseif ($CompareWithPrevious) {
        Write-Host 'No previous run containing all selected scenes was found; this run is now the baseline.' -ForegroundColor Yellow
    }
} finally {
    if ($PauseAtEnd) { Read-Host 'Press Enter to close' | Out-Null }
}
