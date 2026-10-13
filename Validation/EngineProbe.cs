using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Collections.Generic;

class EngineProbe
{
    static readonly BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static int Main(string[] args)
    {
        try { Run(args[0]); return 0; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    static void Run(string fontPath)
    {
        Console.WriteLine("Runtime " + Environment.Version);
        var modFile = Path.Combine(AppContext.BaseDirectory, "ReadableFonts17.dll");
        if (File.Exists(modFile))
        {
            var mod = Assembly.LoadFrom(modFile);
            var catalog = mod.GetType("ReadableFonts17.InstalledFontCatalog", true);
            var found = (Array)catalog.GetMethod("Scan").Invoke(null, null);
            Check(found.Length > 0, "production DLL installed-font enumeration");
            Console.WriteLine("Production DLL catalog: " + found.Length + " installed faces");
            Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "0Harmony.dll"));
            var verse = Assembly.Load("Assembly-CSharp");
            var loadedLanguageType = verse.GetType("Verse.LoadedLanguage", true);
            var languageInfoType = verse.GetType("Verse.LanguageInfo", true);
            var replacementType = loadedLanguageType.GetNestedType("KeyedReplacement", All);
            var activeField = verse.GetType("Verse.LanguageDatabase", true).GetField("activeLanguage", All);
            var profileProperty = mod.GetType("ReadableFonts17.GameLanguageFonts", true).GetProperty("Current", All);
            object Language(string english, string native, string uiText)
            {
                var lang = RuntimeHelpers.GetUninitializedObject(loadedLanguageType);
                var info = Activator.CreateInstance(languageInfoType);
                languageInfoType.GetField("friendlyNameEnglish", All).SetValue(info, english);
                languageInfoType.GetField("friendlyNameNative", All).SetValue(info, native);
                loadedLanguageType.GetField("info", All).SetValue(lang, info);
                loadedLanguageType.GetField("folderName", All).SetValue(lang, english);
                loadedLanguageType.GetField("dataIsLoaded", All).SetValue(lang, true);
                var field = loadedLanguageType.GetField("keyedReplacements", All);
                var dict = (System.Collections.IDictionary)Activator.CreateInstance(field.FieldType);
                foreach (var pair in new[]{("Core", "/Data/Core/Languages/" + english + "/Keyed/Misc.xml", uiText), ("ModPreview", "/Mods/Test/Languages/English/Keyed/Preview.xml", "한글日本語")})
                {
                    var item = Activator.CreateInstance(replacementType);
                    replacementType.GetField("fileSourceFullPath", All).SetValue(item, pair.Item2);
                    replacementType.GetField("value", All).SetValue(item, pair.Item3);
                    dict.Add(pair.Item1, item);
                }
                field.SetValue(lang, dict);
                return lang;
            }
            activeField.SetValue(null, Language("English", "English", "Colonists Health"));
            var enProfile = profileProperty.GetValue(null);
            var requiredField = enProfile.GetType().GetProperty("RequiredCharacters");
            Check(!Array.Exists((int[])requiredField.GetValue(enProfile), c => c == 0xD55C || c == 0x65E5), "mod preview scripts excluded from active English profile");
            Check(ReferenceEquals(enProfile, profileProperty.GetValue(null)), "same-language profile reused");
            activeField.SetValue(null, Language("Korean", "한국어", "건강 기분"));
            var koProfile = profileProperty.GetValue(null);
            Check(!ReferenceEquals(enProfile, koProfile), "active game language switch rebuilds profile");
            Check(Array.Exists((int[])requiredField.GetValue(koProfile), c => c == 0xD55C), "active Korean script detected");
            activeField.SetValue(null, Language("German", "Deutsch", "Résumé Ω"));
            var deProfile = profileProperty.GetValue(null);
            Check(Array.Exists((int[])requiredField.GetValue(deProfile), c => c == 0x03A9), "Core UI letters collected via real LoadedLanguage fields");
            Console.WriteLine("PASS: active-language detection, switching, cache and Core/mod text separation on real game assemblies.");
        }
        var brutal = Assembly.Load("Brutal.ImGui");
        var imgui = brutal.GetType("Brutal.ImGuiApi.ImGui", true);
        var create = imgui.GetMethod("CreateContext", All);
        var context = create.Invoke(null, new[] { Activator.CreateInstance(create.GetParameters()[0].ParameterType) });
        var io = imgui.GetMethod("GetIO", All).Invoke(null, null);
        var game = Assembly.Load("RimWorld");
        var hostType = game.GetType("RimWorldWaylith.Imgui.ImGuiHost", true);
        var host = RuntimeHelpers.GetUninitializedObject(hostType);
        foreach(var field in hostType.GetFields(All))
        {
            if(field.IsStatic) continue;
            if(field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(List<>) || field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                field.SetValue(host, Activator.CreateInstance(field.FieldType));
        }
        hostType.GetField("fallbackFonts", All).SetValue(host, Array.Empty<string>());
        hostType.GetField("context", All).SetValue(host, context);
        bool freetype = (bool)hostType.GetMethod("TryEnableFreeTypeRasterizer", All).Invoke(null, new[] { io });
        Console.WriteLine("FreeType " + freetype);
        var register = hostType.GetMethod("RegisterFont", All);
        var data = File.ReadAllBytes(fontPath);
        var ids = new int[3];
        float[] sizes = {12, 14, 21};
        for(int i = 0; i < 3; i++)
        {
            string key = "probe/bold/" + i;
            ids[i] = (int)register.Invoke(host, new object[]{key, data, sizes[i], 1});
            Check(ids[i] == i, "registered index " + i);
            Check((int)register.Invoke(host, new object[]{key, data, sizes[i], 1}) == ids[i], "registration cache " + i);
            Check((float)hostType.GetMethod("ImportSize", All).Invoke(host, new object[]{ids[i]}) == sizes[i], "font size " + i);
        }
        Check((int)register.Invoke(host, new object[]{"invalid", new byte[12], 14f, 1}) == -1, "invalid font rejected");
        var backendType = game.GetType("RimWorldWaylith.Imgui.ImGuiGuiBackend", true);
        var backend = Activator.CreateInstance(backendType, All, null, new object[]{host, null}, null);
        var measure = backendType.GetMethod("MeasureText", All);
        var styleType = Assembly.Load("UnityStubs").GetType("UnityEngine.FontStyle", true);
        var normal = Enum.ToObject(styleType, 0);
        foreach(var size in new[]{12f, 14f, 17.5f, 21f, 26.25f})
        {
            var result = measure.Invoke(backend, new object[]{1, size, normal, "정착민의 건강과 기분 ABC abc 0123456789", 999f});
            float x = (float)result.GetType().GetField("x").GetValue(result);
            float y = (float)result.GetType().GetField("y").GetValue(result);
            Check(x > 0 && y > 0 && float.IsFinite(x) && float.IsFinite(y), "Korean measurement at " + size);
            Console.WriteLine("Measure " + size + ": " + x + " x " + y);
        }
        var unity = Assembly.Load("UnityStubs");
        unity.GetType("UnityStubs.Gui.GuiBackend", true).GetProperty("Current").SetValue(null, backend);
        var fontType = unity.GetType("UnityEngine.Font", true);
        var styleClass = unity.GetType("UnityEngine.GUIStyle", true);
        var contentClass = unity.GetType("UnityEngine.GUIContent", true);
        var fontObject = Activator.CreateInstance(fontType);
        var indexProperty = fontType.GetProperty("GameFontIndex");
        indexProperty.SetValue(fontObject, 1);
        var guiStyle = Activator.CreateInstance(styleClass);
        styleClass.GetProperty("font").SetValue(guiStyle, fontObject);
        var copiedStyle = Activator.CreateInstance(styleClass, new[] {guiStyle});
        var content = Activator.CreateInstance(contentClass, new object[]{"정착민의 건강과 기분 ABC"});
        var calcSize = styleClass.GetMethod("CalcSize", All);
        float Width(object style) { var value = calcSize.Invoke(style, new[] {content}); return (float)value.GetType().GetField("x").GetValue(value); }
        float originalWidth = Width(copiedStyle);
        indexProperty.SetValue(fontObject, 2);
        Check(Width(copiedStyle) > originalWidth, "copied GUIStyle follows shared Font index change");
        Check((int)fontType.GetProperty("fontSize").GetValue(fontObject) == 21, "font metrics follow index change");
        indexProperty.SetValue(fontObject, 1);
        Check(Width(copiedStyle) == originalWidth, "restored Font restores copied GUIStyle width");
        Console.WriteLine("PASS: shared Font replacement, copied GUIStyle, metrics and restoration.");
        unity.GetType("UnityStubs.Gui.GuiBackend", true).GetProperty("Current").SetValue(null, null);
        imgui.GetMethod("DestroyContext", All).Invoke(null, new[]{context});
        Console.WriteLine("PASS: Waylith font registration/cache/invalid-font/size/Korean measurement.");
    }
    static void Check(bool condition, string message) { if(!condition) throw new Exception("FAIL: " + message); }
}
