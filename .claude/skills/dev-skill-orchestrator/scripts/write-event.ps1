[CmdletBinding()]
param([Parameter(Mandatory)][string]$RunId, [Parameter(Mandatory)][string]$EventJson, [string]$DataRoot = (Join-Path $HOME ".agent-orchestrator"))
$ErrorActionPreference = "Stop"
$event = $EventJson | ConvertFrom-Json
if (-not $event.event -or -not $event.task_id) { throw "EventJson must include event and task_id." }
$runDirectory = Join-Path (Join-Path $DataRoot "runs") $RunId
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null
$event | Add-Member -NotePropertyName timestamp -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString("o")) -Force
$event | Add-Member -NotePropertyName run_id -NotePropertyValue $RunId -Force
$line = ($event | ConvertTo-Json -Depth 12 -Compress) + [Environment]::NewLine
[IO.File]::AppendAllText((Join-Path $runDirectory "events.jsonl"), $line, [Text.UTF8Encoding]::new($false))
