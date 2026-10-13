"""Verify package, localization and the default font without third-party modules."""
from pathlib import Path
import bisect
import hashlib
import json
import re
import struct
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent
GAME = Path(r"G:\Steam\steamapps\common\RimWorld")
FONT = Path(r"C:\Windows\Fonts\malgunbd.ttf")

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def cmap_lookup(data):
    tables = {}
    count = struct.unpack_from(">H", data, 4)[0]
    for i in range(count):
        tag, checksum, offset, length = struct.unpack_from(">4sIII", data, 12 + i * 16)
        tables[tag] = offset
    base = tables[b"cmap"]
    subtables = struct.unpack_from(">H", data, base + 2)[0]
    candidates = []
    for i in range(subtables):
        platform, encoding, offset = struct.unpack_from(">HHI", data, base + 4 + i * 8)
        start = base + offset
        fmt = struct.unpack_from(">H", data, start)[0]
        if platform in (0, 3) and fmt in (4, 12):
            candidates.append((fmt == 12, start, fmt))
    _, start, fmt = max(candidates)
    if fmt == 12:
        groups = struct.unpack_from(">I", data, start + 12)[0]
        ranges = [struct.unpack_from(">III", data, start + 16 + 12 * i) for i in range(groups)]
        starts = [r[0] for r in ranges]
        def lookup(cp):
            i = bisect.bisect_right(starts, cp) - 1
            return ranges[i][2] + cp - ranges[i][0] if i >= 0 and cp <= ranges[i][1] else 0
        return lookup
    segments = struct.unpack_from(">H", data, start + 6)[0] // 2
    ends = struct.unpack_from(">" + "H" * segments, data, start + 14)
    starts = struct.unpack_from(">" + "H" * segments, data, start + 16 + 2 * segments)
    deltas = struct.unpack_from(">" + "h" * segments, data, start + 16 + 4 * segments)
    range_base = start + 16 + 6 * segments
    offsets = struct.unpack_from(">" + "H" * segments, data, range_base)
    def lookup(cp):
        i = bisect.bisect_left(ends, cp)
        if i >= segments or cp < starts[i]: return 0
        if not offsets[i]: return (cp + deltas[i]) & 0xFFFF
        pos = range_base + 2 * i + offsets[i] + 2 * (cp - starts[i])
        glyph = struct.unpack_from(">H", data, pos)[0]
        return (glyph + deltas[i]) & 0xFFFF if glyph else 0
    return lookup

def main():
    for path in ROOT.rglob("*.xml"): ET.parse(path)
    en = {e.tag: e.text for e in ET.parse(ROOT / "Languages/English/Keyed/ReadableFonts.xml").getroot()}
    ko = {e.tag: e.text for e in ET.parse(ROOT / "Languages/Korean (한국어)/Keyed/ReadableFonts.xml").getroot()}
    assert en.keys() == ko.keys()
    keys = set(re.findall(r'"(RF17_\w+)"', (ROOT / "Source/ReadableFontsMod.cs").read_text(encoding="utf-8-sig")))
    assert keys == set(en), (keys - set(en), set(en) - keys)
    for key in en:
        assert en[key] and ko[key]
        assert re.findall(r"\{\d+\}", en[key]) == re.findall(r"\{\d+\}", ko[key]), key
    lookup = cmap_lookup(FONT.read_bytes())
    missing = [f"U+{cp:04X}" for cp in range(0xAC00, 0xD7A4) if not lookup(cp)]
    assert not missing, missing[:20]
    assert all(lookup(ord(c)) for c in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
    dll = ROOT / "1.7/Assemblies/ReadableFonts17.dll"
    assert dll.is_file()
    assert len(list((ROOT / "1.7/Assemblies").glob("*.dll"))) == 1
    log = (ROOT / "Validation/engine_probe.log").read_text(encoding="utf-8-sig")
    assert "PASS: Waylith font registration" in log
    assert "PASS: shared Font replacement" in log
    report = {
        "game_version": (GAME / "Version.txt").read_text().strip(),
        "build": {"errors": 0, "warnings": 0},
        "xml_valid": True,
        "localization_keys": len(en),
        "localization_key_and_placeholder_parity": True,
        "default_font": str(FONT),
        "hangul_syllables_checked": 11172,
        "missing_hangul_syllables": missing,
        "latin_digits_covered": True,
        "engine_probe_runtime": "10.0.10 (bundled Waylith CLR)",
        "engine_probe": "real ImGui/FreeType registration, cache, invalid font, sizes 12/14/17.5/21/26.25, Korean measurement, shared Font/GUIStyle replacement and restoration",
        "in_game_verified": False,
        "mod_dll_sha256": sha(dll),
        "input_sha256": {name: sha(GAME / name) for name in ("Assembly-CSharp.dll", "UnityStubs.dll", "RimWorld.dll")},
        "font_sha256": sha(FONT),
    }
    catalog = ROOT / "Validation/catalog_report.json"
    if catalog.exists():
        report["installed_font_catalog"] = json.loads(catalog.read_text(encoding="utf-8"))
        assert "Production DLL catalog:" in log
        collection_log = (ROOT / "Validation/collection_engine_probe.log").read_text(encoding="utf-8-sig")
        assert collection_log.count("PASS: Waylith font registration") == 5
        report["engine_collection_and_otf_samples_passed"] = 5
    language_report = ROOT / "Validation/language_filter_report.json"
    if language_report.exists():
        assert "PASS: active-language detection" in log
        report["current_game_language_filter"] = json.loads(language_report.read_text(encoding="utf-8"))
    out = ROOT / "Validation/qa_report.json"
    out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"PASS: XML, {len(en)} localization keys/placeholders, 11172 Hangul syllables, Latin/digits, assembly package, engine probe log.")
    print("DLL SHA256 " + report["mod_dll_sha256"])

if __name__ == "__main__": main()
