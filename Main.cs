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
                    harmony.Patch(UnityModManager.modEntries.Find(entry => entry.Info.Id == "Overlayer").Assembly.GetType("Overlayer.Main").GetMethod("OnGUI"), new HarmonyMethod(typeof(Main), "OnGUIPrefix"), new HarmonyMethod(typeof(Main), "OnGUIPostfix"));
                    harmony.Patch(AccessTools.Method(typeof(GUILayout), "DoTextField"), new HarmonyMethod(typeof(Main), "DoTextFieldPrefix"), new HarmonyMethod(typeof(Main), "DoTextFieldPostfix"));
                    harmony.Patch(AccessTools.Method(typeof(GUIContent), "Temp", new Type[] { typeof(string) }), prefix: new HarmonyMethod(typeof(Main), "TempPrefix"));
                }
                else
                    harmony.UnpatchAll();
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

