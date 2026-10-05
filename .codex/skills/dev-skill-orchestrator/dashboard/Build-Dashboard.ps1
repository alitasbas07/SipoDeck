[CmdletBinding()]
param(
    [string]$DataRoot = (Join-Path $HOME '.agent-orchestrator'),
    [string]$OutputPath,
    [switch]$Open
)

$ErrorActionPreference = 'Stop'
if (-not $OutputPath) { $OutputPath = Join-Path $DataRoot 'dashboard\output\dashboard.html' }
$runs = @()
$benchmarks = @()
$warnings = [System.Collections.Generic.List[string]]::new()
$runsRoot = Join-Path $DataRoot 'runs'
if (Test-Path -LiteralPath $runsRoot) {
    foreach ($directory in Get-ChildItem -LiteralPath $runsRoot -Directory | Sort-Object Name) {
        $events = @()
        $summary = $null
        $eventsPath = Join-Path $directory.FullName 'events.jsonl'
        if (Test-Path -LiteralPath $eventsPath) {
            $lineNumber = 0
            foreach ($line in Get-Content -LiteralPath $eventsPath -Encoding UTF8) {
                $lineNumber++
                if (-not $line.Trim()) { continue }
                try { $events += ($line | ConvertFrom-Json -ErrorAction Stop) }
                catch { $warnings.Add("$($directory.Name)/events.jsonl line ${lineNumber}: invalid JSON skipped") }
            }
        }
        $summaryPath = Join-Path $directory.FullName 'summary.json'
        if (Test-Path -LiteralPath $summaryPath) {
            try { $summary = Get-Content -LiteralPath $summaryPath -Raw -Encoding UTF8 | ConvertFrom-Json }
            catch { $warnings.Add("$($directory.Name)/summary.json: invalid JSON skipped") }
        }
        $runs += [ordered]@{ run_id = $directory.Name; summary = $summary; events = @($events) }
    }
}
$benchmarkRoot = Join-Path $DataRoot 'benchmarks'
if (Test-Path -LiteralPath $benchmarkRoot) {
    foreach ($file in Get-ChildItem -LiteralPath $benchmarkRoot -File -Filter '*.json' -Recurse | Sort-Object FullName) {
        try {
            $record = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
            if (-not $record.case_id -or $record.variant -notin @('baseline', 'devskill')) {
                throw 'case_id and variant (baseline/devskill) are required'
            }
            $benchmarks += $record
        } catch { $warnings.Add("Benchmark $($file.Name): invalid record skipped") }
    }
}
$payload = [ordered]@{
    generated_at = [DateTimeOffset]::UtcNow.ToString('o')
    runs = @($runs)
    benchmarks = @($benchmarks)
    warnings = @($warnings.ToArray())
}
$json = ConvertTo-Json -InputObject $payload -Depth 40 -Compress
$encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))
$template = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'dashboard-template.html') -Raw -Encoding UTF8
$html = $template.Replace('__DASHBOARD_DATA_BASE64__', $encoded)
$resolvedOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $resolvedOutput) | Out-Null
[IO.File]::WriteAllText($resolvedOutput, $html, [Text.UTF8Encoding]::new($false))
Write-Host "Dashboard: $resolvedOutput"
foreach ($warning in $warnings) { Write-Warning $warning }
if ($Open) { Start-Process -FilePath $resolvedOutput }

