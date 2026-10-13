using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ReadableFonts17
{
    public sealed class InstalledFont
    {
        public string Path;
        public int FaceIndex;
        public string DisplayName;
        public string SearchText;
        public int HangulCount;
        public bool FullKorean => HangulCount == 11172;
        public UnicodeGlyphCoverage Coverage;
        private LanguageFontProfile cachedProfile;
        private int cachedCount;
        public int SupportedCount(LanguageFontProfile profile)
        {
            if (!ReferenceEquals(profile, cachedProfile)) { cachedCount = profile.CountSupported(this); cachedProfile = profile; }
            return cachedCount;
        }
        public bool Supports(LanguageFontProfile profile) => SupportedCount(profile) == profile.RequiredCharacters.Length;
    }

    public static class InstalledFontCatalog
    {
        private static readonly object Gate = new object();
        private static volatile InstalledFont[] entries = Array.Empty<InstalledFont>();
        private static volatile bool started;
        private static volatile bool loading;
        private static volatile string error = "";
        public static InstalledFont[] Entries => entries;
        public static bool Loading => loading;
        public static string Error => error;

        public static void Start(bool refresh = false)
        {
            lock (Gate)
            {
                if (loading || (started && !refresh)) return;
                started = true; loading = true; error = "";
                Task.Run(() =>
                {
                    try { entries = Scan(); }
                    catch (Exception e) { error = e.Message; }
                    finally { loading = false; }
                });
            }
        }

        public static InstalledFont Find(string path, int faceIndex) => entries.FirstOrDefault(f =>
            f.FaceIndex == faceIndex && string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase));

        public static InstalledFont[] Filter(InstalledFont[] fonts, string search, bool languageOnly, LanguageFontProfile profile)
        {
            string query = (search ?? "").Trim();
            return fonts.Where(f => (!languageOnly || f.Supports(profile)) &&
                (query.Length == 0 || f.SearchText.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
        }

        public static InstalledFont[] Scan()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string system = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string user = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");
            ReadRegistry(Registry.LocalMachine, system, paths);
            ReadRegistry(Registry.CurrentUser, user, paths);
            foreach (var directory in new[] {system, user})
            {
                try
                {
                    if (Directory.Exists(directory)) foreach (var file in Directory.GetFiles(directory)) AddPath(file, paths);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            var result = new List<InstalledFont>();
            foreach (string path in paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                try { result.AddRange(FontFiles.ReadEntries(path)); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is OverflowException) { }
            }
            return result.OrderBy(f => f.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(f => f.Path, StringComparer.OrdinalIgnoreCase).ThenBy(f => f.FaceIndex).ToArray();
        }

        private static void ReadRegistry(RegistryKey root, string relativeDirectory, HashSet<string> paths)
        {
            try
            {
                using (var key = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"))
                {
                    if (key == null) return;
                    foreach (string name in key.GetValueNames())
                    {
                        var value = key.GetValue(name) as string;
                        if (string.IsNullOrWhiteSpace(value)) continue;
                        value = Environment.ExpandEnvironmentVariables(value);
                        AddPath(System.IO.Path.IsPathRooted(value) ? value : System.IO.Path.Combine(relativeDirectory, value), paths);
                    }
                }
            }
            catch (System.Security.SecurityException) { }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        private static void AddPath(string path, HashSet<string> paths)
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if ((ext == ".ttf" || ext == ".otf" || ext == ".ttc" || ext == ".otc") && File.Exists(path))
                paths.Add(System.IO.Path.GetFullPath(path));
        }
    }

    // SFNT metadata is read without loading or rasterizing every installed font.
    // TTC/OTC faces are extracted to standalone SFNT data only when selected.
    public static class FontFiles
    {
        private sealed class Table { public uint Tag; public uint Offset; public uint Length; }
        private static ushort U16(byte[] b, int o) { Check(b, o, 2); return (ushort)((b[o] << 8) | b[o + 1]); }
        private static uint U32(byte[] b, int o) { Check(b, o, 4); return ((uint)b[o] << 24) | ((uint)b[o + 1] << 16) | ((uint)b[o + 2] << 8) | b[o + 3]; }
        private static void Put16(byte[] b, int o, int n) { b[o] = (byte)(n >> 8); b[o + 1] = (byte)n; }
        private static void Put32(byte[] b, int o, uint n) { b[o] = (byte)(n >> 24); b[o + 1] = (byte)(n >> 16); b[o + 2] = (byte)(n >> 8); b[o + 3] = (byte)n; }
        private static void Check(byte[] b, int o, int n) { if (o < 0 || n < 0 || o > b.Length - n) throw new InvalidDataException("Invalid font table."); }
        private static byte[] Read(FileStream stream, long offset, int count)
        {
            if (offset < 0 || count < 0 || count > 64 * 1024 * 1024 || offset > stream.Length - count)
                throw new InvalidDataException("Invalid font table bounds.");
            var data = new byte[count]; stream.Position = offset; stream.ReadExactly(data); return data;
        }
        private static uint[] Faces(FileStream stream)
        {
            var header = Read(stream, 0, 12);
            if (U32(header, 0) != 0x74746366) return new uint[] {0}; // ttcf
            uint count = U32(header, 8);
            if (count == 0 || count > 256) throw new InvalidDataException("Invalid font collection.");
            var offsets = Read(stream, 12, checked((int)count * 4));
            return Enumerable.Range(0, (int)count).Select(i => U32(offsets, i * 4)).ToArray();
        }
        private static Table[] Tables(FileStream stream, uint offset)
        {
            var header = Read(stream, offset, 12);
            uint signature = U32(header, 0);
            if (signature != 0x00010000 && signature != 0x4F54544F && signature != 0x74727565)
                throw new InvalidDataException("Unsupported font format.");
            int count = U16(header, 4);
            if (count < 1 || count > 512) throw new InvalidDataException("Invalid font table count.");
            var records = Read(stream, (long)offset + 12, count * 16);
            var tables = new Table[count];
            for (int i = 0; i < count; i++)
            {
                tables[i] = new Table {Tag = U32(records, i * 16), Offset = U32(records, i * 16 + 8), Length = U32(records, i * 16 + 12)};
                if ((ulong)tables[i].Offset + tables[i].Length > (ulong)stream.Length) throw new InvalidDataException("Invalid font table bounds.");
            }
            return tables;
        }

        public static InstalledFont[] ReadEntries(string path)
        {
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                var faces = Faces(stream); var result = new List<InstalledFont>();
                for (int index = 0; index < faces.Length; index++)
                {
                    try
                    {
                        var tables = Tables(stream, faces[index]);
                        var name = tables.FirstOrDefault(t => t.Tag == 0x6E616D65);
                        var cmap = tables.FirstOrDefault(t => t.Tag == 0x636D6170);
                        if (name == null || cmap == null) continue;
                        var names = Names(Read(stream, name.Offset, checked((int)name.Length)));
                        string family = names.Item1.Length == 0 ? System.IO.Path.GetFileNameWithoutExtension(path) : names.Item1;
                        string display = family + (names.Item2.Length == 0 ? "" : " — " + names.Item2);
                        UnicodeGlyphCoverage coverage;
                        try { coverage = ReadCoverage(Read(stream, cmap.Offset, checked((int)cmap.Length))); }
                        catch (InvalidDataException) { coverage = new UnicodeGlyphCoverage(); }
                        // Keep fonts with unreadable coverage in the unfiltered list;
                        // they cannot be marked as supporting the current language.
                        result.Add(new InstalledFont {Path = path, FaceIndex = index, DisplayName = display,
                            SearchText = display + " " + names.Item3 + " " + System.IO.Path.GetFileName(path),
                            Coverage = coverage, HangulCount = coverage.CountRange(0xAC00, 0xD7A3)});
                    }
                    catch (InvalidDataException) { }
                }
                return result.ToArray();
            }
        }

        private static Tuple<string, string, string> Names(byte[] data)
        {
            int count = U16(data, 2), strings = U16(data, 4);
            string family = "", style = ""; int familyScore = -1, styleScore = -1;
            var searchable = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                int r = 6 + i * 12;
                int platform = U16(data, r), language = U16(data, r + 4), id = U16(data, r + 6);
                if (id != 1 && id != 2 && id != 4 && id != 16 && id != 17) continue;
                int length = U16(data, r + 8), offset = strings + U16(data, r + 10); Check(data, offset, length);
                if (platform != 0 && platform != 3) continue;
                string value = Encoding.BigEndianUnicode.GetString(data, offset, length).Trim('\0', ' ');
                value = new string(value.Where(c => !char.IsControl(c)).ToArray()).Replace("<", "‹").Replace(">", "›");
                if (value.Length > 160) value = value.Substring(0, 160);
                if (value.Length == 0) continue;
                searchable.Add(value);
                int score = (language == 0x412 ? 200 : language == 0x409 ? 100 : 50) + ((id == 16 || id == 17) ? 20 : 0);
                if ((id == 1 || id == 16) && score > familyScore) { family = value; familyScore = score; }
                if ((id == 2 || id == 17) && score > styleScore) { style = value; styleScore = score; }
            }
            return Tuple.Create(family, style, string.Join(" ", searchable));
        }

        private static UnicodeGlyphCoverage ReadCoverage(byte[] data)
        {
            int count = U16(data, 2), chosen = -1, format = 0;
            for (int i = 0; i < count; i++)
            {
                int r = 4 + i * 8, platform = U16(data, r);
                if (platform != 0 && platform != 3) continue;
                int offset = checked((int)U32(data, r + 4)), fmt = U16(data, offset);
                if (fmt == 12 || (fmt == 4 && format == 0)) { chosen = offset; format = fmt; }
            }
            var coverage = new UnicodeGlyphCoverage();
            if (chosen < 0) return coverage;
            if (format == 12)
            {
                uint groups = U32(data, chosen + 12);
                if (groups > (data.Length - chosen - 16) / 12) throw new InvalidDataException("Invalid cmap.");
                for (int i = 0; i < groups; i++)
                {
                    int r = chosen + 16 + i * 12;
                    uint first = U32(data, r), last = U32(data, r + 4), glyph = U32(data, r + 8);
                    if (first > last || last > 0x10FFFF) throw new InvalidDataException("Invalid Unicode cmap range.");
                    if (glyph == 0) first++;
                    if (first <= last) coverage.AddRange((int)first, (int)last);
                }
            }
            else
            {
                int segments = U16(data, chosen + 6) / 2;
                int startBase = chosen + 16 + segments * 2, deltaBase = chosen + 16 + segments * 4, rangeBase = chosen + 16 + segments * 6;
                for (int i = 0; i < segments; i++)
                {
                    int first = U16(data, startBase + i * 2), last = U16(data, chosen + 14 + i * 2);
                    int lo = first, hi = last;
                    int delta = (short)U16(data, deltaBase + i * 2), range = U16(data, rangeBase + i * 2);
                    for (int cp = lo; cp <= hi; cp++)
                    {
                        int glyph = range == 0 ? (cp + delta) & 0xFFFF : U16(data, rangeBase + i * 2 + range + (cp - first) * 2);
                        if (range != 0 && glyph != 0) glyph = (glyph + delta) & 0xFFFF;
                        if (glyph != 0) coverage.Add(cp);
                    }
                }
            }
            return coverage;
        }

        public static byte[] ReadFace(string path, int faceIndex)
        {
            using (var stream = File.OpenRead(path))
            {
                if (stream.Length > 256 * 1024 * 1024) throw new InvalidDataException("Font file is too large.");
                var faces = Faces(stream);
                if (faceIndex < 0 || faceIndex >= faces.Length) throw new InvalidDataException("Invalid font face.");
                if (faces.Length == 1 && faces[0] == 0) return Read(stream, 0, checked((int)stream.Length));
                uint face = faces[faceIndex];
                var tables = Tables(stream, face).Where(t => t.Tag != 0x44534947).OrderBy(t => t.Tag).ToArray(); // Discard collection signatures.
                int total = 12 + tables.Length * 16;
                foreach (var table in tables) total = checked(total + (((int)table.Length + 3) & ~3));
                if (total > 64 * 1024 * 1024) throw new InvalidDataException("Font face is too large.");
                var result = new byte[total]; Array.Copy(Read(stream, face, 4), result, 4);
                Put16(result, 4, tables.Length);
                int power = 1, selector = 0; while (power * 2 <= tables.Length) { power *= 2; selector++; }
                Put16(result, 6, power * 16); Put16(result, 8, selector); Put16(result, 10, tables.Length * 16 - power * 16);
                int cursor = 12 + tables.Length * 16, head = -1;
                for (int i = 0; i < tables.Length; i++)
                {
                    var table = tables[i]; var contents = Read(stream, table.Offset, checked((int)table.Length));
                    if (table.Tag == 0x68656164 && contents.Length >= 12) { Put32(contents, 8, 0); head = cursor; }
                    Array.Copy(contents, 0, result, cursor, contents.Length);
                    int r = 12 + i * 16;
                    Put32(result, r, table.Tag); Put32(result, r + 4, Sum(result, cursor, (contents.Length + 3) & ~3));
                    Put32(result, r + 8, (uint)cursor); Put32(result, r + 12, table.Length);
                    cursor += (contents.Length + 3) & ~3;
                }
                if (head >= 0) Put32(result, head + 8, unchecked(0xB1B0AFBAu - Sum(result, 0, result.Length)));
                return result;
            }
        }
        private static uint Sum(byte[] data, int offset, int length)
        {
            uint sum = 0; for (int i = 0; i < length; i += 4) sum = unchecked(sum + U32(data, offset + i)); return sum;
        }
    }
}
