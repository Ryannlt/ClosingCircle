using UnityEngine;

// The only place the mod touches PlayerPrefs, kept to one small file on purpose: it is the one API here whose
// acceptance by the UMod verifier is assumed rather than proven, and if it is refused this is the single file
// that has to change.
//
// Keys carry the steam id the way HoldfastPlayerPrefsManager carries a user id, so two accounts on one machine
// keep their own settings.

namespace ClosingCircle.Systems
{
    public static class ClientPrefs
    {
        private const string Prefix = "ClosingCircle.";

        private static string _scope = string.Empty;

        public static void SetScope(ulong steamId) => _scope = steamId == 0UL ? string.Empty : "." + steamId;

        public static bool Has(string key) => PlayerPrefs.HasKey(Key(key));

        public static string Get(string key, string fallback) => PlayerPrefs.GetString(Key(key), fallback);

        public static bool GetBool(string key, bool fallback) =>
            PlayerPrefs.GetInt(Key(key), fallback ? 1 : 0) != 0;

        // Saved immediately rather than at quit, because a game that crashes should not cost somebody their
        // settings, and these are written rarely enough that the cost does not matter.
        public static void Set(string key, string value)
        {
            PlayerPrefs.SetString(Key(key), value);
            PlayerPrefs.Save();
        }

        public static int GetInt(string key, int fallback) => PlayerPrefs.GetInt(Key(key), fallback);

        public static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(Key(key), value);
            PlayerPrefs.Save();
        }

        public static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(Key(key), value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void Clear(string key)
        {
            PlayerPrefs.DeleteKey(Key(key));
            PlayerPrefs.Save();
        }

        private static string Key(string key) => Prefix + key + _scope;
    }
}
