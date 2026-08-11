# File    : invoke.ps1
# Module  : AI Tooling
# Purpose : Chay RepoWise local voi telemetry bi tat va tool surface da gioi han.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('mcp', 'status', 'update', 'health', 'risk', 'doctor')]
    [string]$Command,

    [Parameter(Position = 1, ValueFromRemainingArguments = $true)]
    [string[]]$RepoWiseArgs
)

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$repoWiseExe = Join-Path $repoRoot '.local-tools\repowise\Scripts\repowise.exe'
$mcpTools = 'get_overview,get_context,get_risk,get_change_risk,get_why,get_health'

if (-not (Test-Path -LiteralPath $repoWiseExe)) {
    throw "Chua cai RepoWise local. Xem docs/ai/REPOWISE_INTEGRATION.md."
}

# SECURITY: Ep tat telemetry do CLI 0.34.0 co the bao status khong nhat quan.
$env:REPOWISE_TELEMETRY_DISABLED = '1'

Push-Location $repoRoot
try {
    switch ($Command) {
        'mcp' {
            & $repoWiseExe mcp $repoRoot --transport stdio --tools $mcpTools
        }
        'update' {
            & $repoWiseExe update $repoRoot --index-only --no-workspace --no-cost-tracking --no-agents @RepoWiseArgs
        }
        default {
            & $repoWiseExe $Command @RepoWiseArgs
        }
    }

    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

exit $exitCode

