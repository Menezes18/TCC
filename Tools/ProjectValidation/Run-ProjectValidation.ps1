[CmdletBinding()]
param(
    [ValidateSet('EveryChange', 'PreRelease', 'Performance')][string]$Profile = 'PreRelease',
    [ValidateSet(2, 4, 6)][int[]]$PlayerCounts = @(2, 4, 6),
    [string]$UnityPath = '',
    [string]$PlayerPath = '',
    [string]$OutputRoot = '',
    [string]$Baseline = '',
    [ValidateRange(1, 100)][double]$RegressionTolerancePercent = 10,
    [switch]$FailOnRegression,
    [switch]$RequireSixPlayers,
    [switch]$SkipBuild,
    [switch]$SkipEditor,
    [switch]$PauseAtEnd
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$runsRoot = if ($OutputRoot) { [IO.Path]::GetFullPath($OutputRoot) } else { Join-Path $projectRoot 'Logs\ProjectValidationRuns' }
$runDirectory = Join-Path $runsRoot $stamp
$editorDirectory = Join-Path $runDirectory 'editor'
$resolvedPlayerPath = if ($PlayerPath) { [IO.Path]::GetFullPath($PlayerPath) } else { Join-Path $projectRoot 'Builds\ProjectValidationPlayer\TCC.exe' }
$shouldBuild = $Profile -ne 'EveryChange' -and -not $SkipBuild
$shouldRunPlayers = $Profile -ne 'EveryChange'
$shouldRunPerformance = $Profile -eq 'Performance'
New-Item -ItemType Directory -Force -Path $editorDirectory | Out-Null

function Resolve-UnityPath {
    if ($UnityPath) {
        $resolved = [IO.Path]::GetFullPath($UnityPath)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) { throw "Unity executable not found: $resolved" }
        return $resolved
    }
    $versionFile = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
    $match = Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion:\s*(.+)$' | Select-Object -First 1
    if (-not $match) { throw 'Could not read the Unity version from ProjectVersion.txt.' }
    $version = $match.Matches[0].Groups[1].Value.Trim()
    $candidate = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { throw "Unity $version not found at '$candidate'. Pass -UnityPath." }
    return $candidate
}

function Wait-ForFile {
    param([string]$Path, [int]$TimeoutSeconds, [string]$Description)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-Path -LiteralPath $Path -PathType Leaf) { return }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for $Description at '$Path'."
}

function Invoke-EditorValidation {
    $openEditor = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf($projectRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 } |
        Select-Object -First 1
    $requestPath = if ($openEditor) {
        Join-Path $projectRoot 'Temp\project-validation-request.json'
    } else {
        # Unity may clean project-local Temp files during cold startup. Keep the
        # explicit batch request in the OS temp directory until executeMethod reads it.
        Join-Path ([IO.Path]::GetTempPath()) ("tcc-project-validation-{0}.json" -f [Guid]::NewGuid().ToString('N'))
    }
    if (Test-Path -LiteralPath $requestPath) { throw "Another project validation request already exists: $requestPath" }
    $request = [ordered]@{
        resultDirectory = $editorDirectory
        buildPath = $resolvedPlayerPath
        buildPlayer = $shouldBuild
    }
    $request | ConvertTo-Json | Set-Content -LiteralPath $requestPath -Encoding UTF8

    if ($openEditor) {
        Write-Host "Using open Unity Editor process $($openEditor.ProcessId) for compile/tests/scenes/build..." -ForegroundColor Cyan
        Wait-ForFile -Path (Join-Path $editorDirectory 'editor-completed.txt') -TimeoutSeconds 1800 -Description 'Unity Editor validation'
    } else {
        $resolvedUnity = Resolve-UnityPath
        $logPath = Join-Path $editorDirectory 'Unity.log'
        $arguments = @(
            '-batchmode', '-nographics', '-quit',
            '-projectPath', ('"{0}"' -f $projectRoot),
            '-executeMethod', 'ProjectValidationRunner.RunFromCommandLine',
            '-projectValidationRequest', ('"{0}"' -f $requestPath),
            '-logFile', ('"{0}"' -f $logPath)
        )
        Write-Host 'Starting Unity batch validation...' -ForegroundColor Cyan
        try {
            $unity = Start-Process -FilePath $resolvedUnity -ArgumentList $arguments -Wait -PassThru
        } finally {
            if (Test-Path -LiteralPath $requestPath -PathType Leaf) { Remove-Item -LiteralPath $requestPath -Force }
        }
        if ($unity.ExitCode -ne 0) { throw "Unity validation exited with code $($unity.ExitCode). See $logPath" }
    }

    $summaryPath = Join-Path $editorDirectory 'editor-summary.json'
    if (-not (Test-Path -LiteralPath $summaryPath -PathType Leaf)) { throw "Editor summary was not produced: $summaryPath" }
    $summary = Get-Content -Raw -LiteralPath $summaryPath | ConvertFrom-Json
    return $summary
}

function Start-ValidationPlayer {
    param([string]$Role, [int]$ClientIndex, [int]$PlayerCount, [int]$Port, [string]$ClusterDirectory)
    $name = if ($Role -eq 'host') { 'host' } else { "client-$ClientIndex" }
    $output = Join-Path $ClusterDirectory $name
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    $log = Join-Path $output 'Player.log'
    $arguments = @(
        '-batchmode', '-nographics', '-logFile', ('"{0}"' -f $log),
        ('--project-validation-role=' + $Role),
        ('--project-validation-client-index=' + $ClientIndex),
        ('--project-validation-player-count=' + $PlayerCount),
        ('--project-validation-port=' + $Port),
        '--project-validation-scene=MN_Run',
        ('--project-validation-output=' + $output),
        '--project-validation-reconnect=true',
        '--project-validation-timeout=210'
    )
    $process = Start-Process -FilePath $resolvedPlayerPath -ArgumentList $arguments -PassThru
    return [pscustomobject]@{ Name = $name; Process = $process; Output = $output; Log = $log }
}

function Invoke-Cluster {
    param([int]$PlayerCount, [int]$Ordinal, [bool]$Required)
    $clusterDirectory = Join-Path $runDirectory "runtime\${PlayerCount}-players"
    New-Item -ItemType Directory -Force -Path $clusterDirectory | Out-Null
    $port = 17800 + $Ordinal
    Write-Host "Starting localhost KCP smoke with $PlayerCount players on port $port..." -ForegroundColor Cyan
    $processes = [Collections.Generic.List[object]]::new()
    try {
        $processes.Add((Start-ValidationPlayer -Role host -ClientIndex 0 -PlayerCount $PlayerCount -Port $port -ClusterDirectory $clusterDirectory))
        Wait-ForFile -Path $processes[0].Log -TimeoutSeconds 30 -Description 'host Player.log initialization'
        for ($index = 1; $index -lt $PlayerCount; $index++) {
            $processes.Add((Start-ValidationPlayer -Role client -ClientIndex $index -PlayerCount $PlayerCount -Port $port -ClusterDirectory $clusterDirectory))
        }

        $deadline = [DateTime]::UtcNow.AddSeconds(260)
        while ([DateTime]::UtcNow -lt $deadline) {
            $remaining = @($processes | Where-Object { -not (Test-Path -LiteralPath (Join-Path $_.Output 'completed.txt') -PathType Leaf) })
            if ($remaining.Count -eq 0) { break }
            foreach ($item in $remaining) {
                if ($item.Process.HasExited -and -not (Test-Path -LiteralPath (Join-Path $item.Output 'runtime-summary.json') -PathType Leaf)) {
                    throw "$($item.Name) exited early with code $($item.Process.ExitCode). See $($item.Log)"
                }
            }
            Start-Sleep -Milliseconds 500
        }
        $unfinished = @($processes | Where-Object { -not (Test-Path -LiteralPath (Join-Path $_.Output 'completed.txt') -PathType Leaf) })
        if ($unfinished.Count -gt 0) { throw "Runtime cluster timed out: $($unfinished.Name -join ', ')" }

        $summaries = @($processes | ForEach-Object { Get-Content -Raw -LiteralPath (Join-Path $_.Output 'runtime-summary.json') | ConvertFrom-Json })
        $failed = @($summaries | Where-Object { -not $_.succeeded })
        $error = if ($failed.Count -gt 0) { "$($failed.Count) process(es) failed the $PlayerCount-player smoke. See $clusterDirectory" } else { '' }
        return [pscustomobject]@{
            playerCount = $PlayerCount
            port = $port
            required = $Required
            classification = if ($Required) { 'required-release-gate' } else { 'optional-capacity-signal' }
            succeeded = $failed.Count -eq 0
            error = $error
            processes = $summaries
        }
    } finally {
        foreach ($item in $processes) {
            if (-not $item.Process.HasExited) { Stop-Process -Id $item.Process.Id -Force -ErrorAction SilentlyContinue }
        }
    }
}

function Compare-RuntimeBaseline {
    param([object[]]$Clusters)
    $signals = [Collections.Generic.List[object]]::new()
    if (-not $Baseline) { return $signals }
    $baselinePath = [IO.Path]::GetFullPath($Baseline)
    if (Test-Path -LiteralPath $baselinePath -PathType Container) { $baselinePath = Join-Path $baselinePath 'validation-summary.json' }
    if (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) { throw "Baseline summary not found: $baselinePath" }
    $baselineSummary = Get-Content -Raw -LiteralPath $baselinePath | ConvertFrom-Json
    $metrics = @('frameP95Milliseconds', 'peakManagedMemoryBytes', 'peakUnityAllocatedMemoryBytes', 'managedMemoryGrowthBytes', 'unityAllocatedMemoryGrowthBytes', 'sceneLoadSeconds', 'networkBytesIn', 'networkBytesOut')
    foreach ($cluster in $Clusters) {
        $beforeCluster = $baselineSummary.runtimeClusters | Where-Object playerCount -eq $cluster.playerCount | Select-Object -First 1
        if (-not $beforeCluster) { continue }
        foreach ($process in $cluster.processes) {
            $before = $beforeCluster.processes | Where-Object { $_.role -eq $process.role -and $_.clientIndex -eq $process.clientIndex } | Select-Object -First 1
            if (-not $before -or $before.processor -ne $process.processor -or $before.graphicsDevice -ne $process.graphicsDevice) { continue }
            foreach ($metric in $metrics) {
                $old = [double]$before.$metric
                $current = [double]$process.$metric
                if ($old -le 0) { continue }
                $delta = (($current - $old) / $old) * 100
                $status = if ($delta -gt $RegressionTolerancePercent) { 'regressed' } elseif ($delta -lt -$RegressionTolerancePercent) { 'improved' } else { 'within-tolerance' }
                $signals.Add([pscustomobject]@{ playerCount = $cluster.playerCount; process = "$($process.role)-$($process.clientIndex)"; metric = $metric; baseline = $old; current = $current; deltaPercent = $delta; status = $status })
            }
        }
    }
    return $signals
}

$exitCode = 0
try {
    $dirtyBaseline = @(& git -C $projectRoot status --porcelain 2>$null)
    $editorSummary = if ($SkipEditor) {
        [pscustomobject]@{ schema = 'tcc-project-validation-editor-skipped/v1'; succeeded = $true; steps = @(); detail = 'Editor stages explicitly skipped; reuse only a freshly validated player.' }
    } else { Invoke-EditorValidation }
    $clusters = @()
    $stageErrors = [Collections.Generic.List[string]]::new()
    if (-not $editorSummary.succeeded) { $stageErrors.Add('One or more Editor validation stages failed.') }
    $buildStep = $editorSummary.steps | Where-Object name -eq 'windows-development-build' | Select-Object -First 1
    $playerReady = ($buildStep.status -eq 'passed' -or $SkipBuild -or $SkipEditor) -and (Test-Path -LiteralPath $resolvedPlayerPath -PathType Leaf)
    if ($shouldRunPlayers -and $playerReady) {
        $ordinal = 0
        foreach ($count in @($PlayerCounts | Sort-Object -Unique)) {
            $required = $count -ne 6 -or $RequireSixPlayers
            try {
                $cluster = Invoke-Cluster -PlayerCount $count -Ordinal $ordinal -Required $required
                $clusters += $cluster
                if ($required -and -not $cluster.succeeded) { $stageErrors.Add("$count-player runtime smoke failed: $($cluster.error)") }
            } catch {
                if ($required) { $stageErrors.Add("$count-player runtime smoke failed: $($_.Exception.Message)") }
                $clusters += [pscustomobject]@{
                    playerCount = $count
                    port = 17800 + $ordinal
                    required = $required
                    classification = if ($required) { 'required-release-gate' } else { 'optional-capacity-signal' }
                    succeeded = $false
                    error = $_.Exception.ToString()
                    processes = @()
                }
            }
            $ordinal++
        }
    } elseif ($shouldRunPlayers) {
        $stageErrors.Add('Runtime matrices skipped because the Development Player build did not pass or its executable is missing.')
    }

    $regressions = @(Compare-RuntimeBaseline -Clusters $clusters)
    $performance = $null
    $performanceArtifact = ''
    if ($shouldRunPerformance -and $playerReady) {
        # Keep captures in one shared history so CompareWithPrevious can find a
        # genuinely prior run instead of an empty per-validation directory.
        $performanceRoot = Join-Path $runsRoot '_PerformanceCaptures'
        $performanceScript = Join-Path $projectRoot 'Tools\PerformanceAudit\Run-PerformanceAudit.ps1'
        try {
            & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $performanceScript -Scenes 'MN_Queda,MN_Run,MN_new_Rua' -SkipBuild -PlayerPath $resolvedPlayerPath -OutputRoot $performanceRoot -CompareWithPrevious
            if ($LASTEXITCODE -ne 0) { throw "Performance audit exited with code $LASTEXITCODE." }
            $latestPerformance = Get-ChildItem -LiteralPath $performanceRoot -Directory | Sort-Object Name -Descending | Select-Object -First 1
            if ($latestPerformance) {
                $performanceArtifact = Join-Path $latestPerformance.FullName 'run-summary.json'
                $performance = Get-Content -Raw -LiteralPath $performanceArtifact | ConvertFrom-Json
            }
        } catch {
            $stageErrors.Add("Performance audit failed: $($_.Exception.Message)")
        }
    } elseif ($shouldRunPerformance) {
        $stageErrors.Add('Performance captures skipped because the Development Player build did not pass.')
    }

    $summary = [ordered]@{
        schema = 'tcc-project-validation/v1'
        utc = [DateTime]::UtcNow.ToString('O')
        profile = $Profile
        projectRoot = $projectRoot
        succeeded = $stageErrors.Count -eq 0
        errors = $stageErrors
        dirtyWorktreeBaseline = $dirtyBaseline
        editor = $editorSummary
        runtimeClusters = $clusters
        regressionTolerancePercent = $RegressionTolerancePercent
        requireSixPlayers = [bool]$RequireSixPlayers
        regressionSignals = $regressions
        performance = $performance
        performanceArtifact = $performanceArtifact
        manualGate = (Join-Path $projectRoot 'Tools\ProjectValidation\MANUAL_RELEASE_CHECKLIST.md')
    }
    if ($FailOnRegression -and @($regressions | Where-Object status -eq 'regressed').Count -gt 0) {
        $summary.succeeded = $false
        $stageErrors.Add('One or more runtime metrics exceeded the selected regression tolerance.')
    }
    if (-not $summary.succeeded) { $exitCode = 2 }
    $summaryPath = Join-Path $runDirectory 'validation-summary.json'
    $summary | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $summaryPath -Encoding UTF8
    Write-Host "Project validation complete: $summaryPath" -ForegroundColor Green
    if ($regressions.Count -gt 0) {
        $regressions | Format-Table playerCount, process, metric, deltaPercent, status -AutoSize
    }
} catch {
    $exitCode = 2
    $failure = [ordered]@{ schema = 'tcc-project-validation-failure/v1'; utc = [DateTime]::UtcNow.ToString('O'); profile = $Profile; succeeded = $false; error = $_.Exception.ToString() }
    $failure | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $runDirectory 'validation-failure.json') -Encoding UTF8
    Write-Error $_
} finally {
    if ($PauseAtEnd) { Read-Host 'Press Enter to close' | Out-Null }
}
exit $exitCode
