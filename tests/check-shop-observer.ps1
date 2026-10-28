param([string]$GamePath = 'D:\SteamLibrary\steamapps\common\Crawl')
$ErrorActionPreference='Stop'
if ($PSVersionTable.PSEdition -eq 'Core') {
 & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -GamePath $GamePath
 if ($LASTEXITCODE -ne 0) { throw 'Legacy Harmony observer test failed.' }; return
}
$root=Split-Path $PSScriptRoot -Parent
$gameCore=Join-Path $GamePath 'BepInEx/core'
$core=Join-Path $root 'artifacts/shop-observer-refs'
New-Item -ItemType Directory -Force $core | Out-Null
foreach ($name in @('0Harmony.dll','Mono.Cecil.dll','MonoMod.Utils.dll','MonoMod.RuntimeDetour.dll')) {
 [IO.File]::WriteAllBytes((Join-Path $core $name),[IO.File]::ReadAllBytes((Join-Path $gameCore $name)))
}
Add-Type -Path (Join-Path $core 'Mono.Cecil.dll')
Add-Type -Path (Join-Path $core '0Harmony.dll')
Add-Type -Path @((Join-Path $root 'src/ShopMagicChoice.cs'), (Join-Path $PSScriptRoot 'ShopObserverTests.cs')) -ReferencedAssemblies @('System.dll','System.Core.dll',(Join-Path $core '0Harmony.dll')) -IgnoreWarnings -WarningAction SilentlyContinue
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GamePath 'Crawl_Data/Managed/Assembly-CSharp.dll'))
try {
 $method=($assembly.MainModule.Types | Where-Object Name -eq 'WeaponData').Methods | Where-Object Name -eq 'GetDescription'
 $opcodes=@{}
 foreach($field in [Reflection.Emit.OpCodes].GetFields([Reflection.BindingFlags]'Public,Static')) { $op=$field.GetValue($null); $opcodes[$op.Name]=$op }
 $code=[Collections.Generic.List[HarmonyLib.CodeInstruction]]::new()
 foreach($instruction in $method.Body.Instructions) {
  $operand=$instruction.Operand
  if($operand -is [Mono.Cecil.FieldReference] -and $operand.Name -eq 'm_weaponMagicDescription') { $operand=[Constants].GetField('m_weaponMagicDescription') }
  $code.Add([HarmonyLib.CodeInstruction]::new($opcodes[$instruction.OpCode.Name],$operand))
 }
 [CrawlLocalization.ShopObserverTests]::Run($code.ToArray())
} finally { $assembly.Dispose() }
