using Microsoft.Win32;

namespace Infantry_Launcher.Helpers
{
    internal class Register
    {
        /// <summary>
        /// Writes or updates our options registry keys
        /// </summary>
        public static void WriteRegistryKeys(string key, string subkey)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(subkey))
            { return; }
            for (int index = 0; index <= 5; ++index)
            {
                RegistryKey subKey = Registry.CurrentUser.CreateSubKey(string.Format("Software\\HarmlessGames\\Infantry\\Profile{0}\\Options", index));
                subKey.SetValue(subkey, key);
            }
        }

        /// <summary>
        /// Writes or updates our directory address keys
        /// </summary>
        public static void WriteAddressKeys(string key1, string key2)
        {
            if (string.IsNullOrWhiteSpace(key1) || string.IsNullOrWhiteSpace(key2))
            { return; }
            for (int index = 0; index <= 5; ++index)
            {
                RegistryKey subKey = Registry.CurrentUser.CreateSubKey(string.Format("Software\\HarmlessGames\\Infantry\\Profile{0}\\Options", index));
                subKey.SetValue("SDirectoryAddress", key1);
                subKey.SetValue("SDirectoryAddressBackup", key2);
            }
        }

        /// <summary>
        /// Writes or updates a specific key location
        /// </summary>
        public static void WriteAddressKey(string key, string location, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(location))
            { return; }

            string subLocation = string.Format("Software\\HarmlessGames\\Infantry\\{0}", location);
            RegistryKey subKey;

            try
            {
                if (Registry.CurrentUser.OpenSubKey(subLocation, false) == null)
                { return; }
            }
            catch
            { return; }
            subKey = Registry.CurrentUser.CreateSubKey(string.Format("Software\\HarmlessGames\\Infantry\\{0}", location));
            subKey.SetValue(key, value);
        }

        /// <summary>
        /// Retrieves key data from a specific registry location
        /// </summary>
        public static string GetKeyData(string key, string location)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(location))
            { return null; }

            string subLocation = string.Format("Software\\HarmlessGames\\Infantry\\{0}", location);
            RegistryKey subKey;

            try
            {
                subKey = Registry.CurrentUser.OpenSubKey(subLocation, false);
                return subKey.GetValue(key).ToString();
            }
            catch
            { return null; }
        }

        /// <summary>
        /// Deletes the value out of a registry key
        /// </summary>
        public static void DeleteValue(string key, string location)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(location))
            { return; }

            string subLocation = string.Format("Software\\HarmlessGames\\Infantry\\{0}", location);
            try
            {
                using (RegistryKey subkey = Registry.CurrentUser.OpenSubKey(subLocation, true))
                {
                    subkey.DeleteValue(key);
                }
            }
            catch
            { return; }
        }
    }
}
