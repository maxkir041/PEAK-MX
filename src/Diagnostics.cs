using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PeakMX
{
    /// <summary>
    /// Background diagnostics: device/hardware, game & BepInEx versions, installed plugins,
    /// SteamID, crash reports and lobby members. All optional, gated by AllowAnonymousStats,
    /// and sent to peak-mx.rkngov.com. Never blocks or crashes the game.
    /// </summary>
    public static class Diagnostics
    {
        private const string DiagUrl = "https://peak-mx.rkngov.com/api/diag";
        private const string CrashUrl = "https://peak-mx.rkngov.com/api/crash";
        private const string LobbyUrl = "https://peak-mx.rkngov.com/api/lobby";

        private static bool _diagSent;
        private static readonly HashSet<string> _seenCrashes = new HashSet<string>();
        private static int _crashCount;
        private static string _lastLobby;

        // Called from the main thread (Unity APIs must run there); POST happens on a worker thread.
        public static void SendDiagOnce()
        {
            if (_diagSent || !ModConfig.AllowAnonymousStats.Value || string.IsNullOrEmpty(ModConfig.InstallId.Value))
                return;
            _diagSent = true;

            string id = ModConfig.InstallId.Value;
            string os = SystemInfo.operatingSystem;
            string cpu = $"{SystemInfo.processorType} ({SystemInfo.processorCount} cores)";
            string gpu = $"{SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB)";
            int ram = SystemInfo.systemMemorySize;
            string screen = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
            string gameVer = SafeStr(() => Application.version);
            string bepinex = SafeBepInEx();
            string mods = string.Join(", ", SafePlugins());
            string steamid = SafeSteamId();
            string nick = SafeStr(() => Photon.Pun.PhotonNetwork.NickName);

            Task.Run(() =>
            {
                try
                {
                    var sb = new StringBuilder("{");
                    JS(sb, "id", id); C(sb); JS(sb, "t", TelemetryToken.Value); C(sb);
                    JS(sb, "os", os); C(sb); JS(sb, "cpu", cpu); C(sb); JS(sb, "gpu", gpu); C(sb);
                    JN(sb, "ram", ram); C(sb); JS(sb, "screen", screen); C(sb);
                    JS(sb, "gameVer", gameVer); C(sb); JS(sb, "bepinex", bepinex); C(sb);
                    JS(sb, "steamid", steamid); C(sb); JS(sb, "nick", nick); C(sb);
                    JS(sb, "mods", mods);
                    sb.Append('}');
                    Post(DiagUrl, sb.ToString());
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
            if (_crashCount >= 20 || !ModConfig.AllowAnonymousStats.Value) return;
            string key = (condition ?? "");
            if (key.Length > 160) key = key.Substring(0, 160);
            if (!_seenCrashes.Add(key)) return; // dedup within session
            _crashCount++;

            string id = ModConfig.InstallId.Value;
            string msg = condition ?? "";
            string st = stack ?? "";
            Task.Run(() =>
            {
                try
                {
                    var sb = new StringBuilder("{");
                    JS(sb, "id", id); C(sb); JS(sb, "t", TelemetryToken.Value); C(sb);
                    JS(sb, "message", msg); C(sb); JS(sb, "stack", st);
                    sb.Append('}');
                    Post(CrashUrl, sb.ToString());
                }
                catch { }
            });
        }

        // Lobby members (call from main thread with current player nicks).
        public static void SendLobby(List<string> nicks)
        {
            if (!ModConfig.AllowAnonymousStats.Value || nicks == null || nicks.Count == 0
                || string.IsNullOrEmpty(ModConfig.InstallId.Value))
                return;
            string joined = string.Join(", ", nicks);
            if (joined == _lastLobby) return;
            _lastLobby = joined;

            string id = ModConfig.InstallId.Value;
            var list = new List<string>(nicks);
            Task.Run(() =>
            {
                try
                {
                    var sb = new StringBuilder("{");
                    JS(sb, "id", id); C(sb); JS(sb, "t", TelemetryToken.Value); C(sb);
                    sb.Append("\"nicks\":[");
                    for (int i = 0; i < list.Count; i++) { if (i > 0) sb.Append(','); Str(sb, list[i]); }
                    sb.Append("]}");
                    Post(LobbyUrl, sb.ToString());
                }
                catch { }
            });
        }

        // ---------- helpers ----------
        private static void Post(string url, string json)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using var c = new WebClient { Encoding = Encoding.UTF8 };
            c.Headers[HttpRequestHeader.ContentType] = "application/json";
            c.UploadString(url, "POST", json);
        }

        private static void C(StringBuilder sb) => sb.Append(',');
        private static void JS(StringBuilder sb, string k, string v) { sb.Append('"').Append(k).Append("\":"); Str(sb, v); }
        private static void JN(StringBuilder sb, string k, int v) { sb.Append('"').Append(k).Append("\":").Append(v); }
        private static void Str(StringBuilder sb, string v)
        {
            if (v == null) { sb.Append("null"); return; }
            sb.Append('"');
            foreach (char ch in v)
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

        private static string SafeSteamId()
        {
            try
            {
                if (Steamworks.SteamAPI.IsSteamRunning())
                    return Steamworks.SteamUser.GetSteamID().m_SteamID.ToString();
            }
            catch { }
            return null;
        }
    }
}
