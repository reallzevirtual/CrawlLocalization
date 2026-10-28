param([string]$GamePath='D:\SteamLibrary\steamapps\common\Crawl')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $GamePath 'BepInEx/core/Mono.Cecil.dll')
Add-Type -Path @((Join-Path $root 'src/LanguagePack.cs'),(Join-Path $root 'src/NumericTranslation.cs'))
$game=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GamePath 'Crawl_Data/Managed/Assembly-CSharp.dll'))
try {
    $literalSet=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $displaySet=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    function Read-TutorialType($type) {
        foreach($method in $type.Methods) {
            if(!$method.HasBody){continue}
            $lastLiteral=$null
            foreach($instruction in $method.Body.Instructions) {
                if($instruction.OpCode.Name -eq 'ldstr') { $lastLiteral=[string]$instruction.Operand; $null=$literalSet.Add($lastLiteral) }
                if($instruction.OpCode.Name -in @('call','callvirt') -and $instruction.Operand.Name -match '^ShowMessage' -and $instruction.Operand.DeclaringType.Name -match '^Tutorial' -and $null -ne $lastLiteral) {
                    if(($lastLiteral -replace '<[^>]*>','') -match '[a-zA-Z]') { $null=$displaySet.Add($lastLiteral) }
                    $lastLiteral=$null
                }
            }
        }
        foreach($nested in $type.NestedTypes) { Read-TutorialType $nested }
    }
    foreach($type in $game.MainModule.Types | Where-Object Name -match '^TutorialSequence') { Read-TutorialType $type }
    $decode={param($s) [Regex]::Replace($s,'\\([nrt\\])',{param($m) switch($m.Groups[1].Value){n{"`n"} r{"`r"} t{"`t"} '\'{'\'}}})}
    $translations=@{}
    foreach($table in Get-ChildItem (Join-Path $root 'Languages/zh-Hans') -Filter '*translations.tsv') {
        foreach($row in Get-Content $table.FullName -Encoding utf8) {
            if(!$row -or $row.StartsWith('#')){continue};$cells=$row.Split("`t");$translations[(& $decode $cells[0])]=(& $decode $cells[1])
        }
    }
    foreach($source in $displaySet) {if(!$translations.ContainsKey($source)){throw "Missing actual tutorial display literal: $source"}}
    $rows=0
    foreach($row in Get-Content (Join-Path $root 'Languages/zh-Hans/tutorial-translations.tsv') -Encoding utf8) {
        if(!$row -or $row.StartsWith('#')){continue};$cells=$row.Split("`t");$source=& $decode $cells[0];$target=& $decode $cells[1]
        if($cells.Length -ne 3 -or !$cells[2]){throw 'Expected three nonempty columns'}
        if(!$literalSet.Contains($source)){throw "Source differs from installed DLL: $source"}
        foreach($pattern in @('<[^>]*>','\[[AB]\]')) {
            $before=@([Regex]::Matches($source,$pattern)|ForEach-Object Value|Sort-Object)
            $after=@([Regex]::Matches($target,$pattern)|ForEach-Object Value|Sort-Object)
            if(($before -join '|') -cne ($after -join '|')){throw "Rich tag/button marker mismatch: $source"}
        }
        $rows++
    }
    $box=$game.MainModule.Types|Where-Object Name -eq 'GuiTutorialMessageBox'
    foreach($name in @('SetContent','ShowButton')) {if(@($box.Methods|Where-Object Name -eq $name).Count -ne 1){throw "Ambiguous/missing patch method $name"}}
    foreach($name in @('m_textMessage','m_textButton')) {if(!($box.Fields|Where-Object {$_.Name -eq $name -and $_.FieldType.FullName -eq 'GUIText'})){throw "Missing GUIText field $name"}}
    Write-Output "PASS: $($displaySet.Count) unique tutorial message literals covered; $rows table rows match installed DLL, preserve rich tags and input markers; both patch signatures verified. DLL read as data only."
} finally {$game.Dispose()}
