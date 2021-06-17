using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Deft.Utils
{
    internal static class Settings
    {
        private static string GetFolderPath()
        {
            return string.Format(@"{0}\{1}\", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Assembly.GetEntryAssembly().GetName().Name);
        }

        public static string GetSettingsPath()
        {
            return GetFolderPath() + "settings.json";
        }

        private static Dictionary<string, string> LoadSettings()
        {
            try
            {
                var path = GetSettingsPath();

                if (!File.Exists(path))
                    return new Dictionary<string, string>();

                var json = File.ReadAllText(path);
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (result == null)
                    return new Dictionary<string, string>();
                return result;
            }
            catch (Exception e)
            {
                Logger.LogError("Exception while loading settings, see exception: " + e.ToString());
                return new Dictionary<string, string>();
            }
        }

        private static void SaveSettings(Dictionary<string, string> settings)
        {
            try
            {
                var path = GetSettingsPath();

                var json = JsonConvert.SerializeObject(settings);

                Directory.CreateDirectory(GetFolderPath());
                File.WriteAllText(path, json);
            }
            catch(Exception e)
            {
                Logger.LogError("Exception while saving settings, see exception: " + e.ToString());
            }
        }

        public static bool HasKey(string key)
        {
            var settings = LoadSettings();
            return settings.ContainsKey(key);
        }

        public static string GetValue(string key)
        {
            var settings = LoadSettings();
            if (settings.TryGetValue(key, out string value))
                return value;
            else
                return null;
        }

        public static void SetValue(string key, string value)
        {
            var settings = LoadSettings();
            settings[key] = value;
            SaveSettings(settings);
        }
    }
}
