param(
    [Parameter(Mandatory=$true)][string]$PluginRoot,
    [Parameter(Mandatory=$true)][string]$Language,
    [switch]$RequireComplete
)
$ErrorActionPreference='Stop'
$PluginRoot=(Resolve-Path -LiteralPath $PluginRoot).Path
if ($Language -notmatch '^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$' -or $Language -ieq 'en') { throw 'Specify an installed language folder, not en.' }
if ($Language -ieq 'zh-CN') {$Language='zh-Hans'}
if ($Language -ieq 'zh-TW') {$Language='zh-Hant'}
$directory=Join-Path $PluginRoot ('Languages\'+$Language)
$reference=Join-Path (Split-Path $PSScriptRoot -Parent) 'catalog'
$utf8=[Text.UTF8Encoding]::new($false,$true)
function Read-Lines([string]$path) { [IO.File]::ReadAllLines($path,$utf8) }
. (Join-Path $PSScriptRoot 'catalog-common.ps1')
$catalog=Read-CrawlCatalog $reference
$baseline=@{}
foreach($entry in $catalog.Entries.Values){$baseline[$entry.Key]=$entry.Table}
$legacyInputs=Get-CrawlLegacyInputs $Language
$seen=@{};$pending=0;$translated=0
foreach($file in Get-ChildItem -LiteralPath $directory -Filter '*translations.tsv') {
    if($file.Name -notin $baseline.Values){throw "Table is not read by this plugin version: $($file.Name)"}
    $lineNumber=0
    foreach($line in (Read-Lines $file.FullName)) {
        $lineNumber++;if(!$line.Trim() -or $line.StartsWith('#')){continue}
        try {
            $cells=$line.Split("`t")
            if($cells.Length -notin @(2,3) -or !$cells[0]){throw 'Expected source<TAB>translation[<TAB>comment]'}
            $source=Decode $cells[0]
            if($seen.ContainsKey($source)){throw "Duplicate source; first seen in $($seen[$source])"}
            if(!$baseline.ContainsKey($source) -and !$legacyInputs.ContainsKey($source)){throw "Extra or changed source/key: [$($cells[0])]. Confirm new supported text in the canonical catalog first; do not auto-import a possible typo"}
            $expectedTable=if($baseline.ContainsKey($source)){$baseline[$source]}else{$legacyInputs[$source]}
            if($file.Name -cne $expectedTable){throw 'Source/key is in the wrong translation table'}
            $seen[$source]=$file.Name+':'+$lineNumber
            if($cells[1] -ceq '@TODO'){$pending++;continue}
            if(!$cells[1] -and !$source.StartsWith('@shop.il:',[StringComparison]::Ordinal)){throw 'Use @TODO for pending rows; blank is only valid for deliberate @shop.il omissions'}
            $target=Decode $cells[1]
            # Inverse-font title underscores are visible black masking/padding, not decoration.
            $paddedTitle=$source.StartsWith('@online:title:',[StringComparison]::Ordinal) -or $source -match '^@progression\.(?:(?:join|leave)-title|prompt-title):'
            if(($source.StartsWith('_') -or $paddedTitle) -and !$target.StartsWith('_')){throw 'Inverse-font title must retain its leading underscore (black padding)'}
            if(($source.EndsWith('_') -or $paddedTitle) -and !$target.EndsWith('_')){throw 'Inverse-font title must retain its trailing underscore (black padding)'}
            foreach($pattern in @('\{[0-9]+(?:[^{}]*)?\}','\[[AB]\]','</?(?:b|i|size|color|material)(?:=[^>]*)?>|<quad\s[^>]*>')) {
                $before=@([Regex]::Matches($source,$pattern)|ForEach-Object Value|Sort-Object -Unique)
                $after=@([Regex]::Matches($target,$pattern)|ForEach-Object Value|Sort-Object -Unique)
                if(($before -join '|') -cne ($after -join '|')){throw 'Placeholder, input icon or rich-text tags changed'}
            }
            $translated++
        } catch {throw "$($file.Name):${lineNumber}: $($_.Exception.Message)"}
    }
}
$missingKeys=@($baseline.Keys | Where-Object {!$seen.ContainsKey($_)} | Sort-Object)
$missing=$missingKeys.Count
foreach($key in $missingKeys){Write-Output "MISSING $($baseline[$key]): $($catalog.Entries[$key].RawKey)"}
$fontRows=0;$fontNames=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach($line in (Read-Lines (Join-Path $directory 'fonts.tsv'))) {
    if(!$line.Trim() -or $line.StartsWith('#')){continue};$cells=$line.Split("`t");$scale=0.0
    if($cells.Length -ne 5 -or !$cells[0] -or !$cells[3] -or $cells[1] -cnotin @('bundle','system','ttf') -or ![double]::TryParse($cells[4],[Globalization.NumberStyles]::Float,[Globalization.CultureInfo]::InvariantCulture,[ref]$scale) -or [double]::IsNaN($scale) -or $scale -le 0 -or $scale -gt 10){throw 'Invalid fonts.tsv mapping'}
    if(!$fontNames.Add($cells[0])){throw "Duplicate font mapping: $($cells[0])"}
    if($cells[1] -ne 'system') {
        $path=[IO.Path]::GetFullPath((Join-Path $PluginRoot $cells[2]))
        if([IO.Path]::IsPathRooted($cells[2]) -or !$path.StartsWith($PluginRoot.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase) -or !(Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing/outside font file: $($cells[2])"}
    }
    $fontRows++
}
foreach($line in (Read-Lines (Join-Path $directory 'language.ini'))) {
    if(!$line.Trim() -or $line.StartsWith('#')){continue};$separator=$line.IndexOf('=')
    if($separator -le 0 -or $line.Substring(0,$separator).Trim() -cnotin @('Name','FallbackFonts')){throw "Unknown language.ini setting: $line"}
}
$expectedPaths=@(Read-Lines (Join-Path $reference 'textmesh-paths.txt')|Where-Object {$_.Trim() -and !$_.StartsWith('#')}|ForEach-Object {$_.Trim()}|Sort-Object -Unique)
$actualPaths=@(Read-Lines (Join-Path $directory 'textmesh-paths.txt')|Where-Object {$_.Trim() -and !$_.StartsWith('#')}|ForEach-Object {$_.Trim()}|Sort-Object -Unique)
if(($expectedPaths -join "`n") -cne ($actualPaths -join "`n")){throw 'textmesh-paths.txt differs from the reference; keep object identifiers unchanged'}
Write-Output "PASS: $translated translated; $pending pending; $missing missing; $fontRows explicit font mappings."
Write-Output 'Pending/missing rows keep original game/mod English. No other language pack is consulted.'
if($RequireComplete -and ($pending -gt 0 -or $missing -gt 0)){throw 'Language pack is incomplete'}
Write-Output 'This checks structure, not translation quality, installed system-font glyphs, or in-game layout.'
