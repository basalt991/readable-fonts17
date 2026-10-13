using ReadableFonts17;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
[assembly: SupportedOSPlatform("windows")]

int assertions = 0;
void Check(bool condition, string message) { assertions++; if(!condition) throw new Exception("FAIL: " + message); }
var timer = Stopwatch.StartNew();
var fonts = InstalledFontCatalog.Scan();
var koreanProfile = LanguageFontProfile.Create("Korean", "한국어");
timer.Stop();
Check(fonts.Length > 0, "installed fonts detected");
var bold = fonts.Single(f => f.Path.Equals(@"C:\Windows\Fonts\malgunbd.ttf", StringComparison.OrdinalIgnoreCase) && f.FaceIndex == 0);
Check(bold.FullKorean && bold.HangulCount == 11172, "Malgun Bold full Hangul coverage");
Check(bold.DisplayName.Contains("맑은 고딕"), "localized Korean family name");
Check(InstalledFontCatalog.Filter(fonts, "malgun", true, koreanProfile).Contains(bold), "filename search");
Check(InstalledFontCatalog.Filter(fonts, "맑은", true, koreanProfile).Contains(bold), "Korean name search");
Check(InstalledFontCatalog.Filter(fonts, "no-font-with-this-name", false, koreanProfile).Length == 0, "empty search results");
Check(InstalledFontCatalog.Filter(fonts, "", true, koreanProfile).All(f => f.FullKorean), "Korean filter excludes partial coverage");
Check(fonts.GroupBy(f => f.Path.ToLowerInvariant() + "|" + f.FaceIndex).All(g => g.Count() == 1), "file/face deduplication");
Check(FontFiles.ReadFace(bold.Path, 0).SequenceEqual(File.ReadAllBytes(bold.Path)), "standalone font unchanged");
var batang = fonts.Where(f => f.Path.EndsWith("batang.ttc", StringComparison.OrdinalIgnoreCase)).ToArray();
Check(batang.Length >= 2, "collection faces listed separately");
Directory.CreateDirectory(args[0]);
foreach (var face in batang)
{
    var data = FontFiles.ReadFace(face.Path, face.FaceIndex);
    Check(data.Length > 12 && data[0] == 0 && data[1] == 1, "TTC extracted to SFNT");
    uint checksum = 0;
    for(int i = 0; i < data.Length; i += 4) checksum = unchecked(checksum + ((uint)data[i] << 24) + ((uint)data[i+1] << 16) + ((uint)data[i+2] << 8) + data[i+3]);
    Check(checksum == 0xB1B0AFBA, "extracted face checksum");
    var extractedPath = Path.Combine(args[0], "batang-face-" + face.FaceIndex + ".ttf");
    File.WriteAllBytes(extractedPath, data);
    var extracted = FontFiles.ReadEntries(extractedPath).Single();
    Check(extracted.DisplayName == face.DisplayName && extracted.HangulCount == face.HangulCount, "extracted face metadata and cmap preserved");
}
try { FontFiles.ReadFace(bold.Path, 1); Check(false, "invalid face rejected"); }
catch (InvalidDataException) { assertions++; }
string damaged = Path.Combine(args[0], "damaged.ttf"); File.WriteAllBytes(damaged, new byte[12]);
try { Check(FontFiles.ReadEntries(damaged).Length == 0, "damaged font skipped"); }
catch (InvalidDataException) { assertions++; }
var englishProfile = LanguageFontProfile.Create("English", "English");
var japaneseProfile = LanguageFontProfile.Create("Japanese", "日本語", new[]{"健康建築文字確認"});
var simplifiedProfile = LanguageFontProfile.Create("ChineseSimplified", "简体中文", new[]{"界面游戏"});
var traditionalProfile = LanguageFontProfile.Create("ChineseTraditional", "繁體中文", new[]{"介面遊戲"});
var russianProfile = LanguageFontProfile.Create("Russian", "Русский");
var germanProfile = LanguageFontProfile.Create("German", "Deutsch", new[]{"ÄÖÜäöüß"});
var arial = fonts.First(f => f.Path.EndsWith("\\arial.ttf", StringComparison.OrdinalIgnoreCase));
Check(arial.Supports(englishProfile), "English-only Latin font is eligible");
Check(!arial.Supports(koreanProfile), "Latin font is rejected for Korean");
Check(!arial.Supports(japaneseProfile), "Latin font is rejected for Japanese");
Check(arial.Supports(russianProfile), "Arial Cyrillic coverage");
Check(arial.Supports(germanProfile), "German accented characters");
Check(InstalledFontCatalog.Filter(fonts, "", true, englishProfile).Length > InstalledFontCatalog.Filter(fonts, "", true, koreanProfile).Length, "English filter is broader than Korean filter");
Check(InstalledFontCatalog.Filter(fonts, "", false, japaneseProfile).Length == fonts.Length, "disabled language filter lists all fonts");
Check(InstalledFontCatalog.Filter(fonts, "", true, japaneseProfile).Length > 0, "Japanese glyph profile");
Check(InstalledFontCatalog.Filter(fonts, "", true, simplifiedProfile).Length > 0, "Simplified Chinese glyph profile");
Check(InstalledFontCatalog.Filter(fonts, "", true, traditionalProfile).Length > 0, "Traditional Chinese glyph profile");
Check(arial.Supports(englishProfile), "cached result updates after changing language");
var formatting = LanguageFontProfile.Create("English", "English", new[]{"<color=Ж>Hello {Ω}</color>"});
Check(!formatting.RequiredCharacters.Contains(0x0416) && !formatting.RequiredCharacters.Contains(0x03A9), "tags and placeholders excluded");
var greekUi = LanguageFontProfile.Create("English", "English", new[]{"alpha α"});
Check(greekUi.RequiredCharacters.Contains(0x03B1), "extra letters from base UI translations included");
var hindi = LanguageFontProfile.Create("Hindi", "हिन्दी", new[]{"हिन्दी"});
Check(hindi.RequiredCharacters.Contains(0x093F) && hindi.RequiredCharacters.Contains(0x094D), "combining vowel and virama marks retained");
var supplementary = LanguageFontProfile.Create("Custom", "\U00010400");
Check(supplementary.RequiredCharacters.Contains(0x10400), "supplementary Unicode letters retained");
var emoji = fonts.First(f => f.Path.EndsWith("\\seguiemj.ttf", StringComparison.OrdinalIgnoreCase));
Check(emoji.Coverage.Has(0x1F600), "format 12 supplementary glyphs");
var languageCounts = new Dictionary<string, int>();
foreach (var profile in new[]{englishProfile, koreanProfile, japaneseProfile, simplifiedProfile, traditionalProfile, russianProfile, germanProfile})
    languageCounts[profile.Name] = InstalledFontCatalog.Filter(fonts, "", true, profile).Length;
File.WriteAllText(Path.Combine(args[0], "language_filter_report.json"), JsonSerializer.Serialize(new {assertions, language_font_counts = languageCounts, shaping_verified = false, in_game_ui_verified = false}, new JsonSerializerOptions {WriteIndented = true}));
var report = new { assertions, installed_faces = fonts.Length, full_korean_faces = fonts.Count(f => f.FullKorean), collection_faces = fonts.Count(f => f.FaceIndex > 0), scan_milliseconds = timer.ElapsedMilliseconds, default_font_name = bold.DisplayName, collection_samples = batang.Select(f => new {f.FaceIndex, f.DisplayName, f.HangulCount}), in_game_ui_verified = false };
File.WriteAllText(Path.Combine(args[0], "catalog_report.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllText(Path.Combine(args[0], "installed_fonts.json"), JsonSerializer.Serialize(fonts.Select(f => new {f.DisplayName, f.Path, f.FaceIndex, f.HangulCount}), new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(report));
Console.WriteLine("PASS: " + assertions + " installed-font enumeration/name/filter/collection/validation checks.");
