using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

namespace PeakMX
{
    internal static class AntiCheat
    {
        private const string EnabledKey = "pmx_ac";
        private const string LockClientsKey = "pmx_ac_lock";
        private const string HostActorKey = "pmx_ac_host";
        private const string HostVersionKey = "pmx_ac_ver";
        private static float _nextPublish;
        private static bool _lastPublishedEnabled;
        private static bool _hostEnabled;
        private static bool _hostLocksClients;
        private static int _hostActor;
        private static string _hostVersion = "";
        private static int _lastLockNoticeHostActor = int.MinValue;
        private static bool _wasMasterClient;

        public static bool HostEnabled => _hostEnabled;
        public static bool HostLocksClients => _hostLocksClients;
        public static int HostActor => _hostActor;
        public static string HostVersion => _hostVersion;

        public static bool ClientToolsLocked
        {
            get
            {
                try
                {
                    return PhotonNetwork.InRoom
                        && _hostEnabled
                        && _hostLocksClients
                        && !PhotonNetwork.IsMasterClient;
                }
                catch { return false; }
            }
        }

        public static void Tick()
        {
            try
            {
                if (!PhotonNetwork.InRoom)
                {
                    _hostEnabled = false;
                    _hostLocksClients = false;
                    _hostActor = 0;
                    _hostVersion = "";
                    _nextPublish = 0f;
                    _lastPublishedEnabled = false;
                    _lastLockNoticeHostActor = int.MinValue;
                    _wasMasterClient = false;
                    return;
                }

                ReadRoomState();
                bool isMasterClient = PhotonNetwork.IsMasterClient;
                bool becameMasterClient = isMasterClient && !_wasMasterClient;
                _wasMasterClient = isMasterClient;

                if (isMasterClient)
                {
                    if (becameMasterClient)
                        AdoptInheritedRoomPolicy();
                    ApplyHostMasterSwitch();
                    PublishHostState();
                }

                if (ClientToolsLocked)
                    DisableRestrictedLocalTools();
                else
                    _lastLockNoticeHostActor = int.MinValue;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug("[AntiCheat] " + e.Message);
            }
        }

        public static void SetHostEnabled(bool enabled)
        {
            ModConfig.AntiCheatEnabled = enabled;
            ModConfig.AdminProtectionEnabled = enabled;
            ApplyHostMasterSwitch();
            _nextPublish = 0f;
            PublishHostState();
        }

        public static string StatusText(Lang lang)
        {
            if (!PhotonNetwork.InRoom)
                return Localization.Pick(
                    lang,
                    "Античит активируется в комнате.",
                    "Anti-cheat becomes active inside a room.",
                    "反作弊会在房间内启用。",
                    "反作弊會在房間內啟用。");

            if (PhotonNetwork.IsMasterClient)
                return ModConfig.AntiCheatEnabled
                    ? Localization.Pick(
                        lang,
                        "Античит хоста включён.",
                        "Host anti-cheat is enabled.",
                        "房主反作弊已启用。",
                        "房主反作弊已啟用。")
                    : Localization.Pick(
                        lang,
                        "Античит хоста выключен.",
                        "Host anti-cheat is disabled.",
                        "房主反作弊已关闭。",
                        "房主反作弊已關閉。");

            if (_hostEnabled)
                return _hostLocksClients
                    ? Localization.Pick(
                        lang,
                        "Хост включил античит. Инструменты PEAK-MX на клиентах заблокированы.",
                        "The host enabled anti-cheat. PEAK-MX tools are locked on clients.",
                        "房主启用了反作弊。客户端 PEAK-MX 工具已锁定。",
                        "房主啟用了反作弊。用戶端 PEAK-MX 工具已鎖定。")
                    : Localization.Pick(
                        lang,
                        "Хост включил античит без блокировки клиентских инструментов.",
                        "The host enabled anti-cheat without locking client tools.",
                        "房主启用了反作弊，但未锁定客户端工具。",
                        "房主啟用了反作弊，但未鎖定用戶端工具。");

            return Localization.Pick(
                lang,
                "Хост не включал античит.",
                "The host has not enabled anti-cheat.",
                "房主未启用反作弊。",
                "房主未啟用反作弊。");
        }

        public static void DisableRestrictedLocalTools()
        {
            ModConfig.SpeedMod = false;
            ModConfig.JumpMod = false;
            ModConfig.InfiniteJumps = false;
            ModConfig.ClimbMod = false;
            ModConfig.VineClimbMod = false;
            ModConfig.RopeClimbMod = false;

            ModConfig.GodMode = false;
            ModConfig.InfiniteStamina = false;
            ModConfig.NoFallDamage = false;
            ModConfig.NoFallingRagdoll = false;
            ModConfig.NoWeight = false;
            ModConfig.LockStatus = false;
            ModConfig.NoStatusEffects = false;
            ModConfig.NoInjury = false;
            ModConfig.NoHunger = false;
            ModConfig.NoCold = false;
            ModConfig.NoPoison = false;
            ModConfig.NoCurse = false;
            ModConfig.NoDrowsy = false;
            ModConfig.NoHot = false;
            ModConfig.NoCrab = false;
            ModConfig.NoThorns = false;
            ModConfig.NoSpores = false;
            ModConfig.NoWeb = false;
            ModConfig.NoArrows = false;
            ModConfig.NoPetrify = false;
            ModConfig.NoFlyTrap = false;
            ModConfig.TeleportToPing = false;
            ModConfig.NoSlipperySurfaces = false;
            ModConfig.LongInteraction = false;
            ModConfig.CinematicCamera = false;
            ModConfig.GlobalVoice = false;
            ModConfig.PlayerEsp = false;
            ModConfig.ItemEsp = false;
            if (ModConfig.WorldEsp != null)
                ModConfig.WorldEsp.Value = false;

            ModConfig.GameSpeedMod = false;
            ModConfig.PingHandSizeMultiplier = 1f;
            ModConfig.OverrideExpeditionTime = false;
            ModConfig.Fly = false;
            ModConfig.Noclip = false;
            ModConfig.InfiniteItems = false;
            ModConfig.UnlimitedLanternFuel = false;
            ModConfig.UnlimitedItemUses = false;
            ModConfig.RapidRandomOutfitColor = false;

            int lockHost = _hostActor > 0 ? _hostActor : -1;
            if (_lastLockNoticeHostActor != lockHost)
            {
                _lastLockNoticeHostActor = lockHost;
                GameApi.AddAdminLog(Localization.Current == Lang.Russian
                    ? "anti-cheat: клиентские инструменты заблокированы хостом"
                    : "anti-cheat: client tools locked by host",
                    false);
            }
        }

        private static void ApplyHostMasterSwitch()
        {
            // MX-AHG owns all plugin-set validation. Retire persisted settings from
            // the former PEAK-MX-to-PEAK-MX signature check so it cannot run beside it.
            if (ModConfig.AntiCheatEnabled)
            {
                if (ModConfig.ManyPlayersPluginAudit != null)
                    ModConfig.ManyPlayersPluginAudit.Value = false;
                if (ModConfig.ManyPlayersPluginAuditKick != null)
                    ModConfig.ManyPlayersPluginAuditKick.Value = false;
            }

            // Keep the legacy runtime flag in sync because the behavior detectors
            // still consume it internally. Individual detector settings remain user-controlled.
            ModConfig.AdminProtectionEnabled = ModConfig.AntiCheatEnabled;
        }

        private static void AdoptInheritedRoomPolicy()
        {
            if (!_hostEnabled || ModConfig.AntiCheatEnabled)
                return;

            ModConfig.AntiCheatEnabled = true;
            ModConfig.AdminProtectionEnabled = true;
            if (ModConfig.AntiCheatLockClients != null)
                ModConfig.AntiCheatLockClients.Value = _hostLocksClients;

            GameApi.AddAdminLog(Localization.Current == Lang.Russian
                ? "anti-cheat: политика комнаты унаследована новым хостом"
                : "anti-cheat: room policy inherited by the new host");
        }

        private static void PublishHostState()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
                return;

            bool enabled = ModConfig.AntiCheatEnabled;
            if (enabled == _lastPublishedEnabled && Time.realtimeSinceStartup < _nextPublish)
                return;

            _nextPublish = Time.realtimeSinceStartup + 2f;
            _lastPublishedEnabled = enabled;

            var props = new Hashtable
            {
                [EnabledKey] = enabled,
                [LockClientsKey] = ModConfig.AntiCheatLockClients == null || ModConfig.AntiCheatLockClients.Value,
                [HostActorKey] = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : 0,
                [HostVersionKey] = Plugin.Version
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            _hostEnabled = enabled;
            _hostLocksClients = ModConfig.AntiCheatLockClients == null || ModConfig.AntiCheatLockClients.Value;
            _hostActor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : 0;
            _hostVersion = Plugin.Version;
        }

        private static void ReadRoomState()
        {
            try
            {
                if (PhotonNetwork.CurrentRoom?.CustomProperties == null)
                    return;

                var props = PhotonNetwork.CurrentRoom.CustomProperties;
                _hostEnabled = props.TryGetValue(EnabledKey, out object enabled) && enabled is bool b && b;
                _hostLocksClients = !props.TryGetValue(LockClientsKey, out object locked) || !(locked is bool lb) || lb;
                _hostActor = props.TryGetValue(HostActorKey, out object actor) && int.TryParse(actor?.ToString(), out int a) ? a : 0;
                _hostVersion = props.TryGetValue(HostVersionKey, out object ver) ? ver?.ToString() ?? "" : "";
            }
            catch
            {
                _hostEnabled = false;
                _hostLocksClients = false;
                _hostActor = 0;
                _hostVersion = "";
            }
        }
    }
}
