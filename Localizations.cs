using GDMiniJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.Networking;

namespace KoreanOverlayer
{
    public static class Localizations
    {
        private static readonly string SPREADSHEET_URL_START = "https://docs.google.com/spreadsheets/d/";
        private static readonly string SPREADSHEET_URL_END = "/gviz/tq?tqx=out:json&tq&gid=";
        private static readonly string KEY = "1QcrRL6LAs8WxJj_hFsEJa3CLM5g3e8Ya0KQlRKXwdlU";
        private static readonly int GID = 828207863;
        private static readonly string LOCALIZATION_PATH = Path.Combine(Main.ModEntry.Path, "localizations.txt");
        private static readonly Dictionary<string, string> localizations = new Dictionary<string, string>();

        public static void Load()
        {
            StaticCoroutine.Do(Download());
        }

        public static bool Get(string key, out string value)
        {
            value = null;
            foreach (var pair in localizations)
            {
                if (pair.Key == key)
                    value = pair.Value;
                else if (pair.Key.StartsWith("*") && pair.Value.StartsWith("*") && key.EndsWith(pair.Key.Substring(1)))
                    value = key.Substring(0, key.Length - pair.Key.Length + 1) + pair.Value.Substring(1);
            }
            return value != null;
        }

        private static IEnumerator Download()
        {
            UnityWebRequest request = UnityWebRequest.Get(SPREADSHEET_URL_START + KEY + SPREADSHEET_URL_END + GID);
            yield return request.SendWebRequest();
            byte[] bytes = request.downloadHandler.data;
            if (bytes == null)
            {
                LoadFromFile();
                yield break;
            }
            string strData = Encoding.UTF8.GetString(bytes);
            strData = strData.Substring(47, strData.Length - 49);
            localizations.Clear();
            StringBuilder sb = new StringBuilder();
            foreach (object obj in ((Json.Deserialize(strData) as Dictionary<string, object>)["table"] as Dictionary<string, object>)["rows"] as List<object>)
            {
                List<object> list = (obj as Dictionary<string, object>)["c"] as List<object>;
                string key = (list[0] as Dictionary<string, object>)["v"] as string;
                string value = (list[1] as Dictionary<string, object>)["v"] as string;
                if (key.IsNullOrEmpty() || value.IsNullOrEmpty())
                    continue;
                localizations.Add(key, value);
                sb.AppendLine(key.Escape() + ":" + value.Escape());
            }
            File.WriteAllText(LOCALIZATION_PATH, sb.ToString());
            Main.Logger.Log($"Loaded {localizations.Count} Localizations from Sheet");
        }

        private static void LoadFromFile()
        {
            if (File.Exists(LOCALIZATION_PATH))
            {
                string[] lines = File.ReadAllLines(LOCALIZATION_PATH);
                localizations.Clear();
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int offset = 0;
                    while (true)
                    {
                        string sub = line.Substring(offset);
                        offset += sub.IndexOf(':');
                        if (offset <= 0 || line[offset - 1] != '\\')
                            break;
                        offset++;
                    }
                    if (offset == -1)
                    {
                        Main.Logger.Log($"Invalid Line in Localizations File! (line: {i + 1})");
                        continue;
                    }
                    string key = line.Substring(0, offset).UnEscape();
                    string value = line.Substring(offset + 1).UnEscape();
                    localizations.Add(key, value);
                }
                Main.Logger.Log($"Loaded {localizations.Count} Localizations from Local File");
                return;
            }
            Main.Logger.Log($"Couldn't Load Localizations!");
        }

        private static string Escape(this string str)
        {
            return str.Replace(@"\", @"\\").Replace(":", @"\:");
        }

        private static string UnEscape(this string str)
        {
            return str.Replace(@"\:", ":").Replace(@"\\", @"\");
        }
    }
}