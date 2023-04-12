using System.Collections.Generic;

namespace PlayerPrefsEditor
{

    /// <summary>
    /// Windowのみ対応
    /// </summary>
    public class PlayerPrefsTools
    {
        private static readonly string[] NON_TARGET_KEY = { "unity.cloud_userid", "unity.player_sessionid", "unity.player_session_count", "UnityGraphicsQuality" };

        public static void GetAllPlayerPrefKeys(ref List<string> keys)
        {
            if (keys != null)
            {
                keys.Clear();
            }
            else
            {
                keys = new List<string>();
            }

#if UNITY_STANDALONE_WIN
            // Unity stores prefs in the registry on Windows
            string regKeyPathPattern =
#if UNITY_EDITOR
        @"Software\Unity\UnityEditor\{0}\{1}";
#else
		@"Software\{0}\{1}";
#endif
            ;

            string regKeyPath = string.Format(regKeyPathPattern, UnityEditor.PlayerSettings.companyName, UnityEditor.PlayerSettings.productName);
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regKeyPath);
            if (regKey == null)
            {
                return;
            }

            string[] playerPrefKeys = regKey.GetValueNames();
            for (int i = 0; i < playerPrefKeys.Length; i++)
            {
                string playerPrefKey = playerPrefKeys[i];

                // Remove the _hXXXXX suffix
                playerPrefKey = playerPrefKey.Substring(0, playerPrefKey.LastIndexOf("_h"));

                if(playerPrefKey != NON_TARGET_KEY[0] && playerPrefKey != NON_TARGET_KEY[1] && playerPrefKey != NON_TARGET_KEY[2] && playerPrefKey != NON_TARGET_KEY[3])
                    keys.Add(playerPrefKey);
            }
#endif
        }
    }
}