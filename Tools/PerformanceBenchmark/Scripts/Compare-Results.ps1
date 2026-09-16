[CmdletBinding()]
param(
    [string]$ResultsRoot = '',
    [string]$BaselineFolder = ''
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot '..'))
if (-not $ResultsRoot) { $ResultsRoot = Join-Path $packageRoot 'Results' }
$ResultsRoot = [IO.Path]::GetFullPath($ResultsRoot)
if (-not (Test-Path -LiteralPath $ResultsRoot -PathType Container)) { throw "Results directory not found: $ResultsRoot" }

function Get-Median([object[]]$Values) {
    $numbers = @($Values | Where-Object { $_ -ne $null -and $_ -ne '' -and $_ -ne 'N/A' } | ForEach-Object { [double]$_ } | Sort-Object)
    if ($numbers.Count -eq 0) { return $null }
    if (($numbers.Count % 2) -eq 1) { return $numbers[[math]::Floor($numbers.Count / 2)] }
    return ($numbers[$numbers.Count / 2 - 1] + $numbers[$numbers.Count / 2]) / 2.0
}

function Format-Value($Value, [int]$Digits = 2) {
    if ($null -eq $Value) { return 'N/A' }
    return [math]::Round([double]$Value, $Digits).ToString([Globalization.CultureInfo]::InvariantCulture)
}

$folders = @(Get-ChildItem -LiteralPath $ResultsRoot -Directory | Where-Object {
    (Test-Path -LiteralPath (Join-Path $_.FullName 'benchmark.csv')) -and
    (Test-Path -LiteralPath (Join-Path $_.FullName 'hardware.json'))
})
if ($folders.Count -eq 0) { throw "No result folders containing hardware.json and benchmark.csv were found under $ResultsRoot" }

$rows = [Collections.Generic.List[object]]::new()
foreach ($folder in $folders) {
    $hardware = Get-Content -Raw -LiteralPath (Join-Path $folder.FullName 'hardware.json') | ConvertFrom-Json
    $data = @(Import-Csv -LiteralPath (Join-Path $folder.FullName 'benchmark.csv') | Where-Object { $_.lane -eq 'Release' })
    if ($data.Count -eq 0) { continue }
    function Scene-P95([string]$Scene) { return Get-Median @($data | Where-Object { $_.scene -eq $Scene -and [int]$_.players -eq 1 } | ForEach-Object frameP95Ms) }
    $four = Get-Median @($data | Where-Object { [int]$_.players -eq 4 -and $_.scene -eq 'MN_Run' -and $_.role -eq 'host' } | ForEach-Object frameP95Ms)
    $peak = @($data | Where-Object { $_.unityAllocatedBytes -ne 'N/A' -and $_.unityAllocatedBytes } | ForEach-Object { [double]$_.unityAllocatedBytes } | Measure-Object -Maximum).Maximum
    $rows.Add([pscustomobject][ordered]@{
        ResultFolder=$folder.Name; Machine=$hardware.machine; CPU=$hardware.cpu; GPU=$hardware.gpu; RAM_GB=$hardware.totalRamGB
        Run_P95_ms=Scene-P95 'MN_Run'; Rua_P95_ms=Scene-P95 'MN_new_Rua'; Queda_P95_ms=Scene-P95 'MN_Queda'
        Batata_P95_ms=Scene-P95 'MN_BatataQ'; Memoria_P95_ms=Scene-P95 'MN_Memoria'; Sumo_P95_ms=Scene-P95 'MN_Sumo'
        FourPlayerHost_P95_ms=$four; PeakUnityAllocatedBytes=$peak
    })
}
if ($rows.Count -eq 0) { throw 'The result folders contained no Release-lane benchmark rows.' }

$baseline = $null
if ($BaselineFolder) {
    $resolvedBaseline = [IO.Path]::GetFullPath($BaselineFolder)
    $baseline = $rows | Where-Object { [IO.Path]::GetFullPath((Join-Path $ResultsRoot $_.ResultFolder)) -eq $resolvedBaseline } | Select-Object -First 1
    if (-not $baseline) { throw "Baseline folder is not one of the parsed result sets: $resolvedBaseline" }
} else {
    $baseline = $rows | Where-Object { $_.GPU -match 'RTX\s*3060' } | Select-Object -First 1
}

$metricNames = @('Run_P95_ms','Rua_P95_ms','Queda_P95_ms','FourPlayerHost_P95_ms','PeakUnityAllocatedBytes')
$export = foreach ($row in $rows) {
    $item = [ordered]@{}
    foreach ($property in $row.PSObject.Properties) { $item[$property.Name] = $property.Value }
    foreach ($metric in $metricNames) {
        $percentName = $metric + '_vs_baseline_percent'
        if ($baseline -and $null -ne $row.$metric -and $null -ne $baseline.$metric -and [double]$baseline.$metric -ne 0) {
            $item[$percentName] = [math]::Round((([double]$row.$metric / [double]$baseline.$metric) - 1.0) * 100.0, 2)
        } else { $item[$percentName] = 'N/A' }
    }
    [pscustomobject]$item
}

$csvPath = Join-Path $ResultsRoot 'Comparison.csv'
$export | Export-Csv -NoTypeInformation -Encoding UTF8 -LiteralPath $csvPath
$lines = [Collections.Generic.List[string]]::new()
$lines.Add('# TCC Cross-Machine Performance Comparison'); $lines.Add('')
if ($baseline) { $lines.Add("Baseline: $($baseline.Machine) / $($baseline.GPU) / $($baseline.ResultFolder)") } else { $lines.Add('Baseline: not supplied and no RTX 3060 result was detected. Percentage columns are N/A.') }
$lines.Add(''); $lines.Add('Lower frame-time percentages are faster. Numbers are shown without a good/bad classification because no explicit performance budget was supplied.'); $lines.Add('')
$lines.Add('| Machine | GPU | Run p95 ms | Rua p95 ms | Queda p95 ms | 4P host p95 ms | Peak Unity allocated | Run vs baseline |')
$lines.Add('|---|---|---:|---:|---:|---:|---:|---:|')
foreach ($row in $export) {
    $pct = $row.Run_P95_ms_vs_baseline_percent
    $pctText = if ($pct -eq 'N/A') { 'N/A' } else { (Format-Value $pct) + '%' }
    $lines.Add("| $($row.Machine) | $($row.GPU) | $(Format-Value $row.Run_P95_ms) | $(Format-Value $row.Rua_P95_ms) | $(Format-Value $row.Queda_P95_ms) | $(Format-Value $row.FourPlayerHost_P95_ms) | $(Format-Value $row.PeakUnityAllocatedBytes 0) | $pctText |")
}
$lines.Add(''); $lines.Add('Comparison.csv contains the remaining scene metrics and percentage differences for every selected metric.')
$mdPath = Join-Path $ResultsRoot 'Comparison.md'
$lines | Set-Content -LiteralPath $mdPath -Encoding UTF8
Write-Host "Created $mdPath" -ForegroundColor Green
Write-Host "Created $csvPath" -ForegroundColor Green
