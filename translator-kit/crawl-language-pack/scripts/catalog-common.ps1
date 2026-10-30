$crawlUtf8=[Text.UTF8Encoding]::new($false,$true)
function Decode([string]$text) {
    $out=[Text.StringBuilder]::new()
    for($i=0;$i -lt $text.Length;$i++) {
        if($text[$i] -ne '\'){$null=$out.Append($text[$i]);continue}
        $i++;if($i -eq $text.Length){throw 'Trailing unescaped backslash'}
        switch($text[$i]) {'n'{$null=$out.Append("`n")};'r'{$null=$out.Append("`r")};'t'{$null=$out.Append("`t")};'\'{$null=$out.Append('\')};default{throw "Unsupported escape: \$($text[$i])"}}
    }
    $out.ToString().Replace("`r`n","`n").Replace("`r","`n")
}
function Read-CrawlCatalog([string]$path) {
    $entries=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::OrdinalIgnoreCase)
    $tables=[ordered]@{}
    foreach($file in Get-ChildItem -LiteralPath $path -Filter '*translations.tsv' | Sort-Object Name) {
        $rows=[Collections.Generic.List[object]]::new();$number=0
        foreach($line in [IO.File]::ReadAllLines($file.FullName,$crawlUtf8)) {
            $number++;if(!$line.Trim() -or $line.StartsWith('#')){continue}
            try {
                $cells=$line.Split("`t")
                if($cells.Length -notin @(2,3) -or !$cells[0]){throw 'Expected source<TAB>@TODO[<TAB>English context]'}
                if($cells[1] -cne '@TODO'){throw 'Canonical catalog targets must all be @TODO'}
                $key=Decode $cells[0]
                if($entries.ContainsKey($key)){throw "Duplicate catalog key; first seen in $($entries[$key].Table):$($entries[$key].Number)"}
                $row=[pscustomobject]@{Key=$key;RawKey=$cells[0];Line=$line;Table=$file.Name;Number=$number}
                $entries.Add($key,$row);$rows.Add($row)
            } catch {throw "catalog/$($file.Name):${number}: $($_.Exception.Message)"}
        }
        $tables[$file.Name]=$rows
    }
    if(!$entries.Count){throw 'No canonical translation entries found'}
    [pscustomobject]@{Entries=$entries;Tables=$tables}
}
function Get-CrawlLegacyInputs([string]$language) {
    $aliases=@{}
    if($language -ieq 'zh-Hans') {
        # Legacy inputs for manually modified Chinese assets; never canonical sources.
        $aliases[[Regex]::Unescape('< \u6e38\u620f\u97f3\u6548: {0} >')]='translations.tsv'
        $aliases[[Regex]::Unescape('< \u80cc\u666f\u97f3\u4e50: {0} >')]='translations.tsv'
        $aliases[[Regex]::Unescape('\u590d\u6d3b\u8282\u5f69\u86cb: ON')]='translations.tsv'
        $aliases[[Regex]::Unescape('\u590d\u6d3b\u8282\u5f69\u86cb: OFF')]='translations.tsv'
    }
    return $aliases
}
