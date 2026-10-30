param([string]$VaultCases, [string]$DeityCases, [string]$ChapterCases)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (!('CrawlLocalization.Tests' -as [type])) {
    Add-Type -Path @(
        (Join-Path $projectRoot 'src\LanguagePack.cs'),
        (Join-Path $projectRoot 'src\ShopText.cs'),
        (Join-Path $projectRoot 'src\OnlineText.cs'),
        (Join-Path $PSScriptRoot 'OnlineTests.cs'),
        (Join-Path $projectRoot 'src\ProgressionText.cs'),
        (Join-Path $PSScriptRoot 'ProgressionTests.cs'),
        (Join-Path $PSScriptRoot 'ShopTests.cs'),
        (Join-Path $projectRoot 'src\NumericTranslation.cs'),
        (Join-Path $projectRoot 'src\ChineseTextWrapper.cs'),
        (Join-Path $projectRoot 'src\MenuTextPolicy.cs'),
        (Join-Path $projectRoot 'src\FontService.cs'),
        (Join-Path $PSScriptRoot 'UnityFontStub.cs'),
        (Join-Path $PSScriptRoot 'Tests.cs')
    )
}
[CrawlLocalization.Tests]::Run($projectRoot)
$null = [CrawlLocalization.ShopTests]::Run($projectRoot)
[CrawlLocalization.ProgressionTests]::Run($projectRoot)
[CrawlLocalization.OnlineTests]::Run($projectRoot)

# Keep the checked-in English catalog independent of the edited Chinese targets.
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $projectRoot 'translator-kit\crawl-language-pack\scripts\check-language.ps1') -PluginRoot $projectRoot -Language zh-Hans -RequireComplete
if ($LASTEXITCODE -ne 0) { throw 'Language catalog validation failed.' }
& (Join-Path $PSScriptRoot 'check-title-padding.ps1')
& (Join-Path $PSScriptRoot 'check-language-sync.ps1')
& (Join-Path $projectRoot 'translator-kit/crawl-language-pack/scripts/sync-languages.ps1') -PluginRoot $projectRoot -Check

if ($VaultCases) {
    [string[]]$sources = @(Get-Content -Raw -LiteralPath $VaultCases -Encoding utf8 | ConvertFrom-Json)
    [CrawlLocalization.Tests]::CheckCatalog($projectRoot, $sources)
}

if ($DeityCases) {
    [string[]]$sources = @(Get-Content -Raw -LiteralPath $DeityCases -Encoding utf8 | ConvertFrom-Json)
    [CrawlLocalization.Tests]::CheckCatalog($projectRoot, $sources)
}

if ($ChapterCases) {
    [string[]]$sources = @(Get-Content -Raw -LiteralPath $ChapterCases -Encoding utf8 | ConvertFrom-Json)
    [CrawlLocalization.Tests]::CheckCatalog($projectRoot, $sources)
}
