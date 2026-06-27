using UnityEngine;

namespace PeakMX
{
    public static partial class Menu
    {
        private static void DrawManyPlayersAdmin()
        {
            if (!BeginSection("admin.many_players", Ru ? "Много игроков" : "Many players", false))
                return;

            GUILayout.Label(ManyPlayers.StatusText(Ru), Theme.LabelDim);

            ModConfig.ManyPlayersEnabled.Value = ToggleRaw(
                Ru ? "Включить большие лобби" : "Enable larger lobbies",
                Ru ? "Меняет лимит комнаты при создании лобби. Лучше включать до создания комнаты." : "Changes the room limit when hosting. Best enabled before creating a room.",
                ModConfig.ManyPlayersEnabled.Value);

            ModConfig.ManyPlayersMaxPlayers.Value = Mathf.RoundToInt(NumberField(
                Ru ? "Макс. игроков" : "Max players",
                ModConfig.ManyPlayersMaxPlayers.Value,
                1f,
                30f));

            ModConfig.ManyPlayersHostOnlyKiosk.Value = ToggleRaw(
                Ru ? "Старт только хостом" : "Host-only start",
                Ru ? "Блокирует запуск экспедиции с киоска для игроков, которые не являются хостом." : "Blocks airport kiosk start actions for non-host players.",
                ModConfig.ManyPlayersHostOnlyKiosk.Value);

            ModConfig.ManyPlayersLobbyDetails.Value = ToggleRaw(
                Ru ? "Лог входов и выходов" : "Join/leave log",
                Ru ? "Пишет события большого лобби в админ-лог PEAK-MX." : "Writes large-lobby events to the PEAK-MX admin log.",
                ModConfig.ManyPlayersLobbyDetails.Value);

            ModConfig.ManyPlayersVoiceFix.Value = ToggleRaw(
                Ru ? "Фикс голосовых групп" : "Voice group fix",
                Ru ? "Переиспользует 4 голосовые группы, чтобы игроки после четвертого не ломали голос." : "Reuses the four voice groups so players after the fourth do not break voice routing.",
                ModConfig.ManyPlayersVoiceFix.Value);

            ModConfig.ManyPlayersUiFix.Value = ToggleRaw(
                Ru ? "Фикс UI игроков" : "Player UI fix",
                Ru ? "Расширяет простые списки имен и ожидания игроков, когда это возможно." : "Expands simple name/waiting UI lists where possible.",
                ModConfig.ManyPlayersUiFix.Value);

            GUILayout.Space(4);
            GUILayout.Label(Ru
                ? "После изменения лимита пересоздай лобби. Для уже созданной комнаты Photon обычно оставляет старый лимит."
                : "Recreate the lobby after changing the limit. Photon usually keeps the old limit for an existing room.",
                Theme.LabelDim);

            EndSection();
        }
    }
}
