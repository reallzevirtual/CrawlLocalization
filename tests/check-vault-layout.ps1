param([string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\5.4.2f2\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $projectRoot 'FontBundleProject'
# Restore ignored font inputs before Unity imports tracked font metadata.
Copy-Item -Path (Join-Path $projectRoot 'Fonts\*.ttf') -Destination (Join-Path $project 'Assets\Fonts') -Force
$output = Join-Path $projectRoot 'artifacts\vault-layout'
$generated = Join-Path $project 'Assets\Editor\ValidationGenerated'
New-Item -ItemType Directory -Force -Path $output,$generated | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\ChineseTextWrapper.cs') -Destination $generated -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ValidateVaultLayout.cs') -Destination $generated -Force
foreach ($phase in @('prepare','validate')) {
    $log = Join-Path $output "$phase.log"
    Set-Content -LiteralPath $log -Value '' -Encoding utf8
    $arguments = '-quit -projectPath "{0}" -logFile "{1}"' -f $project,$log
    if ($phase -eq 'validate') { $arguments += ' -executeMethod ValidateVaultLayout.Run' }
    $previousCpuCapabilities = $env:OPENSSL_ia32cap
    try {
        $env:OPENSSL_ia32cap = '0:0'
        $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    } finally { $env:OPENSSL_ia32cap = $previousCpuCapabilities }
    Write-Output "Unity Vault $phase started (PID $($process.Id))."
    $deadline = (Get-Date).AddMinutes(5)
    while (!$process.WaitForExit(1000)) {
        if ((Get-Date) -gt $deadline) { Stop-Process -InputObject $process; throw 'Editor check timed out.' }
    }
    $process.Refresh()
    if ($process.ExitCode -ne 0 -or (Select-String -LiteralPath $log -Pattern 'error CS[0-9]+' -Quiet)) {
        Get-Content -LiteralPath $log -Tail 40
        throw "Editor Vault $phase failed; see $log"
    }
}
if (!(Select-String -LiteralPath (Join-Path $output 'validate.log') -SimpleMatch 'CRAWL_VAULT_LAYOUT_OK' -Quiet)) {
    throw 'Vault layout check failed; inspect validate.log.'
}
Get-Content -LiteralPath (Join-Path $output 'geometry.txt')
