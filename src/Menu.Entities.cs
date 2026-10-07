using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakMX
{
    public static partial class Menu
    {
        private static int _entitySelection;
        private static int _entityTarget = -1;
        private static int _entityPlacement;
        private static int _entityAmount = 1;
        private static string _entitySearch = "";
        private static string _entityFilterSearch;
        private static Lang _entityFilterLanguage;
        private static readonly List<int> _visibleEntities = new();
        private static Vector2 _entityScroll;
        private static EntitySpawnResult? _entityResult;

        private static string EntitySectionName() => Localization.Pick(Localization.Current,
            "Сущности", "Entities", "实体", "實體", "Сутності", "エンティティ", "개체", "Entidades", "Entidades", "Kreaturen", "Créatures", "Entità", "Istoty", "Varlıklar");

        private static void DrawEntitySpawner()
        {
            if (!BeginSection("inventory.redesign.entities", EntitySectionName(), false)) return;
            EntitySpawner.EnsureCatalog();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Tx("Поиск:", "Search:", "搜索:", "搜尋:"), Theme.LabelDim, GUILayout.Width(54));
            _entitySearch = GUILayout.TextField(_entitySearch, Theme.TextInput, GUILayout.Height(30));
            if (GUILayout.Button(Tx("Обновить", "Reload", "刷新", "重新整理"), Theme.LinkBtn, GUILayout.Width(86), GUILayout.Height(30)))
                EntitySpawner.LoadCatalog();
            GUILayout.EndHorizontal();

            // Freeze the filtered rows for the complete Layout/Repaint cycle.
            if (Event.current.type == EventType.Layout && (_entityFilterSearch != _entitySearch || _entityFilterLanguage != Localization.Current))
            {
                _entityFilterSearch = _entitySearch;
                _entityFilterLanguage = Localization.Current;
                _visibleEntities.Clear();
                for (int i = 0; i < EntitySpawner.Entities.Count; i++)
                {
                    EntityDefinition definition = EntitySpawner.Entities[i];
                    string search = (_entitySearch ?? "").Trim();
                    if (search.Length == 0 || definition.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || definition.ResourcePath.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                        _visibleEntities.Add(i);
                }
            }
            _entityScroll = BeginVerticalScroll(_entityScroll, GUILayout.Height(176));
            int columns = TileColumns();
            float width = TileWidth(columns);
            for (int row = 0; row < _visibleEntities.Count; row += columns)
            {
                GUILayout.BeginHorizontal();
                for (int column = 0; column < columns; column++)
                {
                    int cell = row + column;
                    if (cell < _visibleEntities.Count)
                    {
                        int index = _visibleEntities[cell];
                        EntityDefinition definition = EntitySpawner.Entities[index];
                        bool enabled = GUI.enabled;
                        GUI.enabled = enabled && definition.Prefab != null;
                        if (DrawTile(definition.Name, definition.ResourcePath, _entitySelection == index, width, 48f,
                            _entitySelection == index ? Theme.ListItemActive : Theme.ListItem))
                            _entitySelection = index;
                        GUI.enabled = enabled;
                    }
                    else GUILayout.Space(width);
                    if (column < columns - 1) GUILayout.Space(8f);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
            }
            if (_visibleEntities.Count == 0) GUILayout.Label(Tx("Ничего не найдено", "No matches", "没有匹配项", "沒有符合項目"), Theme.LabelDim);
            GUILayout.EndScrollView();

            _entitySelection = Mathf.Clamp(_entitySelection, 0, EntitySpawner.Entities.Count - 1);
            GUILayout.Label(EntitySpawner.Entities[_entitySelection].Name, Theme.LabelDim);
            Character target = TargetPicker(ref _entityTarget);
            DrawChipBar(ref _entityPlacement, new[]
            {
                Localization.Pick(Localization.Current, "Перед игроком", "In front of player", "玩家前方", "玩家前方", "Перед гравцем", "プレイヤーの前", "플레이어 앞", "Frente al jugador", "À frente do jogador", "Vor dem Spieler", "Devant le joueur", "Davanti al giocatore", "Przed graczem", "Oyuncunun önünde"),
                Localization.Pick(Localization.Current, "В точке прицела", "At crosshair", "准星位置", "準星位置", "У точці прицілу", "照準の位置", "조준점", "En la mira", "Na mira", "Am Fadenkreuz", "Au viseur", "Al mirino", "Na celowniku", "Nişangâhta")
            });

            GUILayout.BeginHorizontal();
            GUILayout.Label(Tx("Количество", "Amount", "数量", "數量"), Theme.LabelDim);
            GUILayout.FlexibleSpace();
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && _entityAmount > 1;
            if (GUILayout.Button("-", Theme.Chip, GUILayout.Width(32), GUILayout.Height(30))) _entityAmount--;
            GUI.enabled = previousEnabled;
            GUILayout.Label(_entityAmount.ToString(), Theme.Label, GUILayout.Width(28), GUILayout.Height(30));
            GUI.enabled = previousEnabled && _entityAmount < EntitySpawner.MaxBatch;
            if (GUILayout.Button("+", Theme.Chip, GUILayout.Width(32), GUILayout.Height(30))) _entityAmount++;
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();

            DrawActionTiles(new ActionTileSpec(
                Localization.Pick(Localization.Current, "Создать", "Spawn", "生成", "生成", "Створити", "スポーン", "생성", "Crear", "Criar", "Erzeugen", "Créer", "Genera", "Utwórz", "Oluştur"),
                Tx("Создать выбранную сущность в лобби.", "Spawn the selected entity in the lobby.", "在大厅中生成所选实体。", "在房間中生成所選實體。"),
                () => _entityResult = EntitySpawner.Spawn(_entitySelection, target, (EntityPlacement)_entityPlacement, _entityAmount), 1));
            if (_entityResult.HasValue) GUILayout.Label(EntityResultText(_entityResult.Value), Theme.LabelDim);
            int alive = EntitySpawner.AliveCount;
            GUILayout.Label(Tx("Создано сейчас: ", "Currently spawned: ", "当前生成数量: ", "目前生成數量: ") + alive + "/" + EntitySpawner.MaxAlive, Theme.LabelDim);
            previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && alive > 0;
            DrawActionTiles(new ActionTileSpec(
                Localization.Pick(Localization.Current, "Удалить созданных", "Remove spawned", "移除已生成实体", "移除已生成實體", "Видалити створених", "生成した個体を削除", "생성한 개체 제거", "Eliminar creados", "Remover criados", "Erzeugte entfernen", "Supprimer les créations", "Rimuovi le entità create", "Usuń utworzone", "Oluşturulanları kaldır"),
                Tx("Удаляет только сущности, созданные здесь в текущем лобби.", "Removes only entities spawned here in the current lobby.", "仅移除在当前大厅中通过此处生成的实体。", "僅移除目前房間中在此處生成的實體。"),
                () => _entityResult = EntitySpawner.ClearSpawned(), 2));
            GUI.enabled = previousEnabled;
            EndSection();
        }

        private static string EntityResultText(EntitySpawnResult result) => result.Status switch
        {
            EntitySpawnStatus.Spawned => Tx("Создано: ", "Spawned: ", "已生成: ", "已生成: ") + result.Count + "/" + result.Requested,
            EntitySpawnStatus.Cleared => Tx("Удалено: ", "Removed: ", "已移除: ", "已移除: ") + result.Count,
            EntitySpawnStatus.Locked => Tx("Инструменты заблокированы античитом хоста.", "Tools are blocked by the host's anti-cheat.", "工具已被房主的反作弊锁定。", "工具已被房主的反作弊鎖定。"),
            EntitySpawnStatus.NoRoom => Tx("Сначала войди в лобби.", "Join a lobby first.", "请先加入大厅。", "請先加入房間。"),
            EntitySpawnStatus.NoPlayer => Tx("Персонаж ещё не готов.", "The character is not ready yet.", "角色尚未就绪。", "角色尚未就緒。"),
            EntitySpawnStatus.Unavailable => Tx("Эта сущность недоступна в текущей версии игры.", "This entity is unavailable in the current game version.", "当前游戏版本中此实体不可用。", "目前遊戲版本中此實體不可用。"),
            EntitySpawnStatus.SceneNotReady => Tx("Менеджер сущностей недоступен на этой карте.", "The entity manager is unavailable on this map.", "此地图没有可用的实体管理器。", "此地圖沒有可用的實體管理器。"),
            EntitySpawnStatus.HostRequired => Tx("Эту сущность может создать только хост.", "Only the host can spawn this entity.", "只有房主可以生成此实体。", "只有房主可以生成此實體。"),
            EntitySpawnStatus.NoTornadoPath => Tx("На этой карте нет маршрута для торнадо.", "This map has no tornado route.", "此地图没有龙卷风路径。", "此地圖沒有龍捲風路徑。"),
            EntitySpawnStatus.NoGround => Tx("В выбранной точке нет подходящей поверхности.", "No suitable surface at the selected position.", "所选位置没有合适的表面。", "所選位置沒有合適的表面。"),
            EntitySpawnStatus.Limit => Tx("Достигнут лимит созданных сущностей: ", "Spawned entity limit reached: ", "已达到生成实体上限: ", "已達到生成實體上限: ") + EntitySpawner.MaxAlive,
            EntitySpawnStatus.Cooldown => Tx("Подожди немного перед следующим спавном.", "Wait briefly before spawning again.", "请稍等片刻再生成。", "請稍等片刻再生成。"),
            _ => Tx("Не удалось создать сущность. Подробности в локальном логе.", "Could not spawn the entity. See the local log for details.", "无法生成实体。详情见本地日志。", "無法生成實體。詳情見本機日誌。")
        };
    }
}
