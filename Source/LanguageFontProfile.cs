using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ReadableFonts17
{
    public sealed class LanguageFontProfile
    {
        public string Name { get; private set; }
        public int[] RequiredCharacters { get; private set; }
        public string Preview { get; private set; }

        public int CountSupported(InstalledFont font) => RequiredCharacters.Count(c => font.Coverage.Has(c));

        public static LanguageFontProfile Create(string englishName, string nativeName, IEnumerable<string> coreTexts = null)
        {
            string language = (englishName ?? "English").ToLowerInvariant();
            var characters = new HashSet<int>();
            AddText(characters, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
            string seed = nativeName ?? "";
            string preview = "Check your colonists' health and mood. ABC abc 0123456789";
            if (language.Contains("korean"))
            {
                for (int c = 0xAC00; c <= 0xD7A3; c++) characters.Add(c);
                preview = "정착민의 건강과 기분을 확인하세요. ABC abc 0123456789";
            }
            else if (language.Contains("japanese"))
            {
                for (int c = 0x3041; c <= 0x3096; c++) characters.Add(c);
                for (int c = 0x30A1; c <= 0x30FA; c++) characters.Add(c);
                seed += "日本語文字健康建築人物";
                preview = "日本語の文字を確認しましょう。ABC abc 0123456789";
            }
            else if (language.Contains("chinese"))
            {
                bool traditional = language.Contains("traditional");
                seed += traditional ? "繁體中文介面遊戲人物建築健康" : "简体中文界面游戏人物建筑健康";
                preview = traditional ? "檢查文字顯示效果。ABC abc 0123456789" : "检查文字显示效果。ABC abc 0123456789";
            }
            else if (language.Contains("russian")) seed += "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";
            else if (language.Contains("ukrainian")) seed += "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯабвгґдеєжзиіїйклмнопрстуфхцчшщьюя";
            else if (language.Contains("bulgarian")) seed += "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЬЮЯабвгдежзийклмнопрстуфхцчшщъьюя";
            else if (language.Contains("serbian")) seed += language.Contains("latin") ? "ČĆĐŠŽčćđšž" : "АБВГДЂЕЖЗИЈКЛЉМНЊОПРСТЋУФХЦЧЏШабвгдђежзијклљмнњопрстћуфхцчџш";
            else if (language.Contains("greek")) seed += "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩαβγδεζηθικλμνξοπρστυφχψωςωάέήίόύώϊϋΐΰΆΈΉΊΌΎΏ";
            else if (language.Contains("french")) seed += "ÀÂÄÆÇÉÈÊËÎÏÔÖŒÙÛÜŸàâäæçéèêëîïôöœùûüÿ";
            else if (language.Contains("german")) seed += "ÄÖÜäöüß";
            else if (language.Contains("spanish")) seed += "ÁÉÍÓÚÜÑáéíóúüñ¿¡";
            else if (language.Contains("portuguese")) seed += "ÁÂÃÀÇÉÊÍÓÔÕÚÜáâãàçéêíóôõúü";
            else if (language.Contains("italian")) seed += "ÀÈÉÌÒÙàèéìòù";
            else if (language.Contains("polish")) seed += "ĄĆĘŁŃÓŚŹŻąćęłńóśźż";
            else if (language.Contains("czech")) seed += "ÁČĎÉĚÍŇÓŘŠŤÚŮÝŽáčďéěíňóřšťúůýž";
            else if (language.Contains("slovak")) seed += "ÁÄČĎÉÍĹĽŇÓÔŔŠŤÚÝŽáäčďéíĺľňóôŕšťúýž";
            else if (language.Contains("hungarian")) seed += "ÁÉÍÓÖŐÚÜŰáéíóöőúüű";
            else if (language.Contains("turkish")) seed += "ÇĞİÖŞÜçğıöşü";
            else if (language.Contains("romanian")) seed += "ĂÂÎȘȚăâîșț";
            else if (language.Contains("swedish") || language.Contains("finnish")) seed += "ÅÄÖåäö";
            else if (language.Contains("danish") || language.Contains("norwegian")) seed += "ÆØÅæøå";
            else if (language.Contains("dutch")) seed += "ÉËÏÖÜéëïöü";
            else if (language.Contains("croatian")) seed += "ČĆĐŠŽčćđšž";
            else if (language.Contains("slovenian")) seed += "ČŠŽčšž";
            else if (language.Contains("estonian")) seed += "ÄÖÕÜŠŽäöõüšž";
            else if (language.Contains("latvian")) seed += "ĀČĒĢĪĶĻŅŠŪŽāčēģīķļņšūž";
            else if (language.Contains("lithuanian")) seed += "ĄČĘĖĮŠŲŪŽąčęėįšųūž";
            else if (language.Contains("vietnamese")) seed += "ĂÂĐÊÔƠƯăâđêôơưÁÀẢÃẠẮẰẲẴẶẤẦẨẪẬÉÈẺẼẸẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌỐỒỔỖỘỚỜỞỠỢÚÙỦŨỤỨỪỬỮỰÝỲỶỸỴáàảãạắằẳẵặấầẩẫậéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụụứừửữựýỳỷỹỵ";
            else if (language.Contains("arabic")) seed += "ابتثجحخدذرزسشصضطظعغفقكلمنهويءآأإؤئىة";
            else if (language.Contains("hebrew")) seed += "אבגדהוזחטיךכלםמןנסעףפץצקרשת";
            else if (language.Contains("thai"))
            {
                for (int c = 0x0E01; c <= 0x0E3A; c++) characters.Add(c);
                for (int c = 0x0E40; c <= 0x0E4E; c++) characters.Add(c);
            }
            AddText(characters, seed);
            AddText(characters, preview);
            if (coreTexts != null) foreach (string text in coreTexts) AddText(characters, text);
            return new LanguageFontProfile {Name = nativeName ?? englishName ?? "English",
                RequiredCharacters = characters.OrderBy(c => c).ToArray(), Preview = preview};
        }

        private static void AddText(HashSet<int> characters, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            // Tags and placeholders are formatting instructions, not displayed letters.
            text = Regex.Replace(text, @"<[^>]*>|\{[^}]*\}|\(\*[^)]*\)|\(/[^)]*\)", "");
            foreach (var rune in text.EnumerateRunes())
            {
                var category = Rune.GetUnicodeCategory(rune);
                if (Rune.IsLetterOrDigit(rune) || category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark)
                    characters.Add(rune.Value);
            }
        }
    }

    public sealed class UnicodeGlyphCoverage
    {
        private readonly byte[] bmp = new byte[8192];
        private readonly List<Tuple<int, int>> supplementary = new List<Tuple<int, int>>();
        internal void Add(int character) { if (character >= 0 && character <= 0xFFFF) bmp[character >> 3] |= (byte)(1 << (character & 7)); }
        internal void AddRange(int first, int last)
        {
            for (int c = first; c <= Math.Min(last, 0xFFFF); c++) Add(c);
            if (last >= 0x10000) supplementary.Add(Tuple.Create(Math.Max(first, 0x10000), last));
        }
        public bool Has(int character) => character >= 0 && character <= 0xFFFF ?
            (bmp[character >> 3] & (1 << (character & 7))) != 0 :
            supplementary.Any(r => character >= r.Item1 && character <= r.Item2);
        public int CountRange(int first, int last) { int count = 0; for (int c = first; c <= last; c++) if (Has(c)) count++; return count; }
    }
}
