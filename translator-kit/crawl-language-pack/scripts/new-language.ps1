param(
    [Parameter(Mandatory=$true)][string]$PluginRoot,
    [Parameter(Mandatory=$true)][string]$Language,
    [string]$DisplayName,
    [string]$FallbackFonts = 'Arial;Segoe UI'
)
$ErrorActionPreference = 'Stop'
$PluginRoot = (Resolve-Path -LiteralPath $PluginRoot).Path
if ($Language -notmatch '^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$') { throw 'Use a language tag such as es, ru, pt-BR or zh-Hant.' }
if ($Language -ieq 'en') { throw 'en is reserved for original game text.' }
if ($Language -ieq 'zh-CN' -or $Language -ieq 'zh-Hans') { $Language='zh-Hans' }
if ($Language -ieq 'zh-TW' -or $Language -ieq 'zh-Hant') { $Language='zh-Hant' }
if (!$DisplayName) { $DisplayName=$Language }
if ($DisplayName -match '[\r\n]' -or $FallbackFonts -match '[\r\n]') { throw 'INI values must stay on one line.' }
$dll=Join-Path $PluginRoot 'CrawlLocalization.dll'
if ((Test-Path -LiteralPath $dll) -and [Reflection.AssemblyName]::GetAssemblyName($dll).Version -lt [version]'0.13.0.0') { throw 'Pending translation rows require Crawl Localization 0.13.0 or newer.' }
$source=Join-Path (Split-Path $PSScriptRoot -Parent) 'catalog'
$destination=Join-Path $PluginRoot ('Languages\'+$Language)
if (!(Test-Path -LiteralPath $source -PathType Container)) { throw 'Missing English catalog next to the authoring scripts.' }
if (Test-Path -LiteralPath $destination) { throw "Language folder already exists; nothing overwritten: $destination" }
$utf8=[Text.UTF8Encoding]::new($false,$true)
$tables=@(Get-ChildItem -LiteralPath $source -Filter '*translations.tsv')
if (!$tables.Count) { throw 'No reference translation tables found.' }
# Prepare every table in memory before creating a new language directory.
$files=@{}
foreach ($table in $tables) {
    $lines=[Collections.Generic.List[string]]::new()
    $lines.Add('# Source/key<TAB>translation (@TODO means not translated)<TAB>English context')
    foreach ($line in [IO.File]::ReadAllLines($table.FullName,$utf8)) {
        if (!$line.Trim() -or $line.StartsWith('#')) { continue }
        $cells=$line.Split("`t")
        if ($cells.Length -notin @(2,3)) { throw "Malformed reference row in $($table.Name)" }
        $comment=if ($cells.Length -eq 3) { $cells[2] } else { '' }
        $lines.Add($cells[0]+"`t@TODO`t"+$comment)
    }
    $files[$table.Name]=($lines -join "`n")+"`n"
}
$files['language.ini']="Name=$DisplayName`nFallbackFonts=$FallbackFonts`n"
$files['fonts.tsv']=[IO.File]::ReadAllText((Join-Path $source 'fonts.tsv'),$utf8)
$files['textmesh-paths.txt']=[IO.File]::ReadAllText((Join-Path $source 'textmesh-paths.txt'),$utf8)
$languageRoot=Join-Path $PluginRoot 'Languages'
New-Item -ItemType Directory -Path $languageRoot -Force | Out-Null
New-Item -ItemType Directory -Path $destination | Out-Null
foreach ($name in $files.Keys) { [IO.File]::WriteAllText((Join-Path $destination $name),$files[$name],$utf8) }
Write-Output "Created: $destination"
Write-Output "Edit the SECOND column of translation tables. Replace @TODO with your translation."
Write-Output "Then set Language = $Language in BepInEx/config/crawl.localization.cfg and press F8 (or restart)."
Write-Output 'This script did not change the active language or start the game.'
