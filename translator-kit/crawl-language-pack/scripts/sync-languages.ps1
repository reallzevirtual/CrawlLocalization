param(
    [Parameter(Mandatory=$true)][string]$PluginRoot,
    [string[]]$Languages=@(),
    [switch]$Apply,
    [switch]$Check,
    [string]$BackupRoot
)
$ErrorActionPreference='Stop'
if($Apply -and $Check){throw 'Use -Apply or -Check, not both'}
$PluginRoot=(Resolve-Path -LiteralPath $PluginRoot).Path
. (Join-Path $PSScriptRoot 'catalog-common.ps1')
$reference=Join-Path (Split-Path $PSScriptRoot -Parent) 'catalog'
$catalog=Read-CrawlCatalog $reference
$languageRoot=Join-Path $PluginRoot 'Languages'
if(!$Languages.Count -and (Test-Path -LiteralPath $languageRoot)) {
    $Languages=@(Get-ChildItem -LiteralPath $languageRoot -Directory | Where-Object {Test-Path -LiteralPath (Join-Path $_.FullName 'language.ini')} | ForEach-Object Name)
}
$Languages=@($Languages | ForEach-Object {if($_ -ieq 'zh-CN'){'zh-Hans'}elseif($_ -ieq 'zh-TW'){'zh-Hant'}else{$_}} | Sort-Object -Unique)
$problems=[Collections.Generic.List[string]]::new()
$plans=[Collections.Generic.List[object]]::new();$total=0
foreach($language in $Languages) {
    if($language -notmatch '^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$' -or $language -ieq 'en'){throw "Invalid language tag: $language"}
    $directory=Join-Path $languageRoot $language
    if(!(Test-Path -LiteralPath (Join-Path $directory 'language.ini'))){throw "Language not initialized: $language; run new-language.ps1 first"}
    $seen=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
    $legacy=Get-CrawlLegacyInputs $language
    $snapshots=@{}
    foreach($file in Get-ChildItem -LiteralPath $directory -Filter '*translations.tsv' | Sort-Object Name) {
        if(!$catalog.Tables.Contains($file.Name)){$problems.Add("${language}/$($file.Name): unsupported table");continue}
        $bytes=[IO.File]::ReadAllBytes($file.FullName)
        $raw=$crawlUtf8.GetString($bytes)
        if($raw.Length -and $raw[0] -eq [char]0xFEFF){$raw=$raw.Substring(1)}
        $snapshots[$file.Name]=[pscustomobject]@{Bytes=$bytes;Text=$raw}
        $number=0
        foreach($line in [Regex]::Split($raw,"`r`n|`n|`r")) {
            $number++;if(!$line.Trim() -or $line.StartsWith('#')){continue}
            $location="${language}/$($file.Name):$number"
            try {
                $cells=$line.Split("`t")
                if($cells.Length -notin @(2,3) -or !$cells[0]){throw 'Malformed raw TSV row'}
                $key=Decode $cells[0];$null=Decode $cells[1]
                if($seen.ContainsKey($key)){throw "Duplicate key [$($cells[0])]; first seen in $($seen[$key])"}
                $seen.Add($key,$location)
                if($catalog.Entries.ContainsKey($key)){$expected=$catalog.Entries[$key].Table}
                elseif($legacy.ContainsKey($key)){$expected=$legacy[$key]}
                else{throw "EXTRA [$($cells[0])]: confirm in canonical catalog first; never auto-import or delete"}
                if($expected -cne $file.Name){throw "Wrong table for [$($cells[0])]; expected $expected"}
            } catch {$problems.Add("${location}: $($_.Exception.Message)")}
        }
    }
    $missing=0
    foreach($table in $catalog.Tables.Keys) {
        $rows=@($catalog.Tables[$table] | Where-Object {!$seen.ContainsKey($_.Key)})
        if(!$rows.Count){continue}
        foreach($row in $rows){Write-Output "MISSING ${language}/${table}: $($row.RawKey)"}
        $file=Join-Path $directory $table
        $exists=$snapshots.ContainsKey($table)
        [byte[]]$before=@();$text='';$ending="`n"
        if($exists) {
            $before=$snapshots[$table].Bytes
            $text=$snapshots[$table].Text
            if($text.Contains("`r`n")){$ending="`r`n"}
        }
        $prefix=if(!$exists){'# Source/key<TAB>translation (@TODO means not translated)<TAB>English context'+$ending}elseif($text.Length -and !$text.EndsWith("`n") -and !$text.EndsWith("`r")){$ending}else{''}
        $addition=$prefix+(@($rows | ForEach-Object Line) -join $ending)+$ending
        # Append bytes only: existing translations, quotes, escapes, BOM and spacing stay intact.
        [byte[]]$after=$before+$crawlUtf8.GetBytes($addition)
        $plans.Add([pscustomobject]@{Language=$language;Table=$table;Path=$file;Exists=$exists;Before=$before;After=$after})
        $missing+=$rows.Count
    }
    $total+=$missing
    Write-Output "${language}: $missing missing canonical entries."
}
if($problems.Count){foreach($problem in $problems){Write-Output $problem};throw 'Catalog conflicts detected; no files changed'}
if(!$Languages.Count){Write-Output 'Catalog valid; no initialized languages found.';return}
if($Check -and $total){throw "$total missing entries; run sync-languages.ps1 -Apply to add @TODO rows"}
if(!$Apply -or !$total){Write-Output "Catalog check complete: $total additions planned; no files changed.";return}
# Preflight completed for every selected language before any write/backup.
if(!$BackupRoot){$BackupRoot=Join-Path $PluginRoot 'authoring-backups'}
$BackupRoot=[IO.Path]::GetFullPath($BackupRoot)
foreach($liveRoot in @($languageRoot,$reference)) {
    $liveRoot=[IO.Path]::GetFullPath($liveRoot).TrimEnd('\')
    if($BackupRoot.StartsWith($liveRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or $BackupRoot -ieq $liveRoot){throw 'Keep backups outside Languages and catalog'}
}
$backup=Join-Path $BackupRoot ('sync-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
Copy-Item -LiteralPath $reference -Destination (Join-Path $backup 'catalog') -Recurse
New-Item -ItemType Directory -Path (Join-Path $backup 'Languages') | Out-Null
foreach($language in @($plans | ForEach-Object Language | Sort-Object -Unique)) {
    Copy-Item -LiteralPath (Join-Path $languageRoot $language) -Destination (Join-Path $backup ('Languages/'+$language)) -Recurse
}
Write-Output "Backup: $backup"
foreach($plan in $plans) {
    # Refuse stale plans rather than overwriting edits made after the preflight.
    if((Test-Path -LiteralPath $plan.Path) -ne $plan.Exists){throw "File changed during sync: $($plan.Path); backup: $backup"}
    if($plan.Exists -and [Convert]::ToBase64String([IO.File]::ReadAllBytes($plan.Path)) -cne [Convert]::ToBase64String($plan.Before)){throw "File changed during sync: $($plan.Path); backup: $backup"}
    $temporary=$plan.Path+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
    try {
        [IO.File]::WriteAllBytes($temporary,$plan.After)
        if($plan.Exists){[IO.File]::Replace($temporary,$plan.Path,[NullString]::Value)}else{[IO.File]::Move($temporary,$plan.Path)}
    } finally {if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary -Force}}
}
Write-Output "Added $total @TODO entries. Existing translations and canonical catalog were not changed. Run check-language.ps1 for each target language."
