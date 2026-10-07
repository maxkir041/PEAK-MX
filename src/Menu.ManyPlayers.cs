using UnityEngine;

namespace PeakMX
{
    public static partial class Menu
    {
        private static void DrawManyPlayersAdmin()
        {
            if (!BeginSection("admin.many_players", Tx("Много игроков", "Many players", "多人大厅", "多人房間"), false))
                return;

            GUILayout.Label(ManyPlayers.StatusText(Localization.Current), Theme.LabelDim);

            ModConfig.ManyPlayersEnabled.Value = ToggleRaw(
                Tx("Включить большие лобби", "Enable larger lobbies", "启用大型大厅", "啟用大型房間"),
                Tx("Меняет лимит комнаты при создании лобби. Лучше включать до создания комнаты.", "Changes the room limit when hosting. Best enabled before creating a room.", "创建房间时修改人数上限。最好在创建房间前启用。", "建立房間時修改人數上限。最好在建立房間前啟用。"),
                ModConfig.ManyPlayersEnabled.Value);

            ModConfig.ManyPlayersMaxPlayers.Value = Mathf.RoundToInt(NumberField(
                Tx("Макс. игроков", "Max players", "最大玩家数", "最大玩家數"),
                ModConfig.ManyPlayersMaxPlayers.Value,
                1f,
                30f));

            ModConfig.ManyPlayersHostOnlyKiosk.Value = ToggleRaw(
                Tx("Старт только хостом", "Host-only start", "仅房主可开始", "僅房主可開始"),
                Tx("Блокирует запуск экспедиции с киоска для игроков, которые не являются хостом.", "Blocks airport kiosk start actions for non-host players.", "阻止非房主玩家通过机场终端开始探险。", "阻止非房主玩家透過機場終端開始探險。"),
                ModConfig.ManyPlayersHostOnlyKiosk.Value);

            ModConfig.ManyPlayersLobbyDetails.Value = ToggleRaw(
                Tx("Лог входов и выходов", "Join/leave log", "加入/离开日志", "加入/離開紀錄"),
                Tx("Пишет события большого лобби в админ-лог PEAK-MX.", "Writes large-lobby events to the PEAK-MX admin log.", "把大型大厅的加入和离开事件写入 PEAK-MX 管理日志。", "把大型房間的加入和離開事件寫入 PEAK-MX 管理紀錄。"),
                ModConfig.ManyPlayersLobbyDetails.Value);

            ModConfig.ManyPlayersVoiceFix.Value = ToggleRaw(
                Tx("Фикс голосовых групп", "Voice group fix", "语音分组修复", "語音分組修復"),
                Tx("Переиспользует 4 голосовые группы, чтобы игроки после четвертого не ломали голос.", "Reuses the four voice groups so players after the fourth do not break voice routing.", "复用 4 个语音分组，避免第 5 名之后的玩家语音路由损坏。", "重複使用 4 個語音分組，避免第 5 名之後的玩家語音路由損壞。"),
                ModConfig.ManyPlayersVoiceFix.Value);

            ModConfig.ManyPlayersUiFix.Value = ToggleRaw(
                Tx("Фикс UI игроков", "Player UI fix", "玩家界面修复", "玩家介面修復"),
                Tx("Расширяет простые списки имен и ожидания игроков, когда это возможно.", "Expands simple name/waiting UI lists where possible.", "尽可能扩展玩家姓名和等待列表界面。", "盡可能擴展玩家姓名和等待列表介面。"),
                ModConfig.ManyPlayersUiFix.Value);

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Tx("Еда + рюкзаки", "Food + backpacks", "食物 + 背包", "食物 + 背包"), Theme.DonateBtn, GUILayout.Height(28)))
                GameApi.SpawnCampfireSuppliesForLobby();
            TipLast(Tx("Спавнит у костра еду под размер лобби и рюкзаки примерно на четверть игроков.", "Spawns campfire food for the lobby size and backpacks for roughly a quarter of players.", "按大厅人数生成营火食物，并按约四分之一玩家数生成背包。", "按房間人數生成營火食物，並按約四分之一玩家數生成背包。"));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Tx("Еда у костра", "Food at campfire", "营火食物", "營火食物"), Theme.LinkBtn, GUILayout.Height(28)))
                GameApi.SpawnCampfireFoodForLobby();
            TipLast(Tx("Спавнит у костра хот-доги или маршмеллоу под размер лобби.", "Spawns hot dogs or marshmallows at the campfire for the lobby size.", "按大厅人数在营火旁生成热狗或棉花糖。", "按房間人數在營火旁生成熱狗或棉花糖。"));
            if (GUILayout.Button(Tx("Рюкзаки у костра", "Backpacks at campfire", "营火背包", "營火背包"), Theme.LinkBtn, GUILayout.Height(28)))
                GameApi.SpawnCampfireBackpacksForLobby();
            TipLast(Tx("Спавнит рюкзаки у костра: примерно один на четырех игроков.", "Spawns backpacks at the campfire: roughly one per four players.", "在营火旁生成背包：约每 4 名玩家 1 个。", "在營火旁生成背包：約每 4 名玩家 1 個。"));
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label(Tx(
                "После изменения лимита пересоздай лобби. Для уже созданной комнаты Photon обычно оставляет старый лимит.",
                "Recreate the lobby after changing the limit. Photon usually keeps the old limit for an existing room.",
                "修改人数上限后请重新创建大厅。Photon 通常会保留已有房间的旧上限。",
                "修改人數上限後請重新建立房間。Photon 通常會保留既有房間的舊上限。"),
                Theme.LabelDim);

            EndSection();
        }
    }
}
