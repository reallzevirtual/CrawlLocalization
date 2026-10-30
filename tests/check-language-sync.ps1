$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('crawl-language-sync-'+[Guid]::NewGuid().ToString('N'))
$utf8=[Text.UTF8Encoding]::new($false,$true)
$checks=0
function Assert($ok,[string]$message){$script:checks++;if(!$ok){throw $message}}
function Bytes([string]$path){[Convert]::ToBase64String([IO.File]::ReadAllBytes($path))}
function Fails([scriptblock]$action,[string]$expected) {
    $message='';try{& $action | Out-Null}catch{$message=$_.Exception.Message}
    Assert ($message.Contains($expected)) "Expected '$expected', got '$message'"
}
function Set-FixtureText([string]$path,[string]$value){[IO.File]::WriteAllText($path,$value,$utf8)}
try {
    $kit=Join-Path $fixture 'kit';$catalog=Join-Path $kit 'catalog'
    New-Item -ItemType Directory -Path $catalog -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'translator-kit/crawl-language-pack/scripts') -Destination (Join-Path $kit 'scripts') -Recurse
    $sync=Join-Path $kit 'scripts/sync-languages.ps1'
    $new=Join-Path $kit 'scripts/new-language.ps1'
    $source="# English reference`nHELLO`t@TODO`tGreeting`nNEW\nLINE`t@TODO`tNew text`n"
    Set-FixtureText (Join-Path $catalog 'translations.tsv') $source
    Set-FixtureText (Join-Path $catalog 'shop-il-translations.tsv') "@shop.il:period`t@TODO`tPunctuation`n"
    Set-FixtureText (Join-Path $catalog 'fonts.tsv') "# fonts`n"
    Set-FixtureText (Join-Path $catalog 'textmesh-paths.txt') "# bindings`n"
    foreach($id in @('es','ru')){
        $folder=Join-Path $fixture "Languages/$id";New-Item -ItemType Directory -Path $folder -Force | Out-Null
        Set-FixtureText (Join-Path $folder 'language.ini') "Name=$id`n"
    }
    $es=Join-Path $fixture 'Languages/es/translations.tsv'
    $ru=Join-Path $fixture 'Languages/ru/translations.tsv'
    # BOM, CRLF, literal quotes and significant spaces; no final newline.
    $original="# user comment`r`nHELLO`t  `"Hola`"  `tUser context"
    [IO.File]::WriteAllText($es,$original,[Text.UTF8Encoding]::new($true))
    Set-FixtureText $ru "HELLO`tPrivet`tKept`n"
    $shop=Join-Path $fixture 'Languages/es/shop-il-translations.tsv'
    Set-FixtureText $shop "@shop.il:period`t`tIntentional omission`n"
    $before=Bytes $es;$beforeRu=Bytes $ru;$beforeShop=Bytes $shop
    $catalogBefore=Bytes (Join-Path $catalog 'translations.tsv')
    & $sync -PluginRoot $fixture | Out-Null
    Assert ((Bytes $es) -ceq $before) 'Preview changed translations'
    Assert (!(Test-Path -LiteralPath (Join-Path $fixture 'authoring-backups'))) 'Preview created backup'
    Fails {& $sync -PluginRoot $fixture -Check} 'missing entries'
    & $sync -PluginRoot $fixture -Apply | Out-Null
    Assert ([IO.File]::ReadAllText($es,$utf8) -ceq ($original+"`r`nNEW\nLINE`t@TODO`tNew text`r`n")) 'Append must preserve quotes, spaces, comments and CRLF'
    $actual=[IO.File]::ReadAllBytes($es)
    Assert ($actual[0] -eq 239 -and $actual[1] -eq 187 -and $actual[2] -eq 191) 'UTF-8 BOM was lost'
    Assert ((Bytes $shop) -ceq $beforeShop) 'Intentional empty target changed'
    Assert (Test-Path -LiteralPath (Join-Path $fixture 'Languages/ru/shop-il-translations.tsv')) 'Missing table not created'
    Assert ([IO.File]::ReadAllText($ru,$utf8).Contains("NEW\nLINE`t@TODO")) 'Second language not synchronized'
    Assert ((Bytes (Join-Path $catalog 'translations.tsv')) -ceq $catalogBefore) 'Catalog was mutated'
    $backup=@(Get-ChildItem -LiteralPath (Join-Path $fixture 'authoring-backups') -Directory)
    Assert ($backup.Count -eq 1) 'Expected one durable backup'
    Assert ((Bytes (Join-Path $backup[0].FullName 'Languages/es/translations.tsv')) -ceq $before) 'Backup is not byte-exact'
    Assert ((Bytes (Join-Path $backup[0].FullName 'Languages/ru/translations.tsv')) -ceq $beforeRu) 'Other target backup missing'
    $after=Bytes $es;$time=(Get-Item -LiteralPath $es).LastWriteTimeUtc
    & $sync -PluginRoot $fixture -Apply | Out-Null
    & $sync -PluginRoot $fixture -Check | Out-Null
    Assert ((Bytes $es) -ceq $after -and (Get-Item -LiteralPath $es).LastWriteTimeUtc -eq $time) 'Repeat sync rewrote a complete table'
    Assert (@(Get-ChildItem -LiteralPath (Join-Path $fixture 'authoring-backups') -Directory).Count -eq 1) 'No-op sync created an extra backup'
    # A conflict in ru must stop additions to es too.
    Set-FixtureText (Join-Path $catalog 'translations.tsv') ($source+"NEXT`t@TODO`tNext`n")
    [IO.File]::AppendAllText($ru,"TYPO`tBad`tUnknown`n",$utf8)
    Fails {& $sync -PluginRoot $fixture -Apply} 'Catalog conflicts'
    Assert ((Bytes $es) -ceq $after) 'Conflict allowed partial writes to another language'
    Fails {& $sync -PluginRoot $fixture -Languages es -Apply -BackupRoot (Join-Path $catalog 'backups')} 'outside Languages and catalog'
    Assert ((Bytes $es) -ceq $after) 'Invalid backup location changed translations'
    Set-FixtureText $ru "HELLO`tKept`nhello`tDuplicate`n"
    Fails {& $sync -PluginRoot $fixture -Apply} 'Catalog conflicts'
    Set-FixtureText $ru "@shop.il:period`t@TODO`tWrong file`n"
    Fails {& $sync -PluginRoot $fixture -Apply} 'Catalog conflicts'
    # Invalid master targets/duplicate normalized sources must fail before generation.
    Set-FixtureText (Join-Path $catalog 'translations.tsv') "HELLO`tTranslated master`n"
    Fails {& $new -PluginRoot $fixture -Language fr} 'must all be @TODO'
    Assert (!(Test-Path -LiteralPath (Join-Path $fixture 'Languages/fr'))) 'Invalid catalog created a language folder'
    Set-FixtureText (Join-Path $catalog 'translations.tsv') "HELLO`t@TODO`nhello`t@TODO`n"
    Fails {& $sync -PluginRoot $fixture -Languages es} 'Duplicate catalog key'
    Set-FixtureText (Join-Path $catalog 'translations.tsv') "A\r\nB`t@TODO`nA\nB`t@TODO`n"
    Fails {& $sync -PluginRoot $fixture -Languages es} 'Duplicate catalog key'
    Set-FixtureText (Join-Path $catalog 'translations.tsv') $source
    & $new -PluginRoot $fixture -Language fr | Out-Null
    & $sync -PluginRoot $fixture -Languages fr -Check | Out-Null
    Assert ([IO.File]::ReadAllText((Join-Path $fixture 'Languages/fr/translations.tsv'),$utf8).Contains("HELLO`t@TODO")) 'New pack must use canonical pending rows'
    Write-Output "PASS: $checks catalog/synchronization checks."
} finally {
    $full=[IO.Path]::GetFullPath($fixture)
    $temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
    if([IO.Directory]::GetParent($full).FullName -ne $temp -or [IO.Path]::GetFileName($full) -notlike 'crawl-language-sync-*'){throw 'Unexpected fixture path'}
    if(Test-Path -LiteralPath $full){Remove-Item -LiteralPath $full -Recurse -Force}
}
