param([string]$GamePath='D:\SteamLibrary\steamapps\common\Crawl')
$ErrorActionPreference='Stop'
if ($PSVersionTable.PSEdition -eq 'Core') {
 & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -GamePath $GamePath
 if ($LASTEXITCODE -ne 0) { throw 'CrawlOnline IL check failed.' }; return
}
$root=Split-Path $PSScriptRoot -Parent
$refs=Join-Path $root 'artifacts/online-il-refs'
New-Item -ItemType Directory -Force $refs | Out-Null
foreach($name in @('0Harmony.dll','Mono.Cecil.dll','MonoMod.Utils.dll','MonoMod.RuntimeDetour.dll')) { Copy-Item (Join-Path $GamePath ('BepInEx/core/'+$name)) $refs -Force }
Copy-Item (Join-Path $GamePath 'Crawl_Data/Managed/UnityEngine.dll') $refs -Force
Get-ChildItem -LiteralPath $refs -Filter '*.dll' | Unblock-File
Add-Type -Path (Join-Path $refs 'Mono.Cecil.dll')
Add-Type -Path (Join-Path $refs '0Harmony.dll')
Add-Type -Path (Join-Path $refs 'UnityEngine.dll')
$sources=@('OnlineIntegration.cs','OnlineText.cs','ShopText.cs','LanguagePack.cs','NumericTranslation.cs' | ForEach-Object {Join-Path $root ('src/'+$_)})
Add-Type -Path ($sources+(Join-Path $PSScriptRoot 'OnlineIlTests.cs')) -ReferencedAssemblies @('System.dll','System.Core.dll',(Join-Path $refs '0Harmony.dll'),(Join-Path $refs 'UnityEngine.dll')) -IgnoreWarnings -WarningAction SilentlyContinue
$search=@((Join-Path $GamePath 'Crawl_Data/Managed'),(Join-Path $GamePath 'BepInEx/core'),(Join-Path $GamePath 'BepInEx/plugins/CrawlOnline'))
$resolver=[ResolveEventHandler]{param($sender,$eventArgs) $name=([Reflection.AssemblyName]::new($eventArgs.Name)).Name+'.dll';foreach($dir in $search){$path=Join-Path $dir $name;if(Test-Path $path){$local=Join-Path $refs $name;[IO.File]::WriteAllBytes($local,[IO.File]::ReadAllBytes($path));Unblock-File $local;return [Reflection.Assembly]::LoadFrom($local)}};return $null}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
$modPath=Join-Path $GamePath 'BepInEx/plugins/CrawlOnline/CrawlOnline.dll'
$localMod=Join-Path $refs 'CrawlOnline.dll'
[IO.File]::WriteAllBytes($localMod,[IO.File]::ReadAllBytes($modPath))
$mod=[Reflection.Assembly]::LoadFrom($localMod)
$cecil=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($modPath)
try {
 $opcodes=@{};foreach($f in [Reflection.Emit.OpCodes].GetFields([Reflection.BindingFlags]'Public,Static')){$op=$f.GetValue($null);$opcodes[$op.Name]=$op}
 foreach($class in @('OnlineMenu','Overlay')) {
  $methods=if($class -eq 'OnlineMenu'){[CrawlLocalization.OnlineIlTests]::MenuMethods}else{[CrawlLocalization.OnlineIlTests]::OverlayMethods}
  $type=$cecil.MainModule.Types | Where-Object FullName -eq ('CrawlOnline.UI.'+$class)
  foreach($name in $methods) {
   $method=$type.Methods | Where-Object Name -eq $name
   if(!$method){throw "Missing $class.$name"}
   $code=[Collections.Generic.List[HarmonyLib.CodeInstruction]]::new()
   foreach($i in $method.Body.Instructions) {
    $operand=$i.Operand
    if($operand -is [Mono.Cecil.MethodReference]) {
     $owner=$operand.DeclaringType.FullName
     if($owner -eq 'System.Collections.Generic.List`1/Enumerator<System.String>' -or $owner -in @('System.String','System.Text.StringBuilder','System.Object','UnityEngine.TextMesh','UnityEngine.GUI','UnityEngine.GUILayout','CrawlOnline.Net.SteamLobby','CrawlOnline.Net.LobbyBrowser')) {
      $runtime=if($owner -eq 'System.Collections.Generic.List`1/Enumerator<System.String>'){[Collections.Generic.List[string]].GetNestedType('Enumerator').MakeGenericType([string])}else{$owner -as [type]};if(!$runtime){$runtime=$mod.GetType($owner)}
      [type[]]$parameters=@($operand.Parameters | ForEach-Object {$paramName=$_.ParameterType.FullName.Replace('/','+');$t=$paramName -as [type];if(!$t){$t=[Reflection.Assembly]::LoadFrom((Join-Path $GamePath 'Crawl_Data/Managed/Assembly-CSharp-firstpass.dll')).GetType($paramName)};$t})
      if($parameters -contains $null){throw "Unresolved parameters: $operand"}
      $operand=if($operand.Name -eq '.ctor'){$runtime.GetConstructor($parameters)}else{$runtime.GetMethod($operand.Name,[Reflection.BindingFlags]'Public,NonPublic,Static,Instance',$null,$parameters,$null)}
      if(!$operand){throw "Unresolved method: $($i.Operand)"}
     }
    } elseif($operand -is [Mono.Cecil.FieldReference] -and $operand.DeclaringType.FullName -in @('CrawlOnline.Online','CrawlOnline.Net.LobbyEntry')) {
     $operand=$mod.GetType($operand.DeclaringType.FullName).GetField($operand.Name,[Reflection.BindingFlags]'Public,NonPublic,Static,Instance')
    }
    $code.Add([HarmonyLib.CodeInstruction]::new($opcodes[$i.OpCode.Name],$operand))
   }
   [CrawlLocalization.OnlineIlTests]::Check("$class.$name",$code.ToArray())
  }
 }
 [CrawlLocalization.OnlineIlTests]::Execute($root)
} finally {$cecil.Dispose();[AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)}
