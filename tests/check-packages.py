"""Verify default and Chinese player archives against their source files."""
from pathlib import Path
from zipfile import ZipFile
import re
root=Path(__file__).resolve().parents[1]
base='CrawlLocalization-'+(root/'VERSION').read_text(encoding='utf-8-sig').strip()
language_files=['language.ini','fonts.tsv','textmesh-paths.txt','translations.tsv',
    'vault-translations.tsv','deity-translations.tsv','chapter-translations.tsv',
    'shop-translations.tsv','gameplay-translations.tsv','progression-translations.tsv',
    'shop-il-translations.tsv','crawlonline-translations.tsv','tutorial-translations.tsv']
non_chinese=sorted(p.name for p in (root/'Languages').iterdir() if p.is_dir()
    and (p/'language.ini').is_file() and not re.match(r'^zh(?:-|$)',p.name,re.I) and p.name.lower()!='en')
for suffix,locales,readme in [('',non_chinese,'packaging/README.md'),
    ('-zh-Hans',['zh-Hans'],'packaging/zh-Hans/README.md')]:
    expected={'README.md':root/readme}
    for locale in locales:
        for name in language_files:
            relative=f'Languages/{locale}/{name}';expected[relative]=root/relative
    # Universal fonts ship with BOTH editions; Chinese also includes its own font.
    fonts={'Fonts/crawl-multilingual.bundle'}
    if suffix:fonts.add('Fonts/crawl-fonts.bundle')
    for locale in locales:
        for line in (root/'Languages'/locale/'fonts.tsv').read_text(encoding='utf-8-sig').splitlines():
            if not line.strip() or line.startswith('#'):continue
            cells=line.split('\t')
            if cells[1]!='system':fonts.add(cells[2].replace('\\','/'))
    for relative in fonts:
        expected[relative]=root/relative
        for ext in ('.OFL.txt','.NOTICE.txt'):
            notice=Path(relative).with_suffix(ext)
            if (root/notice).is_file():expected[notice.as_posix()]=root/notice
    with ZipFile(root/'artifacts'/(base+suffix+'.zip')) as z:
        assert z.testzip() is None
        actual={name.replace('\\','/'):name for name in z.namelist() if not name.endswith('/')}
        assert set(actual)==set(expected)|{'CrawlLocalization.dll'},(suffix,set(actual)^(set(expected)|{'CrawlLocalization.dll'}))
        for name,source in expected.items():assert z.read(actual[name])==source.read_bytes(),name
        assert z.read(actual['CrawlLocalization.dll'])==(root/'artifacts'/(base+suffix)/'CrawlLocalization.dll').read_bytes()
        text=z.read(actual['README.md']).decode('utf-8-sig')
        if suffix:
            assert '制作' not in text and 'Translator' not in text and 'Create another language' not in text
            assert fonts=={'Fonts/crawl-fonts.bundle','Fonts/crawl-multilingual.bundle'}
        else:
            assert not re.search('[\u3400-\u9fff]',text)
            assert 'Fonts/crawl-fonts.bundle' not in actual
    print('PASS',base+suffix+'.zip',locales,': exact runtime files, unchanged translations, expected fonts/notices and README.')
