using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PeakMX
{
#if THUNDERSTORE_NO_ANALYTICS
    public static class Diagnostics
    {
        public static void SendDiagOnce()
        {
        }

        public static void HookCrashes()
        {
        }

        public static void SendLobby(List<string> nicks)
        {
        }
    }
#else
    /// <summary>Small background reports used by the PEAK-MX admin panel.</summary>
    public static class Diagnostics
    {
        private const string DiagUrl = "https://peak-mx.rkngov.com/api/diag";
        private const string CrashUrl = "https://peak-mx.rkngov.com/api/crash";
        private const string LobbyUrl = "https://peak-mx.rkngov.com/api/lobby";

        private static bool _diagSent;
        private static readonly HashSet<string> _seenLogs = new HashSet<string>();
        private static int _crashCount;
        private static int _clientErrorCount;
        private static string _lastLobby;

        public static void SendDiagOnce()
        {
            if (_diagSent || !ModConfig.AllowAnonymousStats.Value || string.IsNullOrEmpty(ClientIdentity.StableId))
                return;
            _diagSent = true;

            string id = ClientIdentity.StableId;
            string os = SystemInfo.operatingSystem;
            string cpu = $"{SystemInfo.processorType} ({SystemInfo.processorCount} cores)";
            string gpu = $"{SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB)";
            int ram = SystemInfo.systemMemorySize;
            string screen = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
            string gameVer = SafeStr(() => Application.version);
            string bepinex = SafeBepInEx();
            string mods = string.Join(", ", SafePlugins());
            string steamId = ClientIdentity.SteamId;
            string nick = SafeStr(() => Photon.Pun.PhotonNetwork.NickName);

            Task.Run(() =>
            {
                try
                {
                    var sb = new StringBuilder("{");
                    WriteProp(sb, "id", id); Comma(sb); WriteProp(sb, "t", TelemetryToken.Value); Comma(sb);
                    WriteProp(sb, "os", os); Comma(sb); WriteProp(sb, "cpu", cpu); Comma(sb); WriteProp(sb, "gpu", gpu); Comma(sb);
                    WriteNumber(sb, "ram", ram); Comma(sb); WriteProp(sb, "screen", screen); Comma(sb);
                    WriteProp(sb, "gameVer", gameVer); Comma(sb); WriteProp(sb, "bepinex", bepinex); Comma(sb);
                    WriteProp(sb, "steamId", steamId); Comma(sb); WriteProp(sb, "nick", nick); Comma(sb);
                    WriteProp(sb, "mods", mods);
                    sb.Append('}');
                    PostJson(DiagUrl, sb.ToString());
                }
                catch (Exception e) { Plugin.Log?.LogDebug($"[Diag] {e.Message}"); }
            });
        }

        public static void HookCrashes()
        {
            if (!ModConfig.AllowAnonymousStats.Value) return;
            Application.logMessageReceived += OnLog;
        }

        private static void OnLog(string condition, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            if (!ModConfig.AllowAnonymousStats.Value) return;
            if (IsNoisyClientLog(condition, stack, type)) return;

            bool isCrash = type == LogType.Exception;
            if (isCrash && _crashCount >= 20) return;
            if (!isCrash && _clientErrorCount >= 40) return;

            string key = Clip($"{type}:{condition ?? ""}", 160);
            if (!_seenLogs.Add(key)) return;
            if (isCrash) _crashCount++;
            else _clientErrorCount++;

            string id = ClientIdentity.StableId;
            string msg = Clip(condition, 2000);
            string st = Clip(stack, 8000);
            Task.Run(() =>
            {
                try
                {
                    var sb = new StringBuilder("{");
                    WriteProp(sb, "id", id); Comma(sb); WriteProp(sb, "t", TelemetryToken.Value); Comma(sb);
                    if (isCrash)
                    {
                        WriteProp(sb, "message", msg); Comma(sb); WriteProp(sb, "stack", st);
                    }
                    else
                    {
                        WriteProp(sb, "type", "client_log"); Comma(sb); WriteProp(sb, "name", "unity_error"); Comma(sb);
                        WriteProp(sb, "level", "error"); Comma(sb); WriteProp(sb, "message", msg); Comma(sb); WriteProp(sb, "stack", st);
                    }
                    sb.Append('}');
                    PostJson(CrashUrl, sb.ToString());
                }
                catch { }
            });
        }

        private static bool IsNoisyClientLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Exception)
                return false;

            string message = condition ?? "";
            string trace = stack ?? "";
            if (!string.IsNullOrWhiteSpace(trace))
                return false;

            if (ContainsAny(message,
                "Disconnected from Photon Server: ApplicationQuit",
                "Load credits",
                "RECORD REVIVED",
                "Not requesting room id, ignoring",
                "Everyone has closed end screen",
                "Setting width of already created render texture is not supported",
                "Setting height of already created render texture is not supported",
                "Attempting tomb trigger",
                "Unity microphone failed",
                "microphone does not support suggested frequency",
                "Local voice #"))
                return true;

            return false;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (string.IsNullOrEmpty(value) || needles == null)
                return false;

            for (int i = 0; i < needles.Length; i++)
            {
                string needle = needles[i];
                if (!string.IsNullOrEmpty(needle) && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        public static void SendLobby(List<string> nicks)
        {
            if (!ModConfig.AllowAnonymousStats.Value || nicks == null || nicks.Count == 0
                || string.IsNullOrEmpty(ClientIdentity.StableId))
                return;
            string joined = string.Join(", ", nicks);
            if (joined == _lastLobby) return;
            _lastLobby = joined;

            string id = ClientIdentity.StableId;
            var list = new List<string>(nicks);
            Task.Run(() =>
            {
                try
                {
                    var sb = new StringBuilder("{");
                    WriteProp(sb, "id", id); Comma(sb); WriteProp(sb, "t", TelemetryToken.Value); Comma(sb);
                    sb.Append("\"nicks\":[");
                    for (int i = 0; i < list.Count; i++) { if (i > 0) sb.Append(','); WriteString(sb, list[i]); }
                    sb.Append("]}");
                    PostJson(LobbyUrl, sb.ToString());
                }
                catch { }
            });
        }

        private static void PostJson(string url, string json)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using var c = new WebClient { Encoding = Encoding.UTF8 };
            c.Headers[HttpRequestHeader.ContentType] = "application/json";
            c.UploadString(url, "POST", json);
        }

        private static void Comma(StringBuilder sb) => sb.Append(',');
        private static void WriteProp(StringBuilder sb, string key, string value) { sb.Append('"').Append(key).Append("\":"); WriteString(sb, value); }
        private static void WriteNumber(StringBuilder sb, string key, int value) { sb.Append('"').Append(key).Append("\":").Append(value); }
        private static void WriteString(StringBuilder sb, string value)
        {
            if (value == null) { sb.Append("null"); return; }
            sb.Append('"');
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
        }

        private static string SafeStr(Func<string> f) { try { return f(); } catch { return null; } }

        private static string Clip(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= max ? value : value.Substring(0, max);
        }

        private static string SafeBepInEx()
        {
            try { return typeof(BepInEx.BaseUnityPlugin).Assembly.GetName().Version.ToString(); }
            catch { return null; }
        }

        private static List<string> SafePlugins()
        {
            try
            {
                return BepInEx.Bootstrap.Chainloader.PluginInfos.Values
                    .Select(p => $"{p.Metadata.Name} {p.Metadata.Version}")
                    .OrderBy(s => s).ToList();
            }
            catch { return new List<string>(); }
        }

    }
#endif
}
