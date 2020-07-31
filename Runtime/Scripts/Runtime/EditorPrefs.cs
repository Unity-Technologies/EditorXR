#if !UNITY_EDITOR
using UnityEngine;

namespace Unity.EditorXR
{
    class EditorPrefs
    {
        public static bool GetBool(string key, bool defaultValue)
        {
            var value = PlayerPrefs.GetString(key, defaultValue.ToString());
            bool result;
            bool.TryParse(value, out result);
            return result;
        }

        public static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetString(key, value.ToString());
        }

        public static string GetString(string key, string defaultValue)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public static string GetString(string key)
        {
            return PlayerPrefs.GetString(key);
        }

        public static void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }
    }
}
#endif
