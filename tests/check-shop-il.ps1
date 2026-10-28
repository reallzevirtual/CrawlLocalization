param([string]$GamePath = 'D:\SteamLibrary\steamapps\common\Crawl')
$ErrorActionPreference='Stop'
if ($PSVersionTable.PSEdition -eq 'Core') {
 & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -GamePath $GamePath
 if ($LASTEXITCODE -ne 0) { throw 'Shop IL tests failed.' }; return
}
$root=Split-Path $PSScriptRoot -Parent
$gameCore=Join-Path $GamePath 'BepInEx/core'
$core=Join-Path $root 'artifacts/shop-il-refs'
New-Item -ItemType Directory -Force $core | Out-Null
foreach ($name in @('0Harmony.dll','Mono.Cecil.dll','MonoMod.Utils.dll','MonoMod.RuntimeDetour.dll')) {
 [IO.File]::WriteAllBytes((Join-Path $core $name),[IO.File]::ReadAllBytes((Join-Path $gameCore $name)))
}
Add-Type -Path (Join-Path $core 'Mono.Cecil.dll')
Add-Type -Path (Join-Path $core '0Harmony.dll')
$sources=@('src/ShopTextIlPatch.cs','src/ShopText.cs','src/LanguagePack.cs','src/NumericTranslation.cs','tests/ShopIlTests.cs' | ForEach-Object { Join-Path $root $_ })
Add-Type -Path $sources -ReferencedAssemblies @('System.dll','System.Core.dll',(Join-Path $core '0Harmony.dll')) -IgnoreWarnings -WarningAction SilentlyContinue
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GamePath 'Crawl_Data/Managed/Assembly-CSharp.dll'))
try {
 $opcodes=@{}
 foreach($field in [Reflection.Emit.OpCodes].GetFields([Reflection.BindingFlags]'Public,Static')) { $op=$field.GetValue($null); $opcodes[$op.Name]=$op }
 foreach($pair in @(@('WeaponData','GetDisplayName'),@('WeaponData','GetDescription'),@('PropPurchasable','ShowInfoBox'),@('GUIStore','Show'),@('GUIStoreStatsDisplay','Show'))) {
  $method=($assembly.MainModule.Types | Where-Object Name -eq $pair[0]).Methods | Where-Object Name -eq $pair[1]
  $code=[Collections.Generic.List[HarmonyLib.CodeInstruction]]::new()
  foreach($instruction in $method.Body.Instructions) {
   $operand=$instruction.Operand
   if($operand -is [Mono.Cecil.MethodReference] -and $operand.DeclaringType.FullName -in @('System.String','System.Text.StringBuilder','UnityEngine.TextMesh','GUIText')) {
    $type=$operand.DeclaringType.FullName -as [type]
    [type[]]$parameters=@($operand.Parameters | ForEach-Object { $_.ParameterType.FullName -as [type] })
    if($operand.Name -eq '.ctor') { $operand=$type.GetConstructor($parameters) } else { $operand=$type.GetMethod($operand.Name,$parameters) }
    if($null -eq $operand) { throw 'Unresolved inspected method.' }
   }
   $code.Add([HarmonyLib.CodeInstruction]::new($opcodes[$instruction.OpCode.Name],$operand))
  }
  $null=[CrawlLocalization.ShopIlTests]::CheckActual(($pair -join '.'),$code.ToArray(),$pair[0].StartsWith('GUIStore'))
 }
 [CrawlLocalization.ShopIlTests]::Execute($root)
} finally { $assembly.Dispose() }
