using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PeakMX
{
    /// <summary>
    /// Install/usage counter for PEAK-MX.
    ///
    /// What is sent on each launch (only if enabled in the About tab):
    ///   - a locally-generated random GUID (install id),
    ///   - the mod version and selected language.
    /// The server (peak-mx.rkngov.com) additionally logs the request IP and approximate
    /// geo/region for statistics and basic anti-abuse. This data is private and used only
    /// by the author. The whole counter can be turned off in the About tab.
    /// </summary>
    public static class Stats
    {
        // Server endpoints. Contract: GET {Endpoint}?id={guid}&mod={ver}&lang={code}&t={token} -> JSON {"installs":N,...}
        public const string Endpoint = "https://peak-mx.rkngov.com/api/ping";
        public const string NickEndpoint = "https://peak-mx.rkngov.com/api/setnick";

        // Short language codes mirroring the website (index = Lang enum value).
        private static readonly string[] LangCodes =
            { "en", "ru", "uk", "zh-CN", "zh-TW", "ja", "ko", "es", "pt-BR", "de", "fr", "it", "pl", "tr" };

        /// <summary>Current install total, or null until fetched / if disabled.</summary>
        public static int? InstallCount { get; private set; }

        public static void Init()
        {
            if (!ModConfig.AllowAnonymousStats.Value)
                return;

            if (string.IsNullOrEmpty(Endpoint))
                return;

            if (string.IsNullOrEmpty(ModConfig.InstallId.Value))
                ModConfig.InstallId.Value = Guid.NewGuid().ToString("N");

            // Fire-and-forget; never block or crash the game on network issues.
            Task.Run(() => Report(ModConfig.InstallId.Value));
        }

        private static void Report(string id)
        {
            try
            {
                int langIdx = ModConfig.Language.Value;
                string lang = (langIdx >= 0 && langIdx < LangCodes.Length) ? LangCodes[langIdx] : "en";

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string url = $"{Endpoint}?id={Uri.EscapeDataString(id)}"
                           + $"&mod={Uri.EscapeDataString(Plugin.Version)}"
                           + $"&lang={Uri.EscapeDataString(lang)}"
                           + $"&t={Uri.EscapeDataString(TelemetryToken.Value)}";

                using var client = new WebClient();
                string body = client.DownloadString(url);

                // Server replies with JSON like {"installs":123,"counted":true}
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

        /// <summary>Send the player's display name once it becomes available (e.g. from Photon).</summary>
        public static void SendNick(string nick)
        {
            if (string.IsNullOrEmpty(nick) || string.IsNullOrEmpty(ModConfig.InstallId.Value))
                return;

            string id = ModConfig.InstallId.Value;
            Task.Run(() =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    string url = $"{NickEndpoint}?id={Uri.EscapeDataString(id)}"
                               + $"&nick={Uri.EscapeDataString(nick)}"
                               + $"&t={Uri.EscapeDataString(TelemetryToken.Value)}";
                    using var client = new WebClient();
                    client.DownloadString(url);
                }
                catch (Exception e) { Plugin.Log?.LogDebug($"[Stats] nick send failed: {e.Message}"); }
            });
        }
    }
}
