"""Read visible translation characters, matching FontService's rich-tag handling."""
import re

RICH_TAGS = re.compile(r'</?(?:b|i|size|color|material)(?:=[^>]*)?>|<quad\s[^>]*>', re.I)


def visible_text(cell):
    escapes = {'n': '\n', 'r': '\r', 't': '\t', '\\': '\\'}
    text = re.sub(r'\\([nrt\\])', lambda match: escapes[match[1]], cell)
    return RICH_TAGS.sub('', text.replace('{0}', '0123456789'))
