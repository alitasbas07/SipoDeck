[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$testRoot = Join-Path (Split-Path -Parent $PSScriptRoot) ('.dev-skill-test\dashboard-' + [guid]::NewGuid().ToString('N'))
$emptyRoot = Join-Path $testRoot 'empty'
$fixtureRoot = Join-Path $testRoot 'fixtures'
$output = Join-Path $testRoot 'fixture.html'
$utf8 = [Text.UTF8Encoding]::new($false)

function Read-Payload([string]$Path) {
    $html = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $match = [regex]::Match($html, '<script id="dashboard-data" type="text/plain">([^<]+)</script>')
    if (-not $match.Success) { throw 'Missing embedded dashboard data' }
    $decoded = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($match.Groups[1].Value))
    return ($decoded | ConvertFrom-Json)
}

& (Join-Path $PSScriptRoot 'Build-Dashboard.ps1') -DataRoot $emptyRoot -OutputPath (Join-Path $testRoot 'empty.html')
$empty = Read-Payload (Join-Path $testRoot 'empty.html')
if ($empty.runs.Count -ne 0 -or $empty.benchmarks.Count -ne 0) { throw 'Empty dataset failed' }

$runRoot = Join-Path $fixtureRoot 'runs\test-run'
$benchmarkRoot = Join-Path $fixtureRoot 'benchmarks'
New-Item -ItemType Directory -Force -Path $runRoot, $benchmarkRoot | Out-Null
$hostileText = '</script><script>window.injected=true</script>'
$event = @{ task_id='task-test'; event='correction_started'; state='TESTING'; actor='tester'; data=@{ report=$hostileText } } | ConvertTo-Json -Compress
[IO.File]::WriteAllText((Join-Path $runRoot 'events.jsonl'), $event + "`ninvalid-json`n", $utf8)
$taskName = [string][char]0x00D6 + 'deme testi'
$summary = @{ task_id='task-test'; task_name=$taskName; state='READY_FOR_ACCEPTANCE'; metrics=@{ total_tokens=1234; duration_seconds=30 } } | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $runRoot 'summary.json'), $summary, $utf8)
foreach ($variant in @('baseline','devskill')) {
    $record = @{ case_id='test-case'; repeat=1; variant=$variant; metrics=@{ total_tokens=1234; quality_score=80; test_status='PASS' } } | ConvertTo-Json
    [IO.File]::WriteAllText((Join-Path $benchmarkRoot ($variant + '.json')), $record, $utf8)
}
& (Join-Path $PSScriptRoot 'Build-Dashboard.ps1') -DataRoot $fixtureRoot -OutputPath $output
$payload = Read-Payload $output
if ($payload.runs.Count -ne 1 -or $payload.benchmarks.Count -ne 2 -or $payload.warnings.Count -ne 1) { throw 'Record parsing failed' }
if ($payload.runs[0].summary.task_name -ne $taskName) { throw 'UTF-8 roundtrip failed' }
if ($payload.runs[0].events[0].data.report -ne $hostileText) { throw 'Evidence roundtrip failed' }
if ((Get-Content -LiteralPath $output -Raw).Contains($hostileText)) { throw 'Unsafe raw HTML injection' }
if ($null -ne $payload.benchmarks[0].metrics.duration_seconds) { throw 'Unknown metric must remain absent' }
Write-Host 'Dashboard tests passed: empty input, JSONL recovery, benchmarks, UTF-8 and safe embedding.'
Write-Host "Fixture preview: $output"
