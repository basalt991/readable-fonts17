using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using UnityEngine;
using UnityStubs.Gui;
using Verse;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]

namespace ReadableFonts17
{
    public sealed class FontSettings : ModSettings
    {
        public bool enabled = true;
        public string preset = "bold";
        public string customPath = "";
        public string selectedPath = "";
        public int selectedFace;
        public string selectedName = "";
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref preset, "preset", "bold");
            Scribe_Values.Look(ref customPath, "customPath", "");
            Scribe_Values.Look(ref selectedPath, "selectedPath", "");
            Scribe_Values.Look(ref selectedFace, "selectedFace", 0);
            Scribe_Values.Look(ref selectedName, "selectedName", "");
        }
        public string ResolvePath()
        {
            if (!string.IsNullOrWhiteSpace(selectedPath)) return selectedPath;
            if (preset == "custom") return (customPath ?? "").Trim().Trim('"');
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                preset == "regular" ? "malgun.ttf" : "malgunbd.ttf");
        }
        public int ResolveFace() => string.IsNullOrWhiteSpace(selectedPath) ? 0 : selectedFace;
        public string DisplayName => !string.IsNullOrWhiteSpace(selectedName) ? selectedName :
            preset == "bold" ? "RF17_Bold".Translate() : preset == "regular" ? "RF17_Regular".Translate() : Path.GetFileNameWithoutExtension(ResolvePath());
    }

    public sealed class ReadableFontsMod : Mod
    {
        internal static FontSettings Settings;
        private string search = "";
        private bool languageOnly = true;
        private Vector2 scroll;
        private InstalledFont[] lastEntries;
        private InstalledFont[] visibleFonts = Array.Empty<InstalledFont>();
        private string lastSearch;
        private bool lastLanguageOnly;
        private LanguageFontProfile lastLanguageProfile;
        public ReadableFontsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<FontSettings>();
            var harmony = new Harmony("local.readablefonts17");
            harmony.Patch(AccessTools.Method(typeof(Text), nameof(Text.StartOfOnGUI)),
                postfix: new HarmonyMethod(typeof(FontController), nameof(FontController.OnGUI)));
            FontController.RequestApply();
        }
        public override string SettingsCategory() => "RF17_Title".Translate();
        public override void WriteSettings()
        {
            base.WriteSettings();
            FontController.RequestApply();
        }
        public override void DoSettingsWindowContents(Rect rect)
        {
            InstalledFontCatalog.Start();
            var list = new Listing_Standard();
            list.Begin(rect);
            list.CheckboxLabeled("RF17_Enabled".Translate(), ref Settings.enabled);
            list.Label("RF17_Selected".Translate(Settings.DisplayName));
            Rect searchRow = list.GetRect(30f);
            Widgets.Label(new Rect(searchRow.x, searchRow.y, 65f, searchRow.height), "RF17_Search".Translate());
            search = Widgets.TextField(new Rect(searchRow.x + 65f, searchRow.y, searchRow.width - 65f, searchRow.height), search);
            var languageProfile = GameLanguageFonts.Current;
            list.CheckboxLabeled("RF17_LanguageOnly".Translate(languageProfile.Name), ref languageOnly);
            var entries = InstalledFontCatalog.Entries;
            if (!ReferenceEquals(entries, lastEntries) || search != lastSearch || languageOnly != lastLanguageOnly || !ReferenceEquals(languageProfile, lastLanguageProfile))
            {
                visibleFonts = InstalledFontCatalog.Filter(entries, search, languageOnly, languageProfile);
                lastEntries = entries; lastSearch = search; lastLanguageOnly = languageOnly; lastLanguageProfile = languageProfile; scroll = Vector2.zero;
            }
            Rect countRow = list.GetRect(30f);
            string countText = InstalledFontCatalog.Loading ? "RF17_Scanning".Translate() :
                "RF17_Count".Translate(visibleFonts.Length, entries.Length);
            Widgets.Label(new Rect(countRow.x, countRow.y, countRow.width - 120f, countRow.height), countText);
            if (Widgets.ButtonText(new Rect(countRow.xMax - 110f, countRow.y, 110f, countRow.height), "RF17_Refresh".Translate())) InstalledFontCatalog.Start(true);
            Rect viewport = list.GetRect(Math.Max(100f, rect.height - list.CurHeight - 168f));
            DrawFontList(viewport, languageProfile);
            list.Gap(8f);
            Rect buttons = list.GetRect(30f);
            if (Widgets.ButtonText(new Rect(buttons.x, buttons.y, (buttons.width - 8f) / 2, 30f), "RF17_Apply".Translate())) WriteSettings();
            if (Widgets.ButtonText(new Rect(buttons.x + (buttons.width + 8f) / 2, buttons.y, (buttons.width - 8f) / 2, 30f), "RF17_Restore".Translate()))
            { Settings.enabled = false; WriteSettings(); }
            list.Label(FontController.StatusText);
            if (InstalledFontCatalog.Error.Length > 0) list.Label("RF17_ScanFailed".Translate(InstalledFontCatalog.Error));
            list.Label("RF17_Preview".Translate(languageProfile.Preview));
            list.End();
        }
        private void DrawFontList(Rect viewport, LanguageFontProfile languageProfile)
        {
            float rowHeight = Math.Max(32f, Text.LineHeight + 8f);
            Rect view = new Rect(0f, 0f, viewport.width - 20f, Math.Max(viewport.height, visibleFonts.Length * rowHeight));
            Widgets.BeginScrollView(viewport, ref scroll, view);
            string selectedPath = Settings.ResolvePath(); int selectedFace = Settings.ResolveFace();
            for (int i = 0; i < visibleFonts.Length; i++)
            {
                Rect row = new Rect(0f, i * rowHeight, view.width, rowHeight);
                if (row.yMax < scroll.y || row.y > scroll.y + viewport.height) continue;
                var font = visibleFonts[i];
                bool selected = font.FaceIndex == selectedFace && string.Equals(font.Path, selectedPath, StringComparison.OrdinalIgnoreCase);
                if (selected) Widgets.DrawHighlightSelected(row);
                Widgets.DrawHighlightIfMouseover(row);
                Rect nameRect = new Rect(row.x + 8f, row.y + 4f, row.width - 95f, row.height - 4f);
                Widgets.Label(nameRect, Text.ClampTextWithEllipsis(nameRect, font.DisplayName));
                if (font.Supports(languageProfile)) Widgets.Label(new Rect(row.xMax - 80f, row.y + 4f, 75f, row.height - 4f), "RF17_LanguageBadge".Translate());
                TooltipHandler.TipRegion(row, font.DisplayName + "\n" + "RF17_Coverage".Translate(languageProfile.Name,
                    font.SupportedCount(languageProfile), languageProfile.RequiredCharacters.Length));
                if (Widgets.ButtonInvisible(row))
                {
                    Settings.selectedPath = font.Path; Settings.selectedFace = font.FaceIndex; Settings.selectedName = font.DisplayName;
                }
            }
            if (visibleFonts.Length == 0 && !InstalledFontCatalog.Loading)
                Widgets.Label(new Rect(8f, 8f, view.width - 16f, 60f), "RF17_NoMatches".Translate());
            Widgets.EndScrollView();
        }
    }

    public static class FontController
    {
        private static readonly FieldInfo FontsField = AccessTools.Field(typeof(Text), "fonts");
        private static readonly FieldInfo StylesField = AccessTools.Field(typeof(Text), "fontStyles");
        private static readonly FieldInfo HeightsField = AccessTools.Field(typeof(Text), "lineHeights");
        private static readonly FieldInfo SpacingField = AccessTools.Field(typeof(Text), "spaceBetweenLines");
        private static readonly FieldInfo MaterialField = AccessTools.Field(typeof(Font), "_material");
        private static Font[] originals;
        private static int[] originalIndices;
        private static int[] currentIndices;
        private static float[] originalHeights;
        private static float[] originalSpacing;
        private static Material[] originalMaterials;
        private static float[] sizes;
        private static bool pending;
        private static bool applying;
        private static string statusKey = "RF17_Waiting";
        private static string statusDetail = "";
        public static string StatusText => statusKey.Translate(statusDetail);
        public static void RequestApply() => pending = true;

        // The GUI backend and ImGui context are available on the main thread here.
        // Subsequent frames perform no font I/O or reflection.
        public static void OnGUI()
        {
            if (!pending || applying || GuiBackend.Current == null || ReadableFontsMod.Settings == null) return;
            applying = true;
            pending = false;
            try
            {
                CaptureOriginals();
                var settings = ReadableFontsMod.Settings;
                if (!settings.enabled)
                {
                    Restore(); statusKey = "RF17_Restored"; statusDetail = ""; return;
                }
                var path = settings.ResolvePath();
                var data = ReadFont(path, settings.ResolveFace());
                string hash;
                using (var sha = SHA256.Create()) hash = Convert.ToHexString(sha.ComputeHash(data));
                var indices = new int[originals.Length];
                for (int i = 0; i < indices.Length; i++)
                {
                    // Waylith caches only by name and byte length. Include the size slot,
                    // rendering mode and hash so a different font cannot reuse an old slot.
                    var name = "ReadableFonts17/" + hash + "/" + i + "/HintedSmooth";
                    indices[i] = GuiBackend.Current.RegisterFont(name, data, sizes[i], 1);
                    if (indices[i] < 0) throw new InvalidDataException("RF17_InvalidFont".Translate());
                }
                currentIndices = indices;
                for (int i = 0; i < originals.Length; i++)
                {
                    originals[i].GameFontIndex = indices[i];
                    MaterialField?.SetValue(originals[i], null);
                }
                RefreshMetrics();
                statusKey = "RF17_Applied"; statusDetail = settings.DisplayName;
                Log.Message("[Readable Fonts 1.7] Applied " + path + " (indices " + string.Join(",", indices) + ").");
            }
            catch (Exception e)
            {
                Restore(); statusKey = "RF17_Failed"; statusDetail = e.Message;
                Log.Warning("[Readable Fonts 1.7] Font replacement failed; original font restored. " + e);
            }
            finally { applying = false; }
        }
        public static byte[] ReadFont(string path, int faceIndex = 0)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("RF17_EmptyPath".Translate());
            var extension = Path.GetExtension(path);
            if (!extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".otf", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".ttc", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".otc", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("RF17_Extension".Translate());
            var info = new FileInfo(path);
            if (!info.Exists) throw new FileNotFoundException("RF17_NotFound".Translate(), path);
            if (info.Length < 12 || info.Length > 256 * 1024 * 1024) throw new InvalidDataException("RF17_InvalidFont".Translate());
            var data = FontFiles.ReadFace(path, faceIndex);
            bool trueType = data[0] == 0 && data[1] == 1 && data[2] == 0 && data[3] == 0;
            bool openType = data[0] == 'O' && data[1] == 'T' && data[2] == 'T' && data[3] == 'O';
            bool appleTrueType = data[0] == 't' && data[1] == 'r' && data[2] == 'u' && data[3] == 'e';
            if (!trueType && !openType && !appleTrueType) throw new InvalidDataException("RF17_InvalidFont".Translate());
            return data;
        }
        private static void CaptureOriginals()
        {
            if (originals != null) return;
            var fonts = FontsField?.GetValue(null) as Font[];
            if (fonts == null || fonts.Length != 3 || StylesField == null || HeightsField == null || SpacingField == null)
                throw new MissingFieldException("RimWorld 1.7 Text font fields are unavailable.");
            var indices = new int[fonts.Length];
            var pixelSizes = new float[fonts.Length];
            var materials = new Material[fonts.Length];
            for (int i = 0; i < fonts.Length; i++)
            {
                if (fonts[i] == null) throw new InvalidOperationException("RimWorld Text font is null.");
                indices[i] = fonts[i].GameFontIndex;
                pixelSizes[i] = fonts[i].fontSize;
                if (pixelSizes[i] <= 0) throw new InvalidOperationException("RimWorld font size is invalid.");
                materials[i] = MaterialField?.GetValue(fonts[i]) as Material;
            }
            originalHeights = (float[])((float[])HeightsField.GetValue(null)).Clone();
            originalSpacing = (float[])((float[])SpacingField.GetValue(null)).Clone();
            originalIndices = indices; sizes = pixelSizes; originalMaterials = materials;
            originals = (Font[])fonts.Clone();
        }
        private static void RefreshMetrics()
        {
            var styles = (GUIStyle[])StylesField.GetValue(null);
            var heights = (float[])HeightsField.GetValue(null);
            var spacing = (float[])SpacingField.GetValue(null);
            var single = new GUIContent("W"); var multiple = new GUIContent("W\nW");
            for (int i = 0; i < styles.Length; i++)
            {
                heights[i] = styles[i].CalcHeight(single, 999f);
                spacing[i] = styles[i].CalcHeight(multiple, 999f) - heights[i] * 2;
            }
            Widgets.ClearLabelCache();
        }
        private static void Restore()
        {
            if (originals == null || currentIndices == null) return;
            for (int i = 0; i < originals.Length; i++)
            {
                // Preserve any later font change made by another mod.
                if (originals[i].GameFontIndex != currentIndices[i]) continue;
                originals[i].GameFontIndex = originalIndices[i];
                MaterialField?.SetValue(originals[i], originalMaterials[i]);
            }
            Array.Copy(originalHeights, (float[])HeightsField.GetValue(null), originalHeights.Length);
            Array.Copy(originalSpacing, (float[])SpacingField.GetValue(null), originalSpacing.Length);
            currentIndices = null;
            Widgets.ClearLabelCache();
        }
    }
}
