using System;
using System.IO;

namespace PeakMX
{
    public static class ClientIdentity
    {
        private const string FolderName = "PEAK-MX";
        private const string FileName = "client-id.txt";
        private static string _fallbackId;
        private static string _steamId;
        private static string _storagePath;

        public static string StableId
        {
            get
            {
                Init();
                return !string.IsNullOrWhiteSpace(_steamId) ? "steam:" + _steamId : "anon:" + _fallbackId;
            }
        }

        public static string SteamId
        {
            get
            {
                Init();
                return _steamId;
            }
        }

        public static string StoragePath
        {
            get
            {
                Init();
                return _storagePath;
            }
        }

        public static bool UsesSteam
        {
            get
            {
                Init();
                return !string.IsNullOrWhiteSpace(_steamId);
            }
        }

        public static void Init()
        {
            if (!string.IsNullOrWhiteSpace(_fallbackId) && _storagePath != null)
            {
                RefreshSteamId();
                return;
            }

            _storagePath = BuildStoragePath();
            _fallbackId = LoadOrCreateFallbackId(_storagePath);
            RefreshSteamId();
        }

        private static void RefreshSteamId()
        {
            try
            {
                if (Steamworks.SteamAPI.IsSteamRunning())
                {
                    ulong value = Steamworks.SteamUser.GetSteamID().m_SteamID;
                    if (value != 0)
                    {
                        _steamId = value.ToString();
                        return;
                    }
                }
            }
            catch { }
            _steamId = "";
        }

        private static string BuildStoragePath()
        {
            try
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (!string.IsNullOrWhiteSpace(root))
                    return Path.Combine(root, FolderName, FileName);
            }
            catch { }

            return Path.Combine(Path.GetTempPath(), FolderName, FileName);
        }

        private static string LoadOrCreateFallbackId(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string existing = NormalizeId(File.ReadAllText(path));
                    if (!string.IsNullOrWhiteSpace(existing))
                        return existing;
                }

                string created = Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, created);
                return created;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[ClientIdentity] persistent id fallback failed: {e.Message}");
                return Guid.NewGuid().ToString("N");
            }
        }

        private static string NormalizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            string cleaned = value.Trim();
            return cleaned.Length > 64 ? cleaned.Substring(0, 64) : cleaned;
        }
    }
}
