# File    : init-index.ps1
# Module  : AI Tooling
# Purpose : Tao lai index RepoWise local, khong LLM, hook hoac file agent tu sinh.

[CmdletBinding()]
param()

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$repoWiseExe = Join-Path $repoRoot '.local-tools\repowise\Scripts\repowise.exe'

if (-not (Test-Path -LiteralPath $repoWiseExe)) {
    throw "Chua cai RepoWise local. Xem docs/ai/REPOWISE_INTEGRATION.md."
}

$env:REPOWISE_TELEMETRY_DISABLED = '1'

# SECURITY: Index-only khong can provider; bo key khoi rieng process nay de tranh auto-detect.
@(
    'ANTHROPIC_API_KEY',
    'OPENAI_API_KEY',
    'GEMINI_API_KEY',
    'GOOGLE_API_KEY',
    'OPENROUTER_API_KEY',
    'DEEPSEEK_API_KEY',
    'KIMI_API_KEY',
    'LITELLM_API_KEY'
) | ForEach-Object { Remove-Item -Path "Env:$_" -ErrorAction SilentlyContinue }

$excludePatterns = @(
    'bin/',
    'obj/',
    'publish/',
    'artifacts/',
    'TestResults/',
    'coverage/',
    '.gitnexus/',
    '.codex/debug-nav/',
    'src/frontend/source_can_update/',
    'docs/reference/'
)

$arguments = @(
    'init',
    $repoRoot,
    '--index-only',
    '--yes',
    '--no-claude-md',
    '--no-agents',
    '--no-codex',
    '--no-distill-hook',
    '--no-workspace',
    '--no-onboarding',
    '--no-harvest-decisions',
    '--no-cost-tracking'
)

foreach ($pattern in $excludePatterns) {
    $arguments += @('-x', $pattern)
}

Push-Location $repoRoot
try {
    & $repoWiseExe @arguments
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

exit $exitCode

