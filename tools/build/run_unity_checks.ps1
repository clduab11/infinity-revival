param(
    [ValidateSet('Import', 'AuthorScene', 'AuthorInput', 'AuthorCombat', 'EditMode', 'PlayMode')]
    [string]$Mode = 'EditMode',
    [string]$EvidenceName = '',
    [ValidatePattern('^task-[0-9]{2}$')]
    [string]$EvidenceTask = 'task-03',
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
)

$ErrorActionPreference = 'Stop'
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe'
if (-not (Test-Path $unityEditor)) { throw 'Pinned Unity Editor is unavailable.' }
$versionFile = Get-Content (Join-Path $ProjectRoot 'ProjectSettings/ProjectVersion.txt') -Raw
if ($versionFile -notmatch '6000\.6\.0f1' -or $versionFile -notmatch 'f7f8ed4d1e24') {
    throw 'Project Editor pin differs from the approved baseline.'
}
$activeEditors = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" |
    Where-Object { $_.ExecutablePath -eq $unityEditor -and
        $_.CommandLine.IndexOf($ProjectRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 }
if ($activeEditors) { throw 'Close the canonical Unity Editor before running batch checks.' }
if (-not $EvidenceName) { $EvidenceName = $Mode.ToLowerInvariant() }
if ($EvidenceName -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Evidence name must be a simple filename stem.' }
$output = Join-Path $ProjectRoot "output/$EvidenceTask"
New-Item -ItemType Directory -Force -Path $output | Out-Null
$log = Join-Path $output "$EvidenceName.log"
$results = Join-Path $output "$EvidenceName.xml"
$config = Join-Path $ProjectRoot 'ProjectSettings/Packages/com.unity.pipeline/EditorPipelineConfig.json'
$hadConfig = Test-Path $config
$original = if ($hadConfig) { [IO.File]::ReadAllBytes($config) } else { $null }
$control = [Text.Encoding]::UTF8.GetBytes('{"m_AutoStart":false}')
$previousTestOutput = $env:PRAXEN_TEST_OUTPUT
$arguments = @('-batchmode', '-projectPath', "`"$ProjectRoot`"", '-logFile', "`"$log`"")
if ($Mode -in @('EditMode', 'PlayMode')) {
    $arguments += @('-runTests', '-testPlatform', $Mode, '-assemblyNames',
        "Praxen.Game.Tests.$Mode", '-testResults', "`"$results`"")
} else {
    $arguments += '-quit'
    if ($Mode -eq 'AuthorScene') {
        $arguments += @('-executeMethod', 'Praxen.Game.Editor.BootstrapSceneAuthoring.CreateScene')
    }
    if ($Mode -eq 'AuthorInput') {
        $arguments += @('-executeMethod', 'Praxen.Game.Editor.BootstrapSceneAuthoring.ConfigureInput')
    }
    if ($Mode -eq 'AuthorCombat') {
        $arguments += @('-executeMethod', 'Praxen.Game.Editor.GrayboxEncounterAuthoring.CreateScene')
    }
}

try {
    $env:PRAXEN_TEST_OUTPUT = $output
    New-Item -ItemType Directory -Force -Path (Split-Path $config) | Out-Null
    [IO.File]::WriteAllBytes($config, $control)
    $startedAt = [DateTime]::UtcNow
    $process = Start-Process -FilePath $unityEditor -ArgumentList $arguments -PassThru
    $process.WaitForExit()
    $process.Refresh()
    $code = $process.ExitCode
    Write-Output "$Mode Unity exit code: $code; log: $log"
    if ($code -ne 0) { exit $code }
    if ($Mode -in @('EditMode', 'PlayMode')) {
        if (-not (Test-Path $results)) { throw 'Unity did not write test results.' }
        if ((Get-Item $results).LastWriteTimeUtc -lt $startedAt) { throw 'Unity test receipt is stale.' }
        [xml]$receipt = Get-Content $results -Raw
        $run = $receipt.'test-run'
        if ([int]$run.total -le 0 -or [int]$run.failed -ne 0 -or
            [int]$run.passed -ne [int]$run.total) {
            throw "Incomplete or failing tests: total=$($run.total), passed=$($run.passed), failed=$($run.failed)"
        }
        Write-Output "PASS: $($run.passed)/$($run.total) $Mode tests"
    }
} finally {
    $env:PRAXEN_TEST_OUTPUT = $previousTestOutput
    if (Test-Path $config) {
        $current = [IO.File]::ReadAllBytes($config)
        if ([Convert]::ToBase64String($current) -ne [Convert]::ToBase64String($control)) {
            throw 'Pipeline control changed during verification; preserve it for manual review.'
        }
        if ($hadConfig) { [IO.File]::WriteAllBytes($config, $original) }
        else { Move-Item -Path $config -Destination (Join-Path $output "$EvidenceName-pipeline-control.json") -Force }
    }
}
