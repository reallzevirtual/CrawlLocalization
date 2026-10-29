param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\5.4.2f2\Editor\Unity.exe',
    [string]$PythonPath = 'python',
    [switch]$Combined
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $UnityPath)) { throw "Unity 5.4.2f2 editor not found: $UnityPath" }
& $PythonPath -c 'import UnityPy'
if ($LASTEXITCODE -ne 0) { throw 'Font bundle building requires Python with UnityPy.' }
$project = Join-Path $PSScriptRoot 'FontBundleProject'
$source = Join-Path $PSScriptRoot 'Fonts'
$import = Join-Path $project 'Assets\Fonts'
$bundleName = if ($Combined) { 'crawl-combined.bundle' } else { 'crawl-chinese-fonts.bundle' }
$edition = if ($Combined) { 'font-bundle-combined' } else { 'font-bundle' }
$output = Join-Path $PSScriptRoot ('artifacts/' + $edition)
if ($Combined) {
    & $PythonPath (Join-Path $PSScriptRoot 'tools/extend_chinese_fonts.py')
    if ($LASTEXITCODE -ne 0) { throw 'Combined font generation failed.' }
}
$inputFonts = if ($Combined) { Join-Path $source 'ChineseFallback/*.ttf' } else { Join-Path $source '*.ttf' }
New-Item -ItemType Directory -Force -Path $import,$output | Out-Null
Copy-Item -Path $inputFonts -Destination $import -Force

function Invoke-FontEditor([string]$Phase, [string]$Method, [string]$Marker) {
    $log = Join-Path $output "$Phase.log"
    Set-Content -LiteralPath $log -Value '' -Encoding utf8
    # Use the normal editor's supported command-line automation; the legacy
    # -batchmode activation path fails on this installation.
    $arguments = '-quit -projectPath "{0}" -logFile "{1}"' -f $project,$log
    if ($Method) { $arguments += ' -executeMethod ' + $Method }
    $previousCpuCapabilities = $env:OPENSSL_ia32cap
    $previousEdition = $env:CRAWL_COMBINED_FONTS
    try {
        # Old OpenSSL's SHA-1 acceleration crashes on this CPU; child process only.
        $env:OPENSSL_ia32cap = '0:0'
        $env:CRAWL_COMBINED_FONTS = if ($Combined) { '1' } else { '0' }
        $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    } finally { $env:OPENSSL_ia32cap = $previousCpuCapabilities; $env:CRAWL_COMBINED_FONTS = $previousEdition }
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
Invoke-FontEditor 'build' 'BuildCrawlFonts.Build' 'CRAWL_FONT_BUNDLE_RAW_OK'
$bundle = Join-Path $output $bundleName
if (!(Test-Path -LiteralPath $bundle)) { throw 'Unity did not create the font bundle.' }
& $PythonPath (Join-Path $project 'preserve_layout.py') $bundle
if ($LASTEXITCODE -ne 0) { throw 'Failed to preserve Crawl font layout.' }
# A new editor must read the final bytes, without using the build pipeline cache.
Invoke-FontEditor 'validate' 'BuildCrawlFonts.ValidateBuiltBundle' 'CRAWL_FONT_BUNDLE_OK'
Copy-Item -LiteralPath $bundle -Destination (Join-Path $source $bundleName) -Force
$mapping = Join-Path $project 'fonts.bundle.tsv'
if (!$Combined) { foreach ($locale in @('zh-Hans')) {
    Copy-Item -LiteralPath $mapping -Destination (Join-Path $PSScriptRoot "Languages\$locale\fonts.tsv") -Force
}
}
Write-Output "Font bundle ready: $(Join-Path $source $bundleName)"
