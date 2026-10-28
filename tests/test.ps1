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
