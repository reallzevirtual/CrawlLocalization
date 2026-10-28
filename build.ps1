param(
    [string]$GamePath = 'D:\SteamLibrary\steamapps\common\Crawl',
    [switch]$Install,
    [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
if (!$SkipTests) {
    & pwsh -NoProfile -File (Join-Path $projectRoot 'tests\test.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Offline tests failed.' }
}
$managed = Join-Path $GamePath 'Crawl_Data\Managed'
$core = Join-Path $GamePath 'BepInEx\core'
$output = Join-Path $projectRoot 'artifacts\CrawlLocalization-0.6.0'
New-Item -ItemType Directory -Force -Path $output | Out-Null

# Compile against the game's own Mono 3.5 reference assemblies, not a modern .NET runtime.
# The installed SDK supplies only the C# compiler; no NuGet download is required.
$sdkLine = @(& dotnet --list-sdks)[-1]
if ($sdkLine -notmatch '^([^ ]+) \[(.+)\]$') { throw 'No .NET SDK found.' }
$compiler = Join-Path (Join-Path $Matches[2] $Matches[1]) 'Roslyn\bincore\csc.dll'
$references = @(
    (Join-Path $managed 'mscorlib.dll'),
    (Join-Path $managed 'System.dll'),
    (Join-Path $managed 'System.Core.dll'),
    (Join-Path $managed 'UnityEngine.dll'),
    (Join-Path $managed 'Assembly-CSharp.dll'),
    (Join-Path $managed 'Assembly-CSharp-firstpass.dll'),
    (Join-Path $managed 'ex2D.Runtime.dll'),
    (Join-Path $core 'BepInEx.dll'),
    (Join-Path $core '0Harmony.dll')
)
$arguments = @('/nologo', '/noconfig', '/nostdlib+', '/target:library', '/langversion:7.3', '/optimize+', '/deterministic+', '/utf8output', ('/out:' + (Join-Path $output 'CrawlLocalization.dll')))
foreach ($reference in $references) {
    if (!(Test-Path -LiteralPath $reference)) { throw "Missing reference: $reference" }
    $arguments += '/reference:' + $reference
}
$arguments += @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
& dotnet $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $LASTEXITCODE" }
foreach ($folder in @('Languages')) {
    $target = Join-Path $output $folder
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Copy-Item -Path (Join-Path $projectRoot "$folder\*") -Destination $target -Recurse -Force
}
# Ship only font files referenced by the language packs. Source TTFs remain in
# the project for rebuilding, but are unnecessary when the packs use a bundle.
$fontFiles = @{}
$rootPrefix = [IO.Path]::GetFullPath($projectRoot).TrimEnd('\') + '\'
foreach ($table in Get-ChildItem -Path (Join-Path $projectRoot 'Languages\*\fonts.tsv')) {
    foreach ($line in Get-Content -LiteralPath $table.FullName -Encoding utf8) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
        $columns = $line.Split("`t")
        if ($columns.Length -ne 5) { throw "Malformed font mapping: $($table.FullName)" }
        if ($columns[1] -eq 'system') { continue }
        $relative = $columns[2]
        $sourceFont = [IO.Path]::GetFullPath((Join-Path $projectRoot $relative))
        if ([IO.Path]::IsPathRooted($relative) -or !$sourceFont.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Invalid font path: $relative" }
        $fontFiles[$relative] = $sourceFont
    }
}
foreach ($relative in $fontFiles.Keys) {
    $fontDestination = Join-Path $output $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $fontDestination -Parent) | Out-Null
    Copy-Item -LiteralPath $fontFiles[$relative] -Destination $fontDestination -Force
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'CHANGELOG.md') -Destination $output -Force
$archive = Join-Path $projectRoot 'artifacts\CrawlLocalization-0.6.0.zip'
$packageFiles = @(Get-ChildItem -LiteralPath $output | ForEach-Object FullName)
Compress-Archive -Path $packageFiles -DestinationPath $archive -Force
Write-Output "Built: $output"
Write-Output "Packaged: $archive (extract into BepInEx/plugins/CrawlLocalization)"
if ($Install) {
    if (Get-Process -Name Crawl -ErrorAction SilentlyContinue) { throw 'Close Crawl before installing the plugin.' }
    $destination = Join-Path $GamePath 'BepInEx\plugins\CrawlLocalization'
    if (Test-Path -LiteralPath $destination) {
        $backupRoot = Join-Path $projectRoot 'artifacts\backups'
        New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
        $backup = Join-Path $backupRoot ('CrawlLocalization-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
        Copy-Item -LiteralPath $destination -Destination $backup -Recurse
        Write-Output "Previous plugin backed up: $backup"
    }
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Copy-Item -Path (Join-Path $output '*') -Destination $destination -Recurse -Force
    Write-Output "Installed: $destination"
}
