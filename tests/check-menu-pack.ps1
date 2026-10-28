param([Parameter(Mandatory=$true)][string]$PluginRoot)
$ErrorActionPreference='Stop'
$PluginRoot=(Resolve-Path -LiteralPath $PluginRoot).Path
$root=Split-Path $PSScriptRoot -Parent
$examples=Get-Content (Join-Path $root 'examples/menu-localization/translations.json') -Raw -Encoding utf8 | ConvertFrom-Json
# Inspect the released DLL itself, without loading Unity or running Crawl.
$assembly=[Reflection.Assembly]::LoadFile((Join-Path $PluginRoot 'CrawlLocalization.dll'))
$type=$assembly.GetType('CrawlLocalization.LanguagePack',$true)
$static=[Reflection.BindingFlags]'Static,NonPublic'
$instance=[Reflection.BindingFlags]'Instance,NonPublic'
$warnings=[Collections.Generic.List[string]]::new()
$warn=[Action[string]]{param($message) $warnings.Add($message)}
foreach($locale in @('ru','es')) {
    $pack=$type.GetMethod('Load',$static).Invoke($null,@($PluginRoot,$locale,$warn))
    if($type.GetProperty('Count',$instance).GetValue($pack) -ne 6){throw "Unexpected translation count: $locale"}
    if($type.GetProperty('UsesChineseTextRules',$instance).GetValue($pack)){throw 'Chinese rules enabled for another locale.'}
    if($type.GetField('Fonts',$instance).GetValue($pack).Count -ne 10){throw 'Font mappings missing.'}
    foreach($translation in $examples.$locale.Translations.PSObject.Properties) {
        $arguments=[object[]]@($translation.Name,$null)
        $found=$type.GetMethod('TryTranslate',$instance).Invoke($pack,$arguments)
        if(!$found -or $arguments[1] -cne $translation.Value){throw "Runtime lookup failed: $locale/$($translation.Name)"}
    }
    foreach($pending in @('MONSTERS','READY','Keyboard Setup')) {
        $arguments=[object[]]@($pending,$null)
        if($type.GetMethod('TryTranslate',$instance).Invoke($pack,$arguments)){throw 'Pending text must retain English.'}
    }
}
if($warnings.Count){throw ($warnings -join "`n")}
Write-Output 'PASS: released DLL loads both sample packs; all 12 menu translations resolve, pending labels retain English, no Chinese text rules.'
