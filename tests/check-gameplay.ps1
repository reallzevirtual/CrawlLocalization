param([string]$GamePath = 'D:\SteamLibrary\steamapps\common\Crawl', [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\5.4.2f2\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $projectRoot 'FontBundleProject'
# Restore ignored font inputs before Unity imports tracked font metadata.
Copy-Item -Path (Join-Path $projectRoot 'Fonts\*.ttf') -Destination (Join-Path $project 'Assets\Fonts') -Force
$output = Join-Path $projectRoot 'artifacts\gameplay'
$generated = Join-Path $project 'Assets\Editor\ValidationGenerated'
New-Item -ItemType Directory -Force -Path $output,$generated | Out-Null
foreach ($source in @('ChineseTextWrapper.cs', 'LanguagePack.cs', 'NumericTranslation.cs', 'FontService.cs', 'MenuTextPolicy.cs', 'MenuTextMeshes.cs', 'MenuPatches.cs', 'ChapterTitlePatch.cs', 'HeroStatsPatches.cs', 'GameplayPatches.cs')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('src\' + $source)) -Destination $generated -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ValidateGameplay.cs') -Destination $generated -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ChapterRuntimeStubs.cs') -Destination $generated -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'HeroStatsRuntimeStubs.cs') -Destination $generated -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'GameplayRuntimeStubs.cs') -Destination $generated -Force
& python (Join-Path $projectRoot 'tools\export_gameplay_texts.py') --game-path $GamePath --output (Join-Path $output 'catalog.json')
if ($LASTEXITCODE -ne 0) { throw 'Gameplay asset export failed.' }
foreach ($phase in @('prepare','validate')) {
    $log = Join-Path $output "$phase.log"
    Set-Content -LiteralPath $log -Value '' -Encoding utf8
    $arguments = '-quit -projectPath "{0}" -logFile "{1}"' -f $project,$log
    if ($phase -eq 'validate') { $arguments += ' -executeMethod ValidateGameplay.Run' }
    $previousCpuCapabilities = $env:OPENSSL_ia32cap
    try {
        $env:OPENSSL_ia32cap = '0:0'
        $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    } finally { $env:OPENSSL_ia32cap = $previousCpuCapabilities }
    Write-Output "Unity Gameplay $phase started (PID $($process.Id))."
    $deadline = (Get-Date).AddMinutes(5)
    while (!$process.WaitForExit(1000)) {
        if ((Get-Date) -gt $deadline) { Stop-Process -InputObject $process; throw 'Editor check timed out.' }
    }
    $process.Refresh()
    if ($process.ExitCode -ne 0 -or (Select-String -LiteralPath $log -Pattern 'error CS[0-9]+' -Quiet)) {
        Get-Content -LiteralPath $log -Tail 40
        throw "Editor Gameplay $phase failed; see $log"
    }
}
if (!(Select-String -LiteralPath (Join-Path $output 'validate.log') -SimpleMatch 'CRAWL_GAMEPLAY_OK' -Quiet)) {
    throw 'Gameplay display check failed; inspect validate.log.'
}
Get-Content -LiteralPath (Join-Path $output 'result.txt')
