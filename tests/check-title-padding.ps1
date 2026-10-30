# Negative fixtures never modify the user's real translations.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('crawl-title-padding-'+[Guid]::NewGuid().ToString('N'))
$language=Join-Path $fixture 'Languages/zh-Hans'
$utf8=[Text.UTF8Encoding]::new($false)
try {
    New-Item -ItemType Directory -Path $language -Force | Out-Null
    $cases=@(
        @('translations.tsv','_OPTIONS_','-Options_','leading underscore'),
        @('translations.tsv','_OPTIONS_','_Options-','trailing underscore'),
        @('progression-translations.tsv','@progression.join-title:{0}:{1}','P{0}_{1}_','leading underscore'),
        @('crawlonline-translations.tsv','@online:title:Host lobby','_Host_lobby-','trailing underscore')
    )
    foreach($case in $cases) {
        $file=Join-Path $language $case[0]
        [IO.File]::WriteAllText($file,($case[1]+"`t"+$case[2]+"`n"),$utf8)
        $message=''
        try { & (Join-Path $root 'translator-kit/crawl-language-pack/scripts/check-language.ps1') -PluginRoot $fixture -Language zh-Hans | Out-Null }
        catch { $message=$_.Exception.Message }
        if(!$message.Contains($case[3])) { throw "Padding regression: expected '$($case[3])', got '$message'" }
        Remove-Item -LiteralPath $file
    }
    Write-Output 'PASS: 4 inverse-title padding rejection checks.'
} finally {
    # Delete only the exact private fixture directory created above.
    $full=[IO.Path]::GetFullPath($fixture)
    $temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
    if([IO.Directory]::GetParent($full).FullName -ne $temp -or [IO.Path]::GetFileName($full) -notlike 'crawl-title-padding-*') { throw 'Unexpected fixture path' }
    if(Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force }
}
