using System;
using System.Net;
using System.Threading.Tasks;

namespace PeakMX
{
    /// <summary>
    /// Optional, anonymous install counter.
    ///
    /// Privacy: the ONLY thing ever sent is a locally-generated random GUID (the install id)
    /// plus a "is this a new install" flag. No Steam id, username, IP-linked data or any other
    /// personal information is collected or transmitted. Controlled by ModConfig.AllowAnonymousStats
    /// and fully disclosed in the About tab and README.
    /// </summary>
    public static class Stats
    {
        // Your server endpoint. Leave empty to disable the counter entirely.
        // Expected contract: GET {Endpoint}?id={guid}&new={0|1}  ->  plain-text current total.
        public const string Endpoint = "";

        /// <summary>Current install total, or null until fetched / if disabled.</summary>
        public static int? InstallCount { get; private set; }

        public static void Init()
        {
            if (!ModConfig.AllowAnonymousStats.Value || string.IsNullOrEmpty(Endpoint))
                return;

            if (string.IsNullOrEmpty(ModConfig.InstallId.Value))
                ModConfig.InstallId.Value = Guid.NewGuid().ToString("N");

            bool isNew = !ModConfig.InstallReported.Value;

            // Fire-and-forget; never block or crash the game on network issues.
            Task.Run(() => Report(ModConfig.InstallId.Value, isNew));
        }

        private static void Report(string id, bool isNew)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string url = $"{Endpoint}?id={Uri.EscapeDataString(id)}&new={(isNew ? 1 : 0)}";

                using var client = new WebClient();
                string body = client.DownloadString(url);

                if (isNew)
                    ModConfig.InstallReported.Value = true;

                if (int.TryParse(body.Trim(), out int total))
                    InstallCount = total;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[Stats] disabled/unreachable: {e.Message}");
            }
        }
    }
}
