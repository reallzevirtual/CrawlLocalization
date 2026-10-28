param([string]$Catalog = 'artifacts/shop-catalog.json')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (!('CrawlLocalization.ShopTests' -as [type])) {
 $sources = @('LanguagePack.cs','NumericTranslation.cs','ShopText.cs','ChineseTextWrapper.cs' | ForEach-Object { Join-Path $root ('src/' + $_) })
 Add-Type -Path ($sources + (Join-Path $PSScriptRoot 'ShopTests.cs'))
}
$catalogData = Get-Content -Raw -LiteralPath (Join-Path $root $Catalog) -Encoding utf8 | ConvertFrom-Json
$entries = [Collections.Generic.List[object]]::new()
foreach ($body in [CrawlLocalization.ShopTests]::Run($root)) { $entries.Add(@{ kind='body'; text=$body; source='component combinations' }) }
$names = [Collections.Generic.HashSet[string]]::new()
$sourceCount = 0
foreach ($record in $catalogData.records) {
 $d = $record.data
 if ($record.cls -eq 'Weapon') {
  $variants = @(@{ m_name=$d.m_weaponName; m_description=$d.m_weaponDescription }) + @($d.m_combos)
  foreach ($variant in $variants) {
   $name = $variant.m_name
   $description = if ($variant.m_description) { $variant.m_description } else { $d.m_weaponDescription }
   $null = $names.Add($name)
   if ($null -eq [CrawlLocalization.ShopTests]::Name($root,$name,'')) { throw "Missing weapon name: $name" }
   if ($null -eq [CrawlLocalization.ShopTests]::Description($root,$description)) { throw "Missing weapon description: $description" }
   $entries.Add(@{ kind='body'; text=[CrawlLocalization.ShopTests]::StressBody($root,$name,$description); source=$record.source + ' ' + $name })
   $sourceCount++
  }
 } elseif ($record.cls.StartsWith('Powerup')) {
  if ($d.m_displayName) { $entries.Add(@{ kind='title'; text=[CrawlLocalization.ShopTests]::Name($root,$d.m_displayName,''); source=$record.source }) }
  if ($d.m_description) { $entries.Add(@{ kind='body'; text=[CrawlLocalization.ShopTests]::Description($root,$d.m_description); source=$record.source }) }
  $sourceCount++
 }
}
$prefixes = @('') + @(Get-Content (Join-Path $root 'Languages/zh-Hans/shop-translations.tsv') -Encoding utf8 | Where-Object { $_.StartsWith('@shop.prefix:') } | ForEach-Object { $_.Split("`t")[0].Substring(13) })
foreach ($name in $names) {
 foreach ($prefix in $prefixes) { $entries.Add(@{ kind='title'; text=[CrawlLocalization.ShopTests]::Name($root,$name,$prefix); source=$prefix + ' ' + $name }) }
}
foreach ($entry in $entries) { if ([string]::IsNullOrWhiteSpace($entry.text)) { throw "Missing translation: $($entry.source)" } }
$out = Join-Path $root 'artifacts/shop-layout'
New-Item -ItemType Directory -Force $out | Out-Null
@{ entries=$entries.ToArray(); layouts=$catalogData.layouts } | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $out 'cases.json') -Encoding utf8
Write-Output "PASS: $sourceCount weapon/combo/powerup source records; $($entries.Count) layout cases prepared."
