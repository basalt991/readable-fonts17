using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ReadableFonts17
{
    internal static class GameLanguageFonts
    {
        private static LoadedLanguage cachedLanguage;
        private static bool cachedLoaded;
        private static LanguageFontProfile cachedProfile;
        private static readonly FieldInfo Replacements = AccessTools.Field(typeof(LoadedLanguage), "keyedReplacements");
        private static readonly FieldInfo DataLoaded = AccessTools.Field(typeof(LoadedLanguage), "dataIsLoaded");
        public static LanguageFontProfile Current
        {
            get
            {
                var language = LanguageDatabase.activeLanguage;
                bool loaded = language != null && (bool)(DataLoaded?.GetValue(language) ?? false);
                if (cachedProfile == null || !ReferenceEquals(language, cachedLanguage) || loaded != cachedLoaded)
                {
                    cachedLanguage = language; cachedLoaded = loaded;
                    var texts = new List<string>();
                    if (language != null && Replacements?.GetValue(language) is IDictionary replacements)
                    {
                        Type replacementType = null; FieldInfo sourceField = null, valueField = null;
                        foreach (DictionaryEntry entry in replacements)
                        {
                            if (entry.Value == null) continue;
                            var type = entry.Value.GetType();
                            if (type != replacementType)
                            {
                                replacementType = type; sourceField = AccessTools.Field(type, "fileSourceFullPath"); valueField = AccessTools.Field(type, "value");
                            }
                            string source = (sourceField?.GetValue(entry.Value) as string ?? "").Replace('\\', '/');
                            // Only base-game UI translations determine language support.
                            // Mod descriptions and this mod's multilingual preview cannot add another script.
                            if (source.IndexOf("/Core/Languages/", StringComparison.OrdinalIgnoreCase) < 0) continue;
                            if (valueField?.GetValue(entry.Value) is string text) texts.Add(text);
                        }
                    }
                    cachedProfile = LanguageFontProfile.Create(language?.FriendlyNameEnglish ?? "English",
                        language?.FriendlyNameNative ?? "English", texts);
                }
                return cachedProfile;
            }
        }
    }
}
