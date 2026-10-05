[CmdletBinding()]
param([Parameter(Mandatory)][string]$RunId, [Parameter(Mandatory)][string]$SummaryJson, [string]$DataRoot = (Join-Path $HOME ".agent-orchestrator"))
$ErrorActionPreference = "Stop"
$summary = $SummaryJson | ConvertFrom-Json
if (-not $summary.task_id -or -not $summary.state) { throw "SummaryJson must include task_id and state." }
$runDirectory = Join-Path (Join-Path $DataRoot "runs") $RunId
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null
$summary | Add-Member -NotePropertyName generated_at -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString("o")) -Force
$summary | Add-Member -NotePropertyName run_id -NotePropertyValue $RunId -Force
[IO.File]::WriteAllText((Join-Path $runDirectory "summary.json"), ($summary | ConvertTo-Json -Depth 12), [Text.UTF8Encoding]::new($false))
