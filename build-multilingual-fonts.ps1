param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\5.4.2f2\Editor\Unity.exe',
    [string]$PythonPath = 'python'
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $UnityPath)) { throw "Unity 5.4.2f2 editor not found: $UnityPath" }
& $PythonPath -c 'import UnityPy, fontTools'
if ($LASTEXITCODE -ne 0) { throw 'Font bundle building requires Python with UnityPy and fontTools.' }
$project = Join-Path $PSScriptRoot 'artifacts\multilingual-font-project'
$source = Join-Path $PSScriptRoot 'Fonts\Multilingual\Generated'
$import = Join-Path $project 'Assets\Fonts'
$output = Join-Path $PSScriptRoot 'artifacts\universal-fonts'
New-Item -ItemType Directory -Force -Path $import,$output | Out-Null
& $PythonPath (Join-Path $PSScriptRoot 'tools/merge_multilingual_fonts.py')
if ($LASTEXITCODE -ne 0) { throw 'Font merging failed.' }
Copy-Item -Path (Join-Path $source '*.ttf') -Destination $import -Force
$editor = Join-Path $project 'Assets/Editor'
$original = Join-Path $project 'Assets/Original'
$settings = Join-Path $project 'ProjectSettings'
New-Item -ItemType Directory -Force -Path $editor,$original,$settings | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'tools/BuildMultilingualFonts.cs') $editor -Force
Copy-Item (Join-Path $PSScriptRoot 'Fonts/Multilingual/Original/*.ttf') $original -Force
Copy-Item (Join-Path $PSScriptRoot 'FontBundleProject/ProjectSettings/ProjectVersion.txt') $settings -Force

function Invoke-FontEditor([string]$Phase, [string]$Method, [string]$Marker) {
    $log = Join-Path $output "$Phase.log"
    Set-Content -LiteralPath $log -Value '' -Encoding utf8
    # Use the normal editor's supported command-line automation; the legacy
    # -batchmode activation path fails on this installation.
    $arguments = '-quit -projectPath "{0}" -logFile "{1}"' -f $project,$log
    if ($Method) { $arguments += ' -executeMethod ' + $Method }
    $previousCpuCapabilities = $env:OPENSSL_ia32cap
    $previousFontRoot = $env:CRAWL_FONT_ROOT
    try {
        # Old OpenSSL's SHA-1 acceleration crashes on this CPU; child process only.
        $env:OPENSSL_ia32cap = '0:0'
        $env:CRAWL_FONT_ROOT = $PSScriptRoot
        $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    } finally { $env:OPENSSL_ia32cap = $previousCpuCapabilities; $env:CRAWL_FONT_ROOT = $previousFontRoot }
    Write-Output "Unity font $Phase started (PID $($process.Id)); log: $log"
    $deadline = (Get-Date).AddMinutes(15)
    while (!$process.WaitForExit(1000)) {
        if ((Get-Date) -gt $deadline) {
            Stop-Process -InputObject $process
            throw "Unity font $Phase timed out; see $log"
        }
    }
    $process.Refresh()
    $failed = $process.ExitCode -ne 0 -or (Select-String -LiteralPath $log -Pattern 'error CS[0-9]+' -Quiet)
    if ($Marker -and !(Select-String -LiteralPath $log -SimpleMatch $Marker -Quiet)) { $failed = $true }
    if ($failed) {
        Get-Content -LiteralPath $log -Tail 55
        throw "Unity font $Phase failed (exit $($process.ExitCode)); see $log"
    }
}

# Unity 5.4 can execute the previously compiled method before reloading changed
# editor scripts. Finish import/compilation in its own process first.
Invoke-FontEditor 'prepare' '' ''
Invoke-FontEditor 'build' 'BuildMultilingualFonts.Build' 'CRAWL_FONT_BUNDLE_RAW_OK'
$bundle = Join-Path $output 'crawl-multilingual.bundle'
if (!(Test-Path -LiteralPath $bundle)) { throw 'Unity did not create the font bundle.' }
& $PythonPath (Join-Path $PSScriptRoot 'FontBundleProject/preserve_layout.py') $bundle
if ($LASTEXITCODE -ne 0) { throw 'Failed to preserve Crawl font layout.' }
# A new editor must read the final bytes, without using the build pipeline cache.
Invoke-FontEditor 'validate' 'BuildMultilingualFonts.ValidateBuiltBundle' 'CRAWL_FONT_BUNDLE_OK'
$destination = Join-Path $PSScriptRoot 'Fonts/crawl-multilingual.bundle'
Copy-Item -LiteralPath $bundle -Destination $destination -Force
Write-Output "Font bundle ready: $destination"
