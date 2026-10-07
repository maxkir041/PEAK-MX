using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Photon.Pun;
using Peak.Network;
using UnityEngine;

namespace PeakMX
{
    internal static class CompatibilityDiagnostics
    {
        public struct Row
        {
            public readonly string Name;
            public readonly bool Ok;
            public readonly string Detail;

            public Row(string name, bool ok, string detail)
            {
                Name = name;
                Ok = ok;
                Detail = detail;
            }
        }

        private const BindingFlags AnyInstance =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static List<Row> Rows(bool russian)
        {
            var rows = new List<Row>();
            rows.Add(new Row(russian ? "PEAK-MX" : "PEAK-MX", true, "v" + Plugin.Version));
            rows.Add(new Row(russian ? "Версия игры" : "Game version", true, Safe(() => Application.version, "?")));
            rows.Add(new Row(russian ? "Photon" : "Photon", Safe(() => PhotonNetwork.NetworkingClient != null, false),
                Safe(() => PhotonNetwork.InRoom
                    ? $"{(russian ? "комната" : "room")}: {PhotonNetwork.PlayerList.Length}"
                    : (russian ? "не в комнате" : "not in room"), "?")));

            rows.Add(CheckMethod(typeof(CharacterMovement), "RocketExplodeRPC",
                russian ? "Взрыв игрока" : "Player explosion",
                russian ? "RPC взрыва найден" : "Explosion RPC found"));
            rows.Add(CheckMethod(typeof(CharacterMovement), "FallFactor",
                russian ? "Урон от падения" : "Fall damage",
                russian ? "метод найден" : "method found"));
            rows.Add(CheckMethod(typeof(CharacterMovement), "MaxVelDmg",
                russian ? "Скорость падения" : "Fall velocity",
                russian ? "метод найден" : "method found"));
            rows.Add(CheckMethod(typeof(CharacterMovement), "AcceptableAngle", new[] { typeof(float) },
                russian ? "Угол падения" : "Fall angle",
                russian ? "метод найден" : "method found"));
            rows.Add(CheckMethod(typeof(CharacterInput), "ResetInput",
                russian ? "Блокировка ввода" : "Input blocker",
                russian ? "метод найден" : "method found"));

            rows.Add(CheckField(typeof(Player), "itemSlots",
                russian ? "Основные слоты" : "Main slots"));
            rows.Add(CheckField(typeof(Player), "backpackSlot",
                russian ? "Слот на спине" : "Back slot"));
            rows.Add(CheckField(typeof(Player), "tempFullSlot",
                russian ? "Амулет/временный слот" : "Amulet/temp slot"));
            rows.Add(CheckMember(typeof(ItemSlot), "prefab",
                russian ? "Предмет в слоте" : "Slot item"));
            rows.Add(CheckMember(typeof(CharacterData), "dead",
                russian ? "Состояние смерти" : "Dead state"));
            rows.Add(CheckMember(typeof(CharacterData), "currentItem",
                russian ? "Текущий предмет" : "Current item"));
            rows.Add(CheckMember(typeof(CharacterData), "currentStamina",
                russian ? "Стамина" : "Stamina"));

            rows.Add(CheckMethod(typeof(NetworkingUtilities), "HostRoomOptions",
                russian ? "Большие лобби" : "Large lobbies",
                russian ? "метод комнаты найден" : "room method found"));
            rows.Add(CheckMethod(typeof(AirportCheckInKiosk), "StartGame", new[] { typeof(int) },
                russian ? "Старт экспедиции" : "Expedition start",
                russian ? "метод киоска найден" : "kiosk method found"));
            rows.Add(CheckMethod(typeof(AirportCheckInKiosk), "LoadIslandMaster", new[] { typeof(int), typeof(byte[]) },
                russian ? "Загрузка острова" : "Island loading",
                russian ? "метод загрузки найден" : "load method found"));
            rows.Add(CheckMethod(typeof(PlayerHandler), "AssignMixerGroup", new[] { typeof(Character) },
                russian ? "Голосовые группы" : "Voice groups",
                russian ? "метод найден" : "method found"));
            rows.Add(CheckMethod(typeof(UIPlayerNames), "DisableName", new[] { typeof(int) },
                russian ? "UI имен игроков" : "Player names UI",
                russian ? "метод найден" : "method found"));
            rows.Add(CheckMethod(typeof(PointPinger), "ReceivePoint_Rpc", new[] { typeof(Vector3), typeof(Vector3) },
                russian ? "Пинги карты" : "Map pings",
                russian ? "RPC найден" : "RPC found"));

            rows.Add(CheckEnum<CharacterAfflictions.STATUSTYPE>("Arrow",
                russian ? "Стрелы" : "Arrows"));
            rows.Add(CheckEnum<CharacterAfflictions.STATUSTYPE>("Petrify",
                russian ? "Окаменение" : "Petrify"));
            rows.Add(CheckEnum<CharacterAfflictions.STATUSTYPE>("FlyTrap",
                russian ? "Мухоловка" : "Flytrap"));

            rows.Add(CheckField(typeof(CharacterVoiceHandler), "m_source",
                russian ? "Голосовой источник" : "Voice source"));
            rows.Add(CheckField(typeof(VoiceObscuranceFilter), "_voiceHandler",
                russian ? "Фильтр эха голоса" : "Voice echo filter"));
            rows.Add(CheckField(typeof(MapHandler), "respawnThePeak",
                russian ? "Телепорт на пик" : "Peak teleport"));

            bool itemsOk = Safe(() =>
            {
                GameApi.EnsureItemsLoaded();
                return GameApi.ItemNames.Count > 0;
            }, false);
            rows.Add(new Row(russian ? "База предметов" : "Item database", itemsOk,
                itemsOk
                    ? GameApi.ItemNames.Count.ToString()
                    : (russian ? "предметы не загружены" : "items not loaded")));

            int cosmeticTotal = 0;
            var cosmeticParts = new List<string>();
            foreach (GameApi.CosmeticCategory category in Enum.GetValues(typeof(GameApi.CosmeticCategory)))
            {
                int count = Safe(() => GameApi.GetCosmeticOptions(category).Length, 0);
                cosmeticTotal += count;
                cosmeticParts.Add(category + "=" + count);
            }
            rows.Add(new Row(russian ? "База косметики" : "Cosmetic database", cosmeticTotal > 0,
                cosmeticTotal > 0
                    ? string.Join(", ", cosmeticParts)
                    : (russian ? "косметика не найдена" : "cosmetics not found")));

            rows.Add(new Row(russian ? "Ачивки" : "Achievements", Safe(() => SteamAch.AllTypes.Length > 0, false),
                Safe(() => SteamAch.AllTypes.Length.ToString(), "?")));

            return rows;
        }

        public static string BuildReport(bool russian)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PEAK-MX compatibility report");
            sb.AppendLine("Mod: " + Plugin.Version);
            sb.AppendLine("Game: " + Safe(() => Application.version, "?"));
            sb.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            List<Row> rows = Rows(russian);
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                sb.Append(row.Ok ? "[OK] " : "[WARN] ");
                sb.Append(row.Name);
                if (!string.IsNullOrWhiteSpace(row.Detail))
                    sb.Append(": ").Append(row.Detail);
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine(MxAhgHost.BuildReport(Localization.Current));
            return sb.ToString();
        }

        private static Row CheckMethod(Type type, string method, string name, string okDetail)
        {
            bool ok = Safe(() => type.GetMethod(method, AnyInstance) != null, false);
            return new Row(name, ok, ok ? okDetail : "missing: " + type.Name + "." + method);
        }

        private static Row CheckMethod(Type type, string method, Type[] args, string name, string okDetail)
        {
            bool ok = Safe(() => type.GetMethod(method, AnyInstance, null, args, null) != null, false);
            return new Row(name, ok, ok ? okDetail : "missing: " + type.Name + "." + method);
        }

        private static Row CheckField(Type type, string field, string name)
        {
            bool ok = Safe(() => type.GetField(field, AnyInstance) != null, false);
            return new Row(name, ok, ok ? "ok" : "missing: " + type.Name + "." + field);
        }

        private static Row CheckMember(Type type, string member, string name)
        {
            bool ok = Safe(() =>
                type.GetField(member, AnyInstance) != null ||
                type.GetProperty(member, AnyInstance) != null ||
                type.GetMethod("get_" + member, AnyInstance) != null, false);
            return new Row(name, ok, ok ? "ok" : "missing: " + type.Name + "." + member);
        }

        private static Row CheckEnum<T>(string value, string name) where T : struct
        {
            bool ok = Safe(() => Enum.IsDefined(typeof(T), value), false);
            return new Row(name, ok, ok ? value : "missing: " + value);
        }

        private static T Safe<T>(Func<T> func, T fallback)
        {
            try { return func(); }
            catch { return fallback; }
        }
    }
}
