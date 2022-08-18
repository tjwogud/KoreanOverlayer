using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace KoreanOverlayer
{
    public static class Main
    {
        public static UnityModManager.ModEntry ModEntry;
        public static UnityModManager.ModEntry.ModLogger Logger;

        public static void Setup(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            Logger = modEntry.Logger;
            Harmony harmony = new Harmony(modEntry.Info.Id);
            modEntry.OnToggle = (_, value) =>
            {
                if (value)
                {
                    harmony.Patch(UnityModManager.modEntries.Find(entry => entry.Info.Id == "Overlayer").Assembly.GetType("Overlayer.Main").GetMethod("OnGUI"), 
                        prefix: new HarmonyMethod(typeof(Main), "OnGUIPrefix"), 
                        postfix: new HarmonyMethod(typeof(Main), "OnGUIPostfix"));
                    harmony.Patch(AccessTools.Method(typeof(GUILayout), "DoTextField"), 
                        prefix: new HarmonyMethod(typeof(Main), "DoTextFieldPrefix"), 
                        postfix: new HarmonyMethod(typeof(Main), "DoTextFieldPostfix"));
                    harmony.Patch(AccessTools.Method(typeof(UnityModManager.UI), "PopupToggleGroup", new Type[] { typeof(int), typeof(string[]), typeof(Action<int>), typeof(string), typeof(int), typeof(GUIStyle), typeof(GUILayoutOption[]) }),
                        prefix: new HarmonyMethod(typeof(Main), "PopupToggleGroupPrefix"));
                    harmony.Patch(AccessTools.Method(typeof(GUIContent), "Temp", new Type[] { typeof(string) }), 
                        prefix: new HarmonyMethod(typeof(Main), "TempPrefix"));
                }
                else
                    harmony.UnpatchAll(ModEntry.Info.Id);
                return true;
            };
            Localizations.Load();
        }

        private static bool patch = false;

        public static void OnGUIPrefix()
        {
            patch = true;
        }

        public static void OnGUIPostfix()
        {
            patch = false;
        }

        public static void DoTextFieldPrefix(out bool __state)
        {
            __state = patch;
            patch = false;
        }

        public static void DoTextFieldPostfix(bool __state)
        {
            patch = __state;
        }

        public static void PopupToggleGroupPrefix(string[] values, ref string title) {
            if (!patch)
                return;
            {
                for (int i = 0; i < values.Length; i++)
                    values[i] = Localizations.Get(values[i], out string value) ? value : values[i];
            }
            {
                title = Localizations.Get(title, out string value) ? value : title;
            }
        }

        public static void TempPrefix(ref string t)
        {
            if (!patch)
                return;
            if (Localizations.Get(t, out string value))
                t = value;
            else if (!check.Contains(t)) {
                check.Add(t);
                Logger.Log($"No Localization Found for Text '{t}'!");
            }
        }

        private static readonly List<string> check = new List<string>();
    }
}

