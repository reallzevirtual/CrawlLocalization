"""Restore Crawl's serialized Font layout without modifying merged TTF bytes.

Unity reimports TTF ascent/line spacing during BuildPipeline. In-place asset
replacement keeps those Font fields, so a bundle must explicitly preserve them.
Requires UnityPy; operates only on the generated bundle, never on game assets.
"""
import argparse
from pathlib import Path
import UnityPy

parser = argparse.ArgumentParser()
parser.add_argument("bundle", type=Path)
args = parser.parse_args()
layout = {}
for line in Path(__file__).with_name("font-layout.tsv").read_text(encoding="utf-8-sig").splitlines():
    if line and not line.startswith("#"):
        name, ascent, descent, spacing = line.split("\t")
        layout[name] = dict(zip(("m_Ascent", "m_Descent", "m_LineSpacing"), map(float, (ascent, descent, spacing))))

# Read into memory so UnityPy cannot keep a Windows file handle open when the
# verified output atomically replaces the original build artifact.
env = UnityPy.load(args.bundle.read_bytes())
original_data = {}
for obj in env.objects:
    if obj.type.name != "Font":
        continue
    tree = obj.read_typetree()
    name = tree["m_Name"]
    if name not in layout or name in original_data:
        raise ValueError(f"Unexpected or duplicate font: {name}")
    original_data[name] = tree
    updated = dict(tree)
    updated.update(layout[name])
    obj.save_typetree(updated)
if set(original_data) != set(layout):
    raise ValueError("Bundle does not contain the ten expected fonts")

result = env.file.save(packer="lz4")
# Reparse the final serialized bytes and ensure no unrelated Font field changed.
checked = UnityPy.load(result)
for obj in checked.objects:
    if obj.type.name != "Font":
        continue
    tree = obj.read_typetree()
    name = tree["m_Name"]
    expected = dict(original_data[name])
    expected.update(layout[name])
    if tree != expected:
        raise ValueError(f"Unexpected font serialization change: {name}")
temporary = args.bundle.with_suffix(".verified-layout")
temporary.write_bytes(result)
temporary.replace(args.bundle)
print("Preserved original ascent, descent and line spacing for all ten fonts; all other Font fields unchanged.")
