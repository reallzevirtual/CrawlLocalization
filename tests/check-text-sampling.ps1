param([string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\5.4.2f2\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $projectRoot 'FontBundleProject'
$output = Join-Path $projectRoot 'artifacts\text-sampling'
New-Item -ItemType Directory -Force -Path $output | Out-Null
Copy-Item -Path (Join-Path $projectRoot 'Fonts\*.ttf') -Destination (Join-Path $project 'Assets\Fonts') -Force
foreach ($phase in @('prepare','compare')) {
    $log = Join-Path $output "$phase.log"
    Set-Content -LiteralPath $log -Value '' -Encoding utf8
    $arguments = '-quit -projectPath "{0}" -logFile "{1}"' -f $project,$log
    if ($phase -eq 'compare') { $arguments += ' -executeMethod ValidateTextSampling.Run' }
    $previousCpuCapabilities = $env:OPENSSL_ia32cap
    try {
        $env:OPENSSL_ia32cap = '0:0'
        $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    } finally { $env:OPENSSL_ia32cap = $previousCpuCapabilities }
    Write-Output "Unity sampling $phase started (PID $($process.Id))."
    $deadline = (Get-Date).AddMinutes(5)
    while (!$process.WaitForExit(1000)) {
        if ((Get-Date) -gt $deadline) { Stop-Process -InputObject $process; throw 'Editor check timed out.' }
    }
    $process.Refresh()
    if ($process.ExitCode -ne 0 -or (Select-String -LiteralPath $log -Pattern 'error CS[0-9]+' -Quiet)) {
        Get-Content -LiteralPath $log -Tail 40
        throw "Editor sampling $phase failed; see $log"
    }
}
if (!(Select-String -LiteralPath (Join-Path $output 'compare.log') -SimpleMatch 'CRAWL_TEXT_SAMPLING_OK' -Quiet)) {
    throw 'Sampling comparison failed; inspect compare.log.'
}
Get-Content -LiteralPath (Join-Path $output 'geometry.txt')
Write-Output "Comparison image: $output\sampling-comparison.png"
