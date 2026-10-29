# Package the already built optional font, independently of normal player builds.
$ErrorActionPreference = 'Stop'
$version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'VERSION') -Raw -Encoding utf8).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid VERSION.' }
$artifacts = Join-Path $PSScriptRoot 'artifacts'
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$archive = Join-Path $artifacts ("CrawlLocalization-$version-CombinedFonts.zip")
$readme = Join-Path $PSScriptRoot 'packaging/combined-fonts/README.md'
$files = @('crawl-combined.bundle', 'crawl-combined.OFL.txt', 'crawl-combined.NOTICE.txt')
foreach ($file in $files) {
    if (!(Test-Path -LiteralPath (Join-Path $PSScriptRoot ('Fonts/' + $file)))) { throw "Missing $file; run build-fonts.ps1 -Combined first." }
}
Compress-Archive -LiteralPath $readme -DestinationPath $archive -Force
$zip = [IO.Compression.ZipFile]::Open($archive, [IO.Compression.ZipArchiveMode]::Update)
try {
    foreach ($file in $files) {
        $null = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $PSScriptRoot ('Fonts/' + $file)), 'Fonts/' + $file)
    }
} finally { $zip.Dispose() }
Write-Output "Optional combined font package: $archive"
