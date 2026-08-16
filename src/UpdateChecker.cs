using System;
#if !DISABLE_AUTO_UPDATE_INSTALL
using System.Diagnostics;
using System.IO;
#endif
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PeakMX
{
    public static class UpdateChecker
    {
        private const string ApiUrl = "https://api.github.com/repos/maxkir041/PEAK-MX/releases/latest";
        public const string ReleasesUrl = "https://github.com/maxkir041/PEAK-MX/releases";

        private static readonly object Lock = new object();
        private static bool _started;

        public static bool IsChecking { get; private set; }
        public static bool IsInstalling { get; private set; }
        public static bool InstallQueued { get; private set; }
        public static bool HasChecked { get; private set; }
        public static bool UpdateAvailable { get; private set; }
        public static bool CanAutoInstall { get; private set; }
        public static string LatestVersion { get; private set; }
        public static string LatestTag { get; private set; }
        public static string ReleaseUrl { get; private set; }
        public static string AssetName { get; private set; }
        public static string AssetUrl { get; private set; }
        public static string Status { get; private set; }
        public static string Error { get; private set; }

        public static void Init()
        {
            if (_started)
                return;
            _started = true;
            CheckAsync(false);
        }

        public static void CheckAsync(bool force)
        {
            lock (Lock)
            {
                if (IsChecking)
                    return;
                if (!force && HasChecked)
                    return;

                IsChecking = true;
                Status = "";
                Error = "";
            }

            Task.Run(() =>
            {
                try
                {
                    CheckNow();
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    Status = "";
                    Plugin.Log?.LogDebug($"[Update] check failed: {e.Message}");
                }
                finally
                {
                    HasChecked = true;
                    IsChecking = false;
                }
            });
        }

        private static void CheckNow()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using var client = CreateClient();
            string json = client.DownloadString(ApiUrl);

            string tag = ExtractJsonString(json, "tag_name");
            string htmlUrl = ExtractJsonString(json, "html_url") ?? ReleasesUrl;
            string latest = NormalizeVersion(tag);
            PickAsset(json, out string assetName, out string assetUrl);

            LatestTag = string.IsNullOrWhiteSpace(tag) ? latest : tag;
            LatestVersion = latest;
            ReleaseUrl = htmlUrl;
            AssetName = assetName;
            AssetUrl = assetUrl;
            int compare = CompareVersions(latest, Plugin.Version);
            UpdateAvailable = compare > 0;
#if DISABLE_AUTO_UPDATE_INSTALL
            CanAutoInstall = false;
#else
            CanAutoInstall = UpdateAvailable && IsWindows() && !string.IsNullOrWhiteSpace(assetUrl)
                && (assetName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    || assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
#endif
            Status = UpdateAvailable
                ? ""
                : compare < 0
                    ? "ahead"
                    : "up_to_date";
        }

        public static void InstallAsync()
        {
#if DISABLE_AUTO_UPDATE_INSTALL
            Error = "auto_install_unavailable";
            Status = "";
#else
            lock (Lock)
            {
                if (IsInstalling || InstallQueued)
                    return;
                IsInstalling = true;
                Error = "";
                Status = "downloading";
            }

            Task.Run(() =>
            {
                try
                {
                    InstallNow();
                    InstallQueued = true;
                    Status = "queued";
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    Status = "";
                    Plugin.Log?.LogDebug($"[Update] install failed: {e.Message}");
                }
                finally
                {
                    IsInstalling = false;
                }
            });
#endif
        }

#if !DISABLE_AUTO_UPDATE_INSTALL
        private static void InstallNow()
        {
            if (!CanAutoInstall)
                throw new InvalidOperationException("auto_install_unavailable");

            string targetDll = typeof(Plugin).Assembly.Location;
            if (string.IsNullOrWhiteSpace(targetDll) || !File.Exists(targetDll))
                throw new InvalidOperationException("current_dll_not_found");

            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PEAK-MX",
                "updates");
            Directory.CreateDirectory(root);

            string safeName = SafeFileName(AssetName);
            string updateFile = Path.Combine(root, safeName);
            string workDir = Path.Combine(root, "extract-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
            string scriptPath = Path.Combine(root, "install-update.ps1");

            using (var client = CreateClient())
                client.DownloadFile(AssetUrl, updateFile);

            File.WriteAllText(scriptPath, BuildInstallScript(targetDll, updateFile, workDir), new UTF8Encoding(true));

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -File " + QuoteArg(scriptPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            Process.Start(psi);
        }
#endif

        private static WebClient CreateClient()
        {
            var client = new WebClient { Encoding = Encoding.UTF8 };
            client.Headers[HttpRequestHeader.UserAgent] = "PEAK-MX/" + Plugin.Version;
            client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
            return client;
        }

#if !DISABLE_AUTO_UPDATE_INSTALL
        private static string BuildInstallScript(string targetDll, string updateFile, string workDir)
        {
            int pid = Process.GetCurrentProcess().Id;
            string latest = LatestTag ?? LatestVersion ?? "";
            return
                "$ErrorActionPreference = 'Stop'\r\n" +
                "$pidToWait = " + pid + "\r\n" +
                "$target = " + PsString(targetDll) + "\r\n" +
                "$update = " + PsString(updateFile) + "\r\n" +
                "$work = " + PsString(workDir) + "\r\n" +
                "$log = Join-Path (Split-Path -LiteralPath $update) 'last-update.log'\r\n" +
                "while (Get-Process -Id $pidToWait -ErrorAction SilentlyContinue) { Start-Sleep -Seconds 1 }\r\n" +
                "if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }\r\n" +
                "New-Item -ItemType Directory -Force -Path $work | Out-Null\r\n" +
                "if ($update.ToLowerInvariant().EndsWith('.zip')) {\r\n" +
                "  Expand-Archive -LiteralPath $update -DestinationPath $work -Force\r\n" +
                "  $dll = Get-ChildItem -LiteralPath $work -Recurse -Filter 'PEAK-MX.dll' | Select-Object -First 1\r\n" +
                "  if ($null -eq $dll) { throw 'PEAK-MX.dll not found in update archive.' }\r\n" +
                "  Copy-Item -LiteralPath $dll.FullName -Destination $target -Force\r\n" +
                "} else {\r\n" +
                "  Copy-Item -LiteralPath $update -Destination $target -Force\r\n" +
                "}\r\n" +
                "$text = 'Installed PEAK-MX " + EscapePowerShellPlainText(latest) + " at ' + (Get-Date).ToString('u')\r\n" +
                "$text | Out-File -LiteralPath $log -Encoding UTF8\r\n";
        }
#endif

        private static void PickAsset(string json, out string assetName, out string assetUrl)
        {
            assetName = "";
            assetUrl = "";
            int bestScore = -1;

            foreach (Match match in Regex.Matches(json ?? "",
                "\"browser_download_url\"\\s*:\\s*\"(?<url>(?:\\\\.|[^\"])*)\"",
                RegexOptions.Singleline))
            {
                int nameIndex = (json ?? "").LastIndexOf("\"name\"", match.Index, StringComparison.Ordinal);
                if (nameIndex < 0)
                    continue;

                string beforeUrl = json.Substring(nameIndex, match.Index - nameIndex);
                string name = ExtractJsonString(beforeUrl, "name");
                string url = JsonUnescape(match.Groups["url"].Value);
                int score = AssetScore(name);
                if (score > bestScore)
                {
                    bestScore = score;
                    assetName = name;
                    assetUrl = url;
                }
            }
        }

        private static int AssetScore(string name)
        {
            string n = (name ?? "").ToLowerInvariant();
#if THUNDERSTORE_NO_ANALYTICS
            if ((n.Contains("thunderstore") || n.Contains("no-analytics") || n.Contains("no_analytics"))
                && (n.EndsWith(".zip") || n.EndsWith(".dll")))
                return 140;
#endif
            if (n == "peak-mx.dll" || (n.Contains("peak-mx") && n.EndsWith(".dll")))
                return 100;
            if (n.Contains("peak-mx") && n.EndsWith(".zip"))
                return 80;
            if (n.Contains("peak_mx") && n.EndsWith(".zip"))
                return 60;
            return -1;
        }

        private static string ExtractJsonString(string json, string name)
        {
            var match = Regex.Match(json ?? "",
                "\"" + Regex.Escape(name) + "\"\\s*:\\s*\"(?<v>(?:\\\\.|[^\"])*)\"",
                RegexOptions.Singleline);
            return match.Success ? JsonUnescape(match.Groups["v"].Value) : null;
        }

        private static string JsonUnescape(string value)
        {
            if (value == null)
                return "";
            return Regex.Unescape(value).Replace("\\/", "/");
        }

        private static int CompareVersions(string left, string right)
        {
            int[] a = VersionParts(left);
            int[] b = VersionParts(right);
            for (int i = 0; i < 4; i++)
            {
                if (a[i] != b[i])
                    return a[i].CompareTo(b[i]);
            }
            return 0;
        }

        private static int[] VersionParts(string value)
        {
            var result = new int[4];
            string normalized = NormalizeVersion(value);
            string[] parts = normalized.Split('.');
            for (int i = 0; i < result.Length && i < parts.Length; i++)
                int.TryParse(parts[i], out result[i]);
            return result;
        }

        private static string NormalizeVersion(string value)
        {
            var match = Regex.Match(value ?? "", "\\d+(?:\\.\\d+){0,3}");
            return match.Success ? match.Value : "0.0.0";
        }

#if !DISABLE_AUTO_UPDATE_INSTALL
        private static bool IsWindows()
        {
            PlatformID platform = Environment.OSVersion.Platform;
            return platform == PlatformID.Win32NT || platform == PlatformID.Win32Windows;
        }

        private static string SafeFileName(string value)
        {
            string name = string.IsNullOrWhiteSpace(value) ? "PEAK-MX-update.bin" : value;
            foreach (char ch in Path.GetInvalidFileNameChars())
                name = name.Replace(ch, '_');
            return name;
        }

        private static string QuoteArg(string value)
        {
            return "\"" + (value ?? "").Replace("\"", "\\\"") + "\"";
        }

        private static string PsString(string value)
        {
            return "'" + (value ?? "").Replace("'", "''") + "'";
        }

        private static string EscapePowerShellPlainText(string value)
        {
            return (value ?? "").Replace("'", "''");
        }
#endif
    }
}
