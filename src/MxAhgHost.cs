using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ExitGames.Client.Photon;
using MxAhg.Shared;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Protocol = MxAhg.Shared.Protocol;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakMX
{
    internal sealed class MxAhgRow
    {
        public int ActorNumber;
        public string PlayerName = "";
        public string AgentVersion = "";
        public bool HasAgent;
        public bool HeartbeatStale;
        public bool HasReport;
        public bool ReportValid;
        public bool Matches;
        public bool Suspicious;
        public bool Trusted;
        public bool PolicyAccepted;
        public float HeartbeatAge;
        public string InvalidReason = "";
        public string DeclaredSignature = "";
        public int DeclaredFileCount;
        public readonly List<string> Missing = new List<string>();
        public readonly List<string> Extra = new List<string>();
        public readonly List<string> Changed = new List<string>();
        public readonly List<string> PatchDifferences = new List<string>();
        public readonly List<string> SuspiciousFindings = new List<string>();

        public bool Accepted => Trusted || PolicyAccepted;
        public int DifferenceCount => Missing.Count + Extra.Count + Changed.Count + PatchDifferences.Count;

        public string StatusText(Lang lang)
        {
            if (Trusted)
                return Localization.Pick(lang, "доверен хостом", "trusted by host", "房主已信任", "房主已信任");
            if (!HasAgent)
                return Localization.Pick(lang, "агент не найден", "agent not found", "未找到代理", "未找到代理");
            if (HeartbeatStale)
                return Localization.Pick(lang, "heartbeat потерян", "heartbeat lost", "心跳已丢失", "心跳已遺失");
            if (!HasReport)
                return Localization.Pick(lang, "ожидание отчёта", "waiting for report", "等待报告", "等待報告");
            if (!ReportValid)
                return Localization.Pick(lang, "отчёт недействителен", "invalid report", "报告无效", "報告無效");
            if (Suspicious)
                return Localization.Pick(lang, $"подозрительно ({SuspiciousFindings.Count})", $"suspicious ({SuspiciousFindings.Count})", $"可疑 ({SuspiciousFindings.Count})", $"可疑 ({SuspiciousFindings.Count})");
            if (!Matches)
            {
                if (PolicyAccepted)
                    return Localization.Pick(lang, $"проверен, различия разрешены ({DifferenceCount})", $"verified, differences allowed ({DifferenceCount})", $"已验证，允许差异 ({DifferenceCount})", $"已驗證，允許差異 ({DifferenceCount})");
                return Localization.Pick(lang, $"набор отличается ({DifferenceCount})", $"pack differs ({DifferenceCount})", $"模组包不同 ({DifferenceCount})", $"模組包不同 ({DifferenceCount})");
            }
            return Localization.Pick(lang, "проверка пройдена", "verified clean", "验证通过", "驗證通過");
        }
    }

    internal static class MxAhgHost
    {
        private const float HeartbeatStaleSeconds = 20f;
        private const float KickDelaySeconds = 2.5f;

        private sealed class AgentState
        {
            public int ActorNumber;
            public string PlayerName = "";
            public string AgentVersion = "";
            public int ProtocolVersion;
            public bool HasAgent;
            public int Heartbeat;
            public float LastHeartbeatAt;
            public float JoinedAt;
            public float LastChallengeAt;
            public float LastReportAt;
            public int ReportHeartbeat;
            public string ExpectedNonce = "";
            public string DeclaredSignature = "";
            public int DeclaredFileCount;
            public string DeclaredLoadedSignature = "";
            public int DeclaredLoadedCount;
            public string DeclaredSelfHash = "";
            public string ComparableSignature = "";
            public int ComparableFileCount;
            public Snapshot Report;
            public bool ReportValid;
            public string InvalidReason = "";
            public bool Matches;
            public bool Suspicious;
            public readonly List<string> Missing = new List<string>();
            public readonly List<string> Extra = new List<string>();
            public readonly List<string> Changed = new List<string>();
            public readonly List<string> PatchDifferences = new List<string>();
            public readonly List<string> SuspiciousFindings = new List<string>();
            public string LastLoggedStatus = "";
        }

        private sealed class PendingKick
        {
            public string Reason = "";
            public float DueAt;
        }

        private static readonly Dictionary<int, AgentState> States = new Dictionary<int, AgentState>();
        private static readonly Dictionary<int, PendingKick> PendingKicks = new Dictionary<int, PendingKick>();
        private static readonly HashSet<int> TrustedActors = new HashSet<int>();
        private static readonly List<MxAhgRow> RowsInternal = new List<MxAhgRow>();
        private static bool _subscribed;
        private static float _nextTick;
        private static float _nextLocalScan;
        private static string _roomName = "";
        private static Snapshot _localSnapshot;

        public static IReadOnlyList<MxAhgRow> Rows => RowsInternal;
        public static string LocalSignature => _localSnapshot == null ? "?" : Protocol.Short(_localSnapshot.FileSignature);
        public static int LocalFileCount => _localSnapshot?.Files.Count ?? 0;
        public static string LocalError => _localSnapshot?.Error ?? "";

        public static void Init()
        {
            EnsureSubscribed();
            RefreshLocal();
        }

        public static void Dispose()
        {
            if (_subscribed)
            {
                try { PhotonNetwork.NetworkingClient.EventReceived -= OnEvent; }
                catch { }
            }
            _subscribed = false;
            ResetRoom();
        }

        public static void Tick()
        {
            EnsureSubscribed();
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            {
                ResetRoom();
                return;
            }

            string room = PhotonNetwork.CurrentRoom.Name ?? "room";
            if (!string.Equals(room, _roomName, StringComparison.Ordinal))
            {
                ResetRoom();
                _roomName = room;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                RowsInternal.Clear();
                return;
            }
            if (Time.realtimeSinceStartup < _nextTick)
                return;

            _nextTick = Time.realtimeSinceStartup + 1f;
            RefreshStates();
            if ((ModConfig.MxAhgRequireAgent?.Value ?? false) || HasAgentState())
                EnsureLocalSnapshot(false);
            BuildRows();
            EnforcePolicy();
            ProcessPendingKicks();
        }

        private static bool HasAgentState()
        {
            foreach (AgentState state in States.Values)
            {
                if (state.HasAgent)
                    return true;
            }
            return false;
        }

        public static bool HasLiveAgent(int actorNumber)
        {
            if (!States.TryGetValue(actorNumber, out AgentState state)
                || !state.HasAgent
                || !state.ReportValid)
                return false;
            if (state.LastHeartbeatAt <= 0f || Time.realtimeSinceStartup - state.LastHeartbeatAt > HeartbeatStaleSeconds)
                return false;
            if (TrustedActors.Contains(actorNumber))
                return true;
            return !ModConfig.AntiCheatEnabled || state.Matches;
        }

        public static void RefreshLocal()
        {
            try
            {
                _localSnapshot = Scanner.Scan(typeof(Plugin).Assembly.Location, true);
                _nextLocalScan = Time.realtimeSinceStartup + 30f;
                foreach (AgentState state in States.Values)
                {
                    if (state.ReportValid)
                        Compare(state);
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[MX-AHG] host scan failed: " + e.Message);
            }
        }

        public static void RequestAll()
        {
            EnsureLocalSnapshot(true);
            if (PhotonNetwork.PlayerList == null)
                return;
            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            {
                PhotonPlayer player = PhotonNetwork.PlayerList[i];
                if (player != null && !player.IsLocal)
                    RequestReport(player.ActorNumber, true);
            }
        }

        public static void RequestReport(int actorNumber, bool force = true)
        {
            if (!States.TryGetValue(actorNumber, out AgentState state))
            {
                PhotonPlayer player = FindPlayer(actorNumber);
                if (player == null)
                    return;
                state = CreateState(player);
                States[actorNumber] = state;
            }
            RequestReportInternal(state, force);
        }

        public static bool IsTrusted(int actorNumber) => TrustedActors.Contains(actorNumber);

        public static void SetTrusted(int actorNumber, bool trusted)
        {
            if (trusted)
            {
                TrustedActors.Add(actorNumber);
                PendingKicks.Remove(actorNumber);
            }
            else
            {
                TrustedActors.Remove(actorNumber);
            }
            BuildRows();
        }

        public static void Kick(int actorNumber, string reason = "mxahg_admin")
        {
            PhotonPlayer player = FindPlayer(actorNumber);
            if (player != null)
                GameApi.KickPhotonPlayer(player, reason);
        }

        public static void KickRejected()
        {
            for (int i = 0; i < RowsInternal.Count; i++)
            {
                if (!RowsInternal[i].Accepted)
                    Kick(RowsInternal[i].ActorNumber, "mxahg_rejected");
            }
        }

        public static string BuildReport(Lang lang)
        {
            var sb = new StringBuilder();
            sb.AppendLine("MX-AHG host report");
            sb.AppendLine(Localization.Pick(lang, "Эталон хоста: ", "Host baseline: ", "房主基准: ", "房主基準: ") + LocalSignature + $" ({LocalFileCount} DLL)");
            if (!string.IsNullOrWhiteSpace(LocalError))
                sb.AppendLine(Localization.Pick(lang, "Ошибка сканирования: ", "Scan note: ", "扫描提示: ", "掃描提示: ") + LocalError);

            for (int i = 0; i < RowsInternal.Count; i++)
            {
                MxAhgRow row = RowsInternal[i];
                sb.AppendLine();
                sb.AppendLine($"{row.PlayerName} [actor {row.ActorNumber}] - {row.StatusText(lang)}");
                if (!string.IsNullOrWhiteSpace(row.AgentVersion)) sb.AppendLine("Agent: " + row.AgentVersion);
                if (!string.IsNullOrWhiteSpace(row.DeclaredSignature)) sb.AppendLine($"Set: {row.DeclaredSignature} ({row.DeclaredFileCount} DLL)");
                AppendList(sb, "Missing", row.Missing);
                AppendList(sb, "Extra", row.Extra);
                AppendList(sb, "Changed", row.Changed);
                AppendList(sb, "Harmony", row.PatchDifferences);
                AppendList(sb, "Suspicious", row.SuspiciousFindings);
                if (!string.IsNullOrWhiteSpace(row.InvalidReason)) sb.AppendLine("Invalid: " + row.InvalidReason);
            }
            return sb.ToString();
        }

        private static void EnsureSubscribed()
        {
            if (_subscribed || PhotonNetwork.NetworkingClient == null)
                return;
            try
            {
                PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
                _subscribed = true;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug("[MX-AHG] subscribe failed: " + e.Message);
            }
        }

        private static void EnsureLocalSnapshot(bool force)
        {
            if (force || _localSnapshot == null || Time.realtimeSinceStartup >= _nextLocalScan)
                RefreshLocal();
        }

        private static void RefreshStates()
        {
            if (PhotonNetwork.PlayerList == null)
                return;

            float now = Time.realtimeSinceStartup;
            var active = new HashSet<int>();
            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            {
                PhotonPlayer player = PhotonNetwork.PlayerList[i];
                if (player == null || player.IsLocal)
                    continue;

                active.Add(player.ActorNumber);
                if (!States.TryGetValue(player.ActorNumber, out AgentState state))
                {
                    state = CreateState(player);
                    States[player.ActorNumber] = state;
                }

                state.PlayerName = PlayerName(player);
                state.AgentVersion = ReadString(player, Protocol.AgentVersionKey);
                state.ProtocolVersion = ReadInt(player, Protocol.ProtocolKey);
                state.HasAgent = !string.IsNullOrWhiteSpace(state.AgentVersion) && state.ProtocolVersion == Protocol.Version;
                state.DeclaredSignature = ReadString(player, Protocol.FileSignatureKey);
                state.DeclaredFileCount = ReadInt(player, Protocol.FileCountKey);
                state.DeclaredLoadedSignature = ReadString(player, Protocol.LoadedSignatureKey);
                state.DeclaredLoadedCount = ReadInt(player, Protocol.LoadedCountKey);
                state.DeclaredSelfHash = ReadString(player, Protocol.SelfHashKey);

                int heartbeat = ReadInt(player, Protocol.HeartbeatKey);
                if (state.HasAgent && (state.LastHeartbeatAt <= 0f || heartbeat != state.Heartbeat))
                {
                    state.Heartbeat = heartbeat;
                    state.LastHeartbeatAt = now;
                }

                if (state.Report != null && state.Heartbeat != state.ReportHeartbeat)
                {
                    bool reportChanged = !string.Equals(state.DeclaredSignature, Protocol.Short(state.Report.FileSignature), StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(state.DeclaredLoadedSignature, Protocol.Short(state.Report.LoadedSignature), StringComparison.OrdinalIgnoreCase);
                    if (reportChanged)
                    {
                        state.ReportValid = false;
                        state.InvalidReason = "reported state changed";
                    }
                    else
                    {
                        state.ReportHeartbeat = state.Heartbeat;
                    }
                }

                float recheck = ModConfig.MxAhgRecheckSeconds?.Value ?? 60f;
                if (state.HasAgent && (!state.ReportValid || now - state.LastReportAt >= recheck))
                    RequestReportInternal(state, false);
            }

            var remove = new List<int>();
            foreach (int actor in States.Keys)
            {
                if (!active.Contains(actor))
                    remove.Add(actor);
            }
            for (int i = 0; i < remove.Count; i++)
            {
                States.Remove(remove[i]);
                PendingKicks.Remove(remove[i]);
                TrustedActors.Remove(remove[i]);
            }
        }

        private static AgentState CreateState(PhotonPlayer player)
        {
            return new AgentState
            {
                ActorNumber = player.ActorNumber,
                PlayerName = PlayerName(player),
                JoinedAt = Time.realtimeSinceStartup
            };
        }

        private static void RequestReportInternal(AgentState state, bool force)
        {
            if (state == null || !PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
                return;
            float now = Time.realtimeSinceStartup;
            if (!force && now - state.LastChallengeAt < 6f)
                return;

            string nonce = Guid.NewGuid().ToString("N");
            object[] payload = { Protocol.Magic, Protocol.Version, Protocol.CommandChallenge, nonce };
            var options = new RaiseEventOptions { TargetActors = new[] { state.ActorNumber } };
            if (PhotonNetwork.RaiseEvent(Protocol.EventCode, payload, options, SendOptions.SendReliable))
            {
                state.ExpectedNonce = nonce;
                state.LastChallengeAt = now;
            }
        }

        private static void OnEvent(EventData data)
        {
            try
            {
                if (data == null || data.Code != Protocol.EventCode || !PhotonNetwork.IsMasterClient)
                    return;
                if (!(data.CustomData is object[] payload) || payload.Length < 14)
                    return;
                if (!string.Equals(payload[0]?.ToString(), Protocol.Magic, StringComparison.Ordinal)
                    || Convert.ToInt32(payload[1]) != Protocol.Version
                    || Convert.ToInt32(payload[2]) != Protocol.CommandReport)
                    return;

                PhotonPlayer player = FindPlayer(data.Sender);
                if (player == null)
                    return;
                if (!States.TryGetValue(data.Sender, out AgentState state))
                {
                    state = CreateState(player);
                    States[data.Sender] = state;
                }

                string nonce = Protocol.Limit(payload[3]?.ToString() ?? "", 96);
                if (string.IsNullOrWhiteSpace(state.ExpectedNonce)
                    || !string.Equals(nonce, state.ExpectedNonce, StringComparison.Ordinal))
                    return;

                var snapshot = new Snapshot();
                snapshot.Files.AddRange(Protocol.ParseFiles(payload[4]));
                snapshot.Plugins.AddRange(Protocol.ParsePlugins(payload[5]));
                string[] owners = Protocol.ReadStringArray(payload[6]);
                for (int i = 0; i < owners.Length && snapshot.HarmonyOwners.Count < Protocol.MaxHarmonyOwners; i++)
                {
                    string owner = Protocol.Limit(owners[i], 180);
                    if (!string.IsNullOrWhiteSpace(owner))
                        snapshot.HarmonyOwners.Add(owner);
                }
                snapshot.SelfHash = Protocol.Limit(payload[9]?.ToString() ?? "", 64);
                snapshot.Error = Protocol.Limit(payload[11]?.ToString() ?? "", 500);
                snapshot.Truncated = Convert.ToBoolean(payload[13]);
                snapshot.Recalculate();

                string sentFileSignature = Protocol.Limit(payload[7]?.ToString() ?? "", 64);
                string sentLoadedSignature = Protocol.Limit(payload[8]?.ToString() ?? "", 64);
                string sentProof = Protocol.Limit(payload[10]?.ToString() ?? "", 64);
                string expectedProof = Protocol.ComputeProof(nonce, snapshot);
                bool valid = string.Equals(sentFileSignature, snapshot.FileSignature, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(sentLoadedSignature, snapshot.LoadedSignature, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(sentProof, expectedProof, StringComparison.OrdinalIgnoreCase);

                state.AgentVersion = Protocol.Limit(payload[12]?.ToString() ?? state.AgentVersion, 40);
                state.Report = snapshot;
                state.ReportValid = valid;
                state.InvalidReason = valid ? "" : "signature or nonce proof mismatch";
                state.LastReportAt = Time.realtimeSinceStartup;
                state.ReportHeartbeat = state.Heartbeat;
                state.ExpectedNonce = "";
                if (valid)
                    Compare(state);
                LogStatusIfChanged(state);
                BuildRows();
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug("[MX-AHG] report failed: " + e.Message);
            }
        }

        private static void Compare(AgentState state)
        {
            EnsureLocalSnapshot(false);
            state.Missing.Clear();
            state.Extra.Clear();
            state.Changed.Clear();
            state.PatchDifferences.Clear();
            state.SuspiciousFindings.Clear();
            if (_localSnapshot == null || state.Report == null)
            {
                state.Matches = false;
                return;
            }

            Snapshot localSnapshot = BuildComparableSnapshot(_localSnapshot);
            Snapshot remoteSnapshot = BuildComparableSnapshot(state.Report);
            state.ComparableSignature = remoteSnapshot.FileSignature;
            state.ComparableFileCount = remoteSnapshot.Files.Count;

            var remoteFiles = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < remoteSnapshot.Files.Count; i++)
            {
                string key = NormalizePath(remoteSnapshot.Files[i].RelativePath);
                if (!remoteFiles.ContainsKey(key))
                    remoteFiles[key] = remoteSnapshot.Files[i];
                else
                    AddUnique(state.SuspiciousFindings, "duplicate DLL path: " + remoteSnapshot.Files[i].RelativePath);
            }
            var consumedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < localSnapshot.Files.Count; i++)
            {
                FileEntry local = localSnapshot.Files[i];
                string localKey = NormalizePath(local.RelativePath);
                string remoteKey = localKey;
                remoteFiles.TryGetValue(localKey, out FileEntry remote);

                if (remote == null)
                {
                    AddUnique(state.Missing, local.RelativePath);
                    continue;
                }
                consumedFiles.Add(remoteKey);
                if (!string.Equals(local.Sha256, remote.Sha256, StringComparison.OrdinalIgnoreCase))
                    AddUnique(state.Changed, remote.RelativePath + " (SHA-256)");
            }
            foreach (KeyValuePair<string, FileEntry> pair in remoteFiles)
            {
                if (!consumedFiles.Contains(pair.Key))
                    AddUnique(state.Extra, pair.Value.RelativePath);
            }

            var remotePlugins = new Dictionary<string, PluginEntry>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < remoteSnapshot.Plugins.Count; i++)
            {
                string key = remoteSnapshot.Plugins[i].Guid ?? "";
                if (!remotePlugins.ContainsKey(key))
                    remotePlugins[key] = remoteSnapshot.Plugins[i];
                else
                    AddUnique(state.SuspiciousFindings, "duplicate plugin GUID: " + key);
            }
            var consumedPlugins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < localSnapshot.Plugins.Count; i++)
            {
                PluginEntry local = localSnapshot.Plugins[i];
                if (!remotePlugins.TryGetValue(local.Guid, out PluginEntry remote))
                {
                    AddUnique(state.Missing, "plugin: " + local.Guid);
                    continue;
                }
                consumedPlugins.Add(local.Guid);
                bool versionDiffers = !string.Equals(local.Version, remote.Version, StringComparison.OrdinalIgnoreCase);
                bool hashDiffers = !string.IsNullOrWhiteSpace(local.Sha256)
                    && !string.IsNullOrWhiteSpace(remote.Sha256)
                    && !string.Equals(local.Sha256, remote.Sha256, StringComparison.OrdinalIgnoreCase);
                if (versionDiffers || hashDiffers)
                    AddUnique(state.Changed, "plugin: " + remote.Guid + " " + remote.Version);
            }
            foreach (KeyValuePair<string, PluginEntry> pair in remotePlugins)
            {
                if (!consumedPlugins.Contains(pair.Key))
                    AddUnique(state.Extra, "plugin: " + pair.Key + " " + pair.Value.Version);
            }

            var localOwners = new HashSet<string>(localSnapshot.HarmonyOwners, StringComparer.OrdinalIgnoreCase);
            var remoteOwners = new HashSet<string>(remoteSnapshot.HarmonyOwners, StringComparer.OrdinalIgnoreCase);
            foreach (string owner in localOwners)
            {
                if (!remoteOwners.Contains(owner))
                    AddUnique(state.PatchDifferences, "missing owner: " + owner);
            }
            foreach (string owner in remoteOwners)
            {
                if (!localOwners.Contains(owner))
                    AddUnique(state.PatchDifferences, "extra owner: " + owner);
            }

            FindDeniedKeywords(state);
            if (state.Report.Truncated)
                AddUnique(state.SuspiciousFindings, "agent report was truncated");
            state.Suspicious = state.SuspiciousFindings.Count > 0;
            state.Matches = state.Missing.Count == 0
                && state.Extra.Count == 0
                && state.Changed.Count == 0
                && state.PatchDifferences.Count == 0;
        }

        private static void FindDeniedKeywords(AgentState state)
        {
            string source = ModConfig.MxAhgDeniedKeywords?.Value ?? "";
            string[] keywords = source.Split(new[] { ';', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < keywords.Length; i++)
            {
                string keyword = keywords[i].Trim();
                if (keyword.Length < 3)
                    continue;

                for (int j = 0; j < state.Report.Files.Count; j++)
                {
                    FileEntry entry = state.Report.Files[j];
                    if (Contains(entry.RelativePath, keyword) || Contains(entry.AssemblyName, keyword))
                        AddUnique(state.SuspiciousFindings, $"keyword '{keyword}': {entry.RelativePath}");
                }
                for (int j = 0; j < state.Report.Plugins.Count; j++)
                {
                    PluginEntry entry = state.Report.Plugins[j];
                    if (Contains(entry.Guid, keyword) || Contains(entry.Name, keyword) || Contains(entry.RelativePath, keyword))
                        AddUnique(state.SuspiciousFindings, $"keyword '{keyword}': {entry.Guid}");
                }
                for (int j = 0; j < state.Report.HarmonyOwners.Count; j++)
                {
                    if (Contains(state.Report.HarmonyOwners[j], keyword))
                        AddUnique(state.SuspiciousFindings, $"keyword '{keyword}': {state.Report.HarmonyOwners[j]}");
                }
            }
        }

        private static void BuildRows()
        {
            RowsInternal.Clear();
            float now = Time.realtimeSinceStartup;
            foreach (AgentState state in States.Values)
            {
                var row = new MxAhgRow
                {
                    ActorNumber = state.ActorNumber,
                    PlayerName = state.PlayerName,
                    AgentVersion = state.AgentVersion,
                    HasAgent = state.HasAgent,
                    HeartbeatStale = state.HasAgent && (state.LastHeartbeatAt <= 0f || now - state.LastHeartbeatAt > HeartbeatStaleSeconds),
                    HasReport = state.Report != null,
                    ReportValid = state.ReportValid,
                    Matches = state.Matches,
                    Suspicious = state.Suspicious,
                    Trusted = TrustedActors.Contains(state.ActorNumber),
                    PolicyAccepted = state.HasAgent
                        && state.LastHeartbeatAt > 0f
                        && now - state.LastHeartbeatAt <= HeartbeatStaleSeconds
                        && state.ReportValid
                        && (!(ModConfig.MxAhgExactPack?.Value ?? true) || state.Matches)
                        && (!(ModConfig.MxAhgKickSuspicious?.Value ?? false) || !state.Suspicious),
                    HeartbeatAge = state.LastHeartbeatAt <= 0f ? -1f : now - state.LastHeartbeatAt,
                    InvalidReason = state.InvalidReason,
                    DeclaredSignature = string.IsNullOrWhiteSpace(state.ComparableSignature)
                        ? state.DeclaredSignature
                        : state.ComparableSignature,
                    DeclaredFileCount = state.Report == null
                        ? state.DeclaredFileCount
                        : state.ComparableFileCount
                };
                row.Missing.AddRange(state.Missing);
                row.Extra.AddRange(state.Extra);
                row.Changed.AddRange(state.Changed);
                row.PatchDifferences.AddRange(state.PatchDifferences);
                row.SuspiciousFindings.AddRange(state.SuspiciousFindings);
                RowsInternal.Add(row);
            }
            RowsInternal.Sort((a, b) => string.Compare(a.PlayerName, b.PlayerName, StringComparison.OrdinalIgnoreCase));
        }

        private static void EnforcePolicy()
        {
            if (ModConfig.MxAhgRequireAgent == null || !ModConfig.MxAhgRequireAgent.Value)
            {
                PendingKicks.Clear();
                return;
            }

            float now = Time.realtimeSinceStartup;
            float grace = ModConfig.MxAhgGraceSeconds?.Value ?? 18f;
            foreach (AgentState state in States.Values)
            {
                if (TrustedActors.Contains(state.ActorNumber))
                {
                    PendingKicks.Remove(state.ActorNumber);
                    continue;
                }
                if (now - state.JoinedAt < grace)
                    continue;

                string reason = "";
                if (!state.HasAgent && (ModConfig.MxAhgKickMissing?.Value ?? true))
                    reason = "mxahg_missing_agent";
                else if (state.HasAgent && (state.Report == null || !state.ReportValid) && (ModConfig.MxAhgKickMissing?.Value ?? true))
                    reason = "mxahg_invalid_report";
                else if (state.HasAgent && now - state.LastHeartbeatAt > HeartbeatStaleSeconds && (ModConfig.MxAhgKickStale?.Value ?? false))
                    reason = "mxahg_stale_heartbeat";
                else if (state.ReportValid && state.Suspicious && (ModConfig.MxAhgKickSuspicious?.Value ?? false))
                    reason = "mxahg_suspicious_plugin";
                else if (state.ReportValid && !state.Matches && (ModConfig.MxAhgExactPack?.Value ?? true) && (ModConfig.MxAhgKickMismatch?.Value ?? true))
                    reason = "mxahg_pack_mismatch";

                if (string.IsNullOrWhiteSpace(reason))
                    PendingKicks.Remove(state.ActorNumber);
                else
                    ScheduleKick(state, reason);
            }
        }

        private static void ScheduleKick(AgentState state, string reason)
        {
            if (PendingKicks.TryGetValue(state.ActorNumber, out PendingKick old)
                && string.Equals(old.Reason, reason, StringComparison.Ordinal))
                return;
            PendingKicks[state.ActorNumber] = new PendingKick
            {
                Reason = reason,
                DueAt = Time.realtimeSinceStartup + KickDelaySeconds
            };
            GameApi.AddAdminLog($"MX-AHG: {state.PlayerName} -> {reason}");
        }

        private static void ProcessPendingKicks()
        {
            if (PendingKicks.Count == 0)
                return;
            float now = Time.realtimeSinceStartup;
            var remove = new List<int>();
            foreach (KeyValuePair<int, PendingKick> pair in PendingKicks)
            {
                if (now < pair.Value.DueAt)
                    continue;
                PhotonPlayer player = FindPlayer(pair.Key);
                if (player != null)
                    GameApi.KickPhotonPlayer(player, pair.Value.Reason);
                remove.Add(pair.Key);
            }
            for (int i = 0; i < remove.Count; i++)
                PendingKicks.Remove(remove[i]);
        }

        private static void LogStatusIfChanged(AgentState state)
        {
            string status = !state.ReportValid ? "invalid" : state.Suspicious ? "suspicious" : state.Matches ? "clean" : "mismatch";
            if (string.Equals(status, state.LastLoggedStatus, StringComparison.Ordinal))
                return;
            state.LastLoggedStatus = status;
            GameApi.AddAdminLog($"MX-AHG: {state.PlayerName} -> {status}", status != "clean");
        }

        private static void ResetRoom()
        {
            States.Clear();
            PendingKicks.Clear();
            TrustedActors.Clear();
            RowsInternal.Clear();
            _roomName = "";
            _nextTick = 0f;
        }

        private static PhotonPlayer FindPlayer(int actorNumber)
        {
            if (PhotonNetwork.PlayerList == null)
                return null;
            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            {
                PhotonPlayer player = PhotonNetwork.PlayerList[i];
                if (player != null && player.ActorNumber == actorNumber)
                    return player;
            }
            return null;
        }

        private static string ReadString(PhotonPlayer player, string key)
        {
            try
            {
                if (player?.CustomProperties != null && player.CustomProperties.TryGetValue(key, out object value))
                    return value?.ToString() ?? "";
            }
            catch { }
            return "";
        }

        private static int ReadInt(PhotonPlayer player, string key)
        {
            try
            {
                if (player?.CustomProperties != null && player.CustomProperties.TryGetValue(key, out object value))
                {
                    if (value is int number) return number;
                    if (int.TryParse(value?.ToString(), out int parsed)) return parsed;
                }
            }
            catch { }
            return 0;
        }

        private static string PlayerName(PhotonPlayer player)
        {
            return string.IsNullOrWhiteSpace(player?.NickName) ? "Player " + (player?.ActorNumber ?? 0) : player.NickName;
        }

        private static Snapshot BuildComparableSnapshot(Snapshot source)
        {
            var result = new Snapshot
            {
                Error = source?.Error ?? "",
                Truncated = source?.Truncated ?? false
            };
            if (source == null)
            {
                result.Recalculate();
                return result;
            }

            for (int i = 0; i < source.Files.Count; i++)
            {
                FileEntry entry = source.Files[i];
                if (entry == null || Scanner.IsTechnicalFile(entry.RelativePath))
                    continue;
                result.Files.Add(entry);
            }
            for (int i = 0; i < source.Plugins.Count; i++)
            {
                PluginEntry entry = source.Plugins[i];
                if (entry == null || Scanner.IsTechnicalPlugin(entry.Guid))
                    continue;
                result.Plugins.Add(entry);
            }
            for (int i = 0; i < source.HarmonyOwners.Count; i++)
            {
                string owner = source.HarmonyOwners[i];
                if (Scanner.IsTechnicalPlugin(owner) || Scanner.IsUnstableHarmonyOwner(owner))
                    continue;
                result.HarmonyOwners.Add(owner);
            }
            result.Recalculate();
            return result;
        }

        private static string NormalizePath(string value)
        {
            return (value ?? "").Replace('/', '\\').TrimStart('\\').ToLowerInvariant();
        }

        private static bool Contains(string value, string part)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AddUnique(List<string> list, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], value, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            list.Add(value);
        }

        private static void AppendList(StringBuilder sb, string label, IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
                return;
            sb.AppendLine(label + ":");
            for (int i = 0; i < values.Count; i++)
                sb.AppendLine("  - " + values[i]);
        }
    }
}
