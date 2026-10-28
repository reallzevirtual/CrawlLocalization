param([string]$PythonPath = 'python')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$version = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
$archive = Join-Path $root "artifacts/CrawlLocalization-$version-Translator.zip"
if (!(Test-Path -LiteralPath $archive)) { throw 'Build the Translator ZIP first.' }
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$workspace = Join-Path $root "artifacts/menu-examples-$version-$stamp"
$authoring = Join-Path $workspace 'authoring'
$backup = Join-Path $root "docs/my/backups/menu-examples-$version-$stamp"
New-Item -ItemType Directory -Path $backup -Force | Out-Null
Expand-Archive -LiteralPath $archive -DestinationPath $authoring
Copy-Item (Join-Path $authoring 'crawl-language-pack/catalog') (Join-Path $backup 'catalog') -Recurse
$examples = Get-Content (Join-Path $root 'examples/menu-localization/translations.json') -Raw -Encoding utf8 | ConvertFrom-Json
$utf8 = [Text.UTF8Encoding]::new($false, $true)
foreach ($locale in @('ru','es')) {
    $example = $examples.$locale
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $authoring 'crawl-language-pack/scripts/new-language.ps1') -PluginRoot $authoring -Language $locale -DisplayName $example.Name
    if ($LASTEXITCODE -ne 0) { throw 'Initializer failed.' }
    $language = Join-Path $authoring "Languages/$locale"
    Copy-Item $language (Join-Path $backup $locale) -Recurse
    $table = Join-Path $language 'translations.tsv'
    $before = [IO.File]::ReadAllLines($table, $utf8)
    $after = [Collections.Generic.List[string]]::new()
    $changes = 0
    foreach ($line in $before) {
        $cells = $line.Split("`t")
        if ($cells.Length -eq 3 -and $example.Translations.PSObject.Properties.Name -ccontains $cells[0]) {
            if ($cells[1] -cne '@TODO') { throw 'Template was not English-only pending data.' }
            $cells[1] = $example.Translations.($cells[0]); $changes++
            $after.Add($cells -join "`t")
        } else { $after.Add($line) }
    }
    if ($changes -ne 6 -or $before.Length -ne $after.Count) { throw 'Unexpected menu/source count.' }
    [IO.File]::WriteAllLines($table, $after, $utf8)
    # Compare all files to the pre-edit backup; only six target cells may differ.
    foreach ($file in Get-ChildItem $language -File) {
        $old = [IO.File]::ReadAllLines((Join-Path $backup "$locale/$($file.Name)"),$utf8)
        $new = [IO.File]::ReadAllLines($file.FullName,$utf8)
        if ($old.Length -ne $new.Length) { throw 'Row count changed.' }
        for ($i=0; $i -lt $old.Length; $i++) {
            if ($old[$i] -ceq $new[$i]) { continue }
            $a=$old[$i].Split("`t"); $b=$new[$i].Split("`t")
            if ($file.Name -cne 'translations.tsv' -or $a.Length -ne 3 -or $b.Length -ne 3 -or $a[0] -cne $b[0] -or $a[2] -cne $b[2] -or $b[1] -cne $example.Translations.($a[0])) { throw 'Unexpected change relative to backup.' }
        }
    }
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $authoring 'crawl-language-pack/scripts/check-language.ps1') -PluginRoot $authoring -Language $locale
    if ($LASTEXITCODE -ne 0) { throw 'Language check failed.' }
    $stage = Join-Path $workspace $locale
    New-Item -ItemType Directory -Path (Join-Path $stage 'Languages') -Force | Out-Null
    Copy-Item $language (Join-Path $stage "Languages/$locale") -Recurse
    Copy-Item (Join-Path $authoring 'Fonts') $stage -Recurse
    Copy-Item (Join-Path $authoring 'CrawlLocalization.dll') $stage
    $readme = [IO.File]::ReadAllText((Join-Path $root 'examples/menu-localization/README.md'),$utf8).Replace('__LANGUAGE__',$locale).Replace('__NAME__',$example.Name).Replace('__VERSION__',$version)
    [IO.File]::WriteAllText((Join-Path $stage 'README.md'),$readme,$utf8)
    $zip = Join-Path $root "artifacts/CrawlLocalization-$version-$locale-MenuDemo.zip"
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
    Write-Output "Menu example: $zip"
}
& pwsh -NoProfile -File (Join-Path $root 'tests/check-menu-pack.ps1') -PluginRoot $authoring
if ($LASTEXITCODE -ne 0) { throw 'Released DLL integration check failed.' }
& $PythonPath (Join-Path $root 'tests/check_multilingual_fonts.py') --plugin-root $authoring
if ($LASTEXITCODE -ne 0) { throw 'Portable font/sample coverage check failed.' }
Write-Output "Authoring workspace: $authoring"
Write-Output "Immutable pre-translation backup: $backup"
