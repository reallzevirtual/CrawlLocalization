$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (!('CrawlLocalization.Tests' -as [type])) {
    Add-Type -Path @(
        (Join-Path $projectRoot 'src\LanguagePack.cs'),
        (Join-Path $projectRoot 'src\MenuTextPolicy.cs'),
        (Join-Path $projectRoot 'src\FontService.cs'),
        (Join-Path $PSScriptRoot 'UnityFontStub.cs'),
        (Join-Path $PSScriptRoot 'Tests.cs')
    )
}
[CrawlLocalization.Tests]::Run($projectRoot)
