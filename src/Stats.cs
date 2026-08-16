using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PeakMX
{
#if THUNDERSTORE_NO_ANALYTICS
    public static class Stats
    {
        public static int? InstallCount { get; private set; }

        public static void Init()
        {
        }

        public static void SendNick(string nick)
        {
        }
    }
#else
    /// <summary>Install counter and basic launch ping.</summary>
    public static class Stats
    {
        public const string Endpoint = "https://peak-mx.rkngov.com/api/ping";
        public const string NickEndpoint = "https://peak-mx.rkngov.com/api/setnick";

        private static readonly string[] LangCodes =
            { "en", "ru", "uk", "zh-CN", "zh-TW", "ja", "ko", "es", "pt-BR", "de", "fr", "it", "pl", "tr" };

        public static int? InstallCount { get; private set; }

        public static void Init()
        {
            if (!ModConfig.AllowAnonymousStats.Value)
                return;

            if (string.IsNullOrEmpty(Endpoint))
                return;

            string id = ClientIdentity.StableId;
            if (!string.Equals(ModConfig.InstallId.Value, id, StringComparison.Ordinal))
                ModConfig.InstallId.Value = id;

            Task.Run(() => Report(id));
        }

        private static void Report(string id)
        {
            try
            {
                int langIdx = ModConfig.Language.Value;
                string lang = (langIdx >= 0 && langIdx < LangCodes.Length) ? LangCodes[langIdx] : "en";
                string steamId = ClientIdentity.SteamId;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string url = $"{Endpoint}?id={Uri.EscapeDataString(id)}"
                           + $"&mod={Uri.EscapeDataString(Plugin.Version)}"
                           + $"&lang={Uri.EscapeDataString(lang)}"
                           + $"&idKind={Uri.EscapeDataString(ClientIdentity.UsesSteam ? "steam" : "anon")}"
                           + (string.IsNullOrEmpty(steamId) ? "" : $"&steamId={Uri.EscapeDataString(steamId)}")
                           + $"&t={Uri.EscapeDataString(TelemetryToken.Value)}";

                using var client = new WebClient();
                string body = client.DownloadString(url);

                var m = Regex.Match(body, "\"installs\"\\s*:\\s*(\\d+)");
                if (m.Success && int.TryParse(m.Groups[1].Value, out int total))
                {
                    InstallCount = total;
                    if (!ModConfig.InstallReported.Value)
                        ModConfig.InstallReported.Value = true;
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[Stats] unreachable: {e.Message}");
            }
        }

        public static void SendNick(string nick)
        {
            string id = ClientIdentity.StableId;
            if (string.IsNullOrEmpty(nick) || string.IsNullOrEmpty(id))
                return;

            string steamId = ClientIdentity.SteamId;
            Task.Run(() =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    string url = $"{NickEndpoint}?id={Uri.EscapeDataString(id)}"
                               + $"&nick={Uri.EscapeDataString(nick)}"
                               + $"&idKind={Uri.EscapeDataString(ClientIdentity.UsesSteam ? "steam" : "anon")}"
                               + (string.IsNullOrEmpty(steamId) ? "" : $"&steamId={Uri.EscapeDataString(steamId)}")
                               + $"&t={Uri.EscapeDataString(TelemetryToken.Value)}";
                    using var client = new WebClient();
                    client.DownloadString(url);
                }
                catch (Exception e) { Plugin.Log?.LogDebug($"[Stats] nick send failed: {e.Message}"); }
            });
        }
    }
#endif
}
