using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakMX
{
    internal static class QuickActions
    {
        private static readonly HashSet<string> FavoriteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static string _favoriteSource;

        public struct Definition
        {
            public readonly string Id;
            public readonly string Ru;
            public readonly string En;
            public readonly string TipRu;
            public readonly string TipEn;

            public Definition(string id, string ru, string en, string tipRu, string tipEn)
            {
                Id = id;
                Ru = ru;
                En = en;
                TipRu = tipRu;
                TipEn = tipEn;
            }
        }

        public static readonly Definition[] Actions =
        {
            new Definition("none", "Нет", "None", "Слот выключен.", "This slot is disabled."),
            new Definition("toggle_fly", "Переключить полет", "Toggle fly", "Включает или выключает полет.", "Turns fly mode on or off."),
            new Definition("toggle_noclip", "Переключить noclip", "Toggle noclip", "Включает или выключает прохождение через стены во время полета.", "Turns fly noclip on or off."),
            new Definition("toggle_god", "Переключить бессмертие", "Toggle god mode", "Включает или выключает бессмертие.", "Turns god mode on or off."),
            new Definition("toggle_infinite_stamina", "Переключить стамину", "Toggle stamina", "Включает или выключает бесконечную стамину.", "Turns infinite stamina on or off."),
            new Definition("full_stamina", "Заполнить стамину", "Fill stamina", "Сразу заполняет стамину локального персонажа.", "Immediately fills the local character stamina."),
            new Definition("revive_self", "Воскресить себя", "Revive self", "Пробует воскресить локального персонажа.", "Tries to revive the local character."),
            new Definition("clear_status", "Вылечить себя", "Cure self", "Очищает все недуги локального персонажа.", "Clears every affliction from the local character."),
            new Definition("warp_spawn", "Телепорт на спавн", "Warp to spawn", "Телепортирует тебя на твою точку спавна.", "Warps you to your spawn point."),
            new Definition("toggle_ping_tp", "Переключить ТП к пингу", "Toggle ping TP", "Включает или выключает телепорт к пингу.", "Turns teleport-to-ping on or off."),
            new Definition("anti_stuck", "Анти-застревание", "Anti-stuck", "Сбрасывает скорость/падение и поднимает персонажа вверх.", "Resets velocity/fall state and lifts the character up."),
            new Definition("toggle_player_esp", "ESP игроков", "Player ESP", "Включает или выключает ESP игроков.", "Turns player ESP on or off."),
            new Definition("toggle_item_esp", "ESP предметов", "Item ESP", "Включает или выключает ESP предметов.", "Turns item ESP on or off."),
            new Definition("toggle_world_esp", "ESP опасностей", "Danger ESP", "Включает или выключает ESP опасностей и важных объектов.", "Turns danger/object ESP on or off."),
            new Definition("toggle_hear_everyone", "Слышно всех", "Hear everyone", "Включает или выключает локальную слышимость всех игроков.", "Turns local hear-everyone mode on or off."),
            new Definition("random_outfit", "Случайная одежда", "Random outfit", "Случайно меняет одежду, цвет и остальные категории косметики.", "Randomizes outfit, color, and cosmetic categories."),
            new Definition("campfire_food", "Еда у костра", "Campfire food", "Спавнит еду у костра под размер лобби.", "Spawns campfire food for the lobby size."),
            new Definition("campfire_backpacks", "Рюкзаки у костра", "Campfire backpacks", "Спавнит рюкзаки у костра примерно на четверть игроков.", "Spawns campfire backpacks for roughly a quarter of the lobby."),
            new Definition("campfire_supplies", "Еда + рюкзаки", "Campfire supplies", "Спавнит еду и рюкзаки у костра.", "Spawns campfire food and backpacks."),
            new Definition("force_win", "Завершить забег", "Force win", "Пробует мгновенно завершить текущий забег победой.", "Tries to instantly finish the current run as a win."),
        };

        public static void Tick(bool menuOpen)
        {
            if (menuOpen || Menu.IsCapturingHotkey)
                return;
            if (AntiCheat.ClientToolsLocked)
                return;
            if (ModConfig.QuickActionKeys == null || ModConfig.QuickActionIds == null)
                return;

            for (int i = 0; i < ModConfig.QuickActionSlotCount; i++)
            {
                KeyCode key = ModConfig.QuickActionKeys[i]?.Value ?? KeyCode.None;
                if (key == KeyCode.None || !Input.GetKeyDown(key))
                    continue;

                Execute(ModConfig.QuickActionIds[i]?.Value);
            }
        }

        public static int IndexOf(string id)
        {
            string value = NormalizeId(id);
            for (int i = 0; i < Actions.Length; i++)
                if (string.Equals(Actions[i].Id, value, StringComparison.OrdinalIgnoreCase))
                    return i;
            return 0;
        }

        public static string NormalizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "none";
            string value = id.Trim().ToLowerInvariant();
            return IndexOfKnown(value) >= 0 ? value : "none";
        }

        public static string Label(string id, bool russian)
        {
            int index = IndexOf(id);
            Definition def = Actions[Mathf.Clamp(index, 0, Actions.Length - 1)];
            return russian ? def.Ru : def.En;
        }

        public static string Tip(string id, bool russian)
        {
            int index = IndexOf(id);
            Definition def = Actions[Mathf.Clamp(index, 0, Actions.Length - 1)];
            return russian ? def.TipRu : def.TipEn;
        }

        public static void SetSlotAction(int slot, string id)
        {
            if (slot < 0 || slot >= ModConfig.QuickActionSlotCount || ModConfig.QuickActionIds == null)
                return;
            ModConfig.QuickActionIds[slot].Value = NormalizeId(id);
        }

        public static bool IsFavorite(string id)
        {
            EnsureFavorites();
            return FavoriteIds.Contains(NormalizeId(id));
        }

        public static void SetFavorite(string id, bool favorite)
        {
            string action = NormalizeId(id);
            if (action == "none" || ModConfig.FavoriteActionIds == null)
                return;

            EnsureFavorites();
            if (favorite)
                FavoriteIds.Add(action);
            else
                FavoriteIds.Remove(action);

            var ordered = new List<string>();
            for (int i = 1; i < Actions.Length; i++)
            {
                if (FavoriteIds.Contains(Actions[i].Id))
                    ordered.Add(Actions[i].Id);
            }
            string value = string.Join(";", ordered.ToArray());
            ModConfig.FavoriteActionIds.Value = value;
            _favoriteSource = value;
        }

        public static int FavoriteCount
        {
            get
            {
                EnsureFavorites();
                return FavoriteIds.Count;
            }
        }

        private static int IndexOfKnown(string id)
        {
            for (int i = 0; i < Actions.Length; i++)
                if (string.Equals(Actions[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        public static void Execute(string id)
        {
            string action = NormalizeId(id);
            if (action == "none")
                return;

            try
            {
                Character local = Character.localCharacter;
                switch (action)
                {
                    case "toggle_fly":
                        ModConfig.Fly = !ModConfig.Fly;
                        break;
                    case "toggle_noclip":
                        ModConfig.Noclip = !ModConfig.Noclip;
                        if (ModConfig.Noclip)
                            ModConfig.Fly = true;
                        break;
                    case "toggle_god":
                        ModConfig.GodMode = !ModConfig.GodMode;
                        break;
                    case "toggle_infinite_stamina":
                        ModConfig.InfiniteStamina = !ModConfig.InfiniteStamina;
                        break;
                    case "full_stamina":
                        GameApi.FullStamina(local);
                        break;
                    case "revive_self":
                        GameApi.ReviveSelf();
                        break;
                    case "clear_status":
                        GameApi.ClearAllStatus(local);
                        break;
                    case "warp_spawn":
                        GameApi.WarpToSpawn();
                        break;
                    case "toggle_ping_tp":
                        ModConfig.TeleportToPing = !ModConfig.TeleportToPing;
                        break;
                    case "anti_stuck":
                        GameApi.AntiStuck(local);
                        break;
                    case "toggle_player_esp":
                        ModConfig.PlayerEsp = !ModConfig.PlayerEsp;
                        break;
                    case "toggle_item_esp":
                        ModConfig.ItemEsp = !ModConfig.ItemEsp;
                        break;
                    case "toggle_world_esp":
                        if (ModConfig.WorldEsp != null)
                            ModConfig.WorldEsp.Value = !ModConfig.WorldEsp.Value;
                        break;
                    case "toggle_hear_everyone":
                        ModConfig.GlobalVoice = !ModConfig.GlobalVoice;
                        break;
                    case "random_outfit":
                        GameApi.RandomizeOutfitAndColor();
                        break;
                    case "campfire_food":
                        GameApi.SpawnCampfireFoodForLobby();
                        break;
                    case "campfire_backpacks":
                        GameApi.SpawnCampfireBackpacksForLobby();
                        break;
                    case "campfire_supplies":
                        GameApi.SpawnCampfireSuppliesForLobby();
                        break;
                    case "force_win":
                        GameApi.ForceWin();
                        break;
                }

                GameApi.AddAdminLog("quick action: " + Label(action, Localization.Current == Lang.Russian));
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[QuickActions] " + e.Message);
            }
        }

        private static void EnsureFavorites()
        {
            string source = ModConfig.FavoriteActionIds?.Value ?? "";
            if (string.Equals(source, _favoriteSource, StringComparison.Ordinal))
                return;

            FavoriteIds.Clear();
            string[] parts = source.Split(new[] { ';', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string action = NormalizeId(parts[i]);
                if (action != "none")
                    FavoriteIds.Add(action);
            }
            _favoriteSource = source;
        }
    }
}
