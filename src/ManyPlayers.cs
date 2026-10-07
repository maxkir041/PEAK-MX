using System;
using System.Reflection;
using HarmonyLib;
using Peak.Network;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace PeakMX
{
    internal static class ManyPlayers
    {
        public static bool Enabled => ModConfig.ManyPlayersEnabled != null && ModConfig.ManyPlayersEnabled.Value;
        public static int MaxPlayers => Mathf.Clamp(ModConfig.ManyPlayersMaxPlayers?.Value ?? 4, 1, 30);

        public static string StatusText(Lang lang)
        {
            if (!Enabled)
                return Localization.Pick(
                    lang,
                    "Выключено",
                    "Disabled",
                    "已关闭",
                    "已關閉",
                    uk: "Вимкнено",
                    ja: "無効",
                    ko: "꺼짐",
                    es: "Desactivado",
                    ptBr: "Desativado",
                    de: "Deaktiviert",
                    fr: "Désactivé",
                    it: "Disattivato",
                    pl: "Wyłączone",
                    tr: "Kapalı");

            string count = PhotonNetwork.InRoom
                ? $"{PhotonNetwork.CurrentRoom?.PlayerCount ?? 0}/{MaxPlayers}"
                : MaxPlayers.ToString();
            return Localization.Pick(
                lang,
                $"Включено, лимит: {count}",
                $"Enabled, limit: {count}",
                $"已启用，人数上限: {count}",
                $"已啟用，人數上限: {count}",
                uk: $"Увімкнено, ліміт: {count}",
                ja: $"有効、上限: {count}",
                ko: $"켜짐, 제한: {count}",
                es: $"Activado, límite: {count}",
                ptBr: $"Ativado, limite: {count}",
                de: $"Aktiviert, Limit: {count}",
                fr: $"Activé, limite : {count}",
                it: $"Attivo, limite: {count}",
                pl: $"Włączone, limit: {count}",
                tr: $"Açık, limit: {count}");
        }

        public static string StatusText(bool russian) => StatusText(russian ? Lang.Russian : Lang.English);

        public static void LogLobby(string message)
        {
            if (ModConfig.ManyPlayersLobbyDetails == null || !ModConfig.ManyPlayersLobbyDetails.Value)
                return;

            string line = "[PEAK-MX multiplayer] " + message;
            Plugin.Log?.LogInfo(line);
            GameApi.AddAdminLog(line);
        }

        public static byte VoiceGroupFor(Character character)
        {
            try
            {
                PhotonView view = character != null ? character.GetComponent<PhotonView>() : null;
                int actor = view?.OwnerActorNr ?? 0;
                if (actor <= 0)
                    actor = character != null ? character.GetInstanceID() : 0;
                return (byte)(Mathf.Abs(actor) % 4);
            }
            catch
            {
                return 0;
            }
        }

        public static void EnsureComponentArrayCapacity(object instance, string fieldName, int minCount)
        {
            if (instance == null || minCount <= 0)
                return;

            try
            {
                FieldInfo field = AccessTools.Field(instance.GetType(), fieldName);
                if (field == null)
                    return;

                Array current = field.GetValue(instance) as Array;
                if (current == null || current.Length >= minCount)
                    return;

                Type elementType = current.GetType().GetElementType();
                if (elementType == null || !typeof(Component).IsAssignableFrom(elementType))
                    return;

                Component template = null;
                for (int i = current.Length - 1; i >= 0; i--)
                {
                    template = current.GetValue(i) as Component;
                    if (template != null)
                        break;
                }

                if (template == null)
                    return;

                Array expanded = Array.CreateInstance(elementType, minCount);
                Array.Copy(current, expanded, current.Length);

                Transform parent = template.transform.parent;
                for (int i = current.Length; i < minCount; i++)
                {
                    GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, parent);
                    clone.name = template.gameObject.name + "_mx_" + i;
                    clone.SetActive(false);
                    expanded.SetValue(clone.GetComponent(elementType), i);
                }

                field.SetValue(instance, expanded);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[PEAK-MX multiplayer] UI expand skipped: {e.Message}");
            }
        }

        public static bool IsIndexInsideArray(object instance, string fieldName, int index)
        {
            try
            {
                FieldInfo field = AccessTools.Field(instance.GetType(), fieldName);
                Array current = field?.GetValue(instance) as Array;
                return current == null || (index >= 0 && index < current.Length);
            }
            catch
            {
                return true;
            }
        }

        public static bool BlockNonHostKiosk()
        {
            if (!Enabled || ModConfig.ManyPlayersHostOnlyKiosk == null || !ModConfig.ManyPlayersHostOnlyKiosk.Value)
                return false;

            if (PhotonNetwork.IsMasterClient)
                return false;

            LogLobby("Non-host kiosk action blocked.");
            return true;
        }
    }

    [HarmonyPatch(typeof(NetworkingUtilities), "get_MAX_PLAYERS")]
    internal static class ManyPlayersMaxPlayersPatch
    {
        private static bool Prefix(ref int __result)
        {
            if (!ManyPlayers.Enabled)
                return true;

            __result = ManyPlayers.MaxPlayers;
            return false;
        }
    }

    [HarmonyPatch(typeof(NetworkingUtilities), "HostRoomOptions")]
    internal static class ManyPlayersHostRoomOptionsPatch
    {
        private static bool Prefix(ref RoomOptions __result)
        {
            if (!ManyPlayers.Enabled)
                return true;

            __result = new RoomOptions
            {
                IsVisible = false,
                MaxPlayers = (byte)ManyPlayers.MaxPlayers,
                PublishUserId = true
            };
            return false;
        }
    }

    [HarmonyPatch(typeof(AirportCheckInKiosk), "StartGame", new[] { typeof(int) })]
    internal static class ManyPlayersStartGamePatch
    {
        private static bool Prefix()
        {
            return !ManyPlayers.BlockNonHostKiosk();
        }
    }

    [HarmonyPatch(typeof(AirportCheckInKiosk), "LoadIslandMaster", new[] { typeof(int), typeof(byte[]) })]
    internal static class ManyPlayersLoadIslandPatch
    {
        private static bool Prefix()
        {
            return !ManyPlayers.BlockNonHostKiosk();
        }
    }

    [HarmonyPatch(typeof(PlayerConnectionLog), "OnPlayerEnteredRoom", new[] { typeof(Photon.Realtime.Player) })]
    internal static class ManyPlayersJoinLogPatch
    {
        private static void Postfix(Photon.Realtime.Player __0)
        {
            if (!ManyPlayers.Enabled)
                return;

            string name = string.IsNullOrEmpty(__0?.NickName) ? "unknown" : __0.NickName;
            int count = PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
            ManyPlayers.LogLobby($"Joined: {name} ({count}/{ManyPlayers.MaxPlayers})");
        }
    }

    [HarmonyPatch(typeof(PlayerConnectionLog), "OnPlayerLeftRoom", new[] { typeof(Photon.Realtime.Player) })]
    internal static class ManyPlayersLeftLogPatch
    {
        private static void Postfix(Photon.Realtime.Player __0)
        {
            if (!ManyPlayers.Enabled)
                return;

            string name = string.IsNullOrEmpty(__0?.NickName) ? "unknown" : __0.NickName;
            int count = PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
            ManyPlayers.LogLobby($"Left: {name} ({count}/{ManyPlayers.MaxPlayers})");
        }
    }

    [HarmonyPatch(typeof(PlayerHandler), "AssignMixerGroup", new[] { typeof(Character) })]
    internal static class ManyPlayersVoiceGroupPatch
    {
        private static bool Prefix(Character __0, ref byte __result)
        {
            if (!ManyPlayers.Enabled || ModConfig.ManyPlayersVoiceFix == null || !ModConfig.ManyPlayersVoiceFix.Value)
                return true;

            __result = ManyPlayers.VoiceGroupFor(__0);
            return false;
        }
    }

    [HarmonyPatch(typeof(UIPlayerNames), "Init")]
    internal static class ManyPlayersNameInitPatch
    {
        private static void Prefix(UIPlayerNames __instance)
        {
            if (ManyPlayers.Enabled && ModConfig.ManyPlayersUiFix != null && ModConfig.ManyPlayersUiFix.Value)
                ManyPlayers.EnsureComponentArrayCapacity(__instance, "playerNameText", ManyPlayers.MaxPlayers);
        }
    }

    [HarmonyPatch(typeof(UIPlayerNames), "DisableName", new[] { typeof(int) })]
    internal static class ManyPlayersDisableNamePatch
    {
        private static bool Prefix(UIPlayerNames __instance, int __0)
        {
            if (!ManyPlayers.Enabled || ModConfig.ManyPlayersUiFix == null || !ModConfig.ManyPlayersUiFix.Value)
                return true;

            return ManyPlayers.IsIndexInsideArray(__instance, "playerNameText", __0);
        }
    }

    [HarmonyPatch(typeof(WaitingForPlayersUI), "Update")]
    internal static class ManyPlayersWaitingUiPatch
    {
        private static void Prefix(WaitingForPlayersUI __instance)
        {
            if (ManyPlayers.Enabled && ModConfig.ManyPlayersUiFix != null && ModConfig.ManyPlayersUiFix.Value)
                ManyPlayers.EnsureComponentArrayCapacity(__instance, "scoutImages", ManyPlayers.MaxPlayers);
        }
    }
}
