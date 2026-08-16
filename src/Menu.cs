using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BepInEx.Configuration;
using UnityEngine;

namespace PeakMX
{
    /// <summary>PEAK-MX overlay drawn with Unity IMGUI.</summary>
    public static partial class Menu
    {
        // maxkir041's links — opened in the default browser on click.
        private const string UrlSteam = "https://steamcommunity.com/id/everyng/";
        private const string UrlTelegram = "https://t.me/maxkir041";
        private const string UrlThunderstore = "https://thunderstore.io/c/peak/p/maxkir041/PEAK_MX/";
        private const string UrlNexus = "https://www.nexusmods.com/peak/mods/179";
        private const string UrlDonate = "https://www.donationalerts.com/r/maxkir041";
        private const string UrlGitHub = "https://github.com/maxkir041/PEAK-MX";
        private const string UrlWebsite = "https://peak-mx.rkngov.com";
        private const string UrlQr = "https://files.donationalerts.com/uploads/qr/8215314/qr_6de7aa0b93950f375ec25fdfd0bd0c53.png";

        private static readonly string[] Tabs =
            { "tab.character", "tab.cheats", "tab.admin", "tab.inventory", "tab.world", "tab.badges", "tab.cosmetics", "tab.about" };
        private static readonly string[] FeedbackTypeKeys = { "suggestion", "bug", "other" };
        private static readonly string[] FeedbackTypesRu = { "Предложение", "Проблема", "Другое" };
        private static readonly string[] FeedbackTypesEn = { "Suggestion", "Problem", "Other" };

        private const float HeaderHeight = 62f;
        private const float WindowPadding = 12f;
        private const float ResizeGrip = 8f;
        private const float MinWindowWidth = 540f;
        private const float MinWindowHeight = 500f;

        [Flags]
        private enum ResizeEdge
        {
            None = 0,
            Left = 1,
            Right = 2,
            Top = 4,
            Bottom = 8,
        }

        private static Rect _rect = new Rect(80, 80, 680, 580);
        private static Rect _noticeRect = new Rect(40, 40, 360, 0);
        private static int _tab;
        private static Vector2 _scroll;
        private static bool _noticeDismissed;
        private static string _itemSearch = "";
        private static int _selItem = -1;
        private static int _selSlot = 0;
        private static int _selBackpackSlot = 0;
        private static Vector2 _itemScroll;
        private static int _invTarget = -1; // -1 = себе; иначе индекс в GameApi.PlayerChars
        private static bool _excludeSelf = true;
        private static string _tpX = "0", _tpY = "0", _tpZ = "0";
        private static Vector2 _luggageScroll;
        private static int _selLuggage = -1;
        private static int _selStatus;
        private static float _statusAmount = 0.5f;
        private static int _statusTarget = -1; // -1 = себе
        private static int _prankTarget = -1;
        private static int _adminTarget = -1;
        private static Vector2 _adminScroll;
        private static bool _targetPickerOpen;
        private static bool _inventoryTargetPickerOpen;
        private static float _timeOfDay = 9f;
        private static bool _timeOfDayDirty;
        private static int _timePreset = -1;
        private static Vector2 _badgeScroll;
        private static bool[] _badgeUnlockedCache;
        private static ACHIEVEMENTTYPE[] _badgeTypeCache;
        private static float _badgeCacheTime = -999f;
        private static Vector2 _cosmeticScroll;
        private static int _cosmeticCategory;
        private static readonly Dictionary<string, bool> _sectionOpen = new Dictionary<string, bool>();
        private static readonly Dictionary<string, string> _numberText = new Dictionary<string, string>();
        private static ResizeEdge _resizeEdge;
        private static Rect _resizeStartRect;
        private static Vector2 _resizeStartMouse;
        private static Rect _contentRect;
        private static string _hoverTip;
        private static string _lastTip;
        private static float _lastTipUntil;
        private static Vector2 _tipScroll;
        private static bool _closeRequested;
        private static bool _waitingForMenuKey;
        private static bool _showAllDonors;
        private static string _nicknameInput = "";
        private static float _clientIdCopiedUntil;
        private static int _themeVersion = -1;
        private static string _accentHexText;
        private static string _actionHexText;
        private static int _feedbackKind;
        private static string _feedbackTitle = "";
        private static string _feedbackMessage = "";
        private static string _feedbackContact = "";
        private static bool _feedbackRepliesOpen = true;
        private static bool _feedbackAttachScreenshot;
        private static bool _feedbackCommentAttachScreenshot;
        private static bool _feedbackPickForComment;
        private static bool _feedbackPickerRestoreFullscreen;
        private static FullScreenMode _feedbackPickerFullscreenMode;
        private static int _feedbackPickerWidth;
        private static int _feedbackPickerHeight;
        private static string _feedbackSelectedTicket = "";
        private static string _feedbackCommentMessage = "";
        private static string _feedbackScreenshotError = "";
        private static readonly List<FeedbackAttachment> _feedbackAttachments = new List<FeedbackAttachment>();
        private static readonly List<FeedbackAttachment> _feedbackCommentAttachments = new List<FeedbackAttachment>();

        private struct ThemeColorPreset
        {
            public readonly string RuName;
            public readonly string EnName;
            public readonly string AccentHex;
            public readonly string ActionHex;

            public ThemeColorPreset(string ruName, string enName, string accentHex, string actionHex)
            {
                RuName = ruName;
                EnName = enName;
                AccentHex = accentHex;
                ActionHex = actionHex;
            }
        }

        private static readonly ThemeColorPreset[] ThemeColorPresets =
        {
            new ThemeColorPreset("Классика", "Classic", Theme.DefaultAccentHex, Theme.DefaultActionHex),
            new ThemeColorPreset("Лед", "Ice", "#48DDE8", "#7AA8FF"),
            new ThemeColorPreset("Фиолет", "Violet", "#A977FF", "#FF6BCB"),
            new ThemeColorPreset("Янтарь", "Amber", "#FFB84A", "#FF6F3C"),
            new ThemeColorPreset("Роза", "Rose", "#FF6B88", "#9D7BFF"),
        };

        private static string L(string key) => Localization.T(key);
        private static bool Ru => Localization.Current == Lang.Russian;
        private static bool UseModernUi => true;
        public static bool IsCapturingHotkey => _waitingForMenuKey;

        // Default Unity font (Arial) covers Latin + Cyrillic but not CJK. For Chinese/Japanese/
        // Korean we swap in a CJK-capable OS font so the glyphs actually render.
        private static Font _langFont;
        private static Lang _langFontFor = (Lang)(-1);
        private static Font _origFont;
        private static bool _origFontSaved;

        private static void ApplyFont()
        {
            if (!_origFontSaved) { _origFont = GUI.skin.font; _origFontSaved = true; }
            if (_langFontFor != Localization.Current)
            {
                _langFontFor = Localization.Current;
                string os = Localization.RequiredOsFont(Localization.Current);
                try { _langFont = string.IsNullOrEmpty(os) ? null : Font.CreateDynamicFontFromOSFont(os, 14); }
                catch (System.Exception e) { Plugin.Log?.LogWarning($"[Font] {e.Message}"); _langFont = null; }
            }
            GUI.skin.font = _langFont != null ? _langFont : _origFont;
        }

        public static void Draw()
        {
            Theme.EnsureBuilt();
            SyncThemeTextureCache();
            ApplyFont();
            ClampWindowToScreen();
            _hoverTip = null;
            HandleWindowResize(Event.current);
            _rect = GUI.Window(0xAC10, _rect, DrawWindow, GUIContent.none, Theme.Window);
            ClampWindowToScreen();
            DrawTooltipOverlay();
        }

        public static bool ConsumeCloseRequest()
        {
            bool value = _closeRequested;
            _closeRequested = false;
            return value;
        }

        private static void DrawWindow(int id)
        {
            if (UseModernUi)
            {
                DrawWindowModern(id);
                return;
            }
            if (_headerTex == null)
                _headerTex = Theme.GradientTex(Theme.HeaderBg, Theme.HeaderDim, Mathf.RoundToInt(HeaderHeight));
            if (_accentTex == null) _accentTex = Theme.Tex(Theme.Accent);
            GUI.DrawTexture(new Rect(0f, 0f, _rect.width, HeaderHeight), _headerTex);
            GUI.DrawTexture(new Rect(0f, HeaderHeight, _rect.width, 2f), _accentTex);
            if (Event.current != null)
            {
                float navWidth = Mathf.Clamp(_rect.width * 0.22f, 136f, 176f);
                float bodyTop = HeaderHeight + 12f;
                float bodyHeight = _rect.height - bodyTop - WindowPadding;
                float contentX = WindowPadding + navWidth + 12f;
                float contentWidth = _rect.width - contentX - WindowPadding;

                GUILayout.BeginArea(new Rect(16f, 10f, _rect.width - 32f, HeaderHeight - 18f));
                GUILayout.BeginHorizontal();
                DrawBrandMark(32f);
                GUILayout.Space(8f);
                GUILayout.BeginVertical();
                GUILayout.Label("PEAK-MX", Theme.Title);
                GUILayout.Label($"v{Plugin.Version}  |  {L(Tabs[_tab])}", Theme.Subtitle);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", Theme.CloseBtn, GUILayout.Width(34), GUILayout.Height(26)))
                    _closeRequested = true;
                GUILayout.EndHorizontal();
                GUILayout.EndArea();

                Rect navRect = new Rect(WindowPadding, bodyTop, navWidth, bodyHeight);
                Rect contentRect = new Rect(contentX, bodyTop, contentWidth, bodyHeight);

                GUI.Box(navRect, GUIContent.none, Theme.Panel9);
                GUI.Box(contentRect, GUIContent.none, Theme.Card);

                GUILayout.BeginArea(new Rect(navRect.x + 10f, navRect.y + 10f, navRect.width - 20f, navRect.height - 20f));
                for (int i = 0; i < Tabs.Length; i++)
                {
                    var style = i == _tab ? Theme.NavItemActive : Theme.NavItem;
                    if (GUILayout.Button(L(Tabs[i]), style, GUILayout.Height(32)))
                        _tab = i;
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label(Ru ? "Тяни за край, чтобы менять размер" : "Drag any edge to resize", Theme.LabelDim);
                GUILayout.EndArea();

                GUILayout.BeginArea(new Rect(contentRect.x + 12f, contentRect.y + 10f, contentRect.width - 24f, contentRect.height - 20f));
                _scroll = GUILayout.BeginScrollView(_scroll);
                GUILayout.Space(2);
                switch (_tab)
                {
                    case 0: DrawCharacter(); break;
                    case 1: DrawCheats(); break;
                    case 2: DrawAdminModern(); break;
                    case 3: DrawInventory(); break;
                    case 4: DrawWorld(); break;
                    case 5: DrawBadges(); break;
                    case 6: DrawCosmetics(); break;
                    case 7: DrawAbout(); break;
                }
                GUILayout.Space(4);
                GUILayout.EndScrollView();
                GUILayout.EndArea();

                GUI.DragWindow(new Rect(0f, 0f, _rect.width, HeaderHeight));
                return;
            }
            GUILayout.Space(9);
            GUILayout.BeginHorizontal();
            GUILayout.Space(16);
            GUILayout.BeginVertical();
            GUILayout.Label("PEAK-MX", Theme.Title);
            GUILayout.Label($"v{Plugin.Version}  •  by maxkir041", Theme.Subtitle);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(14);

            // Body: nav + content
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(150));
            GUILayout.Space(6);
            for (int i = 0; i < Tabs.Length; i++)
            {
                var style = i == _tab ? Theme.NavItemActive : Theme.NavItem;
                if (GUILayout.Button(L(Tabs[i]), style, GUILayout.Height(34)))
                    _tab = i;
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            GUILayout.BeginVertical();
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Space(4);
            switch (_tab)
            {
                case 0: DrawCharacter(); break;
                case 1: DrawCheats(); break;
                case 2: DrawAdminModern(); break;
                case 3: DrawInventory(); break;
                case 4: DrawWorld(); break;
                case 5: DrawBadges(); break;
                case 6: DrawCosmetics(); break;
                case 7: DrawAbout(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // Footer: hovered-item description, then donation line
            HLine();
            string tip = GUI.tooltip;
            GUILayout.Label(string.IsNullOrEmpty(tip)
                ? (Ru ? "ⓘ Наведи курсор на чит, чтобы увидеть описание" : "ⓘ Hover a cheat to see its description")
                : tip, Theme.TipText);
            GUILayout.Label(Ru ? "❤️ Мод бесплатный — поддержать можно во вкладке «О моде»"
                               : "❤️ This mod is free — you can support it in the About tab", Theme.DonateText);

            GUI.DragWindow(new Rect(0f, 0f, _rect.width, HeaderHeight));
        }

        // ---------- tabs ----------
        private static void DrawCharacter()
        {
            if (UseModernUi && Event.current != null) { DrawCharacterModern(); return; }
            if (Event.current != null)
            {
                if (BeginSection("character.move", L("tab.character"), true))
                {
                    ModConfig.SpeedMod = Toggle("feat.speed", ModConfig.SpeedMod);
                    if (ModConfig.SpeedMod) ModConfig.SpeedAmount = Slider("x", ModConfig.SpeedAmount, 0.5f, 5f);
                    ModConfig.JumpMod = Toggle("feat.jump", ModConfig.JumpMod);
                    if (ModConfig.JumpMod) ModConfig.JumpAmount = Slider("x", ModConfig.JumpAmount, 1f, 5f);
                    EndSection();
                }

                if (BeginSection("character.climb", Ru ? "Лазание" : "Climbing", false))
                {
                    ModConfig.ClimbMod = Toggle("feat.climb", ModConfig.ClimbMod);
                    if (ModConfig.ClimbMod) ModConfig.ClimbAmount = Slider("x", ModConfig.ClimbAmount, 1f, 5f);
                    ModConfig.VineClimbMod = Toggle("feat.vineclimb", ModConfig.VineClimbMod);
                    if (ModConfig.VineClimbMod) ModConfig.VineClimbAmount = Slider("x", ModConfig.VineClimbAmount, 1f, 5f);
                    ModConfig.RopeClimbMod = Toggle("feat.ropeclimb", ModConfig.RopeClimbMod);
                    if (ModConfig.RopeClimbMod) ModConfig.RopeClimbAmount = Slider("x", ModConfig.RopeClimbAmount, 1f, 5f);
                    EndSection();
                }

                if (BeginSection("character.status", Ru ? "Стамина и недуги" : "Stamina & afflictions", false))
                {
                    Character statusTargetChar = TargetPicker(ref _statusTarget);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "Полная стамина" : "Full stamina", Theme.LinkBtn, GUILayout.Height(30)))
                        GameApi.FullStamina(statusTargetChar);
                    if (GUILayout.Button(Ru ? "Снять все недуги" : "Clear afflictions", Theme.LinkBtn, GUILayout.Height(30)))
                        GameApi.ClearAllStatus(statusTargetChar);
                    GUILayout.EndHorizontal();
                    var afflictionNames = Ru ? GameApi.StatusRu : GameApi.StatusEn;
                    _selStatus = GUILayout.SelectionGrid(_selStatus, afflictionNames, 3, Theme.ListItem, GUILayout.Height(140));
                    _statusAmount = Slider(Ru ? "Сила" : "Amount", _statusAmount, 0f, 1f);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "Наложить" : "Apply", Theme.DonateBtn, GUILayout.Height(32)))
                        GameApi.SetStatus(statusTargetChar, _selStatus, _statusAmount);
                    if (GUILayout.Button(Ru ? "Убрать этот" : "Remove this", Theme.LinkBtn, GUILayout.Width(140), GUILayout.Height(32)))
                        GameApi.SetStatus(statusTargetChar, _selStatus, 0f);
                    GUILayout.EndHorizontal();
                    EndSection();
                }
                return;
            }

            GUILayout.Label(L("tab.character"), Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            ModConfig.SpeedMod = Toggle("feat.speed", ModConfig.SpeedMod);
            if (ModConfig.SpeedMod) ModConfig.SpeedAmount = Slider("x", ModConfig.SpeedAmount, 0.5f, 5f);
            ModConfig.JumpMod = Toggle("feat.jump", ModConfig.JumpMod);
            if (ModConfig.JumpMod) ModConfig.JumpAmount = Slider("x", ModConfig.JumpAmount, 1f, 5f);
            GUILayout.EndVertical();

            GUILayout.Label(Ru ? "Лазание" : "Climbing", Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            ModConfig.ClimbMod = Toggle("feat.climb", ModConfig.ClimbMod);
            if (ModConfig.ClimbMod) ModConfig.ClimbAmount = Slider("x", ModConfig.ClimbAmount, 1f, 5f);
            ModConfig.VineClimbMod = Toggle("feat.vineclimb", ModConfig.VineClimbMod);
            if (ModConfig.VineClimbMod) ModConfig.VineClimbAmount = Slider("x", ModConfig.VineClimbAmount, 1f, 5f);
            ModConfig.RopeClimbMod = Toggle("feat.ropeclimb", ModConfig.RopeClimbMod);
            if (ModConfig.RopeClimbMod) ModConfig.RopeClimbAmount = Slider("x", ModConfig.RopeClimbAmount, 1f, 5f);
            GUILayout.EndVertical();

            // Stamina / afflictions editor (the status bar) — works on yourself or any player.
            GUILayout.Label(Ru ? "Стамина и недуги" : "Stamina & afflictions", Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            Character target = TargetPicker(ref _statusTarget);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Полная стамина" : "Full stamina", Theme.LinkBtn, GUILayout.Height(30)))
                GameApi.FullStamina(target);
            if (GUILayout.Button(Ru ? "Снять все недуги" : "Clear afflictions", Theme.LinkBtn, GUILayout.Height(30)))
                GameApi.ClearAllStatus(target);
            GUILayout.EndHorizontal();
            var statusNames = Ru ? GameApi.StatusRu : GameApi.StatusEn;
            _selStatus = GUILayout.SelectionGrid(_selStatus, statusNames, 3, Theme.ListItem, GUILayout.Height(140));
            _statusAmount = Slider(Ru ? "Сила" : "Amount", _statusAmount, 0f, 1f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Наложить" : "Apply", Theme.DonateBtn, GUILayout.Height(32)))
                GameApi.SetStatus(target, _selStatus, _statusAmount);
            if (GUILayout.Button(Ru ? "Убрать этот" : "Remove this", Theme.LinkBtn, GUILayout.Width(140), GUILayout.Height(32)))
                GameApi.SetStatus(target, _selStatus, 0f);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void DrawCheats()
        {
            if (UseModernUi && Event.current != null) { DrawCheatsModern(); return; }
            if (Event.current != null)
            {
                if (BeginSection("cheats.core", L("tab.cheats"), true))
                {
                    ModConfig.GodMode = Toggle("feat.god", ModConfig.GodMode);
                    ModConfig.InfiniteStamina = Toggle("feat.infstam", ModConfig.InfiniteStamina);
                    ModConfig.NoFallDamage = Toggle("feat.nofall", ModConfig.NoFallDamage);
                    ModConfig.NoWeight = Toggle("feat.noweight", ModConfig.NoWeight);
                    ModConfig.LockStatus = Toggle("feat.lockstatus", ModConfig.LockStatus);
                    EndSection();
                }

                if (BeginSection("cheats.fly", Ru ? "Полет и телепорт" : "Fly & teleport", false))
                {
                ModConfig.TeleportToPing = Toggle("feat.tpping", ModConfig.TeleportToPing);
                ModConfig.Fly = Toggle("feat.fly", ModConfig.Fly);
                if (ModConfig.Fly)
                {
                    ModConfig.Noclip = ToggleRaw(
                        Ru ? "Ноклип" : "Noclip",
                        Ru ? "Отключает локальные коллайдеры во время полёта, чтобы можно было проходить сквозь стены."
                           : "Disables local colliders while flying so you can move through walls.",
                        ModConfig.Noclip);
                    ModConfig.FlySpeed = Slider(Ru ? "Скорость" : "Speed", ModConfig.FlySpeed, 1f, 50f);
                    ModConfig.FlyAcceleration = Slider(Ru ? "Ускорение" : "Accel", ModConfig.FlyAcceleration, 1f, 100f);
                }
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Ru ? "Коорд.:" : "Coords:", Theme.LabelDim, GUILayout.Width(54));
                    GUILayout.Label("X", Theme.LabelDim, GUILayout.Width(12)); _tpX = GUILayout.TextField(_tpX ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                    GUILayout.Label("Y", Theme.LabelDim, GUILayout.Width(12)); _tpY = GUILayout.TextField(_tpY ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                    GUILayout.Label("Z", Theme.LabelDim, GUILayout.Width(12)); _tpZ = GUILayout.TextField(_tpZ ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                    if (GUILayout.Button(Ru ? "ТП" : "TP", Theme.DonateBtn, GUILayout.Width(54), GUILayout.Height(26)))
                    {
                        if (float.TryParse(_tpX, out float x) & float.TryParse(_tpY, out float y) & float.TryParse(_tpZ, out float z))
                            GameApi.TeleportToCoords(x, y, z);
                    }
                    GUILayout.EndHorizontal();
                    EndSection();
                }

                if (BeginSection("cheats.actions", Ru ? "Действия" : "Actions", false))
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "Воскресить себя" : "Revive self", Theme.LinkBtn, GUILayout.Height(28))) GameApi.ReviveSelf();
                    if (GUILayout.Button(Ru ? "Убить себя" : "Kill self", Theme.LinkBtn, GUILayout.Height(28))) GameApi.KillSelf();
                    if (GUILayout.Button(Ru ? "К точке спавна" : "Warp to spawn", Theme.LinkBtn, GUILayout.Height(28))) GameApi.WarpToSpawn();
                    GUILayout.EndHorizontal();
                    EndSection();
                }

                if (BeginSection("cheats.players", Ru ? "Игроки" : "Players", false))
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "Обновить список" : "Refresh", Theme.LinkBtn, GUILayout.Height(26))) GameApi.RefreshPlayers();
                    if (GUILayout.Button(Ru ? "Воскресить всех" : "Revive all", Theme.LinkBtn, GUILayout.Height(26))) GameApi.ReviveAll();
                    if (GUILayout.Button(Ru ? "Притянуть всех" : "Warp all to me", Theme.LinkBtn, GUILayout.Height(26))) GameApi.WarpAllToMe();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "Убить всех" : "Kill all", Theme.LinkBtn, GUILayout.Height(26))) GameApi.KillAll(_excludeSelf);
                    _excludeSelf = GUILayout.Toggle(_excludeSelf, Ru ? " Не трогать себя" : " Exclude me", Theme.RowLabel, GUILayout.Height(26));
                    GUILayout.EndHorizontal();
                    if (GameApi.PlayerChars.Count == 0)
                        GUILayout.Label(Ru ? "Список пуст - нажми «Обновить» в лобби" : "Empty - press Refresh in a lobby", Theme.LabelDim);
                    for (int i = 0; i < GameApi.PlayerChars.Count; i++)
                    {
                        var c = GameApi.PlayerChars[i];
                        if (c == null) continue;
                        bool local = false; try { local = c.IsLocal; } catch { }
                        GUILayout.BeginHorizontal();
                        GUILayout.Label((i < GameApi.PlayerNames.Count ? GameApi.PlayerNames[i] : "?") + (local ? (Ru ? " (ты)" : " (you)") : ""), Theme.RowLabel, GUILayout.Width(140));
                    if (!local)
                    {
                        if (GUILayout.Button("TP", Theme.LinkBtn, GUILayout.Width(44))) GameApi.WarpToPlayer(c);
                        if (GUILayout.Button(Ru ? "Спавн" : "Spawn", Theme.LinkBtn, GUILayout.Width(70))) GameApi.WarpPlayerToSpawn(c);
                        if (GUILayout.Button(Ru ? "Притянуть" : "Bring", Theme.LinkBtn, GUILayout.Width(78))) GameApi.BringPlayer(c);
                        if (GUILayout.Button(Ru ? "Убить" : "Kill", Theme.LinkBtn, GUILayout.Width(58))) GameApi.KillPlayer(c);
                        if (GUILayout.Button(Ru ? "Ожив." : "Revive", Theme.LinkBtn, GUILayout.Width(66))) GameApi.RevivePlayer(c);
                        }
                        if (GUILayout.Button(Ru ? "Скаут" : "Scout", Theme.LinkBtn, GUILayout.Width(64))) GameApi.SpawnScoutmaster(c);
                        GUILayout.EndHorizontal();
                    }
                    EndSection();
                }

                if (BeginSection("cheats.pranks", Ru ? "Приколы" : "Pranks", false))
                {
                    Character prankTargetChar = TargetPicker(ref _prankTarget);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "Наслать все недуги" : "Pile on afflictions", Theme.LinkBtn, GUILayout.Height(30)))
                        GameApi.PrankAfflict(prankTargetChar);
                    if (GUILayout.Button(Ru ? "Вылечить" : "Cure", Theme.LinkBtn, GUILayout.Height(30)))
                        GameApi.ClearAllStatus(prankTargetChar);
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button(Ru ? "Забить инвентарь выбранным предметом" : "Stuff inventory with selected item", Theme.DonateBtn, GUILayout.Height(32)))
                        GameApi.FillInventoryWith(prankTargetChar, _selItem);
                    GUILayout.Label(Ru ? "Предмет берётся из вкладки «Инвентарь». Физические шарики есть в новой версии вкладки."
                                       : "The item comes from the Inventory tab. Physical balloons are available in the newer tab.", Theme.LabelDim);
                    EndSection();
                }
                return;
            }

            GUILayout.Label(L("tab.cheats"), Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            ModConfig.GodMode = Toggle("feat.god", ModConfig.GodMode);
            ModConfig.InfiniteStamina = Toggle("feat.infstam", ModConfig.InfiniteStamina);
            ModConfig.NoFallDamage = Toggle("feat.nofall", ModConfig.NoFallDamage);
            ModConfig.NoWeight = Toggle("feat.noweight", ModConfig.NoWeight);
            ModConfig.LockStatus = Toggle("feat.lockstatus", ModConfig.LockStatus);
            GUILayout.EndVertical();

            GUILayout.Label(Ru ? "Полёт и телепорт" : "Fly & teleport", Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            ModConfig.TeleportToPing = Toggle("feat.tpping", ModConfig.TeleportToPing);
            ModConfig.Fly = Toggle("feat.fly", ModConfig.Fly);
            if (ModConfig.Fly)
            {
                ModConfig.FlySpeed = Slider(Ru ? "Скорость" : "Speed", ModConfig.FlySpeed, 1f, 50f);
                ModConfig.FlyAcceleration = Slider(Ru ? "Ускорение" : "Accel", ModConfig.FlyAcceleration, 1f, 100f);
            }
            GUILayout.EndVertical();

            // Quick self-actions
            GUILayout.Label(Ru ? "Действия" : "Actions", Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Воскресить себя" : "Revive self", Theme.LinkBtn, GUILayout.Height(28))) GameApi.ReviveSelf();
            if (GUILayout.Button(Ru ? "Убить себя" : "Kill self", Theme.LinkBtn, GUILayout.Height(28))) GameApi.KillSelf();
            if (GUILayout.Button(Ru ? "К точке спавна" : "Warp to spawn", Theme.LinkBtn, GUILayout.Height(28))) GameApi.WarpToSpawn();
            GUILayout.EndHorizontal();

            // Teleport to coordinates
            GUILayout.BeginHorizontal();
            GUILayout.Label(Ru ? "Коорд.:" : "Coords:", Theme.LabelDim, GUILayout.Width(54));
            GUILayout.Label("X", Theme.LabelDim, GUILayout.Width(12)); _tpX = GUILayout.TextField(_tpX ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
            GUILayout.Label("Y", Theme.LabelDim, GUILayout.Width(12)); _tpY = GUILayout.TextField(_tpY ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
            GUILayout.Label("Z", Theme.LabelDim, GUILayout.Width(12)); _tpZ = GUILayout.TextField(_tpZ ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
            if (GUILayout.Button(Ru ? "ТП" : "TP", Theme.DonateBtn, GUILayout.Width(54), GUILayout.Height(26)))
            {
                if (float.TryParse(_tpX, out float x) & float.TryParse(_tpY, out float y) & float.TryParse(_tpZ, out float z))
                    GameApi.TeleportToCoords(x, y, z);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            // Player targeting (works in lobbies; networked actions)
            GUILayout.Label(Ru ? "Игроки" : "Players", Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Обновить список" : "Refresh", Theme.LinkBtn, GUILayout.Height(26))) GameApi.RefreshPlayers();
            if (GUILayout.Button(Ru ? "Воскресить всех" : "Revive all", Theme.LinkBtn, GUILayout.Height(26))) GameApi.ReviveAll();
            if (GUILayout.Button(Ru ? "Притянуть всех" : "Warp all to me", Theme.LinkBtn, GUILayout.Height(26))) GameApi.WarpAllToMe();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Убить всех" : "Kill all", Theme.LinkBtn, GUILayout.Height(26))) GameApi.KillAll(_excludeSelf);
            _excludeSelf = GUILayout.Toggle(_excludeSelf, Ru ? " Не трогать себя" : " Exclude me", Theme.RowLabel, GUILayout.Height(26));
            GUILayout.EndHorizontal();
            if (GameApi.PlayerChars.Count == 0)
                GUILayout.Label(Ru ? "Список пуст — нажми «Обновить» в лобби" : "Empty — press Refresh in a lobby", Theme.LabelDim);
            for (int i = 0; i < GameApi.PlayerChars.Count; i++)
            {
                var c = GameApi.PlayerChars[i];
                if (c == null) continue;
                bool local = false; try { local = c.IsLocal; } catch { }
                GUILayout.BeginHorizontal();
                GUILayout.Label((i < GameApi.PlayerNames.Count ? GameApi.PlayerNames[i] : "?") + (local ? (Ru ? " (ты)" : " (you)") : ""), Theme.RowLabel, GUILayout.Width(140));
                if (!local)
                {
                    if (GUILayout.Button("TP", Theme.LinkBtn, GUILayout.Width(44))) GameApi.WarpToPlayer(c);
                    if (GUILayout.Button(Ru ? "Притянуть" : "Bring", Theme.LinkBtn, GUILayout.Width(78))) GameApi.BringPlayer(c);
                    if (GUILayout.Button(Ru ? "Убить" : "Kill", Theme.LinkBtn, GUILayout.Width(58))) GameApi.KillPlayer(c);
                    if (GUILayout.Button(Ru ? "Ожив." : "Revive", Theme.LinkBtn, GUILayout.Width(66))) GameApi.RevivePlayer(c);
                }
                if (GUILayout.Button(Ru ? "Скаут" : "Scout", Theme.LinkBtn, GUILayout.Width(64))) GameApi.SpawnScoutmaster(c);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            // Pranks — light-hearted mischief aimed at a chosen player.
            GUILayout.Label(Ru ? "Приколы 😈" : "Pranks 😈", Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            Character pt = TargetPicker(ref _prankTarget);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Наслать все недуги" : "Pile on afflictions", Theme.LinkBtn, GUILayout.Height(30)))
                GameApi.PrankAfflict(pt);
            if (GUILayout.Button(Ru ? "Вылечить" : "Cure", Theme.LinkBtn, GUILayout.Height(30)))
                GameApi.ClearAllStatus(pt);
            GUILayout.EndHorizontal();
            if (GUILayout.Button(Ru ? "🎒 Забить инвентарь выбранным предметом" : "🎒 Stuff inventory with selected item", Theme.DonateBtn, GUILayout.Height(32)))
                GameApi.FillInventoryWith(pt, _selItem);
            GUILayout.Label(Ru ? "Предмет берётся из вкладки «Инвентарь». Физические шарики на игроке — добавлю отдельно."
                               : "Item comes from the Inventory tab. Physical balloons on a player will come separately.", Theme.LabelDim);
            GUILayout.EndVertical();
        }

        private static void DrawInventory()
        {
            if (UseModernUi && Event.current != null) { DrawInventoryModern(); return; }
            if (Event.current != null)
            {
                if (BeginSection("inventory.spawn", Ru ? "Спавн предметов" : "Item spawner", true))
                {
                    bool hasItems = GameApi.ItemNames.Count > 0;
                    if (!hasItems)
                    {
                        if (GUILayout.Button(Ru ? "Загрузить список предметов" : "Load item list", Theme.DonateBtn, GUILayout.Height(30)))
                            GameApi.EnsureItemsLoaded();
                    }
                    else
                    {
                        GUILayout.Label((Ru ? "Предметов: " : "Items: ") + GameApi.ItemNames.Count, Theme.LabelDim);

                        if (_invTarget >= GameApi.PlayerChars.Count) _invTarget = -1;
                        Character invChar = (_invTarget >= 0 && _invTarget < GameApi.PlayerChars.Count) ? GameApi.PlayerChars[_invTarget] : null;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(Ru ? "Кому:" : "Target:", Theme.LabelDim, GUILayout.Width(54));
                        if (GUILayout.Button(Ru ? "Себе" : "Me", _invTarget < 0 ? Theme.ChipActive : Theme.Chip, GUILayout.Height(26)))
                            _invTarget = -1;
                        for (int i = 0; i < GameApi.PlayerChars.Count; i++)
                        {
                            var pc = GameApi.PlayerChars[i];
                            bool me = false; try { me = pc != null && pc.IsLocal; } catch { }
                            if (me) continue;
                            string nm = i < GameApi.PlayerNames.Count ? GameApi.PlayerNames[i] : "?";
                            if (GUILayout.Button(nm, _invTarget == i ? Theme.ChipActive : Theme.Chip, GUILayout.Height(26)))
                                _invTarget = i;
                        }
                        if (GUILayout.Button(Ru ? "Обн." : "Sync", Theme.LinkBtn, GUILayout.Width(52), GUILayout.Height(26)))
                            GameApi.RefreshPlayers();
                        GUILayout.EndHorizontal();

                        int slots = invChar != null ? GameApi.SlotCountFor(invChar) : GameApi.SlotCount();
                        if (slots <= 0) slots = 3;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(Ru ? "Слот:" : "Slot:", Theme.LabelDim, GUILayout.Width(54));
                        for (int i = 0; i < slots; i++)
                            if (GUILayout.Button((i + 1).ToString(), _selSlot == i ? Theme.ChipActive : Theme.Chip, GUILayout.Width(36), GUILayout.Height(30)))
                                _selSlot = i;
                        GUILayout.EndHorizontal();

                        _itemSearch = GUILayout.TextField(_itemSearch ?? "", Theme.LinkBtn, GUILayout.Height(26));
                        _itemScroll = GUILayout.BeginScrollView(_itemScroll, GUILayout.Height(170));
                        string q = (_itemSearch ?? "").Trim().ToLowerInvariant();
                        for (int i = 0; i < GameApi.ItemNames.Count; i++)
                        {
                            string nm = GameApi.ItemNames[i];
                            if (q.Length > 0 && nm.ToLowerInvariant().IndexOf(q) < 0) continue;
                            if (DrawItemListRow(i, nm, _selItem == i))
                                _selItem = i;
                        }
                        GUILayout.EndScrollView();

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(Ru ? "Заспавнить в слот" : "Spawn to slot", Theme.DonateBtn, GUILayout.Height(34)))
                            if (_selItem >= 0)
                            {
                                if (invChar != null) GameApi.SpawnToSlotFor(invChar, _selItem, _selSlot);
                                else GameApi.SpawnToSlot(_selItem, _selSlot);
                            }
                        if (GUILayout.Button(Ru ? "Очистить слот" : "Clear slot", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(28)))
                        {
                            if (invChar != null) GameApi.ClearSlotFor(invChar, _selSlot);
                            else GameApi.ClearSlot(_selSlot);
                        }
                        GUILayout.EndHorizontal();
                        if (_selItem >= 0 && _selItem < GameApi.ItemNames.Count)
                            GUILayout.Label((Ru ? "Выбрано: " : "Selected: ") + GameApi.ItemNames[_selItem], Theme.LabelDim);
                        if (invChar != null)
                            GUILayout.Label((Ru ? "Редактируешь инвентарь игрока: " : "Editing player's inventory: ")
                                            + (_invTarget < GameApi.PlayerNames.Count ? GameApi.PlayerNames[_invTarget] : "?"), Theme.LabelDim);
                    }
                    EndSection();
                }

                if (BeginSection("inventory.recharge", Ru ? "Перезарядка предметов" : "Item recharge", false))
                {
                    ModConfig.RechargeValue = NumberField(Ru ? "Заряд" : "Charge", ModConfig.RechargeValue, 0f, 999f);
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < Mathf.Min(3, GameApi.SlotCount()); i++)
                    {
                        int slot = i;
                        if (GUILayout.Button((Ru ? "Слот " : "Slot ") + (slot + 1), Theme.LinkBtn, GUILayout.Height(28)))
                            GameApi.RechargeSlot(slot, ModConfig.RechargeValue);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "Зарядить выбранный слот" : "Recharge selected slot", Theme.LinkBtn, GUILayout.Height(30)))
                        GameApi.RechargeSlot(_selSlot, ModConfig.RechargeValue);
                    if (GUILayout.Button(Ru ? "Зарядить все" : "Recharge all", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(30)))
                        for (int s = 0; s < GameApi.SlotCount(); s++) GameApi.RechargeSlot(s, ModConfig.RechargeValue);
                    GUILayout.EndHorizontal();
                    EndSection();
                }
                return;
            }
            GUILayout.Label(Ru ? "Спавн предметов" : "Item spawner", Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            if (GameApi.ItemNames.Count == 0)
            {
                if (GUILayout.Button(Ru ? "📦 Загрузить список предметов" : "📦 Load item list", Theme.DonateBtn, GUILayout.Height(30)))
                    GameApi.EnsureItemsLoaded();
            }
            else
            {
                GUILayout.Label((Ru ? "Предметов: " : "Items: ") + GameApi.ItemNames.Count, Theme.LabelDim);

                // Target selector — себе или другому игроку
                if (_invTarget >= GameApi.PlayerChars.Count) _invTarget = -1;
                Character invChar = (_invTarget >= 0 && _invTarget < GameApi.PlayerChars.Count) ? GameApi.PlayerChars[_invTarget] : null;
                GUILayout.BeginHorizontal();
                GUILayout.Label(Ru ? "Кому:" : "Target:", Theme.LabelDim, GUILayout.Width(54));
                if (GUILayout.Button(Ru ? "Себе" : "Me", _invTarget < 0 ? Theme.ChipActive : Theme.Chip, GUILayout.Height(26)))
                    _invTarget = -1;
                for (int i = 0; i < GameApi.PlayerChars.Count; i++)
                {
                    var pc = GameApi.PlayerChars[i];
                    bool me = false; try { me = pc != null && pc.IsLocal; } catch { }
                    if (me) continue; // «Себе» уже есть отдельной кнопкой
                    string nm = i < GameApi.PlayerNames.Count ? GameApi.PlayerNames[i] : "?";
                    if (GUILayout.Button(nm, _invTarget == i ? Theme.ChipActive : Theme.Chip, GUILayout.Height(26)))
                        _invTarget = i;
                }
                if (GUILayout.Button(Ru ? "Обн." : "Sync", Theme.LinkBtn, GUILayout.Width(52), GUILayout.Height(26)))
                    GameApi.RefreshPlayers();
                TipLast(Ru ? "Обновить список игроков для просмотра и редактирования их инвентаря." : "Refresh the player list for viewing and editing inventories.");
                GUILayout.EndHorizontal();

                // Slot selector
                int slots = invChar != null ? GameApi.SlotCountFor(invChar) : GameApi.SlotCount();
                if (slots <= 0) slots = 3;
                GUILayout.BeginHorizontal();
                GUILayout.Label(Ru ? "Слот:" : "Slot:", Theme.LabelDim, GUILayout.Width(54));
                for (int i = 0; i < slots; i++)
                    if (GUILayout.Button((i + 1).ToString(), _selSlot == i ? Theme.ChipActive : Theme.Chip, GUILayout.Width(36), GUILayout.Height(30)))
                        _selSlot = i;
                GUILayout.EndHorizontal();

                // Search
                _itemSearch = GUILayout.TextField(_itemSearch ?? "", Theme.LinkBtn, GUILayout.Height(26));

                // Filtered list
                _itemScroll = GUILayout.BeginScrollView(_itemScroll, GUILayout.Height(170));
                string q = (_itemSearch ?? "").Trim().ToLowerInvariant();
                for (int i = 0; i < GameApi.ItemNames.Count; i++)
                {
                    string nm = GameApi.ItemNames[i];
                    if (q.Length > 0 && nm.ToLowerInvariant().IndexOf(q) < 0) continue;
                    if (DrawItemListRow(i, nm, _selItem == i))
                        _selItem = i;
                }
                GUILayout.EndScrollView();

                // Actions
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Заспавнить в слот" : "Spawn to slot", Theme.DonateBtn, GUILayout.Height(34)))
                    if (_selItem >= 0)
                    {
                        if (invChar != null) GameApi.SpawnToSlotFor(invChar, _selItem, _selSlot);
                        else GameApi.SpawnToSlot(_selItem, _selSlot);
                    }
                if (GUILayout.Button(Ru ? "Очистить слот" : "Clear slot", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(28)))
                {
                    if (invChar != null) GameApi.ClearSlotFor(invChar, _selSlot);
                    else GameApi.ClearSlot(_selSlot);
                }
                GUILayout.EndHorizontal();
                if (_selItem >= 0 && _selItem < GameApi.ItemNames.Count)
                    GUILayout.Label((Ru ? "Выбрано: " : "Selected: ") + GameApi.ItemNames[_selItem], Theme.LabelDim);
                if (invChar != null)
                    GUILayout.Label((Ru ? "⚠ Меняешь инвентарь игрока: " : "⚠ Editing player's inventory: ")
                                    + (_invTarget < GameApi.PlayerNames.Count ? GameApi.PlayerNames[_invTarget] : "?"), Theme.LabelDim);
            }
            GUILayout.EndVertical();

            GUILayout.Label(Ru ? "Перезарядка предметов" : "Item recharge", Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            ModConfig.RechargeValue = Slider(Ru ? "Заряд" : "Charge", ModConfig.RechargeValue, 0f, 999f);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Mathf.Min(3, GameApi.SlotCount()); i++)
            {
                int slot = i;
                if (GUILayout.Button((Ru ? "Слот " : "Slot ") + (slot + 1), Theme.LinkBtn, GUILayout.Height(28)))
                    GameApi.RechargeSlot(slot, ModConfig.RechargeValue);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Зарядить выбранный слот" : "Recharge selected slot", Theme.LinkBtn, GUILayout.Height(30)))
                GameApi.RechargeSlot(_selSlot, ModConfig.RechargeValue);
            if (GUILayout.Button(Ru ? "Зарядить все" : "Recharge all", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(30)))
                for (int s = 0; s < GameApi.SlotCount(); s++) GameApi.RechargeSlot(s, ModConfig.RechargeValue);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void DrawWorld()
        {
            if (UseModernUi && Event.current != null) { DrawWorldModern(); return; }
            if (Event.current != null)
            {
                float runSecsNow = GameApi.ExpeditionTimeSeconds();
                float timeOfDayNow = GameApi.TimeOfDay();
                int dayNow = GameApi.DayCount();
                if (Event.current.type == EventType.Repaint)
                    SyncTimeOfDayInput(timeOfDayNow);

                if (BeginSection("world.time", Ru ? "Время и забег" : "Time & run", true))
                {
                    GUILayout.Label((Ru ? "Время забега: " : "Run time: ") + FormatRunTime(runSecsNow), Theme.LabelDim);
                    GUILayout.Label((Ru ? "День: " : "Day: ") + dayNow + "   " + (Ru ? "Час: " : "Hour: ") + timeOfDayNow.ToString("0.0"), Theme.LabelDim);

                bool holdRunTime = Toggle(Ru ? "Держать время забега" : "Hold run timer", ModConfig.OverrideExpeditionTime);
                if (holdRunTime != ModConfig.OverrideExpeditionTime)
                {
                    if (holdRunTime && ModConfig.ExpeditionTimeSeconds <= 0f)
                        ModConfig.ExpeditionTimeSeconds = runSecsNow;
                    ModConfig.OverrideExpeditionTime = holdRunTime;
                }

                    ModConfig.ExpeditionTimeSeconds = NumberField(Ru ? "Секунды забега" : "Run seconds", ModConfig.ExpeditionTimeSeconds, 0f, 7200f);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "-60 сек" : "-60 sec", Theme.LinkBtn, GUILayout.Height(28)))
                    {
                        ModConfig.ExpeditionTimeSeconds = Mathf.Max(0f, ModConfig.ExpeditionTimeSeconds - 60f);
                        GameApi.SetExpeditionTime(ModConfig.ExpeditionTimeSeconds);
                    }
                    if (GUILayout.Button(Ru ? "Применить" : "Apply", Theme.DonateBtn, GUILayout.Height(28)))
                        GameApi.SetExpeditionTime(ModConfig.ExpeditionTimeSeconds);
                    if (GUILayout.Button(Ru ? "+60 сек" : "+60 sec", Theme.LinkBtn, GUILayout.Height(28)))
                    {
                        ModConfig.ExpeditionTimeSeconds += 60f;
                        GameApi.SetExpeditionTime(ModConfig.ExpeditionTimeSeconds);
                    }
                    GUILayout.EndHorizontal();

                    float editedTime = NumberField(TimeOfDayLabel(), _timeOfDay, 0f, 24f);
                    if (Mathf.Abs(editedTime - _timeOfDay) > 0.001f)
                    {
                        _timeOfDay = editedTime;
                        _timePreset = PresetForHour(_timeOfDay);
                        _timeOfDayDirty = true;
                    }
                    GUILayout.BeginHorizontal();
                    TimePresetButton(Ru ? "Рассвет" : "Dawn", 6f, 0, Ru ? "Выбрать рассвет." : "Choose dawn.");
                    TimePresetButton(Ru ? "День" : "Day", 12f, 1, Ru ? "Выбрать день." : "Choose daytime.");
                    TimePresetButton(Ru ? "Закат" : "Dusk", 19f, 2, Ru ? "Выбрать закат." : "Choose dusk.");
                    TimePresetButton(Ru ? "Ночь" : "Night", 23f, 3, Ru ? "Выбрать ночь." : "Choose night.");
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button(Ru ? "Применить время суток" : "Apply time of day", Theme.DonateBtn, GUILayout.Height(30)))
                        ApplySelectedTimeOfDay();
                    EndSection();
                }

                if (BeginSection("world.finish", Ru ? "Финиш" : "Finish", false))
                {
                    GUILayout.Label(Ru ? "Мгновенно завершает текущий забег победой." : "Instantly ends the current run as a win.", Theme.LabelDim);
                    if (GUILayout.Button(Ru ? "Мгновенно пройти" : "Force win", Theme.DonateBtn, GUILayout.Height(30)))
                        GameApi.ForceWin();
                    EndSection();
                }

                if (BeginSection("world.luggage", Ru ? "Контейнеры рядом" : "Nearby containers", false))
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "Обновить (300 м)" : "Refresh (300 m)", Theme.LinkBtn, GUILayout.Height(26)))
                    { GameApi.RefreshLuggage(); _selLuggage = -1; }
                    if (GUILayout.Button(Ru ? "Открыть все рядом" : "Open all nearby", Theme.DonateBtn, GUILayout.Height(26)))
                        GameApi.OpenAllNearbyLuggage();
                    GUILayout.EndHorizontal();

                    if (GameApi.LuggageLabels.Count == 0)
                        GUILayout.Label(Ru ? "Список пуст — нажми «Обновить» в катке." : "Empty — press Refresh during a run.", Theme.LabelDim);
                    else
                    {
                        _luggageScroll = GUILayout.BeginScrollView(_luggageScroll, GUILayout.Height(150));
                        for (int i = 0; i < GameApi.LuggageLabels.Count; i++)
                            if (GUILayout.Button(GameApi.LuggageLabels[i], _selLuggage == i ? Theme.ListItemActive : Theme.ListItem, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                                _selLuggage = i;
                        GUILayout.EndScrollView();

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(Ru ? "Открыть выбранный" : "Open selected", Theme.LinkBtn, GUILayout.Height(28)))
                        { if (_selLuggage >= 0) GameApi.OpenLuggage(_selLuggage); }
                        if (GUILayout.Button(Ru ? "Телепорт к нему" : "Warp to it", Theme.LinkBtn, GUILayout.Height(28)))
                        { if (_selLuggage >= 0) GameApi.WarpToLuggage(_selLuggage); }
                        GUILayout.EndHorizontal();
                    }
                    EndSection();
                }
                return;
            }
            GUILayout.Label(Ru ? "Время и забег" : "Time & run", Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            float runSecs = GameApi.ExpeditionTimeSeconds();
            float tod = GameApi.TimeOfDay();
            int day = GameApi.DayCount();
            if (Event.current.type == EventType.Repaint)
                SyncTimeOfDayInput(tod);

            GUILayout.Label((Ru ? "Время забега: " : "Run time: ") + FormatRunTime(runSecs), Theme.LabelDim);
            GUILayout.Label((Ru ? "День: " : "Day: ") + day + "   " + (Ru ? "Час: " : "Hour: ") + tod.ToString("0.0"), Theme.LabelDim);

            bool holdTime = Toggle(Ru ? "Держать время забега" : "Hold run timer", ModConfig.OverrideExpeditionTime);
            if (holdTime != ModConfig.OverrideExpeditionTime)
            {
                ModConfig.OverrideExpeditionTime = holdTime;
                if (holdTime) ModConfig.ExpeditionTimeSeconds = runSecs;
            }

            ModConfig.ExpeditionTimeSeconds = Slider(Ru ? "Секунды забега" : "Run seconds", ModConfig.ExpeditionTimeSeconds, 0f, 7200f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "-60 сек" : "-60 sec", Theme.LinkBtn, GUILayout.Height(28)))
            {
                ModConfig.ExpeditionTimeSeconds = Mathf.Max(0f, ModConfig.ExpeditionTimeSeconds - 60f);
                GameApi.SetExpeditionTime(ModConfig.ExpeditionTimeSeconds);
            }
            if (GUILayout.Button(Ru ? "Применить" : "Apply", Theme.DonateBtn, GUILayout.Height(28)))
                GameApi.SetExpeditionTime(ModConfig.ExpeditionTimeSeconds);
            if (GUILayout.Button(Ru ? "+60 сек" : "+60 sec", Theme.LinkBtn, GUILayout.Height(28)))
            {
                ModConfig.ExpeditionTimeSeconds += 60f;
                GameApi.SetExpeditionTime(ModConfig.ExpeditionTimeSeconds);
            }
            GUILayout.EndHorizontal();

            float sliderTime = Slider(TimeOfDayLabel(), _timeOfDay, 0f, 24f);
            if (Mathf.Abs(sliderTime - _timeOfDay) > 0.001f)
            {
                _timeOfDay = sliderTime;
                _timePreset = PresetForHour(_timeOfDay);
                _timeOfDayDirty = true;
            }
            GUILayout.BeginHorizontal();
            TimePresetButton(Ru ? "Рассвет" : "Dawn", 6f, 0, Ru ? "Выбрать рассвет." : "Choose dawn.");
            TimePresetButton(Ru ? "День" : "Day", 12f, 1, Ru ? "Выбрать день." : "Choose daytime.");
            TimePresetButton(Ru ? "Закат" : "Dusk", 19f, 2, Ru ? "Выбрать закат." : "Choose dusk.");
            TimePresetButton(Ru ? "Ночь" : "Night", 23f, 3, Ru ? "Выбрать ночь." : "Choose night.");
            GUILayout.EndHorizontal();
            if (GUILayout.Button(Ru ? "Применить время суток" : "Apply time of day", Theme.DonateBtn, GUILayout.Height(30)))
                ApplySelectedTimeOfDay();
            GUILayout.EndVertical();

            GUILayout.Label(Ru ? "Финиш" : "Finish", Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            GUILayout.Label(Ru ? "Мгновенно завершает текущий забег победой." : "Instantly ends the current run as a win.", Theme.LabelDim);
            if (GUILayout.Button(Ru ? "Мгновенно пройти" : "Force win", Theme.DonateBtn, GUILayout.Height(30)))
                GameApi.ForceWin();
            GUILayout.EndVertical();

            GUILayout.Label(Ru ? "Контейнеры рядом" : "Nearby containers", Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Обновить (300 м)" : "Refresh (300 m)", Theme.LinkBtn, GUILayout.Height(26)))
            { GameApi.RefreshLuggage(); _selLuggage = -1; }
            if (GUILayout.Button(Ru ? "Открыть все рядом" : "Open all nearby", Theme.DonateBtn, GUILayout.Height(26)))
                GameApi.OpenAllNearbyLuggage();
            GUILayout.EndHorizontal();

            if (GameApi.LuggageLabels.Count == 0)
                GUILayout.Label(Ru ? "Список пуст — нажми «Обновить» в катке." : "Empty — press Refresh during a run.", Theme.LabelDim);
            else
            {
                _luggageScroll = GUILayout.BeginScrollView(_luggageScroll, GUILayout.Height(150));
                for (int i = 0; i < GameApi.LuggageLabels.Count; i++)
                    if (GUILayout.Button(GameApi.LuggageLabels[i], _selLuggage == i ? Theme.ListItemActive : Theme.ListItem, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                        _selLuggage = i;
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Открыть выбранный" : "Open selected", Theme.LinkBtn, GUILayout.Height(28)))
                    { if (_selLuggage >= 0) GameApi.OpenLuggage(_selLuggage); }
                if (GUILayout.Button(Ru ? "Телепорт к нему" : "Warp to it", Theme.LinkBtn, GUILayout.Height(28)))
                    { if (_selLuggage >= 0) GameApi.WarpToLuggage(_selLuggage); }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private static string FormatRunTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int h = s / 3600;
            int m = (s % 3600) / 60;
            int sec = s % 60;
            return h > 0 ? $"{h:00}:{m:00}:{sec:00}" : $"{m:00}:{sec:00}";
        }

        private static string TimeOfDayLabel() => Ru ? "Время суток" : "Time of day";

        private static void SyncTimeOfDayInput(float timeOfDayNow)
        {
            if (_timeOfDayDirty)
                return;

            _timeOfDay = Mathf.Repeat(timeOfDayNow, 24f);
            _timePreset = PresetForHour(_timeOfDay);
            SetNumberText(TimeOfDayLabel(), 0f, 24f, _timeOfDay);
        }

        private static int PresetForHour(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            if (Mathf.Abs(Mathf.DeltaAngle(hour * 15f, 6f * 15f)) <= 0.75f) return 0;
            if (Mathf.Abs(Mathf.DeltaAngle(hour * 15f, 12f * 15f)) <= 0.75f) return 1;
            if (Mathf.Abs(Mathf.DeltaAngle(hour * 15f, 19f * 15f)) <= 0.75f) return 2;
            if (Mathf.Abs(Mathf.DeltaAngle(hour * 15f, 23f * 15f)) <= 0.75f) return 3;
            return -1;
        }

        private static void TimePresetButton(string label, float hour, int preset, string tip)
        {
            bool active = _timePreset == preset || PresetForHour(_timeOfDay) == preset;
            if (GUILayout.Button(label, active ? Theme.ChipActive : Theme.LinkBtn, GUILayout.Height(28)))
            {
                _timeOfDay = hour;
                _timePreset = preset;
                _timeOfDayDirty = true;
                SetNumberText(TimeOfDayLabel(), 0f, 24f, _timeOfDay);
                ActionTracker.Track("world_time_preset_select", hour, new Dictionary<string, object> { ["preset"] = label });
            }
            TipLast(tip);
        }

        private static void ApplySelectedTimeOfDay()
        {
            _timeOfDay = Mathf.Repeat(_timeOfDay, 24f);
            GameApi.SetTimeOfDay(_timeOfDay);
            _timeOfDayDirty = false;
            _timePreset = PresetForHour(_timeOfDay);
            SetNumberText(TimeOfDayLabel(), 0f, 24f, _timeOfDay);
        }

        // Locked-badge frame color (kept local — palette in Theme is intentionally unchanged).
        private static readonly Color BadgeLocked = new Color(0.85f, 0.27f, 0.27f);
        private static Texture2D _frameOn, _frameOff, _tileBg;

        private static void DrawBadges()
        {
            if (UseModernUi)
            {
                DrawBadgesModern();
                return;
            }
            var all = SteamAch.AllTypes;
            int total = all.Length;
            var unlocked = new bool[total];
            int got = 0;
            for (int i = 0; i < total; i++)
            {
                bool on = SteamAch.IsUnlocked(all[i]);
                unlocked[i] = on;
                if (on) got++;
            }

            GUILayout.Label(L("tab.badges"), Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            GUILayout.BeginHorizontal();
            GUILayout.Label((Ru ? "Получено: " : "Unlocked: ") + got + " / " + total, Theme.LabelDim);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Ru ? "Выдать все" : "Unlock all", Theme.DonateBtn, GUILayout.Height(28)))
                SteamAch.UnlockAll();
            if (GUILayout.Button(Ru ? "Отозвать все" : "Revoke all", Theme.LinkBtn, GUILayout.Height(28)))
                SteamAch.RevokeAll();
            GUILayout.EndHorizontal();
            GUILayout.Label(Ru ? "Клик по иконке — выдать / отозвать. Зелёная = получена, красная = нет."
                               : "Click an icon to unlock / revoke. Green = unlocked, red = locked.", Theme.LabelDim);

            if (_tileBg == null) _tileBg = Theme.RoundedTex(Theme.Panel, 8);
            if (_frameOn == null) _frameOn = Theme.RoundedTex(Theme.Accent, 8);
            if (_frameOff == null) _frameOff = Theme.RoundedTex(BadgeLocked, 8);

            const float tile = 80f, pad = 6f, icon = 60f;
            float viewWidth = Mathf.Max(1f, _rect.width - 270f);
            int cols = Mathf.Max(3, Mathf.FloorToInt((viewWidth + pad) / (tile + pad)));
            float scrollHeight = Mathf.Clamp(_rect.height - 240f, 160f, 420f);
            _badgeScroll = GUILayout.BeginScrollView(_badgeScroll, GUILayout.Height(scrollHeight));
            for (int i = 0; i < total; i++)
            {
                if (i % cols == 0) GUILayout.BeginHorizontal();

                var t = all[i];
                bool on = unlocked[i];
                Rect r = GUILayoutUtility.GetRect(tile, tile, GUILayout.Width(tile), GUILayout.Height(tile));

                if (Event.current.type == EventType.Repaint)
                {
                    GUI.DrawTexture(r, on ? _frameOn : _frameOff);                 // colored frame
                    GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), _tileBg); // inner bg
                    var tex = SteamAch.Icon(t);
                    float ix = r.x + (tile - icon) * 0.5f, iy = r.y + (tile - icon) * 0.5f;
                    if (tex != null)
                        GUI.DrawTexture(new Rect(ix, iy, icon, icon), tex);
                    else
                        GUI.Label(new Rect(r.x, r.y, tile, tile), "…", Theme.LabelDim);
                }

                string tip = SteamAch.DisplayName(t);
                string d = SteamAch.Desc(t);
                if (!string.IsNullOrEmpty(d)) tip += "\n" + d;
                if (GUI.Button(r, new GUIContent("", tip), GUIStyle.none))
                    SteamAch.Toggle(t);

                GUILayout.Space(pad);
                if (i % cols == cols - 1 || i == total - 1) GUILayout.EndHorizontal();
                if (i % cols == cols - 1) GUILayout.Space(pad);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static void DrawCosmetics()
        {
            GUILayout.Label(L("tab.cosmetics"), Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            if (GUILayout.Button(Ru ? "Разблокировать всю косметику" : "Unlock all cosmetics", Theme.DonateBtn, GUILayout.Height(32)))
            {
                SteamAch.UnlockAllCosmetics();
            }
            GUILayout.Label(Ru ? "Косметика в PEAK открывается через достижения и стат максимальной высоты. "
                                 + "Кнопка выдаёт все ачивки и поднимает этот стат — после этого наряды доступны в настройке персонажа."
                               : "Cosmetics in PEAK unlock via achievements and the max height stat. This grants all achievements and "
                                 + "raises that stat, so outfits become available in character customization.", Theme.LabelDim);
            GUILayout.EndVertical();
        }

        private static void DrawAbout()
        {
            if (Event.current != null)
            {
                GUILayout.Label("PEAK-MX  v" + Plugin.Version, Theme.Section);
                GUILayout.Label(Ru ? "Автор: maxkir041" : "Author: maxkir041", Theme.Label);

                GUILayout.Space(6);
                GUILayout.Label(L("ui.language"), Theme.Section);
                DrawLanguagePicker();
                DrawMenuKeyBinding();
                DrawClientIdentity();
                DrawUpdateChecker();
                DrawThemeColorSettings();

                GUILayout.Space(6);
                GUILayout.Label(Ru ? "Окно" : "Window", Theme.Section);
                GUILayout.Label(
                    Ru
                        ? "Перетаскивай меню за шапку и тяни за любую грань или угол, как обычное окно."
                        : "Drag the menu by its header and resize it by any edge or corner, like a regular app window.",
                    Theme.LabelDim);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Стандартный размер" : "Default size", Theme.LinkBtn, GUILayout.Height(28)))
                    _rect = DefaultWindowRect();
                if (GUILayout.Button(Ru ? "По центру" : "Center", Theme.LinkBtn, GUILayout.Width(100), GUILayout.Height(28)))
                    CenterWindow();
                GUILayout.EndHorizontal();
                GUILayout.Label(
                    (Ru ? "Текущий размер: " : "Current size: ")
                    + Mathf.RoundToInt(_rect.width)
                    + " x "
                    + Mathf.RoundToInt(_rect.height),
                    Theme.LabelDim);

                GUILayout.Space(6);
                GUILayout.Label(Ru ? "Ссылки:" : "Links:", Theme.Section);
                GUILayout.BeginHorizontal();
                LinkButton("GitHub", UrlGitHub);
                LinkButton("Thunderstore", UrlThunderstore);
                LinkButton("Nexus", UrlNexus);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                LinkButton("Steam", UrlSteam);
                LinkButton("Telegram", UrlTelegram);
                LinkButton("Website", UrlWebsite);
                GUILayout.EndHorizontal();

                DrawFeedbackPanel();

                GUILayout.Space(8);
                GUILayout.Label(SupportText(), Theme.DonateText);
                if (GUILayout.Button(DonateTextLabel(), Theme.DonateBtn, GUILayout.Width(160)))
                    OpenUrl(UrlDonate);
                GUILayout.Space(6);
                DrawQr(150);
                GUILayout.Label(QrHintText(), Theme.LabelDim);
                DrawDonationSupportPanel(false);

                GUILayout.Space(6);
                bool showReminder = ModConfig.ShowDonateNotice.Value;
                bool updatedShowReminder = GUILayout.Toggle(showReminder, ReminderToggleText(), Theme.Label);
                if (updatedShowReminder != showReminder) ModConfig.ShowDonateNotice.Value = updatedShowReminder;

                DrawAnalyticsStatus();

                GUILayout.Space(6);
                GUILayout.Label(Ru
                    ? "Неофициальный фанатский мод. Не связан с разработчиками PEAK. Сторонние библиотеки остаются под своими лицензиями."
                    : "Unofficial fan-made mod. Not affiliated with PEAK's developers. Third-party libraries keep their own licenses.",
                    Theme.LabelDim);
                return;
            }

            GUILayout.Label("PEAK-MX  v" + Plugin.Version, Theme.Section);
            GUILayout.Label(Ru ? "Автор: maxkir041" : "Author: maxkir041", Theme.Label);

            GUILayout.Space(6);
            GUILayout.Label(L("ui.language"), Theme.Section);
            DrawLanguagePicker();
            DrawMenuKeyBinding();
            DrawClientIdentity();
            DrawUpdateChecker();
            DrawThemeColorSettings();

            GUILayout.Space(6);
            GUILayout.Label(Ru ? "Размер меню" : "Menu size", Theme.Section);
            GUILayout.BeginHorizontal();
            float sc = ModConfig.UiScale.Value;
            if (GUILayout.Button("−", Theme.LinkBtn, GUILayout.Width(40), GUILayout.Height(28)))
                ModConfig.UiScale.Value = Mathf.Clamp(sc - 0.1f, 0.6f, 2f);
            GUILayout.Label((sc * 100f).ToString("0") + "%", Theme.Label, GUILayout.Width(64));
            if (GUILayout.Button("+", Theme.LinkBtn, GUILayout.Width(40), GUILayout.Height(28)))
                ModConfig.UiScale.Value = Mathf.Clamp(sc + 0.1f, 0.6f, 2f);
            if (GUILayout.Button(Ru ? "Сброс" : "Reset", Theme.LinkBtn, GUILayout.Width(80), GUILayout.Height(28)))
                ModConfig.UiScale.Value = 1f;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label(Ru ? "Ссылки:" : "Links:", Theme.Section);
            GUILayout.BeginHorizontal();
            LinkButton("GitHub", UrlGitHub);
            LinkButton("Thunderstore", UrlThunderstore);
            LinkButton("Nexus", UrlNexus);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            LinkButton("Steam", UrlSteam);
            LinkButton("Telegram", UrlTelegram);
            LinkButton("Website", UrlWebsite);
            GUILayout.EndHorizontal();

            DrawFeedbackPanel();

            GUILayout.Space(8);
            GUILayout.Label(SupportText(), Theme.DonateText);
            if (GUILayout.Button(DonateTextLabel(), Theme.DonateBtn, GUILayout.Width(160)))
                OpenUrl(UrlDonate);
            GUILayout.Space(6);
            DrawQr(150);
            GUILayout.Label(QrHintText(), Theme.LabelDim);
            DrawDonationSupportPanel(false);

            GUILayout.Space(6);
            bool show = ModConfig.ShowDonateNotice.Value;
            bool newShow = GUILayout.Toggle(show, ReminderToggleText(), Theme.Label);
            if (newShow != show) ModConfig.ShowDonateNotice.Value = newShow;

            DrawAnalyticsStatus();

            GUILayout.Space(6);
            GUILayout.Label(Ru
                ? "Неофициальный фанатский мод. Не связан с разработчиками PEAK. Сторонние библиотеки — под своими лицензиями."
                : "Unofficial fan-made mod. Not affiliated with PEAK's developers. Third-party libraries keep their own licenses.",
                Theme.LabelDim);
        }

        private static bool _langOpen;
        private static void DrawLanguagePicker()
        {
            string cur = "";
            foreach (var (lang, name) in Localization.Options)
                if (lang == Localization.Current) cur = name;

            if (GUILayout.Button(cur + (_langOpen ? "   ▲" : "   ▼"), Theme.LinkBtn, GUILayout.Width(210), GUILayout.Height(28)))
                _langOpen = !_langOpen;

            if (!_langOpen)
                return;

            // Vertical dropdown-style list.
            GUILayout.BeginVertical(Theme.Card, GUILayout.Width(210));
            foreach (var (lang, name) in Localization.Options)
            {
                bool active = lang == Localization.Current;
                if (GUILayout.Button(name, active ? Theme.NavItemActive : Theme.NavItem, GUILayout.Width(190)))
                {
                    Localization.Current = lang;
                    ModConfig.Language.Value = (int)lang;
                    _langOpen = false;
                }
            }
            GUILayout.EndVertical();
        }

        private static void DrawMenuKeyBinding()
        {
            CaptureMenuKeyEvent();

            GUILayout.Space(6);
            GUILayout.Label(Ru ? "Клавиша меню" : "Menu key", Theme.Section);
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                (Ru ? "Текущая: " : "Current: ") + ModConfig.MenuToggleKey.Value,
                Theme.Label,
                GUILayout.Width(180));

            string button = _waitingForMenuKey
                ? (Ru ? "Нажми клавишу..." : "Press a key...")
                : (Ru ? "Изменить" : "Change");
            if (GUILayout.Button(button, Theme.LinkBtn, GUILayout.Height(28)))
                _waitingForMenuKey = true;

            if (GUILayout.Button(Ru ? "Сброс" : "Reset", Theme.LinkBtn, GUILayout.Width(80), GUILayout.Height(28)))
            {
                ModConfig.MenuToggleKey.Value = KeyCode.Insert;
                _waitingForMenuKey = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                _waitingForMenuKey
                    ? (Ru ? "Нажми новую клавишу. Esc отменяет." : "Press the new key. Esc cancels.")
                    : (Ru ? "Клавиша применяется сразу и сохраняется в конфиге." : "The key applies immediately and is saved to config."),
                Theme.LabelDim);
        }

        private static void DrawClientIdentity()
        {
            GUILayout.Space(6);
            GUILayout.Label(Ru ? "Личный ID" : "Personal ID", Theme.Section);
            GUILayout.BeginHorizontal();
            GUILayout.Label(ClientIdentity.StableId, Theme.LabelDim);
            if (GUILayout.Button(Ru ? "Скопировать" : "Copy", Theme.LinkBtn, GUILayout.Width(120), GUILayout.Height(26)))
            {
                GUIUtility.systemCopyBuffer = ClientIdentity.StableId;
                _clientIdCopiedUntil = Time.realtimeSinceStartup + 2f;
            }
            GUILayout.EndHorizontal();
            if (Time.realtimeSinceStartup < _clientIdCopiedUntil)
                GUILayout.Label(Ru ? "ID скопирован в буфер обмена." : "ID copied to clipboard.", Theme.LabelDim);

            string source = ClientIdentity.UsesSteam
                ? (Ru ? "Используется SteamID." : "Using SteamID.")
                : (Ru ? "Сохранен вне папки игры: " : "Saved outside the game folder: ") + ClientIdentity.StoragePath;
            GUILayout.Label(source, Theme.LabelDim);
        }

        private static void DrawUpdateChecker()
        {
            GUILayout.Space(6);
            GUILayout.Label(Ru ? "Обновления" : "Updates", Theme.Section);
            GUILayout.Label((Ru ? "Установлена версия: " : "Installed version: ") + Plugin.Version, Theme.LabelDim);

            if (UpdateChecker.IsChecking)
            {
                GUILayout.Label(Ru ? "Проверяю GitHub Releases..." : "Checking GitHub Releases...", Theme.LabelDim);
            }
            else if (!string.IsNullOrWhiteSpace(UpdateChecker.Error))
            {
                GUILayout.Label((Ru ? "Не удалось проверить: " : "Could not check: ") + UpdateChecker.Error, Theme.LabelDim);
            }
            else if (!UpdateChecker.HasChecked)
            {
                GUILayout.Label(Ru ? "Проверка еще не запускалась." : "No update check has run yet.", Theme.LabelDim);
            }
            else if (UpdateChecker.UpdateAvailable)
            {
                GUILayout.Label(
                    (Ru ? "Доступна новая версия: " : "New version available: ")
                    + (string.IsNullOrWhiteSpace(UpdateChecker.LatestVersion) ? UpdateChecker.LatestTag : UpdateChecker.LatestVersion),
                    Theme.Label);
                if (!string.IsNullOrWhiteSpace(UpdateChecker.AssetName))
                    GUILayout.Label((Ru ? "Файл релиза: " : "Release file: ") + UpdateChecker.AssetName, Theme.LabelDim);
            }
            else if (string.Equals(UpdateChecker.Status, "ahead", StringComparison.Ordinal))
            {
                GUILayout.Label(
                    Ru
                        ? "Установленная версия новее последнего релиза GitHub. Автообновление не требуется."
                        : "Installed version is newer than the latest GitHub release. Auto-update is not needed.",
                    Theme.LabelDim);
                if (!string.IsNullOrWhiteSpace(UpdateChecker.LatestVersion) || !string.IsNullOrWhiteSpace(UpdateChecker.LatestTag))
                {
                    GUILayout.Label(
                        (Ru ? "Последний релиз GitHub: " : "Latest GitHub release: ")
                        + (string.IsNullOrWhiteSpace(UpdateChecker.LatestVersion) ? UpdateChecker.LatestTag : UpdateChecker.LatestVersion),
                        Theme.LabelDim);
                }
            }
            else
            {
                GUILayout.Label(Ru ? "Установлена последняя версия." : "You are on the latest version.", Theme.LabelDim);
            }

            if (UpdateChecker.InstallQueued)
            {
                GUILayout.Label(
                    Ru
                        ? "Обновление скачано. Закрой игру, и DLL будет заменена автоматически."
                        : "Update downloaded. Close the game and the DLL will be replaced automatically.",
                    Theme.LabelDim);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Проверить" : "Check", Theme.LinkBtn, GUILayout.Width(100), GUILayout.Height(28)))
                UpdateChecker.CheckAsync(true);

            if (UpdateChecker.UpdateAvailable && UpdateChecker.CanAutoInstall)
            {
                string installText = UpdateChecker.IsInstalling
                    ? (Ru ? "Скачиваю..." : "Downloading...")
                    : UpdateChecker.InstallQueued
                        ? (Ru ? "Готово" : "Ready")
                        : (Ru ? "Установить после выхода" : "Install after exit");

                GUI.enabled = !UpdateChecker.IsInstalling && !UpdateChecker.InstallQueued;
                if (GUILayout.Button(installText, Theme.DonateBtn, GUILayout.Height(28)))
                    UpdateChecker.InstallAsync();
                GUI.enabled = true;
            }

            if (GUILayout.Button(Ru ? "Открыть релизы" : "Open releases", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(28)))
                OpenUrl(string.IsNullOrWhiteSpace(UpdateChecker.ReleaseUrl) ? UpdateChecker.ReleasesUrl : UpdateChecker.ReleaseUrl);
            GUILayout.EndHorizontal();
        }

        private static void DrawThemeColorSettings()
        {
            GUILayout.Space(6);
            GUILayout.Label(Ru ? "Акцентные цвета" : "Accent colors", Theme.Section);
            GUILayout.Label(
                Ru
                    ? "Меняет цвет выделения, полос, переключателей и основных кнопок. Сохраняется в конфиге."
                    : "Changes highlights, stripes, switches, and primary buttons. Saved in config.",
                Theme.LabelDim);

            GUILayout.BeginHorizontal();
            foreach (ThemeColorPreset preset in ThemeColorPresets)
            {
                string label = Ru ? preset.RuName : preset.EnName;
                if (GUILayout.Button(label, Theme.Chip, GUILayout.Height(26)))
                    ApplyThemePalette(preset.AccentHex, preset.ActionHex);
            }
            if (GUILayout.Button(Ru ? "Сброс" : "Reset", Theme.LinkBtn, GUILayout.Width(80), GUILayout.Height(26)))
                ApplyThemePalette(Theme.DefaultAccentHex, Theme.DefaultActionHex);
            GUILayout.EndHorizontal();

            DrawColorEditor(Ru ? "Акцент" : "Accent", ModConfig.AccentColorHex, Theme.DefaultAccentHex, ref _accentHexText);
            DrawColorEditor(Ru ? "Кнопки" : "Buttons", ModConfig.ActionColorHex, Theme.DefaultActionHex, ref _actionHexText);
        }

        private static void DrawFeedbackPanel()
        {
            ConsumePickedFeedbackImages();

            GUILayout.Space(8);
            GUILayout.Label(Ru ? "Предложить / сообщить о проблеме" : "Suggest / report a problem", Theme.Section);
            GUILayout.BeginVertical(Theme.Panel9);

            _feedbackKind = Mathf.Clamp(_feedbackKind, 0, FeedbackTypeKeys.Length - 1);
            _feedbackKind = GUILayout.SelectionGrid(
                _feedbackKind,
                Ru ? FeedbackTypesRu : FeedbackTypesEn,
                3,
                Theme.ListItem,
                GUILayout.Height(32));

            GUILayout.Label(Ru ? "Тема" : "Title", Theme.LabelDim);
            _feedbackTitle = ClipInput(GUILayout.TextField(_feedbackTitle ?? "", Theme.TextInput, GUILayout.Height(28)), 120);

            GUILayout.Label(Ru ? "Сообщение" : "Message", Theme.LabelDim);
            _feedbackMessage = ClipInput(GUILayout.TextArea(_feedbackMessage ?? "", Theme.TextArea, GUILayout.Height(92)), 3000);

            GUILayout.Label(Ru ? "Контакт для ответа, если хочешь" : "Contact for a reply, optional", Theme.LabelDim);
            _feedbackContact = ClipInput(GUILayout.TextField(_feedbackContact ?? "", Theme.TextInput, GUILayout.Height(28)), 160);
            DrawFeedbackAttachmentControls(_feedbackAttachments, ref _feedbackAttachScreenshot, false);

            if (!string.IsNullOrWhiteSpace(FeedbackClient.LastError))
                GUILayout.Label((Ru ? "Ошибка: " : "Error: ") + FeedbackClient.LastError, Theme.LabelDim);
            else if (!string.IsNullOrWhiteSpace(_feedbackScreenshotError))
                GUILayout.Label((Ru ? "Изображения: " : "Images: ") + _feedbackScreenshotError, Theme.LabelDim);
            else if (FeedbackClient.IsSending)
                GUILayout.Label(Ru ? "Отправляю..." : "Sending...", Theme.LabelDim);
            else if (FeedbackClient.IsClosing)
                GUILayout.Label(Ru ? "Закрываю обращение..." : "Closing ticket...", Theme.LabelDim);
            else if (string.Equals(FeedbackClient.LastCloseStatus, "closed", StringComparison.Ordinal))
                GUILayout.Label(Ru ? "Обращение закрыто." : "Ticket closed.", Theme.LabelDim);
            else if (string.Equals(FeedbackClient.LastStatus, "sent", StringComparison.Ordinal))
            {
                string code = string.IsNullOrWhiteSpace(FeedbackClient.LastTicketCode) ? "" : " #" + FeedbackClient.LastTicketCode;
                GUILayout.Label((Ru ? "Отправлено" : "Sent") + code, Theme.LabelDim);
            }

            GUILayout.BeginHorizontal();
            bool canSend = !FeedbackClient.IsSending && !string.IsNullOrWhiteSpace(_feedbackMessage);
            GUI.enabled = canSend;
            if (GUILayout.Button(FeedbackClient.IsSending ? (Ru ? "Отправляю..." : "Sending...") : (Ru ? "Отправить" : "Send"), Theme.DonateBtn, GUILayout.Height(30)))
            {
                var attachments = BuildFeedbackAttachments(_feedbackAttachments, _feedbackAttachScreenshot);
                if (attachments != null)
                    FeedbackClient.SubmitAsync(
                        FeedbackTypeKeys[Mathf.Clamp(_feedbackKind, 0, FeedbackTypeKeys.Length - 1)],
                        _feedbackTitle,
                        _feedbackMessage,
                        _feedbackContact,
                        attachments);
            }
            GUI.enabled = true;

            if (GUILayout.Button(
                FeedbackClient.IsCheckingReplies ? (Ru ? "Проверяю..." : "Checking...") : (Ru ? "Проверить ответы" : "Check replies"),
                Theme.LinkBtn,
                GUILayout.Width(Ru ? 150 : 135),
                GUILayout.Height(30)))
                FeedbackClient.CheckRepliesAsync();
            GUILayout.EndHorizontal();

            DrawFeedbackReplies();
            GUILayout.EndVertical();
        }

        private static void DrawFeedbackReplies()
        {
            var tickets = FeedbackClient.Tickets;
            int count = tickets != null ? tickets.Count : 0;
            if (count <= 0)
                return;

            GUILayout.Space(4);
            string foldText = _feedbackRepliesOpen
                ? (Ru ? "Ответы разработчика ▲" : "Developer replies ▲")
                : (Ru ? $"Ответы разработчика ({count}) ▼" : $"Developer replies ({count}) ▼");
            if (GUILayout.Button(foldText, _feedbackRepliesOpen ? Theme.FoldoutBtnOpen : Theme.FoldoutBtn, GUILayout.Height(30)))
                _feedbackRepliesOpen = !_feedbackRepliesOpen;
            if (!_feedbackRepliesOpen)
                return;

            int max = Mathf.Min(count, 5);
            for (int i = 0; i < max; i++)
            {
                FeedbackTicket ticket = tickets[i];
                GUILayout.BeginVertical(Theme.Panel9);
                GUILayout.Label(
                    "#" + ticket.Code
                    + " · "
                    + FeedbackTypeText(ticket.Type)
                    + " · "
                    + FeedbackStatusText(ticket.Status),
                    Theme.Label);
                if (!string.IsNullOrWhiteSpace(ticket.Title))
                    GUILayout.Label(ClipUi(ticket.Title, 140), Theme.LabelDim);
                if (ticket.CommentsCount > 0)
                    GUILayout.Label(
                        (Ru ? "Дополнений: " : "Updates: ")
                        + ticket.CommentsCount
                        + (string.IsNullOrWhiteSpace(ticket.LatestCommentAt) ? "" : " · " + ShortDonationDate(ticket.LatestCommentAt)),
                        Theme.LabelDim);
                if (!string.IsNullOrWhiteSpace(ticket.AdminReply))
                    GUILayout.Label((Ru ? "Ответ: " : "Reply: ") + ClipUi(ticket.AdminReply, 700), Theme.Label);
                else
                    GUILayout.Label(Ru ? "Ответа пока нет." : "No reply yet.", Theme.LabelDim);
                if (!string.IsNullOrWhiteSpace(ticket.UpdatedAt))
                    GUILayout.Label(ShortDonationDate(ticket.UpdatedAt), Theme.LabelDim);
                GUILayout.BeginHorizontal();
                bool open = FeedbackTicketIsOpen(ticket.Status);
                if (open && GUILayout.Button(Ru ? "Дополнить" : "Add update", Theme.LinkBtn, GUILayout.Height(26)))
                {
                    _feedbackSelectedTicket = ticket.Code;
                    _feedbackCommentMessage = "";
                    _feedbackCommentAttachScreenshot = false;
                    _feedbackCommentAttachments.Clear();
                }
                if (open)
                {
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = wasEnabled && !FeedbackClient.IsClosing;
                    if (GUILayout.Button(
                            FeedbackClient.IsClosing ? (Ru ? "Закрываю..." : "Closing...") : (Ru ? "Закрыть обращение" : "Close ticket"),
                            Theme.LinkBtn,
                            GUILayout.Width(Ru ? 150 : 120),
                            GUILayout.Height(26)))
                    {
                        if (string.Equals(_feedbackSelectedTicket, ticket.Code, StringComparison.OrdinalIgnoreCase))
                        {
                            _feedbackSelectedTicket = "";
                            _feedbackCommentMessage = "";
                            _feedbackCommentAttachScreenshot = false;
                            _feedbackCommentAttachments.Clear();
                        }
                        FeedbackClient.CloseTicketAsync(ticket.Code);
                    }
                    GUI.enabled = wasEnabled;
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            DrawFeedbackCommentBox();
        }

        private static void DrawFeedbackCommentBox()
        {
            if (string.IsNullOrWhiteSpace(_feedbackSelectedTicket))
                return;

            GUILayout.Space(4);
            GUILayout.BeginVertical(Theme.Panel9);
            GUILayout.Label((Ru ? "Дополнение к " : "Update for ") + "#" + _feedbackSelectedTicket, Theme.Label);
            _feedbackCommentMessage = ClipInput(
                GUILayout.TextArea(_feedbackCommentMessage ?? "", Theme.TextArea, GUILayout.Height(76)),
                3000);
            DrawFeedbackAttachmentControls(_feedbackCommentAttachments, ref _feedbackCommentAttachScreenshot, true);

            if (FeedbackClient.IsCommenting)
                GUILayout.Label(Ru ? "Отправляю дополнение..." : "Sending update...", Theme.LabelDim);
            else if (string.Equals(FeedbackClient.LastCommentStatus, "sent", StringComparison.Ordinal))
                GUILayout.Label(Ru ? "Дополнение отправлено." : "Update sent.", Theme.LabelDim);

            GUILayout.BeginHorizontal();
            bool canSend = !FeedbackClient.IsCommenting
                && (!string.IsNullOrWhiteSpace(_feedbackCommentMessage) || _feedbackCommentAttachScreenshot || _feedbackCommentAttachments.Count > 0);
            GUI.enabled = canSend;
            if (GUILayout.Button(Ru ? "Отправить дополнение" : "Send update", Theme.DonateBtn, GUILayout.Height(28)))
            {
                var attachments = BuildFeedbackAttachments(_feedbackCommentAttachments, _feedbackCommentAttachScreenshot);
                if (attachments != null)
                    FeedbackClient.AddCommentAsync(_feedbackSelectedTicket, _feedbackCommentMessage, attachments);
            }
            GUI.enabled = true;
            if (GUILayout.Button(Ru ? "Отмена" : "Cancel", Theme.LinkBtn, GUILayout.Width(90), GUILayout.Height(28)))
            {
                _feedbackSelectedTicket = "";
                _feedbackCommentMessage = "";
                _feedbackCommentAttachScreenshot = false;
                _feedbackCommentAttachments.Clear();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void DrawFeedbackAttachmentControls(List<FeedbackAttachment> attachments, ref bool attachCurrentScreenshot, bool compact)
        {
            GUILayout.Label(Ru ? "Изображения" : "Images", Theme.LabelDim);
            GUILayout.BeginHorizontal();
            attachCurrentScreenshot = DrawVisibleCheck(
                attachCurrentScreenshot,
                Ru ? "Текущий скрин" : "Current screenshot",
                GUILayout.Width(Ru ? 150 : 165));
            bool picking = FeedbackClient.IsPickingFiles;
            if (picking)
            {
                GUILayout.Label(Ru ? "Окно выбора открыто..." : "File picker is open...", Theme.LabelDim, GUILayout.Height(28));
                if (GUILayout.Button(Ru ? "Отменить выбор" : "Cancel picker", Theme.LinkBtn, GUILayout.Width(Ru ? 135 : 120), GUILayout.Height(28)))
                    CancelFeedbackImagePicker();
            }
            else if (GUILayout.Button(Ru ? "Выбрать файлы..." : "Choose files...", Theme.LinkBtn, GUILayout.Height(28)))
            {
                PickFeedbackImages(attachments);
            }
            if (attachments.Count > 0 && GUILayout.Button(Ru ? "Очистить" : "Clear", Theme.LinkBtn, GUILayout.Width(90), GUILayout.Height(28)))
                attachments.Clear();
            GUILayout.EndHorizontal();

            if (attachments.Count > 0 || attachCurrentScreenshot)
            {
                int count = attachments.Count + (attachCurrentScreenshot ? 1 : 0);
                GUILayout.Label(
                    (Ru ? "Будет отправлено изображений: " : "Images to send: ")
                    + count
                    + (attachments.Count > 0 ? " · " + AttachmentNames(attachments, compact ? 120 : 220) : ""),
                    Theme.LabelDim);
            }
        }

        private static void PickFeedbackImages(List<FeedbackAttachment> attachments)
        {
            if (FeedbackClient.IsPickingFiles)
                return;

            _feedbackPickForComment = ReferenceEquals(attachments, _feedbackCommentAttachments);
            _feedbackScreenshotError = Ru
                ? "Открыл окно выбора файлов. Если его не видно, проверь Alt+Tab или панель задач."
                : "Opened the file picker. If it is not visible, check Alt+Tab or the taskbar.";
            PrepareFeedbackFilePickerWindow();
            FeedbackClient.PickImageFilesAsync();
        }

        private static void CancelFeedbackImagePicker()
        {
            FeedbackClient.CancelPickImageFiles();
            RestoreFeedbackFilePickerWindow();
            _feedbackScreenshotError = Ru ? "Выбор файлов отменен." : "File picking cancelled.";
        }

        private static void ConsumePickedFeedbackImages()
        {
            var picked = FeedbackClient.TakePickedImageFiles(out string error);
            if (picked == null)
                return;

            RestoreFeedbackFilePickerWindow();
            _feedbackScreenshotError = "";
            if (!string.IsNullOrWhiteSpace(error))
                _feedbackScreenshotError = FeedbackAttachmentError(error);
            if (picked == null || picked.Count <= 0)
                return;

            List<FeedbackAttachment> attachments = _feedbackPickForComment
                ? _feedbackCommentAttachments
                : _feedbackAttachments;
            for (int i = 0; i < picked.Count && attachments.Count < 6; i++)
                attachments.Add(picked[i]);
            if (attachments.Count >= 6 && picked.Count > 0)
                _feedbackScreenshotError = string.IsNullOrWhiteSpace(_feedbackScreenshotError)
                    ? (Ru ? "Можно приложить до 6 изображений." : "You can attach up to 6 images.")
                    : _feedbackScreenshotError;
        }

        private static string FeedbackAttachmentError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return "";
            return error.Replace(
                "max_6_images",
                Ru ? "можно приложить до 6 изображений" : "you can attach up to 6 images");
        }

        private static void PrepareFeedbackFilePickerWindow()
        {
            try
            {
                if (_feedbackPickerRestoreFullscreen || !Screen.fullScreen)
                    return;

                _feedbackPickerRestoreFullscreen = true;
                _feedbackPickerFullscreenMode = Screen.fullScreenMode;
                _feedbackPickerWidth = Screen.width;
                _feedbackPickerHeight = Screen.height;
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.fullScreen = false;
            }
            catch
            {
                _feedbackPickerRestoreFullscreen = false;
            }
        }

        private static void RestoreFeedbackFilePickerWindow()
        {
            if (!_feedbackPickerRestoreFullscreen)
                return;

            try
            {
                Screen.SetResolution(
                    Mathf.Max(640, _feedbackPickerWidth),
                    Mathf.Max(480, _feedbackPickerHeight),
                    _feedbackPickerFullscreenMode);
            }
            catch
            {
            }
            finally
            {
                _feedbackPickerRestoreFullscreen = false;
            }
        }

        private static List<FeedbackAttachment> BuildFeedbackAttachments(List<FeedbackAttachment> selected, bool includeCurrentScreenshot)
        {
            _feedbackScreenshotError = "";
            var result = new List<FeedbackAttachment>();
            if (selected != null)
                result.AddRange(selected);

            if (includeCurrentScreenshot)
            {
                byte[] screenshot = CaptureFeedbackScreenshot(true);
                if (screenshot == null)
                    return null;
                result.Insert(0, new FeedbackAttachment
                {
                    Name = "peak-mx-screenshot.jpg",
                    ContentType = "image/jpeg",
                    Data = screenshot,
                });
            }

            return result;
        }

        private static string AttachmentNames(List<FeedbackAttachment> attachments, int max)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < attachments.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(attachments[i].Name);
                if (sb.Length > max)
                    return sb.ToString(0, Math.Max(1, max - 3)) + "...";
            }
            return sb.ToString();
        }

        private static byte[] CaptureFeedbackScreenshot(bool enabled)
        {
            _feedbackScreenshotError = "";
            if (!enabled)
                return null;

            byte[] screenshot = FeedbackClient.CaptureScreenshotJpeg(out string error);
            if (screenshot == null)
                _feedbackScreenshotError = string.IsNullOrWhiteSpace(error)
                    ? (Ru ? "не удалось сделать скриншот" : "could not capture screenshot")
                    : error;
            return screenshot;
        }

        private static bool DrawVisibleCheck(bool value, string label, params GUILayoutOption[] options)
        {
            string text = (value ? "[x] " : "[ ] ") + label;
            GUIStyle style = value ? Theme.SuccessBtn : Theme.LinkBtn;
            var allOptions = new List<GUILayoutOption>(options ?? Array.Empty<GUILayoutOption>());
            allOptions.Add(GUILayout.Height(28));
            if (GUILayout.Button(text, style, allOptions.ToArray()))
                value = !value;
            return value;
        }

        private static string FeedbackTypeText(string type)
        {
            switch ((type ?? "").ToLowerInvariant())
            {
                case "bug": return Ru ? "Проблема" : "Problem";
                case "other": return Ru ? "Другое" : "Other";
                default: return Ru ? "Предложение" : "Suggestion";
            }
        }

        private static string FeedbackStatusText(string status)
        {
            switch ((status ?? "").ToLowerInvariant())
            {
                case "answered": return Ru ? "есть ответ" : "answered";
                case "dev": return Ru ? "в разработке" : "in development";
                case "rejected": return Ru ? "отклонено" : "rejected";
                case "closed": return Ru ? "закрыто" : "closed";
                default: return Ru ? "новое" : "new";
            }
        }

        private static bool FeedbackTicketIsOpen(string status)
        {
            string value = (status ?? "").ToLowerInvariant();
            return value != "closed" && value != "rejected";
        }

        private static string ClipInput(string value, int max)
        {
            if (value == null)
                return "";
            return value.Length <= max ? value : value.Substring(0, max);
        }

        private static string ClipUi(string value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            string text = value.Trim();
            return text.Length <= max ? text : text.Substring(0, Math.Max(1, max - 1)) + "...";
        }

        private static void DrawColorEditor(string label, ConfigEntry<string> entry, string fallbackHex, ref string hexText)
        {
            string currentHex = Theme.NormalizeHex(entry?.Value, fallbackHex);
            Color current = Theme.ColorFromHex(currentHex, Theme.ColorFromHex(fallbackHex, Theme.Accent));
            if (string.IsNullOrEmpty(hexText))
                hexText = currentHex;

            GUILayout.BeginVertical(Theme.Panel9);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, Theme.Label, GUILayout.Width(90));
            DrawColorSwatch(current, 34f, 22f);
            string nextText = GUILayout.TextField(hexText, Theme.LinkBtn, GUILayout.Width(92), GUILayout.Height(28));
            hexText = nextText.Length > 7 ? nextText.Substring(0, 7) : nextText;
            string typedHex = Theme.NormalizeHex(hexText, null);
            bool changed = false;
            if (!string.IsNullOrEmpty(typedHex) && !string.Equals(typedHex, currentHex, StringComparison.OrdinalIgnoreCase))
            {
                current = Theme.ColorFromHex(typedHex, current);
                changed = true;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Ru ? "Сброс" : "Reset", Theme.LinkBtn, GUILayout.Width(80), GUILayout.Height(28)))
            {
                current = Theme.ColorFromHex(fallbackHex, current);
                changed = true;
            }
            GUILayout.EndHorizontal();

            float r = Mathf.Round(Slider(label + " R", current.r * 255f, 0f, 255f));
            float g = Mathf.Round(Slider(label + " G", current.g * 255f, 0f, 255f));
            float b = Mathf.Round(Slider(label + " B", current.b * 255f, 0f, 255f));
            Color sliderColor = new Color(r / 255f, g / 255f, b / 255f, 1f);
            if (Theme.ColorToHex(sliderColor) != Theme.ColorToHex(current))
            {
                current = sliderColor;
                changed = true;
            }

            if (changed)
                ApplyThemeColor(entry, current, ref hexText);

            GUILayout.EndVertical();
        }

        private static void DrawColorSwatch(Color color, float width, float height)
        {
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void ApplyThemePalette(string accentHex, string actionHex)
        {
            if (ModConfig.AccentColorHex != null)
                ModConfig.AccentColorHex.Value = Theme.NormalizeHex(accentHex, Theme.DefaultAccentHex);
            if (ModConfig.ActionColorHex != null)
                ModConfig.ActionColorHex.Value = Theme.NormalizeHex(actionHex, Theme.DefaultActionHex);
            _accentHexText = ModConfig.AccentColorHex?.Value;
            _actionHexText = ModConfig.ActionColorHex?.Value;
            Theme.EnsureBuilt();
            SyncThemeTextureCache(true);
        }

        private static void ApplyThemeColor(ConfigEntry<string> entry, Color color, ref string hexText)
        {
            string hex = Theme.ColorToHex(color);
            if (entry != null && !string.Equals(entry.Value, hex, StringComparison.OrdinalIgnoreCase))
                entry.Value = hex;
            hexText = hex;
            Theme.EnsureBuilt();
            SyncThemeTextureCache(true);
        }

        private static void CaptureMenuKeyEvent()
        {
            Event e = Event.current;
            if (!_waitingForMenuKey || e == null || e.type != EventType.KeyDown)
                return;

            if (e.keyCode == KeyCode.Escape)
            {
                _waitingForMenuKey = false;
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.None)
                return;

            ModConfig.MenuToggleKey.Value = e.keyCode;
            _waitingForMenuKey = false;
            GUI.FocusControl(null);
            e.Use();
        }

        private static void DrawAnalyticsStatus()
        {
#if THUNDERSTORE_NO_ANALYTICS
            return;
#else
            ModConfig.AllowAnonymousStats.Value = true;
            string label = InstallStatsToggleText().Trim();
            string text = Localization.Current switch
            {
                Lang.Russian => label + ": включена",
                Lang.Ukrainian => label + ": увімкнена",
                Lang.Spanish => label + ": activada",
                Lang.PortugueseBR => label + ": ativa",
                Lang.German => label + ": aktiv",
                Lang.French => label + ": active",
                Lang.Italian => label + ": attiva",
                Lang.Polish => label + ": wlaczona",
                Lang.Turkish => label + ": acik",
                Lang.ChineseSimplified => label + ": 已启用",
                Lang.ChineseTraditional => label + ": 已啟用",
                Lang.Japanese => label + ": 有効",
                Lang.Korean => label + ": 켜짐",
                _ => label + ": enabled",
            };
            GUILayout.Label(text, Theme.LabelDim);
            if (Stats.InstallCount.HasValue)
                GUILayout.Label(InstallsLabel() + Stats.InstallCount.Value, Theme.LabelDim);
#endif
        }

        private static string SupportText()
        {
            switch (Localization.Current)
            {
                case Lang.Russian:
                    return "Я делаю PEAK-MX почти в одиночку, ночами чиню баги и всё равно держу мод бесплатным. Если он тебе помогает, поддержка правда спасает мотивацию и даёт силы выпускать новые обновления.";
                case Lang.Ukrainian:
                    return "Я роблю PEAK-MX майже сам, ночами виправляю баги й усе одно залишаю мод безкоштовним. Якщо він тобі допомагає, підтримка справді рятує мотивацію та дає сили на нові оновлення.";
                case Lang.Spanish:
                    return "Hago PEAK-MX casi solo, arreglo bugs de noche y aun asi lo mantengo gratis. Si el mod te ayuda, tu apoyo de verdad me salva la motivacion y me da fuerzas para seguir actualizando.";
                case Lang.PortugueseBR:
                    return "Eu faco o PEAK-MX quase sozinho, corrijo bugs de madrugada e ainda mantenho o mod gratuito. Se ele te ajuda, o apoio realmente salva a motivacao e me da forca para continuar atualizando.";
                case Lang.German:
                    return "Ich baue PEAK-MX fast allein, fixe nachts Bugs und halte den Mod trotzdem kostenlos. Wenn er dir hilft, rettet deine Unterstuetzung wirklich meine Motivation und macht weitere Updates moeglich.";
                case Lang.French:
                    return "Je fais PEAK-MX presque seul, je corrige les bugs la nuit et je garde quand meme le mod gratuit. Si le mod t'aide, ton soutien sauve vraiment ma motivation et m'aide a continuer les mises a jour.";
                case Lang.Italian:
                    return "Sviluppo PEAK-MX quasi da solo, sistemo bug di notte e lo tengo comunque gratuito. Se il mod ti aiuta, il supporto salva davvero la motivazione e mi da forza per continuare gli aggiornamenti.";
                case Lang.Polish:
                    return "Tworze PEAK-MX prawie sam, naprawiam bugi nocami i nadal udostepniam mod za darmo. Jesli ci pomaga, wsparcie naprawde ratuje motywacje i daje sile na kolejne aktualizacje.";
                case Lang.Turkish:
                    return "PEAK-MX'i neredeyse tek basima yapiyorum, geceleri hatalari duzeltiyorum ve modu yine de ucretsiz tutuyorum. Mod isine yariyorsa destek gercekten motivasyonumu kurtarir ve yeni guncellemeler icin guc verir.";
                case Lang.ChineseSimplified:
                    return "PEAK-MX 基本都是我一个人维护，很多 bug 都是在深夜修的，但我还是让它免费。如果这个模组帮到了你，你的支持真的能救回我的动力，让我继续更新。";
                case Lang.ChineseTraditional:
                    return "PEAK-MX 基本都是我一個人維護，很多 bug 都是在深夜修的，但我還是讓它免費。如果這個模組幫到了你，你的支持真的能救回我的動力，讓我繼續更新。";
                case Lang.Japanese:
                    return "PEAK-MX はほぼ一人で作っていて、夜中にバグを直しながら、それでも無料で公開しています。この MOD が役に立ったなら、支援は本当に励みになり、次の更新を続ける力になります。";
                case Lang.Korean:
                    return "PEAK-MX는 거의 혼자 만들고, 밤마다 버그를 고치면서도 무료로 유지하고 있습니다. 이 모드가 도움이 됐다면 후원은 정말 큰 힘이 되고 다음 업데이트를 계속할 동기가 됩니다.";
                default:
                    return "I build PEAK-MX mostly alone, fix bugs late at night, and still keep the mod free. If it helps you, support genuinely rescues my motivation and gives me the strength to keep updates coming.";
            }
        }

        private static string DonateTextLabel()
        {
            switch (Localization.Current)
            {
                case Lang.Russian: return "Поддержать";
                case Lang.Ukrainian: return "Підтримати";
                case Lang.Spanish: return "Apoyar";
                case Lang.PortugueseBR: return "Apoiar";
                case Lang.German: return "Unterstuetzen";
                case Lang.French: return "Soutenir";
                case Lang.Italian: return "Supporta";
                case Lang.Polish: return "Wesprzyj";
                case Lang.Turkish: return "Destekle";
                case Lang.ChineseSimplified:
                case Lang.ChineseTraditional: return "支持";
                case Lang.Japanese: return "支援する";
                case Lang.Korean: return "후원";
                default: return "Support";
            }
        }

        private static string CloseTextLabel()
        {
            switch (Localization.Current)
            {
                case Lang.Russian: return "Закрыть";
                case Lang.Ukrainian: return "Закрити";
                case Lang.Spanish: return "Cerrar";
                case Lang.PortugueseBR: return "Fechar";
                case Lang.German: return "Schliessen";
                case Lang.French: return "Fermer";
                case Lang.Italian: return "Chiudi";
                case Lang.Polish: return "Zamknij";
                case Lang.Turkish: return "Kapat";
                case Lang.ChineseSimplified:
                case Lang.ChineseTraditional: return "关闭";
                case Lang.Japanese: return "閉じる";
                case Lang.Korean: return "닫기";
                default: return "Close";
            }
        }

        private static string ReminderToggleText()
        {
            switch (Localization.Current)
            {
                case Lang.Russian: return " Показывать напоминание при запуске";
                case Lang.Ukrainian: return " Показувати нагадування при запуску";
                case Lang.Spanish: return " Mostrar recordatorio al iniciar";
                case Lang.PortugueseBR: return " Mostrar lembrete ao iniciar";
                case Lang.German: return " Erinnerung beim Start zeigen";
                case Lang.French: return " Afficher le rappel au lancement";
                case Lang.Italian: return " Mostra promemoria all'avvio";
                case Lang.Polish: return " Pokazuj przypomnienie przy starcie";
                case Lang.Turkish: return " Baslangicta hatirlatma goster";
                case Lang.ChineseSimplified: return " 启动时显示提醒";
                case Lang.ChineseTraditional: return " 啟動時顯示提醒";
                case Lang.Japanese: return " 起動時にリマインダーを表示";
                case Lang.Korean: return " 시작 시 알림 표시";
                default: return " Show reminder on launch";
            }
        }

        private static string InstallStatsToggleText()
        {
#if THUNDERSTORE_NO_ANALYTICS
            return "";
#else
            switch (Localization.Current)
            {
                case Lang.Russian: return " Анонимная аналитика";
                case Lang.Ukrainian: return " Анонімна аналітика";
                case Lang.Spanish: return " Analitica anonima";
                case Lang.PortugueseBR: return " Analise anonima";
                case Lang.German: return " Anonyme Analyse";
                case Lang.French: return " Analyse anonyme";
                case Lang.Italian: return " Analisi anonima";
                case Lang.Polish: return " Anonimowa analityka";
                case Lang.Turkish: return " Anonim analiz";
                case Lang.ChineseSimplified: return " 匿名分析";
                case Lang.ChineseTraditional: return " 匿名分析";
                case Lang.Japanese: return " 匿名分析";
                case Lang.Korean: return " 익명 분석";
                default: return " Anonymous analytics";
            }
#endif
        }

        private static string InstallsLabel()
        {
            switch (Localization.Current)
            {
                case Lang.Russian: return "Установок: ";
                case Lang.Ukrainian: return "Встановлень: ";
                case Lang.Spanish: return "Instalaciones: ";
                case Lang.PortugueseBR: return "Instalacoes: ";
                case Lang.German: return "Installationen: ";
                case Lang.French: return "Installations: ";
                case Lang.Italian: return "Installazioni: ";
                case Lang.Polish: return "Instalacje: ";
                case Lang.Turkish: return "Kurulumlar: ";
                case Lang.ChineseSimplified: return "安装数: ";
                case Lang.ChineseTraditional: return "安裝數: ";
                case Lang.Japanese: return "インストール数: ";
                case Lang.Korean: return "설치 수: ";
                default: return "Installs: ";
            }
        }

        private static string QrHintText()
        {
            switch (Localization.Current)
            {
                case Lang.Russian: return "Сканируй QR для поддержки";
                case Lang.Ukrainian: return "Скануй QR для підтримки";
                case Lang.Spanish: return "Escanea el QR para apoyar";
                case Lang.PortugueseBR: return "Escaneie o QR para apoiar";
                case Lang.German: return "QR scannen zum Unterstuetzen";
                case Lang.French: return "Scanne le QR pour soutenir";
                case Lang.Italian: return "Scansiona il QR per supportare";
                case Lang.Polish: return "Zeskanuj QR, aby wesprzec";
                case Lang.Turkish: return "Destek icin QR'yi tara";
                case Lang.ChineseSimplified: return "扫描二维码支持";
                case Lang.ChineseTraditional: return "掃描 QR 碼支持";
                case Lang.Japanese: return "QR を読み取って支援";
                case Lang.Korean: return "QR을 스캔해 후원";
                default: return "Scan the QR to support";
            }
        }

        private static string CosmeticCategoryLabel(GameApi.CosmeticCategory category)
        {
            return category switch
            {
                GameApi.CosmeticCategory.Skin => Ru ? "Кожа" : "Skin",
                GameApi.CosmeticCategory.Accessory => Ru ? "Аксессуар" : "Accessory",
                GameApi.CosmeticCategory.Eyes => Ru ? "Глаза" : "Eyes",
                GameApi.CosmeticCategory.Mouth => Ru ? "Рот" : "Mouth",
                GameApi.CosmeticCategory.Outfit => Ru ? "Одежда" : "Outfit",
                GameApi.CosmeticCategory.Hat => Ru ? "Шляпа" : "Hat",
                GameApi.CosmeticCategory.Sash => Ru ? "Лента" : "Sash",
                GameApi.CosmeticCategory.Medal => Ru ? "Медаль" : "Medal",
                _ => category.ToString(),
            };
        }

        private static string CosmeticDisplayName(CustomizationOption option)
        {
            if (option == null) return "-";
            try
            {
                if (option.isBlank) return Ru ? "Пусто" : "Empty";
            }
            catch { }

            string raw = "";
            try { raw = option.name ?? ""; } catch { }
            if (string.IsNullOrWhiteSpace(raw)) raw = "Cosmetic";
            string human = HumanizeName(raw);
            return Ru ? TranslateCosmeticRu(human) : human;
        }

        private static string HumanizeName(string raw)
        {
            string spaced = "";
            char prev = '\0';
            for (int i = 0; i < raw.Length; i++)
            {
                char ch = raw[i];
                if (ch == '_' || ch == '-')
                {
                    spaced += ' ';
                }
                else
                {
                    if (i > 0 && char.IsUpper(ch) && (char.IsLower(prev) || char.IsDigit(prev)))
                        spaced += ' ';
                    spaced += ch;
                }
                prev = ch;
            }

            spaced = WebUtility.HtmlDecode(spaced).Trim();
            while (spaced.Contains("  ")) spaced = spaced.Replace("  ", " ");
            return string.IsNullOrWhiteSpace(spaced) ? raw : spaced;
        }

        private static string TranslateCosmeticRu(string human)
        {
            string[] words = human.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["eye"] = "глаз", ["eyes"] = "глаза", ["mouth"] = "рот", ["skin"] = "кожа",
                ["hat"] = "шляпа", ["cap"] = "кепка", ["crown"] = "корона", ["goat"] = "козёл",
                ["sash"] = "лента", ["fit"] = "наряд", ["outfit"] = "одежда", ["accessory"] = "аксессуар",
                ["red"] = "красный", ["blue"] = "синий", ["green"] = "зелёный", ["yellow"] = "жёлтый",
                ["black"] = "чёрный", ["white"] = "белый", ["gold"] = "золотой", ["silver"] = "серебряный",
                ["pink"] = "розовый", ["purple"] = "фиолетовый", ["orange"] = "оранжевый", ["brown"] = "коричневый",
                ["small"] = "маленький", ["big"] = "большой", ["large"] = "большой", ["round"] = "круглый",
                ["sad"] = "грустный", ["happy"] = "счастливый", ["angry"] = "злой", ["sleepy"] = "сонный",
                ["normal"] = "обычный", ["default"] = "обычный", ["empty"] = "пусто",
                ["glasses"] = "очки", ["mask"] = "маска", ["bandana"] = "бандана", ["flower"] = "цветок",
            };

            for (int i = 0; i < words.Length; i++)
                if (map.TryGetValue(words[i], out string translated))
                    words[i] = translated;

            if (words.Length == 0) return human;
            string result = string.Join(" ", words);
            return char.ToUpperInvariant(result[0]) + (result.Length > 1 ? result.Substring(1) : "");
        }

        private static void DrawCosmeticsModern2()
        {
            GUILayout.Label(L("tab.cosmetics"), Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            GUILayout.Label(Ru
                ? "Здесь можно выбирать именно косметику по категориям, а не только выдавать связанные достижения."
                : "Here you can choose specific cosmetics by category, not just grant the linked achievements.",
                Theme.LabelDim);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Разблокировать всю косметику" : "Unlock all cosmetics", Theme.DonateBtn, GUILayout.Height(30)))
                SteamAch.UnlockAllCosmetics();
            if (GUILayout.Button(Ru ? "Сбросить стат высоты" : "Reset height stat", Theme.LinkBtn, GUILayout.Height(30)))
                SteamAch.SetMaxAscent(0);
            GUILayout.EndHorizontal();

            var categories = (GameApi.CosmeticCategory[])Enum.GetValues(typeof(GameApi.CosmeticCategory));
            GUILayout.BeginHorizontal();
            for (int i = 0; i < categories.Length; i++)
            {
                if (GUILayout.Button(CosmeticCategoryLabel(categories[i]), _cosmeticCategory == i ? Theme.ChipActive : Theme.Chip, GUILayout.Height(26)))
                    _cosmeticCategory = i;
            }
            GUILayout.EndHorizontal();

            var category = categories[Mathf.Clamp(_cosmeticCategory, 0, categories.Length - 1)];
            var options = GameApi.GetCosmeticOptions(category);
            int current = GameApi.GetCurrentCosmeticIndex(category);
            int blank = GameApi.FindBlankCosmeticIndex(category);

            GUILayout.BeginHorizontal();
            GUILayout.Label((Ru ? "Активно: " : "Active: ") + (current >= 0 && current < options.Length && options[current] != null ? options[current].name : "-"), Theme.LabelDim);
            GUILayout.FlexibleSpace();
            if (blank >= 0 && GUILayout.Button(Ru ? "Выключить" : "Clear", Theme.LinkBtn, GUILayout.Height(26)))
                GameApi.SetCosmetic(category, blank);
            GUILayout.EndHorizontal();

            _cosmeticScroll = GUILayout.BeginScrollView(_cosmeticScroll, GUILayout.Height(Mathf.Clamp(_rect.height - 260f, 180f, 360f)));
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (option == null) continue;
                bool active = i == current;
                string label = CosmeticDisplayName(option);
                try
                {
                    if (!option.isBlank && option.IsLocked) label += Ru ? " [закрыто]" : " [locked]";
                }
                catch { }

                if (GUILayout.Button(label, active ? Theme.ListItemActive : Theme.ListItem, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                    GameApi.SetCosmetic(category, i);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static void DrawCosmeticsModern3()
        {
            GUILayout.Label(L("tab.cosmetics"), Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            GUILayout.Label(Ru
                ? "Выбирай конкретную категорию и конкретный предмет косметики. Элементы можно открывать и сбрасывать отдельно."
                : "Choose a category and a specific cosmetic item. Options can be unlocked and reset one by one.",
                Theme.LabelDim);
            GUILayout.Label(Ru
                ? "Важно: закрытие некоторых предметов сбрасывает связанное достижение, стат или максимальную высоту, поэтому может повлиять на несколько косметик сразу."
                : "Important: locking some cosmetics resets a linked achievement, stat, or max height value, so it can affect multiple cosmetics at once.",
                Theme.LabelDim);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Разблокировать всё" : "Unlock all", Theme.DonateBtn, GUILayout.Height(30)))
                GameApi.UnlockAllCosmeticOptions();
            TipLast(Ru ? "Локально открывает все элементы косметики и дополнительно выставляет связанные Steam-условия." : "Locally unlocks every cosmetic option and also applies related Steam requirements.");
            if (GUILayout.Button(Ru ? "Сбросить стат высоты" : "Reset height stat", Theme.LinkBtn, GUILayout.Height(30)))
                SteamAch.SetMaxAscent(0);
            TipLast(Ru ? "Сбрасывает стат максимальной высоты до 0. Может закрыть косметику, завязанную на высоту." : "Resets the max height stat to 0. Can lock height-gated cosmetics.");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Случайная одежда и цвет" : "Random outfit + color", Theme.LinkBtn, GUILayout.Height(28)))
                GameApi.RandomizeOutfitAndColor();
            TipLast(Ru ? "Случайно выбирает цвет кожи и одежду из доступных вариантов." : "Randomly picks skin color and outfit from available options.");
            ModConfig.RapidRandomOutfitColor = ToggleCompact(Ru ? "Быстро менять" : "Rapid shuffle", ModConfig.RapidRandomOutfitColor);
            TipLast(Ru ? "Быстро крутит одежду и цвет, пока включено." : "Rapidly shuffles outfit and color while enabled.");
            GUILayout.EndHorizontal();
            if (ModConfig.RapidRandomOutfitColor)
                ModConfig.RapidRandomOutfitColorInterval = Slider(Ru ? "Пауза смены" : "Shuffle delay", ModConfig.RapidRandomOutfitColorInterval, 0.08f, 1f);

            var categories = (GameApi.CosmeticCategory[])Enum.GetValues(typeof(GameApi.CosmeticCategory));
            GUILayout.BeginHorizontal();
            for (int i = 0; i < categories.Length; i++)
                if (GUILayout.Button(CosmeticCategoryLabel(categories[i]), _cosmeticCategory == i ? Theme.ChipActive : Theme.Chip, GUILayout.Height(26)))
                    _cosmeticCategory = i;
            GUILayout.EndHorizontal();

            var category = categories[Mathf.Clamp(_cosmeticCategory, 0, categories.Length - 1)];
            var options = GameApi.GetCosmeticOptions(category);
            int current = GameApi.GetCurrentCosmeticIndex(category);
            int blank = GameApi.FindBlankCosmeticIndex(category);
            int visibleCount = 0;
            int hiddenBaseCount = 0;
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (option == null) continue;
                if (GameApi.IsCosmeticOptionGrantable(category, option)) visibleCount++;
                else hiddenBaseCount++;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label((Ru ? "Активно: " : "Active: ") + (current >= 0 && current < options.Length && options[current] != null ? CosmeticDisplayName(options[current]) : "-"), Theme.LabelDim);
            GUILayout.FlexibleSpace();
            if (blank >= 0 && GUILayout.Button(Ru ? "Выключить" : "Clear", Theme.LinkBtn, GUILayout.Height(26)))
                GameApi.SetCosmetic(category, blank);
            TipLast(Ru ? "Поставить пустой вариант в текущей категории косметики." : "Select the blank option for the current cosmetic category.");
            GUILayout.EndHorizontal();
            if (hiddenBaseCount > 0)
                GUILayout.Label((Ru ? "Базовые неудаляемые элементы скрыты: " : "Base non-removable items hidden: ") + hiddenBaseCount, Theme.LabelDim);
            if (visibleCount == 0)
                GUILayout.Label(Ru ? "В этой категории нет выдаваемых элементов." : "This category has no grantable items.", Theme.LabelDim);

            _cosmeticScroll = GUILayout.BeginScrollView(_cosmeticScroll, GUILayout.Height(Mathf.Clamp(_rect.height - 260f, 180f, 360f)));
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (option == null) continue;
                if (!GameApi.IsCosmeticOptionGrantable(category, option)) continue;
                bool active = i == current;
                bool locked = false;
                string label = CosmeticDisplayName(option);
                try
                {
                    locked = option.IsLocked;
                }
                catch { }

                GUILayout.BeginHorizontal();
                if (DrawCosmeticListRow(category, option, label + (locked ? (Ru ? " [закрыто]" : " [locked]") : ""), active, locked))
                    GameApi.SetCosmetic(category, i);
                TipLast(locked
                    ? (Ru ? "Элемент закрыт. Нажми «Открыть», чтобы разблокировать его отдельно." : "This option is locked. Press Unlock to unlock it separately.")
                    : (Ru ? "Выбрать этот элемент косметики." : "Select this cosmetic."));
                if (GUILayout.Button(Ru ? "Выбрать" : "Select", Theme.LinkBtn, GUILayout.Width(82), GUILayout.Height(30)))
                    GameApi.SetCosmetic(category, i);
                TipLast(Ru ? "Надеть этот элемент косметики." : "Equip this cosmetic option.");
                if (locked && GUILayout.Button(Ru ? "Открыть" : "Unlock", Theme.LinkBtn, GUILayout.Width(82), GUILayout.Height(30)))
                {
                    GameApi.UnlockCosmeticOption(option);
                    GameApi.SetCosmetic(category, i);
                }
                if (locked)
                    TipLast(Ru ? "Открыть конкретный элемент без разблокировки всей категории." : "Unlock this specific cosmetic without unlocking the whole category.");
                if (!locked && !option.isBlank && GUILayout.Button(Ru ? "Закрыть" : "Lock", Theme.DangerBtn, GUILayout.Width(82), GUILayout.Height(30)))
                {
                    if (active && blank >= 0 && blank != i)
                        GameApi.SetCosmetic(category, blank);
                    GameApi.LockCosmeticOption(option);
                }
                if (!locked && !option.isBlank)
                    TipLast(Ru ? "Сбросить открытие этого элемента. Может сбросить достижение, стат или максимальную высоту." : "Reset this option's unlock. May reset a linked achievement, stat, or max height value.");
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static bool DrawCosmeticListRow(GameApi.CosmeticCategory category, CustomizationOption option, string name, bool active, bool locked)
        {
            Rect r = GUILayoutUtility.GetRect(10f, 34f, GUILayout.ExpandWidth(true), GUILayout.Height(34f));
            bool clicked = GUI.Button(r, GUIContent.none, active ? Theme.ListItemActive : Theme.ListItem);

            Rect iconRect = new Rect(r.x + 6f, r.y + 5f, 24f, 24f);
            Texture tex = null;
            Color swatch = Color.clear;
            bool hasSwatch = false;
            try
            {
                tex = option != null ? option.texture : null;
                if (tex == null && option != null && option.fitMaterial != null)
                    tex = option.fitMaterial.mainTexture;
                if (tex == null && option != null && option.fitHatMaterial != null)
                    tex = option.fitHatMaterial.mainTexture;
                if (option != null && !option.isBlank)
                {
                    swatch = option.color;
                    hasSwatch = swatch.a > 0.01f;
                }
            }
            catch { }

            if (tex != null)
            {
                Color prev = GUI.color;
                GUI.color = locked ? new Color(1f, 1f, 1f, 0.52f) : Color.white;
                if (category == GameApi.CosmeticCategory.Eyes)
                    GUI.DrawTextureWithTexCoords(iconRect, tex, new Rect(0f, 1f, 1f, -1f), true);
                else
                    GUI.DrawTexture(iconRect, tex, ScaleMode.ScaleToFit, true);
                GUI.color = prev;
            }
            else if (hasSwatch)
            {
                Texture2D fill = Theme.Tex(locked ? new Color(swatch.r, swatch.g, swatch.b, 0.5f) : swatch);
                GUI.DrawTexture(iconRect, fill);
            }
            else
            {
                string letter = string.IsNullOrWhiteSpace(name) ? "?" : name.Trim()[0].ToString().ToUpperInvariant();
                GUI.Label(iconRect, letter, ItemIconFallbackStyle());
            }

            GUI.Label(new Rect(r.x + 38f, r.y + 2f, r.width - 44f, r.height - 4f), name, Theme.RowLabel);
            return clicked;
        }

        private static void DrawCosmeticsModern()
        {
            GUILayout.Label(L("tab.cosmetics"), Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            GUILayout.Label(Ru
                ? "Косметика в PEAK в основном открывается через достижения и стат максимальной высоты. Здесь можно точечно управлять связанными достижениями."
                : "PEAK cosmetics mostly unlock through achievements and max height progression. You can manage the linked unlocks more precisely here.",
                Theme.LabelDim);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Разблокировать всю косметику" : "Unlock all cosmetics", Theme.DonateBtn, GUILayout.Height(32)))
                SteamAch.UnlockAllCosmetics();
            if (GUILayout.Button(Ru ? "Сбросить стат высоты" : "Reset height stat", Theme.LinkBtn, GUILayout.Height(32)))
                SteamAch.SetMaxAscent(0);
            GUILayout.EndHorizontal();

            GUILayout.Label(Ru ? "Стат максимальной высоты" : "Max height stat", Theme.LabelDim);
            GUILayout.BeginHorizontal();
            for (int i = 0; i <= 8; i++)
            {
                int level = i;
                if (GUILayout.Button(level.ToString(), Theme.Chip, GUILayout.Width(34), GUILayout.Height(26)))
                    SteamAch.SetMaxAscent(level);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            DrawBadgesModern();
        }

        private static void DrawAboutModern()
        {
            GUILayout.Label("PEAK-MX  v" + Plugin.Version, Theme.Section);
            GUILayout.Label(Ru ? "Автор: maxkir041" : "Author: maxkir041", Theme.Label);
            GUILayout.Label(Ru ? "Разработчик: maxkir041" : "Developer: maxkir041", Theme.LabelDim);

            GUILayout.Space(6);
            GUILayout.Label(L("ui.language"), Theme.Section);
            DrawLanguagePicker();
            DrawMenuKeyBinding();
            DrawClientIdentity();
            DrawUpdateChecker();
            DrawThemeColorSettings();

            GUILayout.Space(6);
            GUILayout.Label(Ru ? "Ссылки" : "Links", Theme.Section);
            GUILayout.BeginHorizontal();
            LinkButton("GitHub", UrlGitHub);
            LinkButton("Thunderstore", UrlThunderstore);
            LinkButton("Nexus", UrlNexus);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            LinkButton("Steam", UrlSteam);
            LinkButton("Telegram", UrlTelegram);
            LinkButton("Website", UrlWebsite);
            GUILayout.EndHorizontal();

            DrawFeedbackPanel();

            GUILayout.Space(8);
            GUILayout.Label(SupportText(), Theme.DonateText);
            if (GUILayout.Button(DonateTextLabel(), Theme.DonateBtn, GUILayout.Width(180), GUILayout.Height(30)))
                OpenUrl(UrlDonate);
            GUILayout.Space(6);
            DrawQr(150);
            GUILayout.Label(QrHintText(), Theme.LabelDim);
            DrawDonationSupportPanel(false);

            GUILayout.Space(6);
            bool showReminder = ModConfig.ShowDonateNotice.Value;
            bool updatedShowReminder = ToggleRaw(ReminderToggleText().Trim(), Ru ? "Показывать окно поддержки при запуске игры." : "Show the support reminder when the game starts.", showReminder);
            if (updatedShowReminder != showReminder) ModConfig.ShowDonateNotice.Value = updatedShowReminder;

            DrawAnalyticsStatus();

        }

        private static void DrawWindowModern(int id)
        {
            if (_headerTex == null)
                _headerTex = Theme.GradientTex(Theme.HeaderBg, Theme.HeaderDim, Mathf.RoundToInt(HeaderHeight));
            if (_accentTex == null) _accentTex = Theme.Tex(Theme.Accent);

            GUI.DrawTexture(new Rect(0f, 0f, _rect.width, HeaderHeight), _headerTex);
            GUI.DrawTexture(new Rect(0f, HeaderHeight, _rect.width, 2f), _accentTex);

            float navWidth = Mathf.Clamp(_rect.width * 0.22f, 136f, 176f);
            float bodyTop = HeaderHeight + 12f;
            float bodyHeight = _rect.height - bodyTop - WindowPadding;
            float contentX = WindowPadding + navWidth + 12f;
            float contentWidth = _rect.width - contentX - WindowPadding;
            float footerHeight = Mathf.Clamp(_rect.height * 0.18f, 86f, 142f);

            GUILayout.BeginArea(new Rect(16f, 10f, _rect.width - 32f, HeaderHeight - 18f));
            GUILayout.BeginHorizontal();
            DrawBrandMark(32f);
            GUILayout.Space(8f);
            GUILayout.BeginVertical(GUILayout.Height(40f));
            GUILayout.Label("PEAK-MX", Theme.Title, GUILayout.Height(22f));
            GUILayout.Label($"v{Plugin.Version}  |  by maxkir041  |  {L(Tabs[_tab])}", Theme.Subtitle, GUILayout.Height(16f));
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", Theme.CloseBtn, GUILayout.Width(34), GUILayout.Height(26)))
                _closeRequested = true;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            Rect navRect = new Rect(WindowPadding, bodyTop, navWidth, bodyHeight);
            _contentRect = new Rect(contentX, bodyTop, contentWidth, bodyHeight);

            GUI.Box(navRect, GUIContent.none, Theme.Panel9);
            GUI.Box(_contentRect, GUIContent.none, Theme.Card);

            GUILayout.BeginArea(new Rect(navRect.x + 10f, navRect.y + 10f, navRect.width - 20f, navRect.height - 20f));
            for (int i = 0; i < Tabs.Length; i++)
            {
                var style = i == _tab ? Theme.NavItemActive : Theme.NavItem;
                if (GUILayout.Button(L(Tabs[i]), style, GUILayout.Height(32)))
                    _tab = i;
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("maxkir041", Theme.LabelDim);
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(_contentRect.x + 12f, _contentRect.y + 10f, _contentRect.width - 24f, _contentRect.height - 24f - footerHeight));
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Space(2);
            DrawCurrentModernTabSafe();
            GUILayout.Space(4);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            DrawTooltipFooter(new Rect(_contentRect.x + 12f, _contentRect.yMax - footerHeight - 10f, _contentRect.width - 24f, footerHeight));
            GUI.DragWindow(new Rect(0f, 0f, _rect.width, HeaderHeight));
        }

        private static void DrawCurrentModernTabSafe()
        {
            try
            {
                switch (_tab)
                {
                    case 0: DrawCharacterModern(); break;
                    case 1: DrawCheatsModern3(); break;
                    case 2: DrawAdminModern(); break;
                    case 3: DrawInventoryModern3(); break;
                    case 4: DrawWorldModern(); break;
                    case 5: DrawBadgesModern(); break;
                    case 6: DrawCosmeticsModern3(); break;
                    case 7: DrawAboutModern(); break;
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"[Menu:{Tabs[Mathf.Clamp(_tab, 0, Tabs.Length - 1)]}] {e}");
                GUILayout.Label(Ru ? "Вкладка временно недоступна после обновления игры. Ошибка записана в лог BepInEx." : "This tab hit a game-update compatibility error. Details were written to the BepInEx log.", Theme.LabelDim);
                if (GUILayout.Button(Ru ? "Обновить игроков/предметы" : "Refresh players/items", Theme.LinkBtn, GUILayout.Height(28)))
                {
                    GameApi.RefreshPlayers();
                    GameApi.LoadItems();
                }
            }
        }

        private static void DrawCharacterModern()
        {
            if (BeginSection("character.move", L("tab.character"), true))
            {
                ModConfig.SpeedMod = Toggle("feat.speed", ModConfig.SpeedMod);
                if (ModConfig.SpeedMod) ModConfig.SpeedAmount = Slider("x", ModConfig.SpeedAmount, 0.5f, 5f);
                ModConfig.JumpMod = Toggle("feat.jump", ModConfig.JumpMod);
                if (ModConfig.JumpMod) ModConfig.JumpAmount = Slider("x", ModConfig.JumpAmount, 1f, 5f);
                ModConfig.InfiniteJumps = ToggleRaw(
                    Ru ? "Бесконечные прыжки" : "Infinite jumps",
                    Ru ? "Позволяет прыгать повторно в воздухе без ожидания земли." : "Allows repeated jumps in the air without touching the ground.",
                    ModConfig.InfiniteJumps);
                ModConfig.NoSlipperySurfaces = ToggleRaw(
                    Ru ? "Без скользких поверхностей" : "No slippery surfaces",
                    Ru ? "Сильно снижает соскальзывание и считает крутые поверхности пригодными для стояния." : "Greatly reduces sliding and treats steep surfaces as standable.",
                    ModConfig.NoSlipperySurfaces);
                EndSection();
            }

            if (BeginSection("character.climb", Ru ? "Лазание" : "Climbing", false))
            {
                ModConfig.ClimbMod = Toggle("feat.climb", ModConfig.ClimbMod);
                if (ModConfig.ClimbMod) ModConfig.ClimbAmount = Slider("x", ModConfig.ClimbAmount, 1f, 5f);
                ModConfig.VineClimbMod = Toggle("feat.vineclimb", ModConfig.VineClimbMod);
                if (ModConfig.VineClimbMod) ModConfig.VineClimbAmount = Slider("x", ModConfig.VineClimbAmount, 1f, 5f);
                ModConfig.RopeClimbMod = Toggle("feat.ropeclimb", ModConfig.RopeClimbMod);
                if (ModConfig.RopeClimbMod) ModConfig.RopeClimbAmount = Slider("x", ModConfig.RopeClimbAmount, 1f, 5f);
                ModConfig.ClimbStaminaConsumptionPercent = Slider(Ru ? "Расход стамины при лазании %" : "Climbing stamina cost %", ModConfig.ClimbStaminaConsumptionPercent, 0f, 300f);
                EndSection();
            }

            if (BeginSection("character.status", Ru ? "Стамина и недуги" : "Stamina & afflictions", false))
            {
                Character statusTargetChar = TargetPicker(ref _statusTarget);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Полная стамина" : "Full stamina", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.FullStamina(statusTargetChar);
                if (GUILayout.Button(Ru ? "Полная экстра" : "Full extra", Theme.LinkBtn, GUILayout.Height(30)))
                {
                    ModConfig.ExtraStaminaPercent = 100f;
                    SetNumberText(Ru ? "Экстра стамина %" : "Extra stamina %", 0f, 100f, ModConfig.ExtraStaminaPercent);
                    GameApi.FullExtraStamina(statusTargetChar);
                }
                if (GUILayout.Button(Ru ? "Снять все недуги" : "Clear afflictions", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.ClearAllStatus(statusTargetChar);
                GUILayout.EndHorizontal();

                string extraLabel = Ru ? "Экстра стамина %" : "Extra stamina %";
                ModConfig.ExtraStaminaPercent = NumberField(extraLabel, ModConfig.ExtraStaminaPercent, 0f, 100f);
                if (GUILayout.Button(Ru ? "Выдать экстрастамину" : "Set extra stamina", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.SetExtraStamina(statusTargetChar, ModConfig.ExtraStaminaPercent / 100f);
                TipLast(Ru ? "Выставляет точное количество дополнительной стамины выбранной цели." : "Sets an exact amount of extra stamina for the selected target.");

                ModConfig.StaminaConsumptionPercent = Slider(Ru ? "Расход стамины %" : "Stamina cost %", ModConfig.StaminaConsumptionPercent, 0f, 300f);
                ModConfig.StaminaRegenPercent = Slider(Ru ? "Восстановление %" : "Stamina regen %", ModConfig.StaminaRegenPercent, 0f, 300f);
                ModConfig.StaminaRegenDelayMod = ToggleRaw(
                    Ru ? "Своя задержка восстановления" : "Custom regen delay",
                    Ru ? "Заменяет задержку перед восстановлением стамины на значение ниже." : "Overrides the delay before stamina starts regenerating.",
                    ModConfig.StaminaRegenDelayMod);
                if (ModConfig.StaminaRegenDelayMod)
                    ModConfig.StaminaRegenDelay = Slider(Ru ? "Задержка" : "Delay", ModConfig.StaminaRegenDelay, 0f, 5f);

                var afflictionNames = Ru ? GameApi.StatusRu : GameApi.StatusEn;
                _selStatus = GUILayout.SelectionGrid(_selStatus, afflictionNames, 3, Theme.ListItem, GUILayout.Height(140));
                _statusAmount = Slider(Ru ? "Сила" : "Amount", _statusAmount, 0f, 1f);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Наложить" : "Apply", Theme.DonateBtn, GUILayout.Height(32)))
                    GameApi.SetStatus(statusTargetChar, _selStatus, _statusAmount);
                if (GUILayout.Button(Ru ? "Убрать этот" : "Remove this", Theme.LinkBtn, GUILayout.Width(140), GUILayout.Height(32)))
                    GameApi.SetStatus(statusTargetChar, _selStatus, 0f);
                GUILayout.EndHorizontal();

                ModConfig.StatusIncreasePercent = Slider(Ru ? "Увеличение статуса %" : "Status increase %", ModConfig.StatusIncreasePercent, 0f, 100f);
                if (GUILayout.Button(Ru ? "Увеличить выбранный" : "Increase selected", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.IncreaseStatus(statusTargetChar, _selStatus, ModConfig.StatusIncreasePercent / 100f);
                TipLast(Ru ? "Прибавить выбранный статус на указанный процент." : "Increase the selected status by the chosen percentage.");

                DrawStatusIncreaseButtons(statusTargetChar);
                EndSection();
            }
        }

        private static void DrawCheatsModern()
        {
            if (BeginSection("cheats.core", L("tab.cheats"), true))
            {
                ModConfig.GodMode = Toggle("feat.god", ModConfig.GodMode);
                ModConfig.InfiniteStamina = Toggle("feat.infstam", ModConfig.InfiniteStamina);
                ModConfig.NoFallDamage = Toggle("feat.nofall", ModConfig.NoFallDamage);
                ModConfig.NoWeight = Toggle("feat.noweight", ModConfig.NoWeight);
                ModConfig.LockStatus = Toggle("feat.lockstatus", ModConfig.LockStatus);
                EndSection();
            }

            if (BeginSection("cheats.fly", Ru ? "Полёт и телепорт" : "Fly & teleport", false))
            {
                ModConfig.TeleportToPing = Toggle("feat.tpping", ModConfig.TeleportToPing);
                ModConfig.Fly = Toggle("feat.fly", ModConfig.Fly);
                if (ModConfig.Fly)
                {
                    ModConfig.FlySpeed = Slider(Ru ? "Скорость" : "Speed", ModConfig.FlySpeed, 1f, 50f);
                    ModConfig.FlyAcceleration = Slider(Ru ? "Ускорение" : "Accel", ModConfig.FlyAcceleration, 1f, 100f);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(Ru ? "Коорд.:" : "Coords:", Theme.LabelDim, GUILayout.Width(54));
                GUILayout.Label("X", Theme.LabelDim, GUILayout.Width(12)); _tpX = GUILayout.TextField(_tpX ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                GUILayout.Label("Y", Theme.LabelDim, GUILayout.Width(12)); _tpY = GUILayout.TextField(_tpY ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                GUILayout.Label("Z", Theme.LabelDim, GUILayout.Width(12)); _tpZ = GUILayout.TextField(_tpZ ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                if (GUILayout.Button(Ru ? "ТП" : "TP", Theme.DonateBtn, GUILayout.Width(54), GUILayout.Height(26)))
                {
                    if (float.TryParse(_tpX, out float x) & float.TryParse(_tpY, out float y) & float.TryParse(_tpZ, out float z))
                        GameApi.TeleportToCoords(x, y, z);
                }
                GUILayout.EndHorizontal();
                EndSection();
            }

            if (BeginSection("cheats.actions", Ru ? "Действия" : "Actions", false))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Воскресить себя" : "Revive self", Theme.LinkBtn, GUILayout.Height(28))) GameApi.ReviveSelf();
                if (GUILayout.Button(Ru ? "Убить себя" : "Kill self", Theme.LinkBtn, GUILayout.Height(28))) GameApi.KillSelf();
                if (GUILayout.Button(Ru ? "К точке спавна" : "Warp to spawn", Theme.LinkBtn, GUILayout.Height(28))) GameApi.WarpToSpawn();
                GUILayout.EndHorizontal();
                EndSection();
            }

            if (BeginSection("cheats.players", Ru ? "Игроки" : "Players", false))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Обновить список" : "Refresh", Theme.LinkBtn, GUILayout.Height(26))) GameApi.RefreshPlayers();
                if (GUILayout.Button(Ru ? "Воскресить всех" : "Revive all", Theme.LinkBtn, GUILayout.Height(26))) GameApi.ReviveAll();
                if (GUILayout.Button(Ru ? "Притянуть всех" : "Warp all to me", Theme.LinkBtn, GUILayout.Height(26))) GameApi.WarpAllToMe();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Убить всех" : "Kill all", Theme.LinkBtn, GUILayout.Height(26))) GameApi.KillAll(_excludeSelf);
                _excludeSelf = GUILayout.Toggle(_excludeSelf, Ru ? " Не трогать себя" : " Exclude me", Theme.RowLabel, GUILayout.Height(26));
                GUILayout.EndHorizontal();

                if (GameApi.PlayerChars.Count == 0)
                    GUILayout.Label(Ru ? "Список пуст — нажми «Обновить» в лобби" : "Empty — press Refresh in a lobby", Theme.LabelDim);

                for (int i = 0; i < GameApi.PlayerChars.Count; i++)
                {
                    var c = GameApi.PlayerChars[i];
                    if (c == null) continue;
                    bool local = false; try { local = c.IsLocal; } catch { }
                    GUILayout.BeginHorizontal();
                    GUILayout.Label((i < GameApi.PlayerNames.Count ? GameApi.PlayerNames[i] : "?") + (local ? (Ru ? " (ты)" : " (you)") : ""), Theme.RowLabel, GUILayout.Width(140));
                    if (!local)
                    {
                        if (GUILayout.Button("TP", Theme.LinkBtn, GUILayout.Width(44))) GameApi.WarpToPlayer(c);
                        if (GUILayout.Button(Ru ? "Притянуть" : "Bring", Theme.LinkBtn, GUILayout.Width(78))) GameApi.BringPlayer(c);
                        if (GUILayout.Button(Ru ? "Убить" : "Kill", Theme.LinkBtn, GUILayout.Width(58))) GameApi.KillPlayer(c);
                        if (GUILayout.Button(Ru ? "Ожив." : "Revive", Theme.LinkBtn, GUILayout.Width(66))) GameApi.RevivePlayer(c);
                    }
                    if (GUILayout.Button(Ru ? "Скаут" : "Scout", Theme.LinkBtn, GUILayout.Width(64))) GameApi.SpawnScoutmaster(c);
                    GUILayout.EndHorizontal();
                }
                EndSection();
            }

            if (BeginSection("cheats.pranks", Ru ? "Приколы" : "Pranks", false))
            {
                Character prankTargetChar = TargetPicker(ref _prankTarget);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Наслать все недуги" : "Pile on afflictions", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.PrankAfflict(prankTargetChar);
                if (GUILayout.Button(Ru ? "Вылечить" : "Cure", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.ClearAllStatus(prankTargetChar);
                GUILayout.EndHorizontal();
                if (GUILayout.Button(Ru ? "Забить инвентарь выбранным предметом" : "Stuff inventory with selected item", Theme.DonateBtn, GUILayout.Height(32)))
                    GameApi.FillInventoryWith(prankTargetChar, _selItem);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Забить золотом" : "Fill with gold", Theme.LinkBtn, GUILayout.Height(28)))
                {
                    int gold = GameApi.FindItemIndex("gold", "coin", "treasure");
                    if (gold >= 0) GameApi.FillInventoryWith(prankTargetChar, gold);
                }
                if (GUILayout.Button(Ru ? "Забить паутиной" : "Fill with webs", Theme.LinkBtn, GUILayout.Height(28)))
                {
                    int web = GameApi.FindItemIndex("web", "spider");
                    if (web >= 0) GameApi.FillInventoryWith(prankTargetChar, web);
                }
                GUILayout.EndHorizontal();
                GUILayout.Label(Ru ? "Предмет берётся из вкладки «Инвентарь». Физические шарики на игроке добавлю отдельно."
                                   : "Item comes from the Inventory tab. Physical balloons on a player will come separately.", Theme.LabelDim);
                EndSection();
            }
        }

        private static void DrawCheatsModern2()
        {
            if (BeginSection("cheats.core", L("tab.cheats"), true))
            {
                ModConfig.GodMode = Toggle("feat.god", ModConfig.GodMode);
                ModConfig.InfiniteStamina = Toggle("feat.infstam", ModConfig.InfiniteStamina);
                ModConfig.NoFallDamage = Toggle("feat.nofall", ModConfig.NoFallDamage);
                ModConfig.NoWeight = Toggle("feat.noweight", ModConfig.NoWeight);
                ModConfig.LockStatus = Toggle("feat.lockstatus", ModConfig.LockStatus);
                EndSection();
            }

            if (BeginSection("cheats.fly2", Ru ? "Полёт и телепорт" : "Fly & teleport", false))
            {
                ModConfig.TeleportToPing = Toggle("feat.tpping", ModConfig.TeleportToPing);
                ModConfig.Fly = Toggle("feat.fly", ModConfig.Fly);
                if (ModConfig.Fly)
                {
                    ModConfig.Noclip = ToggleRaw(
                        Ru ? "Ноклип" : "Noclip",
                        Ru ? "Отключает локальные коллайдеры и столкновения во время полёта." : "Disables local colliders and collisions while flying.",
                        ModConfig.Noclip);
                    ModConfig.FlySpeed = Slider(Ru ? "Скорость" : "Speed", ModConfig.FlySpeed, 1f, 50f);
                    ModConfig.FlyAcceleration = Slider(Ru ? "Ускорение" : "Accel", ModConfig.FlyAcceleration, 1f, 100f);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(Ru ? "Коорд.:" : "Coords:", Theme.LabelDim, GUILayout.Width(54));
                GUILayout.Label("X", Theme.LabelDim, GUILayout.Width(12)); _tpX = GUILayout.TextField(_tpX ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                GUILayout.Label("Y", Theme.LabelDim, GUILayout.Width(12)); _tpY = GUILayout.TextField(_tpY ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                GUILayout.Label("Z", Theme.LabelDim, GUILayout.Width(12)); _tpZ = GUILayout.TextField(_tpZ ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                if (GUILayout.Button(Ru ? "ТП" : "TP", Theme.DonateBtn, GUILayout.Width(54), GUILayout.Height(26)))
                {
                    if (float.TryParse(_tpX, out float x) & float.TryParse(_tpY, out float y) & float.TryParse(_tpZ, out float z))
                        GameApi.TeleportToCoords(x, y, z);
                }
                GUILayout.EndHorizontal();
                EndSection();
            }

            if (BeginSection("cheats.actions2", Ru ? "Действия" : "Actions", false))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Воскресить себя" : "Revive self", Theme.LinkBtn, GUILayout.Height(28))) GameApi.ReviveSelf();
                if (GUILayout.Button(Ru ? "Убить себя" : "Kill self", Theme.LinkBtn, GUILayout.Height(28))) GameApi.KillSelf();
                if (GUILayout.Button(Ru ? "К точке спавна" : "Warp to spawn", Theme.LinkBtn, GUILayout.Height(28))) GameApi.WarpToSpawn();
                GUILayout.EndHorizontal();
                EndSection();
            }

            if (BeginSection("cheats.players2", Ru ? "Игроки" : "Players", false))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Обновить список" : "Refresh", Theme.LinkBtn, GUILayout.Height(26))) GameApi.RefreshPlayers();
                if (GUILayout.Button(Ru ? "Воскресить всех" : "Revive all", Theme.LinkBtn, GUILayout.Height(26))) GameApi.ReviveAll();
                if (GUILayout.Button(Ru ? "Притянуть всех" : "Warp all to me", Theme.LinkBtn, GUILayout.Height(26))) GameApi.WarpAllToMe();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Убить всех" : "Kill all", Theme.LinkBtn, GUILayout.Height(26))) GameApi.KillAll(_excludeSelf);
                _excludeSelf = GUILayout.Toggle(_excludeSelf, Ru ? " Не трогать себя" : " Exclude me", Theme.RowLabel, GUILayout.Height(26));
                GUILayout.EndHorizontal();

                if (GameApi.PlayerChars.Count == 0)
                    GUILayout.Label(Ru ? "Список пуст — нажми «Обновить» в лобби." : "Empty - press Refresh in a lobby.", Theme.LabelDim);

                for (int i = 0; i < GameApi.PlayerChars.Count; i++)
                {
                    var c = GameApi.PlayerChars[i];
                    if (c == null) continue;
                    bool local = false; try { local = c.IsLocal; } catch { }
                    GUILayout.BeginHorizontal();
                    GUILayout.Label((i < GameApi.PlayerNames.Count ? GameApi.PlayerNames[i] : "?") + (local ? (Ru ? " (ты)" : " (you)") : ""), Theme.RowLabel, GUILayout.Width(92));
                    if (!local)
                    {
                        if (GUILayout.Button("TP", Theme.LinkBtn, GUILayout.Width(44))) GameApi.WarpToPlayer(c);
                        if (GUILayout.Button(Ru ? "Спавн" : "Spawn", Theme.LinkBtn, GUILayout.Width(70))) GameApi.WarpPlayerToSpawn(c);
                        if (GUILayout.Button(Ru ? "Притянуть" : "Bring", Theme.LinkBtn, GUILayout.Width(78))) GameApi.BringPlayer(c);
                        if (GUILayout.Button(Ru ? "Убить" : "Kill", Theme.LinkBtn, GUILayout.Width(58))) GameApi.KillPlayer(c);
                        if (GUILayout.Button(Ru ? "Ожив." : "Revive", Theme.LinkBtn, GUILayout.Width(66))) GameApi.RevivePlayer(c);
                    }
                    if (GUILayout.Button(Ru ? "Скаут" : "Scout", Theme.LinkBtn, GUILayout.Width(64))) GameApi.SpawnScoutmaster(c);
                    GUILayout.EndHorizontal();
                }
                EndSection();
            }

            if (BeginSection("cheats.pranks2", Ru ? "Приколы" : "Pranks", false))
            {
                Character prankTargetChar = TargetPicker(ref _prankTarget);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Наслать все недуги" : "Pile on afflictions", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.PrankAfflict(prankTargetChar);
                if (GUILayout.Button(Ru ? "Вылечить" : "Cure", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.ClearAllStatus(prankTargetChar);
                GUILayout.EndHorizontal();

                if (GUILayout.Button(Ru ? "Забить инвентарь выбранным предметом" : "Stuff inventory with selected item", Theme.DonateBtn, GUILayout.Height(32)))
                    GameApi.FillInventoryWith(prankTargetChar, _selItem);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Забить золотом" : "Fill with gold", Theme.LinkBtn, GUILayout.Height(28)))
                {
                    int gold = GameApi.FindItemIndex("gold", "coin", "treasure");
                    if (gold >= 0) GameApi.FillInventoryWith(prankTargetChar, gold);
                }
                if (GUILayout.Button(Ru ? "Забить паутиной" : "Fill with webs", Theme.LinkBtn, GUILayout.Height(28)))
                {
                    int web = GameApi.FindItemIndex("web", "spider");
                    if (web >= 0) GameApi.FillInventoryWith(prankTargetChar, web);
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(Ru
                    ? "Предмет берётся из вкладки «Инвентарь». Физические шарики и другие приколы можно добавить позже отдельно."
                    : "The item comes from the Inventory tab. Physical balloon-style pranks can be added separately later.",
                    Theme.LabelDim);
                EndSection();
            }
        }

        private static void DrawInventoryModern()
        {
            if (BeginSection("inventory.spawn", Ru ? "Спавн предметов" : "Item spawner", true))
            {
                bool hasItems = GameApi.ItemNames.Count > 0;
                if (!hasItems)
                {
                    if (GUILayout.Button(Ru ? "Загрузить список предметов" : "Load item list", Theme.DonateBtn, GUILayout.Height(30)))
                        GameApi.EnsureItemsLoaded();
                }
                else
                {
                    GUILayout.Label((Ru ? "Предметов: " : "Items: ") + GameApi.ItemNames.Count, Theme.LabelDim);

                    if (_invTarget >= GameApi.PlayerChars.Count) _invTarget = -1;
                    Character invChar = (_invTarget >= 0 && _invTarget < GameApi.PlayerChars.Count) ? GameApi.PlayerChars[_invTarget] : null;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Ru ? "Кому:" : "Target:", Theme.LabelDim, GUILayout.Width(54));
                    if (GUILayout.Button(Ru ? "Себе" : "Me", _invTarget < 0 ? Theme.ChipActive : Theme.Chip, GUILayout.Height(26)))
                        _invTarget = -1;
                    for (int i = 0; i < GameApi.PlayerChars.Count; i++)
                    {
                        var pc = GameApi.PlayerChars[i];
                        bool me = false; try { me = pc != null && pc.IsLocal; } catch { }
                        if (me) continue;
                        string nm = i < GameApi.PlayerNames.Count ? GameApi.PlayerNames[i] : "?";
                        if (GUILayout.Button(nm, _invTarget == i ? Theme.ChipActive : Theme.Chip, GUILayout.Height(26)))
                            _invTarget = i;
                    }
                    if (GUILayout.Button(Ru ? "Обн." : "Sync", Theme.LinkBtn, GUILayout.Width(52), GUILayout.Height(26)))
                        GameApi.RefreshPlayers();
                    GUILayout.EndHorizontal();

                    int slots = invChar != null ? GameApi.SlotCountFor(invChar) : GameApi.SlotCount();
                    if (slots <= 0) slots = 3;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Ru ? "Слот:" : "Slot:", Theme.LabelDim, GUILayout.Width(54));
                    for (int i = 0; i < slots; i++)
                    {
                        if (GUILayout.Button((i + 1).ToString(), _selSlot == i ? Theme.ChipActive : Theme.Chip, GUILayout.Width(36), GUILayout.Height(30)))
                            _selSlot = i;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label(Ru ? "Содержимое слотов" : "Slot contents", Theme.LabelDim);
                    for (int i = 0; i < slots; i++)
                    {
                        string itemName = GameApi.DescribeSlot(invChar, i);
                        GUILayout.Label($"{i + 1}. {(!string.IsNullOrEmpty(itemName) ? itemName : "-")}", Theme.LabelDim);
                    }

                    _itemSearch = GUILayout.TextField(_itemSearch ?? "", Theme.LinkBtn, GUILayout.Height(26));
                    _itemScroll = GUILayout.BeginScrollView(_itemScroll, GUILayout.Height(170));
                    string q = (_itemSearch ?? "").Trim().ToLowerInvariant();
                    for (int i = 0; i < GameApi.ItemNames.Count; i++)
                    {
                        string nm = GameApi.ItemNames[i];
                        if (q.Length > 0 && nm.ToLowerInvariant().IndexOf(q) < 0) continue;
                        if (DrawItemListRow(i, nm, _selItem == i))
                            _selItem = i;
                    }
                    GUILayout.EndScrollView();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "Заспавнить в слот" : "Spawn to slot", Theme.DonateBtn, GUILayout.Height(34)))
                    {
                        if (_selItem >= 0)
                        {
                            if (invChar != null) GameApi.SpawnToSlotFor(invChar, _selItem, _selSlot);
                            else GameApi.SpawnToSlot(_selItem, _selSlot);
                        }
                    }
                    if (GUILayout.Button(Ru ? "Очистить слот" : "Clear slot", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(28)))
                    {
                        if (invChar != null) GameApi.ClearSlotFor(invChar, _selSlot);
                        else GameApi.ClearSlot(_selSlot);
                    }
                    GUILayout.EndHorizontal();

                    if (_selItem >= 0 && _selItem < GameApi.ItemNames.Count)
                        GUILayout.Label((Ru ? "Выбрано: " : "Selected: ") + GameApi.ItemNames[_selItem], Theme.LabelDim);
                    if (invChar != null)
                        GUILayout.Label((Ru ? "Смотришь и редактируешь инвентарь игрока: " : "Viewing and editing player's inventory: ")
                                        + (_invTarget < GameApi.PlayerNames.Count ? GameApi.PlayerNames[_invTarget] : "?"), Theme.LabelDim);
                }
                EndSection();
            }

            if (BeginSection("inventory.recharge", Ru ? "Перезарядка предметов" : "Item recharge", false))
            {
                ModConfig.RechargeValue = NumberField(Ru ? "Заряд" : "Charge", ModConfig.RechargeValue, 0f, 999f);
                GUILayout.BeginHorizontal();
                for (int i = 0; i < Mathf.Min(3, GameApi.SlotCount()); i++)
                {
                    int slot = i;
                    if (GUILayout.Button((Ru ? "Слот " : "Slot ") + (slot + 1), Theme.LinkBtn, GUILayout.Height(28)))
                        GameApi.RechargeSlot(slot, ModConfig.RechargeValue);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Зарядить выбранный слот" : "Recharge selected slot", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.RechargeSlot(_selSlot, ModConfig.RechargeValue);
                if (GUILayout.Button(Ru ? "Зарядить все" : "Recharge all", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(30)))
                    for (int s = 0; s < GameApi.SlotCount(); s++) GameApi.RechargeSlot(s, ModConfig.RechargeValue);
                GUILayout.EndHorizontal();
                EndSection();
            }
        }

        private static void DrawInventoryModern2()
        {
            if (BeginSection("inventory.spawn2", Ru ? "Спавн предметов" : "Item spawner", true))
            {
                bool hasItems = GameApi.ItemNames.Count > 0;
                if (!hasItems)
                {
                    if (GUILayout.Button(Ru ? "Загрузить список предметов" : "Load item list", Theme.DonateBtn, GUILayout.Height(30)))
                        GameApi.EnsureItemsLoaded();
                }
                else
                {
                    GUILayout.Label((Ru ? "Предметов: " : "Items: ") + GameApi.ItemNames.Count, Theme.LabelDim);

                    if (_invTarget >= GameApi.PlayerChars.Count) _invTarget = -1;
                    Character invChar = (_invTarget >= 0 && _invTarget < GameApi.PlayerChars.Count) ? GameApi.PlayerChars[_invTarget] : null;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Ru ? "Кому:" : "Target:", Theme.LabelDim, GUILayout.Width(54));
                    if (GUILayout.Button(Ru ? "Себе" : "Me", _invTarget < 0 ? Theme.ChipActive : Theme.Chip, GUILayout.Height(26)))
                        _invTarget = -1;
                    for (int i = 0; i < GameApi.PlayerChars.Count; i++)
                    {
                        var pc = GameApi.PlayerChars[i];
                        bool me = false; try { me = pc != null && pc.IsLocal; } catch { }
                        if (me) continue;
                        string nm = i < GameApi.PlayerNames.Count ? GameApi.PlayerNames[i] : "?";
                        if (GUILayout.Button(nm, _invTarget == i ? Theme.ChipActive : Theme.Chip, GUILayout.Height(26)))
                            _invTarget = i;
                    }
                    if (GUILayout.Button(Ru ? "Обн." : "Sync", Theme.LinkBtn, GUILayout.Width(52), GUILayout.Height(26)))
                        GameApi.RefreshPlayers();
                    GUILayout.EndHorizontal();

                    int slots = invChar != null ? GameApi.SlotCountFor(invChar) : GameApi.SlotCount();
                    if (slots <= 0) slots = 3;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Ru ? "Слот:" : "Slot:", Theme.LabelDim, GUILayout.Width(54));
                    for (int i = 0; i < slots; i++)
                    {
                        if (GUILayout.Button((i + 1).ToString(), _selSlot == i ? Theme.ChipActive : Theme.Chip, GUILayout.Width(36), GUILayout.Height(30)))
                            _selSlot = i;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label(Ru ? "Содержимое слотов" : "Slot contents", Theme.LabelDim);
                    for (int i = 0; i < slots; i++)
                    {
                        string itemName = GameApi.DescribeSlot(invChar, i);
                        GUILayout.Label($"{i + 1}. {(!string.IsNullOrEmpty(itemName) ? itemName : "-")}", Theme.LabelDim);
                    }

                    _itemSearch = GUILayout.TextField(_itemSearch ?? "", Theme.LinkBtn, GUILayout.Height(26));
                    _itemScroll = GUILayout.BeginScrollView(_itemScroll, GUILayout.Height(170));
                    string q = (_itemSearch ?? "").Trim().ToLowerInvariant();
                    for (int i = 0; i < GameApi.ItemNames.Count; i++)
                    {
                        string nm = GameApi.ItemNames[i];
                        if (q.Length > 0 && nm.ToLowerInvariant().IndexOf(q) < 0) continue;
                        if (DrawItemListRow(i, nm, _selItem == i))
                            _selItem = i;
                    }
                    GUILayout.EndScrollView();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "Заспавнить в слот" : "Spawn to slot", Theme.DonateBtn, GUILayout.Height(34)))
                    {
                        if (_selItem >= 0)
                        {
                            if (invChar != null) GameApi.SpawnToSlotFor(invChar, _selItem, _selSlot);
                            else GameApi.SpawnToSlot(_selItem, _selSlot);
                        }
                    }
                    if (GUILayout.Button(Ru ? "Очистить слот" : "Clear slot", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(28)))
                    {
                        if (invChar != null) GameApi.ClearSlotFor(invChar, _selSlot);
                        else GameApi.ClearSlot(_selSlot);
                    }
                    GUILayout.EndHorizontal();

                    if (_selItem >= 0 && _selItem < GameApi.ItemNames.Count)
                        GUILayout.Label((Ru ? "Выбрано: " : "Selected: ") + GameApi.ItemNames[_selItem], Theme.LabelDim);
                    if (invChar != null)
                        GUILayout.Label((Ru ? "Смотришь и редактируешь инвентарь игрока: " : "Viewing and editing player's inventory: ")
                                        + (_invTarget < GameApi.PlayerNames.Count ? GameApi.PlayerNames[_invTarget] : "?"), Theme.LabelDim);
                }
                EndSection();
            }

            if (BeginSection("inventory.recharge2", Ru ? "Перезарядка предметов" : "Item recharge", false))
            {
                ModConfig.RechargeValue = NumberField(Ru ? "Заряд" : "Charge", ModConfig.RechargeValue, 0f, 999f);
                int localSlots = Mathf.Max(1, GameApi.SlotCount());
                GUILayout.BeginHorizontal();
                GUILayout.Label(Ru ? "Слот:" : "Slot:", Theme.LabelDim, GUILayout.Width(54));
                for (int i = 0; i < localSlots; i++)
                {
                    if (GUILayout.Button((i + 1).ToString(), _selSlot == i ? Theme.ChipActive : Theme.Chip, GUILayout.Width(36), GUILayout.Height(28)))
                        _selSlot = i;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Зарядить выбранный слот" : "Recharge selected slot", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.RechargeSlot(_selSlot, ModConfig.RechargeValue);
                if (GUILayout.Button(Ru ? "Зарядить все" : "Recharge all", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(30)))
                    for (int s = 0; s < GameApi.SlotCount(); s++) GameApi.RechargeSlot(s, ModConfig.RechargeValue);
                GUILayout.EndHorizontal();
                EndSection();
            }
        }

        private static void DrawCheatsModern3()
        {
            if (BeginSection("cheats.core3", L("tab.cheats"), true))
            {
                ModConfig.GodMode = Toggle("feat.god", ModConfig.GodMode);
                ModConfig.InfiniteStamina = Toggle("feat.infstam", ModConfig.InfiniteStamina);
                ModConfig.NoFallDamage = Toggle("feat.nofall", ModConfig.NoFallDamage);
                ModConfig.NoFallingRagdoll = ToggleRaw(
                    Ru ? "Без падения в ragdoll" : "No falling ragdoll",
                    Ru ? "Блокирует принудительный ragdoll от падения и соскальзывания." : "Blocks forced ragdoll from falling and sliding.",
                    ModConfig.NoFallingRagdoll);
                ModConfig.NoWeight = Toggle("feat.noweight", ModConfig.NoWeight);
                ModConfig.LockStatus = Toggle("feat.lockstatus", ModConfig.LockStatus);
                ModConfig.GlobalVoice = ToggleRaw(
                    Ru ? "Голос по всей карте" : "Global voice hearing",
                    Ru ? "Убирает дистанционное затухание чужих голосов на твоем клиенте. Другим игрокам для такого же эффекта нужен свой мод." : "Removes distance falloff for other players' voices on your client. Other players need their own mod for the same effect.",
                    ModConfig.GlobalVoice);
                ModConfig.NoStatusEffects = ToggleRaw(
                    Ru ? "Без эффектов статуса" : "No status effects",
                    Ru ? "Постоянно очищает все недуги локального персонажа." : "Continuously clears every affliction from the local character.",
                    ModConfig.NoStatusEffects);
                if (!ModConfig.NoStatusEffects)
                {
                    ModConfig.NoInjury = ToggleRaw(Ru ? "Без травм" : "No injury", Ru ? "Постоянно очищает травмы." : "Continuously clears injury.", ModConfig.NoInjury);
                    ModConfig.NoHunger = ToggleRaw(Ru ? "Без голода" : "No hunger", Ru ? "Постоянно очищает голод." : "Continuously clears hunger.", ModConfig.NoHunger);
                    ModConfig.NoCold = ToggleRaw(Ru ? "Без холода" : "No cold", Ru ? "Постоянно очищает холод." : "Continuously clears cold.", ModConfig.NoCold);
                    ModConfig.NoPoison = ToggleRaw(Ru ? "Без яда" : "No poison", Ru ? "Постоянно очищает яд." : "Continuously clears poison.", ModConfig.NoPoison);
                    ModConfig.NoCurse = ToggleRaw(Ru ? "Без проклятия" : "No curse", Ru ? "Постоянно очищает проклятие." : "Continuously clears curse.", ModConfig.NoCurse);
                    ModConfig.NoDrowsy = ToggleRaw(Ru ? "Без сонливости" : "No drowsy", Ru ? "Постоянно очищает сонливость." : "Continuously clears drowsiness.", ModConfig.NoDrowsy);
                    ModConfig.NoHot = ToggleRaw(Ru ? "Без жары" : "No hot", Ru ? "Постоянно очищает жару." : "Continuously clears heat.", ModConfig.NoHot);
                }
                EndSection();
            }

            if (BeginSection("cheats.fly3", Ru ? "Полёт и телепорт" : "Fly & teleport", false))
            {
                ModConfig.TeleportToPing = Toggle("feat.tpping", ModConfig.TeleportToPing);
                ModConfig.LongInteraction = ToggleRaw(
                    Ru ? "Дальнее взаимодействие" : "Long interaction",
                    Ru ? "Увеличивает дистанцию взаимодействия с предметами и объектами." : "Increases interaction distance for items and objects.",
                    ModConfig.LongInteraction);
                if (ModConfig.LongInteraction)
                    ModConfig.InteractionDistance = Slider(Ru ? "Дистанция" : "Distance", ModConfig.InteractionDistance, 2f, 80f);
                ModConfig.CinematicCamera = ToggleRaw(
                    Ru ? "Кинематографичная камера" : "Cinematic camera",
                    Ru ? "Свободная камера: WASD, Space/E вверх, Ctrl/Q вниз, Shift быстрее, мышь поворачивает." : "Free camera: WASD, Space/E up, Ctrl/Q down, Shift faster, mouse rotates.",
                    ModConfig.CinematicCamera);
                if (ModConfig.CinematicCamera)
                {
                    ModConfig.CinematicCameraSpeed = Slider(Ru ? "Скорость камеры" : "Camera speed", ModConfig.CinematicCameraSpeed, 0.5f, 40f);
                    ModConfig.CinematicCameraFov = Slider("FOV", ModConfig.CinematicCameraFov, 1f, 120f);
                }
                ModConfig.Fly = Toggle("feat.fly", ModConfig.Fly);
                if (ModConfig.Fly)
                {
                    ModConfig.Noclip = ToggleRaw(
                        Ru ? "Проходить сквозь стены" : "Noclip through geometry",
                        Ru ? "Во время полёта отключает столкновения локального персонажа с объектами и невидимыми стенами." : "Disables local character collisions while flying, including invisible blockers.",
                        ModConfig.Noclip);
                    ModConfig.FlySpeed = Slider(Ru ? "Скорость" : "Speed", ModConfig.FlySpeed, 1f, 50f);
                    ModConfig.FlyAcceleration = Slider(Ru ? "Ускорение" : "Accel", ModConfig.FlyAcceleration, 1f, 100f);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(Ru ? "Коорд.:" : "Coords:", Theme.LabelDim, GUILayout.Width(54));
                GUILayout.Label("X", Theme.LabelDim, GUILayout.Width(12)); _tpX = GUILayout.TextField(_tpX ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                GUILayout.Label("Y", Theme.LabelDim, GUILayout.Width(12)); _tpY = GUILayout.TextField(_tpY ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                GUILayout.Label("Z", Theme.LabelDim, GUILayout.Width(12)); _tpZ = GUILayout.TextField(_tpZ ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                if (GUILayout.Button(Ru ? "ТП" : "TP", Theme.DonateBtn, GUILayout.Width(54), GUILayout.Height(26)))
                {
                    if (float.TryParse(_tpX, out float x) & float.TryParse(_tpY, out float y) & float.TryParse(_tpZ, out float z))
                        GameApi.TeleportToCoords(x, y, z);
                }
                GUILayout.EndHorizontal();
                EndSection();
            }

            if (BeginSection("cheats.pranks3", Ru ? "Приколы" : "Pranks", false))
            {
                Character target = TargetPicker(ref _prankTarget);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Наслать недуги" : "Pile on afflictions", Theme.LinkBtn, GUILayout.Height(30))) GameApi.PrankAfflict(target);
                TipLast(Ru ? "Накладывает яд, проклятие, краба, сонливость и паутину на выбранную цель. Для других игроков нужен хост." : "Adds poison, curse, crab, drowsy, and web to the target. Remote targets require host.");
                if (GUILayout.Button(Ru ? "Вылечить" : "Cure", Theme.LinkBtn, GUILayout.Height(30))) GameApi.ClearAllStatus(target);
                TipLast(Ru ? "Очищает все недуги выбранной цели." : "Clears every affliction on the selected target.");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Окаменить" : "Petrify", Theme.LinkBtn, GUILayout.Height(28))) GameApi.PetrifyPlayer(target);
                TipLast(Ru ? "Выставляет окаменение выбранной цели на 100%. Для других игроков нужны права хоста." : "Sets the selected target's petrify amount to 100%. Remote targets require host.");
                if (GUILayout.Button(Ru ? "Снять окаменение" : "Clear petrify", Theme.LinkBtn, GUILayout.Height(28))) GameApi.ClearPetrify(target);
                TipLast(Ru ? "Сбрасывает окаменение выбранной цели." : "Clears petrify from the selected target.");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Добавить стрелу" : "Add arrow", Theme.LinkBtn, GUILayout.Height(28))) GameApi.AddArrowPrank(target, 1);
                TipLast(Ru ? "Надежно работает на своем персонаже; чужому игроку стрелу должен добавить его клиент." : "Works reliably on your own character; a remote target's client owns arrow placement.");
                if (GUILayout.Button(Ru ? "Вытащить стрелы" : "Remove arrows", Theme.LinkBtn, GUILayout.Height(28))) GameApi.ClearArrows(target);
                TipLast(Ru ? "Снимает физические стрелы с выбранной цели. У хоста может запросить снятие у владельца цели." : "Removes physical arrows from the selected target. As host, asks the target owner to remove them.");
                GUILayout.EndHorizontal();

                if (GUILayout.Button(Ru ? "Забить выбранным предметом" : "Stuff with selected item", Theme.DonateBtn, GUILayout.Height(32)))
                    GameApi.FillInventoryWith(target, _selItem);
                TipLast(Ru ? "Заполняет все обычные слоты и рюкзак предметом из вкладки «Инвентарь»." : "Fills every regular slot and backpack slot with the selected Inventory item.");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Забить золотом" : "Fill with gold", Theme.LinkBtn, GUILayout.Height(28)))
                {
                    GameApi.FillInventoryWithGold(target);
                }
                TipLast(Ru ? "Ищет золотой идол по игровому тегу и кладёт его во все слоты цели, включая рюкзак." : "Finds the golden idol by game tag and fills the target's slots, including backpack.");
                if (GUILayout.Button(Ru ? "Забить паутиной" : "Fill with webs", Theme.LinkBtn, GUILayout.Height(28)))
                {
                    GameApi.FillInventoryWithWebs(target);
                }
                TipLast(Ru ? "Пытается найти предмет паутины/паука; если такого предмета нет, накладывает эффект паутины." : "Tries to find a web/spider item; if none exists, applies the web status.");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Случайные предметы" : "Random items", Theme.LinkBtn, GUILayout.Height(28))) GameApi.FillInventoryWithRandom(target);
                TipLast(Ru ? "Заполняет слоты цели случайными предметами из загруженного списка, включая рюкзак." : "Fills the target's slots with random loaded items, including backpack.");
                if (GUILayout.Button(Ru ? "Очистить инвентарь" : "Clear inventory", Theme.LinkBtn, GUILayout.Height(28))) GameApi.ClearInventory(target);
                TipLast(Ru ? "Очищает обычные слоты и рюкзак выбранной цели." : "Clears the selected target's regular slots and backpack.");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "7 шариков" : "7 balloons", Theme.LinkBtn, GUILayout.Height(28))) GameApi.TieBalloons(target, 7);
                TipLast(Ru ? "Привязывает к цели семь игровых шариков, чтобы подъём был заметнее." : "Ties seven in-game balloons to the target for a stronger lift.");
                if (GUILayout.Button(Ru ? "Высыпать вещи" : "Drop items", Theme.LinkBtn, GUILayout.Height(28))) GameApi.DropAllInventory(target);
                TipLast(Ru ? "Выбрасывает содержимое основных слотов и надетый рюкзак рядом с целью." : "Drops main-slot items and the equipped backpack near the target.");
                GUILayout.EndHorizontal();

                GUILayout.Label(Ru ? "Предмет для первого прикола выбирается во вкладке «Инвентарь». Список предметов загружается автоматически." : "The selected item comes from the Inventory tab. The item list loads automatically.", Theme.LabelDim);
                EndSection();
            }
        }

        private static void DrawAdminModern()
        {
            GUILayout.Label(Ru ? "Администрирование" : "Administration", Theme.Section);
            if (!GameApi.IsHost())
                GUILayout.Label(Ru ? "Кик, сессионный бан и автозащита работают у хоста комнаты. Локальные действия и просмотр игроков доступны всегда." : "Kick, session ban, and protection checks work for the room host. Local actions and player inspection are always available.", Theme.LabelDim);

            if (BeginSection("admin.players", Ru ? "Игроки" : "Players", true))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Обновить список" : "Refresh players", Theme.LinkBtn, GUILayout.Height(30)))
                {
                    GameApi.RefreshPlayers();
                    ActionTracker.Track("admin_players_refresh");
                }
                TipLast(Ru ? "Обновить список игроков, не меняя размер панели." : "Refresh the player list without changing the panel size.");
                if (GUILayout.Button(Ru ? "Воскресить всех" : "Revive all", Theme.LinkBtn, GUILayout.Height(30))) GameApi.ReviveAll();
                TipLast(Ru ? "Воскресить всех игроков." : "Revive every player.");
                if (GUILayout.Button(Ru ? "Притянуть всех" : "Bring all", Theme.LinkBtn, GUILayout.Height(30))) GameApi.WarpAllToMe();
                TipLast(Ru ? "Телепортировать остальных игроков к тебе." : "Teleport the other players to you.");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Убить всех" : "Kill all", Theme.DangerBtn, GUILayout.Height(30))) GameApi.KillAll(_excludeSelf);
                TipLast(Ru ? "Убить всех игроков. Переключатель справа исключает тебя из действия." : "Kill all players. The switch on the right excludes you.");
                _excludeSelf = ToggleCompact(Ru ? "Не трогать себя" : "Exclude me", _excludeSelf);
                GUILayout.EndHorizontal();

                if (GameApi.PlayerChars.Count == 0)
                    GUILayout.Label(Ru ? "Список пуст. Нажми «Обновить список» в лобби или забеге." : "Empty. Press Refresh in a lobby or run.", Theme.LabelDim);

                _adminScroll = GUILayout.BeginScrollView(_adminScroll, GUILayout.Height(190));
                for (int i = 0; i < GameApi.PlayerChars.Count; i++)
                    DrawAdminPlayerRow(i);
                GUILayout.EndScrollView();

                Character selected = (_adminTarget >= 0 && _adminTarget < GameApi.PlayerChars.Count) ? GameApi.PlayerChars[_adminTarget] : null;
                if (selected != null)
                    DrawAdminSelectedPlayer(selected);
                EndSection();
            }

            if (BeginSection("admin.protection", Ru ? "Админ-защита" : "Admin protection", false))
            {
                GUILayout.Label(Ru
                    ? "Хостовая защита от подозрительного поведения других игроков. На пользователя панели проверки не действуют."
                    : "Host-side protection against suspicious behavior from other players. The panel user is exempt.",
                    Theme.LabelDim);

                ModConfig.AdminProtectionEnabled = ToggleRaw(
                    Ru ? "Включить админ-защиту" : "Enable admin protection",
                    Ru ? "Включает проверки для других игроков. На пользователя панели проверки не действуют." : "Enables checks for other players. The panel user is exempt.",
                    ModConfig.AdminProtectionEnabled);
                ModConfig.AdminWarnOnly = ToggleRaw(
                    Ru ? "Только предупреждать" : "Warn only",
                    Ru ? "Писать подозрительные события в лог, но не кикать автоматически." : "Write suspicious events to the log without automatic kicks.",
                    ModConfig.AdminWarnOnly);
                ModConfig.AdminDetectExtremeMovement = ToggleRaw(
                    Ru ? "Ловить резкие перемещения" : "Detect extreme movement",
                    Ru ? "Проверяет скорость других игроков и отмечает слишком резкие скачки." : "Measures other players' speed and flags extreme jumps.",
                    ModConfig.AdminDetectExtremeMovement);
                ModConfig.AdminAutoKickSessionBans = ToggleRaw(
                    Ru ? "Держать сессионные баны" : "Enforce session bans",
                    Ru ? "Если забаненный игрок вернётся в комнату, хост снова его кикнет." : "If a session-banned player rejoins, the host kicks them again.",
                    ModConfig.AdminAutoKickSessionBans);

                ModConfig.AdminMaxSpeed = NumberField(Ru ? "Макс. скорость" : "Max speed", ModConfig.AdminMaxSpeed, 5f, 300f);
                ModConfig.AdminSpeedStrikes = Mathf.RoundToInt(NumberField(Ru ? "Срабатываний" : "Strikes", ModConfig.AdminSpeedStrikes, 1f, 10f));
                EndSection();
            }

            DrawManyPlayersAdmin();

            if (BeginSection("admin.lists", Ru ? "Баны, запреты и лог" : "Bans, locks & log", false))
            {
                GUILayout.Label(Ru ? "Сессионный бан-лист" : "Session ban list", Theme.LabelDim);
                var bans = GameApi.SessionBanEntries();
                if (bans.Count == 0)
                    GUILayout.Label(Ru ? "Пока пусто." : "Empty.", Theme.LabelDim);
                for (int i = 0; i < bans.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(bans[i].Label, Theme.RowLabel);
                    if (GUILayout.Button(Ru ? "Разбанить" : "Unban", Theme.LinkBtn, GUILayout.Width(120), GUILayout.Height(28)))
                        GameApi.UnbanSession(bans[i].Key);
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(6);
                GUILayout.Label(Ru ? "Запрет инвентаря" : "Inventory locks", Theme.LabelDim);
                var locks = GameApi.InventoryLockEntries();
                if (locks.Count == 0)
                    GUILayout.Label(Ru ? "Пока пусто." : "Empty.", Theme.LabelDim);
                for (int i = 0; i < locks.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(locks[i].Label, Theme.RowLabel);
                    if (GUILayout.Button(Ru ? "Снять запрет" : "Unlock", Theme.LinkBtn, GUILayout.Width(120), GUILayout.Height(28)))
                        GameApi.UnlockInventoryLock(locks[i].Key);
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(6);
                GUILayout.Label(Ru ? "Последние действия" : "Recent actions", Theme.LabelDim);
                if (GameApi.AdminProtectionLog.Count == 0)
                    GUILayout.Label(Ru ? "Лог пока пуст." : "No log events yet.", Theme.LabelDim);
                for (int i = 0; i < GameApi.AdminProtectionLog.Count; i++)
                    GUILayout.Label(GameApi.AdminProtectionLog[i], Theme.LabelDim);
                EndSection();
            }
        }

        private static void DrawAdminPlayerRow(int index)
        {
            Character c = index >= 0 && index < GameApi.PlayerChars.Count ? GameApi.PlayerChars[index] : null;
            if (c == null) return;
            bool dead = GameApi.IsDead(c);
            bool local = false; try { local = c.IsLocal; } catch { }
            string name = index < GameApi.PlayerNames.Count ? GameApi.PlayerNames[index] : GameApi.PlayerDisplayName(c);
            string prefix = dead ? (Ru ? "[мёртв] " : "[dead] ") : (Ru ? "[жив] " : "[alive] ");
            string label = prefix + name + (local ? (Ru ? " (ты)" : " (you)") : "");
            GUIStyle style = _adminTarget == index ? Theme.ListItemActive : (dead ? Theme.DangerBtn : Theme.ListItem);
            if (GUILayout.Button(label, style, GUILayout.Height(30)))
            {
                _adminTarget = index;
                ActionTracker.Track("admin_player_select", null, new Dictionary<string, object> { ["target"] = name });
            }
            TipLast(GameApi.PlayerDetails(c, Ru));
        }

        private static void DrawAdminSelectedPlayer(Character c)
        {
            GUILayout.Space(6);
            GUILayout.Label((Ru ? "Выбран: " : "Selected: ") + GameApi.PlayerDisplayName(c), Theme.Section);
            GUILayout.Label(GameApi.PlayerDetails(c, Ru), Theme.LabelDim);

            if (string.IsNullOrWhiteSpace(_nicknameInput))
                _nicknameInput = GameApi.LocalNickname();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Ru ? "Ник:" : "Nick:", Theme.LabelDim, GUILayout.Width(54));
            _nicknameInput = GUILayout.TextField(_nicknameInput ?? "", Theme.LinkBtn, GUILayout.Height(28));
            if (GUILayout.Button(Ru ? "Сменить" : "Set", Theme.LinkBtn, GUILayout.Width(88), GUILayout.Height(28)))
                GameApi.SetLocalNickname(_nicknameInput);
            GUILayout.EndHorizontal();
            TipLast(Ru ? "Меняет твой отображаемый Photon-ник. SteamID не меняется." : "Changes your displayed Photon nickname. SteamID is unchanged.");

            GUILayout.BeginHorizontal();
            if (AdminActionButton(Ru ? "Скопировать вид" : "Clone look", Ru ? "Копирует одежду, цвет и косметику выбранного игрока на тебя." : "Copies the selected player's outfit, color, and cosmetics onto you.")) GameApi.CloneLocalAppearanceFrom(c, false);
            if (AdminActionButton(Ru ? "Клон + ник" : "Clone + nick", Ru ? "Копирует внешний вид выбранного игрока и ставит тебе такой же отображаемый ник." : "Copies the selected player's appearance and applies the same displayed nickname to you.")) GameApi.CloneLocalAppearanceFrom(c, true);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (AdminActionButton(Ru ? "Телепорт к нему" : "Teleport to", Ru ? "Переместиться к выбранному игроку." : "Move yourself to the selected player.")) GameApi.WarpToPlayer(c);
            if (AdminActionButton(Ru ? "К себе" : "Bring to me", Ru ? "Притянуть выбранного игрока к тебе." : "Bring the selected player to you.")) GameApi.BringPlayer(c);
            if (AdminActionButton(Ru ? "На спавн" : "To spawn", Ru ? "Отправить выбранного игрока на его точку спавна." : "Send the selected player to their spawn point.")) GameApi.WarpPlayerToSpawn(c);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (AdminActionButton(Ru ? "Убить" : "Kill", Ru ? "Убить выбранного игрока." : "Kill the selected player.", Theme.DangerBtn)) GameApi.KillPlayer(c);
            if (AdminActionButton(Ru ? "Воскресить" : "Revive", Ru ? "Воскресить выбранного игрока." : "Revive the selected player.")) GameApi.RevivePlayer(c);
            if (AdminActionButton(Ru ? "Скаут" : "Scoutmaster", Ru ? "Призвать скаутмастера рядом с выбранным игроком. Работает у хоста." : "Spawn a Scoutmaster near the selected player. Host only.")) GameApi.SpawnScoutmaster(c);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool frozen = GameApi.IsFrozen(c);
            if (AdminActionButton(frozen ? (Ru ? "Фриз: вкл" : "Freeze: on") : (Ru ? "Фриз: выкл" : "Freeze: off"), Ru ? "Первое нажатие замораживает, второе снимает фриз." : "First click freezes, second click unfreezes.", frozen ? Theme.DangerBtn : Theme.LinkBtn)) GameApi.ToggleFreeze(c);
            bool muted = GameApi.IsMuted(c);
            bool local = false; try { local = c.IsLocal; } catch { }
            if (AdminActionButton(muted ? (Ru ? "Мут: вкл" : "Mute: on") : (Ru ? "Мут: выкл" : "Mute: off"), local ? (Ru ? "Мут на себя может игнорироваться игрой/Photon. Эта кнопка в основном для других игроков." : "Self mute may be ignored by the game/Photon. This is mainly for other players.") : (Ru ? "Переключить мут выбранного игрока через сетевое свойство Photon." : "Toggle this player's mute state through Photon properties."), muted ? Theme.DangerBtn : Theme.LinkBtn))
            {
                if (local) GameApi.AddAdminLog(Ru ? "Мут на себя: игра может игнорировать действие" : "Self mute: game may ignore it");
                GameApi.ToggleMute(c);
            }
            bool invLock = GameApi.IsInventoryLocked(c);
            if (AdminActionButton(invLock ? (Ru ? "Инвентарь: запрет" : "Inventory: locked") : (Ru ? "Инвентарь: можно" : "Inventory: allowed"), Ru ? "Запрещает цель держать предметы: найденные предметы будут выкидываться." : "Prevents the target from keeping items by dropping them repeatedly.", invLock ? Theme.DangerBtn : Theme.LinkBtn)) GameApi.ToggleInventoryLock(c);
            GUILayout.EndHorizontal();

            if (local)
                GUILayout.Label(Ru ? "Кик, мут и бан предназначены для других игроков. На себе они могут выглядеть как бездействие." : "Kick, mute, and ban are meant for other players. On yourself they can look like no-op actions.", Theme.LabelDim);

            if (GameApi.IsHost())
            {
                GUILayout.BeginHorizontal();
                if (AdminActionButton(Ru ? "Кикнуть" : "Kick", Ru ? "Кикнуть игрока из текущей комнаты. Только хост." : "Kick this player from the current room. Host only.", Theme.DangerBtn)) GameApi.KickPlayer(c);
                bool banned = GameApi.IsSessionBanned(c);
                if (AdminActionButton(banned ? (Ru ? "Бан: вкл" : "Ban: on") : (Ru ? "Бан: выкл" : "Ban: off"), local ? (Ru ? "Сессионный бан нужен для других игроков. Самого себя игра обычно не кикает." : "Session ban is for other players. The game usually does not kick yourself.") : (Ru ? "Сессионный бан: игрок будет снова кикнут при входе в эту комнату." : "Session ban: the player is kicked again if they rejoin."), banned ? Theme.DangerBtn : Theme.LinkBtn))
                {
                    if (local) GameApi.AddAdminLog(Ru ? "Бан на себя: добавлен в список, самокик может игнорироваться" : "Self ban: listed, self-kick may be ignored");
                    GameApi.ToggleSessionBan(c);
                }
                GUILayout.EndHorizontal();
            }
        }

        private static bool AdminActionButton(string label, string tip, GUIStyle style = null)
        {
            bool clicked = GUILayout.Button(label, style ?? Theme.LinkBtn, GUILayout.Height(30));
            TipLast(tip);
            return clicked;
        }

        private static bool ToggleCompact(string label, bool value)
        {
            Rect r = GUILayoutUtility.GetRect(160f, 30f, GUILayout.Width(178f), GUILayout.Height(30f));
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                value = !value;
            GUI.Label(new Rect(r.x, r.y, r.width - 54f, r.height), label, Theme.LabelDim);
            Rect track = new Rect(r.xMax - 48f, r.y + 3f, 46f, 24f);
            GUI.Box(track, GUIContent.none, value ? Theme.SwitchOn : Theme.SwitchOff);
            float knob = 18f;
            float kx = value ? track.xMax - knob - 3f : track.x + 3f;
            if (Theme.KnobTex != null)
                GUI.DrawTexture(new Rect(kx, track.y + 3f, knob, knob), Theme.KnobTex);
            return value;
        }

        private static void DrawStatusIncreaseButtons(Character target)
        {
            GUILayout.Space(4);
            GUILayout.Label(Ru ? "Быстрое увеличение недугов" : "Quick status increases", Theme.LabelDim);
            if (GUILayout.Button(Ru ? "Увеличить основные недуги" : "Increase common statuses", Theme.LinkBtn, GUILayout.Height(28)))
                GameApi.IncreaseCommonStatuses(target, ModConfig.StatusIncreasePercent / 100f);

            DrawStatusIncreaseRow(target, 0, Ru ? "Травма %" : "Injury %", ref ModConfig.InjuryIncreasePercent);
            DrawStatusIncreaseRow(target, 1, Ru ? "Голод %" : "Hunger %", ref ModConfig.HungerIncreasePercent);
            DrawStatusIncreaseRow(target, 2, Ru ? "Холод %" : "Cold %", ref ModConfig.ColdIncreasePercent);
            DrawStatusIncreaseRow(target, 3, Ru ? "Яд %" : "Poison %", ref ModConfig.PoisonIncreasePercent);
            DrawStatusIncreaseRow(target, 5, Ru ? "Проклятие %" : "Curse %", ref ModConfig.CurseIncreasePercent);
            DrawStatusIncreaseRow(target, 6, Ru ? "Сонливость %" : "Drowsy %", ref ModConfig.DrowsyIncreasePercent);
            DrawStatusIncreaseRow(target, 8, Ru ? "Жара %" : "Hot %", ref ModConfig.HotIncreasePercent);
        }

        private static void DrawStatusIncreaseRow(Character target, int statusIndex, string label, ref float percent)
        {
            percent = NumberField(label, percent, 0f, 100f);
            string[] names = Ru ? GameApi.StatusRu : GameApi.StatusEn;
            string name = statusIndex >= 0 && statusIndex < names.Length ? names[statusIndex] : statusIndex.ToString();
            if (GUILayout.Button("+ " + name, Theme.LinkBtn, GUILayout.Height(28), GUILayout.ExpandWidth(true)))
                GameApi.IncreaseStatus(target, statusIndex, percent / 100f);
            TipLast(Ru ? "Прибавить этот недуг выбранной цели." : "Increase this status on the selected target.");
        }

        private static void DrawInventoryModern3()
        {
            if (BeginSection("inventory.spawn3", Ru ? "Инвентарь и предметы" : "Inventory & items", true))
            {
                GameApi.EnsureItemsLoaded();
                if (_invTarget >= GameApi.PlayerChars.Count) _invTarget = -1;
                Character invChar = InventoryTargetPicker();

                int slots = invChar != null ? GameApi.SlotCountFor(invChar) : GameApi.SlotCount();
                if (slots <= 0) slots = 3;
                if (_selSlot >= slots) _selSlot = Mathf.Max(0, slots - 1);

                GUILayout.BeginHorizontal();
                GUILayout.Label(Ru ? "Слот:" : "Slot:", Theme.LabelDim, GUILayout.Width(54));
                for (int i = 0; i < slots; i++)
                    if (GUILayout.Button((i + 1).ToString(), _selSlot == i ? Theme.ChipActive : Theme.Chip, GUILayout.Width(36), GUILayout.Height(30)))
                        _selSlot = i;
                GUILayout.EndHorizontal();

                GUILayout.Label(Ru ? "Содержимое слотов" : "Slot contents", Theme.LabelDim);
                for (int i = 0; i < slots; i++)
                    GUILayout.Label($"{i + 1}. {GameApi.DescribeSlot(invChar, i)}", Theme.LabelDim);

                GUILayout.BeginHorizontal();
                GUILayout.Label((Ru ? "Предметов: " : "Items: ") + GameApi.ItemNames.Count, Theme.LabelDim);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Ru ? "Перезагрузить список" : "Reload list", Theme.LinkBtn, GUILayout.Height(26)))
                    GameApi.LoadItems();
                TipLast(Ru ? "Заново собрать список предметов из игровых ресурсов." : "Reload the item list from game resources.");
                GUILayout.EndHorizontal();

                _itemSearch = GUILayout.TextField(_itemSearch ?? "", Theme.LinkBtn, GUILayout.Height(26));
                _itemScroll = GUILayout.BeginScrollView(_itemScroll, GUILayout.Height(170));
                string q = (_itemSearch ?? "").Trim().ToLowerInvariant();
                for (int i = 0; i < GameApi.ItemNames.Count; i++)
                {
                    string nm = GameApi.ItemNames[i];
                    if (q.Length > 0 && nm.ToLowerInvariant().IndexOf(q) < 0) continue;
                    if (DrawItemListRow(i, nm, _selItem == i))
                        _selItem = i;
                }
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Заспавнить в слот" : "Spawn to slot", Theme.DonateBtn, GUILayout.Height(34)) && _selItem >= 0)
                    GameApi.SpawnToSlotFor(invChar, _selItem, _selSlot);
                TipLast(Ru ? "Положить выбранный предмет в выбранный основной слот цели." : "Put the selected item into the selected main slot.");
                if (GUILayout.Button(Ru ? "Очистить слот" : "Clear slot", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(34)))
                    GameApi.ClearSlotFor(invChar, _selSlot);
                TipLast(Ru ? "Очистить выбранный основной слот цели." : "Clear the selected main slot.");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Во все слоты" : "Spawn to all", Theme.LinkBtn, GUILayout.Height(30)) && _selItem >= 0)
                    GameApi.FillInventoryWith(invChar, _selItem);
                TipLast(Ru ? "Положить выбранный предмет во все основные слоты и рюкзак цели." : "Put the selected item into every main slot and backpack slot.");
                if (GUILayout.Button(Ru ? "Очистить всё" : "Clear all", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.ClearInventory(invChar);
                TipLast(Ru ? "Очистить все основные слоты и рюкзак выбранной цели." : "Clear all main slots and backpack slots for the selected target.");
                GUILayout.EndHorizontal();

                if (_selItem >= 0 && _selItem < GameApi.ItemNames.Count)
                    GUILayout.Label((Ru ? "Выбрано: " : "Selected: ") + GameApi.ItemNames[_selItem], Theme.LabelDim);
                if (invChar != null)
                    GUILayout.Label((Ru ? "Смотришь и редактируешь инвентарь игрока: " : "Viewing and editing player's inventory: ") + (_invTarget < GameApi.PlayerNames.Count ? GameApi.PlayerNames[_invTarget] : "?"), Theme.LabelDim);
                EndSection();
            }

            if (BeginSection("inventory.backpack3", Ru ? "Рюкзак" : "Backpack", false))
            {
                Character invChar = (_invTarget >= 0 && _invTarget < GameApi.PlayerChars.Count) ? GameApi.PlayerChars[_invTarget] : null;
                GUILayout.Label(Ru ? "Ячейки рюкзака используют предмет, выбранный выше в списке." : "Backpack slots use the item selected above.", Theme.LabelDim);
                int backpackSlots = GameApi.BackpackSlotCount(invChar);
                if (backpackSlots <= 0)
                {
                    GUILayout.Label(Ru ? "Рюкзак не надет или ещё не синхронизирован." : "No backpack equipped or not synced yet.", Theme.LabelDim);
                }
                else
                {
                    if (_selBackpackSlot >= backpackSlots) _selBackpackSlot = Mathf.Max(0, backpackSlots - 1);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Ru ? "Слот:" : "Slot:", Theme.LabelDim, GUILayout.Width(54));
                    for (int i = 0; i < backpackSlots; i++)
                        if (GUILayout.Button((i + 1).ToString(), _selBackpackSlot == i ? Theme.ChipActive : Theme.Chip, GUILayout.Width(36), GUILayout.Height(28)))
                            _selBackpackSlot = i;
                    GUILayout.EndHorizontal();

                    for (int i = 0; i < backpackSlots; i++)
                        GUILayout.Label($"B{i + 1}. {GameApi.DescribeBackpackSlot(invChar, i)}", Theme.LabelDim);

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "В рюкзак" : "To backpack", Theme.DonateBtn, GUILayout.Height(30)) && _selItem >= 0)
                        GameApi.SpawnToBackpackSlotFor(invChar, _selItem, _selBackpackSlot);
                    TipLast(Ru ? "Положить выбранный предмет в выбранную ячейку рюкзака." : "Put the selected item into the selected backpack slot.");
                    if (GUILayout.Button(Ru ? "Очистить" : "Clear", Theme.LinkBtn, GUILayout.Width(110), GUILayout.Height(30)))
                        GameApi.ClearBackpackSlotFor(invChar, _selBackpackSlot);
                    TipLast(Ru ? "Очистить выбранную ячейку рюкзака." : "Clear the selected backpack slot.");
                    GUILayout.EndHorizontal();
                }
                EndSection();
            }

            if (BeginSection("inventory.recharge3", Ru ? "Перезарядка предметов" : "Item recharge", false))
            {
                Character invChar = (_invTarget >= 0 && _invTarget < GameApi.PlayerChars.Count) ? GameApi.PlayerChars[_invTarget] : null;
                int slots = invChar != null ? GameApi.SlotCountFor(invChar) : GameApi.SlotCount();
                if (slots <= 0) slots = 3;
                ModConfig.UnlimitedItemUses = ToggleRaw(
                    Ru ? "Бесконечные использования предметов" : "Unlimited item uses",
                    Ru ? "Не даёт предметам тратить количество использований." : "Prevents items from spending their use count.",
                    ModConfig.UnlimitedItemUses);
                ModConfig.InfiniteItems = ToggleRaw(
                    Ru ? "Автоперезарядка предметов" : "Auto recharge items",
                    Ru ? "Периодически выставляет заряд/прочность предметов в слотах на значение ниже." : "Periodically sets charge/durability in slots to the value below.",
                    ModConfig.InfiniteItems);
                ModConfig.UnlimitedLanternFuel = ToggleRaw(
                    Ru ? "Бесконечное топливо фонаря" : "Unlimited lantern fuel",
                    Ru ? "Фонари не расходуют топливо и держатся заполненными." : "Lanterns do not spend fuel and stay filled.",
                    ModConfig.UnlimitedLanternFuel);
                if (!ModConfig.UnlimitedLanternFuel)
                    ModConfig.LanternFuelConsumptionPercent = Slider(Ru ? "Расход топлива фонаря %" : "Lantern fuel cost %", ModConfig.LanternFuelConsumptionPercent, 0f, 300f);
                ModConfig.RechargeValue = NumberField(Ru ? "Заряд" : "Charge", ModConfig.RechargeValue, 0f, 999f);
                GUILayout.BeginHorizontal();
                GUILayout.Label(Ru ? "Слот:" : "Slot:", Theme.LabelDim, GUILayout.Width(54));
                for (int i = 0; i < slots; i++)
                    if (GUILayout.Button((i + 1).ToString(), _selSlot == i ? Theme.ChipActive : Theme.Chip, GUILayout.Width(36), GUILayout.Height(28)))
                        _selSlot = i;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Зарядить выбранный" : "Recharge selected", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.RechargeSlotFor(invChar, _selSlot, ModConfig.RechargeValue);
                TipLast(Ru ? "Выставить заряд/прочность выбранного основного слота." : "Set charge/durability for the selected main slot.");
                if (GUILayout.Button(Ru ? "Зарядить все" : "Recharge all", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(30)))
                    for (int s = 0; s < slots; s++) GameApi.RechargeSlotFor(invChar, s, ModConfig.RechargeValue);
                TipLast(Ru ? "Выставить заряд/прочность всех основных слотов." : "Set charge/durability for all main slots.");
                GUILayout.EndHorizontal();

                int backpackSlots = GameApi.BackpackSlotCount(invChar);
                if (backpackSlots > 0)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "Зарядить слот рюкзака" : "Recharge backpack slot", Theme.LinkBtn, GUILayout.Height(30)))
                        GameApi.RechargeBackpackSlotFor(invChar, _selBackpackSlot, ModConfig.RechargeValue);
                    TipLast(Ru ? "Выставить заряд/прочность выбранной ячейки рюкзака." : "Set charge/durability for the selected backpack slot.");
                    if (GUILayout.Button(Ru ? "Весь рюкзак" : "All backpack", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(30)))
                        for (int s = 0; s < backpackSlots; s++) GameApi.RechargeBackpackSlotFor(invChar, s, ModConfig.RechargeValue);
                    TipLast(Ru ? "Выставить заряд/прочность всех предметов в рюкзаке." : "Set charge/durability for every backpack item.");
                    GUILayout.EndHorizontal();
                }
                EndSection();
            }
        }

        private static void DrawWorldModern()
        {
            float runSecsNow = GameApi.ExpeditionTimeSeconds();
            float timeOfDayNow = GameApi.TimeOfDay();
            int dayNow = GameApi.DayCount();
            if (Event.current.type == EventType.Repaint)
                SyncTimeOfDayInput(timeOfDayNow);

            if (BeginSection("world.time", Ru ? "Время и забег" : "Time & run", true))
            {
                GUILayout.Label((Ru ? "Время забега: " : "Run time: ") + FormatRunTime(runSecsNow), Theme.LabelDim);
                GUILayout.Label((Ru ? "День: " : "Day: ") + dayNow + "   " + (Ru ? "Час: " : "Hour: ") + timeOfDayNow.ToString("0.0"), Theme.LabelDim);

                bool holdRunTime = Toggle(Ru ? "Держать время забега" : "Hold run timer", ModConfig.OverrideExpeditionTime);
                if (holdRunTime != ModConfig.OverrideExpeditionTime)
                {
                    if (holdRunTime && runSecsNow > 0.01f)
                    {
                        ModConfig.ExpeditionTimeSeconds = runSecsNow;
                        SetNumberText(Ru ? "Секунды забега" : "Run seconds", 0f, 7200f, ModConfig.ExpeditionTimeSeconds);
                        GameApi.SetExpeditionTime(ModConfig.ExpeditionTimeSeconds);
                    }
                    ModConfig.OverrideExpeditionTime = holdRunTime;
                }

                ModConfig.ExpeditionTimeSeconds = NumberField(Ru ? "Секунды забега" : "Run seconds", ModConfig.ExpeditionTimeSeconds, 0f, 7200f);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "-60 сек" : "-60 sec", Theme.LinkBtn, GUILayout.Height(28)))
                {
                    ModConfig.ExpeditionTimeSeconds = Mathf.Max(0f, ModConfig.ExpeditionTimeSeconds - 60f);
                    GameApi.SetExpeditionTime(ModConfig.ExpeditionTimeSeconds);
                }
                TipLast(Ru ? "Уменьшить удерживаемое время забега на минуту." : "Decrease the held run time by one minute.");
                if (GUILayout.Button(Ru ? "Применить" : "Apply", Theme.DonateBtn, GUILayout.Height(28)))
                    GameApi.SetExpeditionTime(ModConfig.ExpeditionTimeSeconds);
                TipLast(Ru ? "Применить указанное время забега сейчас." : "Apply the entered run time now.");
                if (GUILayout.Button(Ru ? "+60 сек" : "+60 sec", Theme.LinkBtn, GUILayout.Height(28)))
                {
                    ModConfig.ExpeditionTimeSeconds += 60f;
                    GameApi.SetExpeditionTime(ModConfig.ExpeditionTimeSeconds);
                }
                TipLast(Ru ? "Увеличить удерживаемое время забега на минуту." : "Increase the held run time by one minute.");
                GUILayout.EndHorizontal();

                float editedTime = NumberField(TimeOfDayLabel(), _timeOfDay, 0f, 24f);
                if (Mathf.Abs(editedTime - _timeOfDay) > 0.001f)
                {
                    _timeOfDay = editedTime;
                    _timePreset = PresetForHour(_timeOfDay);
                    _timeOfDayDirty = true;
                }
                GUILayout.BeginHorizontal();
                TimePresetButton(Ru ? "Рассвет" : "Dawn", 6f, 0, Ru ? "Выбрать рассвет. Применится после кнопки ниже." : "Choose dawn. Applies after the button below.");
                TimePresetButton(Ru ? "День" : "Day", 12f, 1, Ru ? "Выбрать день. Применится после кнопки ниже." : "Choose daytime. Applies after the button below.");
                TimePresetButton(Ru ? "Закат" : "Dusk", 19f, 2, Ru ? "Выбрать закат. Применится после кнопки ниже." : "Choose dusk. Applies after the button below.");
                TimePresetButton(Ru ? "Ночь" : "Night", 23f, 3, Ru ? "Выбрать ночь. Применится после кнопки ниже." : "Choose night. Applies after the button below.");
                GUILayout.EndHorizontal();
                if (GUILayout.Button(Ru ? "Применить время суток" : "Apply time of day", Theme.DonateBtn, GUILayout.Height(30)))
                    ApplySelectedTimeOfDay();
                TipLast(Ru ? "Применить указанное время суток." : "Apply the entered time of day.");
                EndSection();
            }

            if (BeginSection("world.modifiers", Ru ? "Модификаторы мира" : "World modifiers", false))
            {
                ModConfig.GameSpeedMod = ToggleRaw(
                    Ru ? "Скорость игры" : "Game speed",
                    Ru ? "Меняет общий Time.timeScale игры. Используй аккуратно." : "Changes the game's global Time.timeScale. Use with care.",
                    ModConfig.GameSpeedMod);
                if (ModConfig.GameSpeedMod)
                    ModConfig.GameSpeed = Slider("x", ModConfig.GameSpeed, 0.05f, 5f);

                ModConfig.PingHandSizeMultiplier = Slider(Ru ? "Размер руки пинга" : "Ping hand size", ModConfig.PingHandSizeMultiplier, 0.1f, 10f);
                EndSection();
            }

            if (BeginSection("world.finish", Ru ? "Финиш" : "Finish", false))
            {
                GUILayout.Label(Ru ? "Мгновенно завершает текущий забег победой." : "Instantly ends the current run as a win.", Theme.LabelDim);
                if (GUILayout.Button(Ru ? "Мгновенно пройти" : "Force win", Theme.DonateBtn, GUILayout.Height(30)))
                    GameApi.ForceWin();
                EndSection();
            }

            if (BeginSection("world.luggage", Ru ? "Контейнеры рядом" : "Nearby containers", false))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Обновить (300 м)" : "Refresh (300 m)", Theme.LinkBtn, GUILayout.Height(26)))
                { GameApi.RefreshLuggage(); _selLuggage = -1; }
                if (GUILayout.Button(Ru ? "Открыть все рядом" : "Open all nearby", Theme.DonateBtn, GUILayout.Height(26)))
                    GameApi.OpenAllNearbyLuggage();
                GUILayout.EndHorizontal();

                if (GameApi.LuggageLabels.Count == 0)
                    GUILayout.Label(Ru ? "Список пуст — нажми «Обновить» во время забега." : "Empty — press Refresh during a run.", Theme.LabelDim);
                else
                {
                    _luggageScroll = GUILayout.BeginScrollView(_luggageScroll, GUILayout.Height(150));
                    for (int i = 0; i < GameApi.LuggageLabels.Count; i++)
                        if (GUILayout.Button(GameApi.LuggageLabels[i], _selLuggage == i ? Theme.ListItemActive : Theme.ListItem, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                            _selLuggage = i;
                    GUILayout.EndScrollView();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Ru ? "Открыть выбранный" : "Open selected", Theme.LinkBtn, GUILayout.Height(28)))
                    { if (_selLuggage >= 0) GameApi.OpenLuggage(_selLuggage); }
                    if (GUILayout.Button(Ru ? "Телепорт к нему" : "Warp to it", Theme.LinkBtn, GUILayout.Height(28)))
                    { if (_selLuggage >= 0) GameApi.WarpToLuggage(_selLuggage); }
                    GUILayout.EndHorizontal();
                }
                EndSection();
            }
        }

        private static readonly Color BadgeLockedModern = new Color(0.85f, 0.27f, 0.27f);
        private static Texture2D _badgeTileOn, _badgeTileOff, _badgeTileBg, _badgeStripeOn, _badgeStripeOff;

        private static void DrawBadgesModern()
        {
            var all = SteamAch.AllTypes;
            int total = all.Length;
            RefreshBadgeCache(false);
            int got = 0;
            for (int i = 0; i < total; i++)
            {
                if (_badgeUnlockedCache != null && i < _badgeUnlockedCache.Length && _badgeUnlockedCache[i])
                    got++;
            }

            GUILayout.Label(L("tab.badges"), Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            GUILayout.BeginHorizontal();
            GUILayout.Label((Ru ? "Получено: " : "Unlocked: ") + got + " / " + total, Theme.LabelDim);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Ru ? "Выдать все" : "Unlock all", Theme.DonateBtn, GUILayout.Height(28)))
            {
                SteamAch.UnlockAll();
                RefreshBadgeCache(true);
            }
            if (GUILayout.Button(Ru ? "Отозвать все" : "Revoke all", Theme.LinkBtn, GUILayout.Height(28)))
            {
                SteamAch.RevokeAll();
                RefreshBadgeCache(true);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(Ru ? "Лёгкий список: маленькая иконка, название, статус и отдельная кнопка действия."
                               : "Lightweight list: small icon, name, status, and a dedicated action button.", Theme.LabelDim);

            if (_badgeTileBg == null) _badgeTileBg = Theme.RoundedTex(Theme.Panel, 10);
            if (_badgeTileOn == null) _badgeTileOn = Theme.RoundedTex(Theme.AccentDim, 10);
            if (_badgeTileOff == null) _badgeTileOff = Theme.RoundedTex(Theme.PanelLight, 10);
            if (_badgeStripeOn == null) _badgeStripeOn = Theme.RoundedTex(Theme.Accent, 6);
            if (_badgeStripeOff == null) _badgeStripeOff = Theme.RoundedTex(BadgeLockedModern, 6);

            float scrollHeight = Mathf.Clamp(_rect.height - 240f, 180f, 440f);

            _badgeScroll = GUILayout.BeginScrollView(_badgeScroll, GUILayout.Height(scrollHeight));
            for (int i = 0; i < total; i++)
            {
                var t = all[i];
                bool on = _badgeUnlockedCache != null && i < _badgeUnlockedCache.Length && _badgeUnlockedCache[i];
                Rect r = GUILayoutUtility.GetRect(10f, 42f, GUILayout.ExpandWidth(true), GUILayout.Height(42f));
                bool hover = Event.current != null && r.Contains(Event.current.mousePosition);

                if (Event.current.type == EventType.Repaint)
                {
                    GUI.Box(r, GUIContent.none, hover ? Theme.RowHover : (on ? Theme.ListItemActive : Theme.ListItem));
                    GUI.DrawTexture(new Rect(r.x + 6f, r.y + 6f, 4f, r.height - 12f), on ? _badgeStripeOn : _badgeStripeOff);
                    var tex = SteamAch.Icon(t);
                    Rect iconRect = new Rect(r.x + 16f, r.y + 7f, 28f, 28f);
                    if (tex != null)
                    {
                        Color prev = GUI.color;
                        GUI.color = on ? Color.white : new Color(1f, 1f, 1f, 0.58f);
                        GUI.DrawTexture(iconRect, tex, ScaleMode.ScaleToFit, true);
                        GUI.color = prev;
                    }
                    else
                    {
                        GUI.Label(iconRect, "?", ItemIconFallbackStyle());
                    }

                    string name = SteamAch.DisplayName(t);
                    GUI.Label(new Rect(r.x + 52f, r.y + 2f, r.width - 220f, 22f), name, Theme.RowLabel);
                    GUI.Label(new Rect(r.x + 52f, r.y + 21f, r.width - 220f, 18f),
                        on ? (Ru ? "Получено" : "Unlocked") : (Ru ? "Не получено" : "Locked"),
                        Theme.LabelDim);
                }

                string tip = SteamAch.DisplayName(t);
                string desc = SteamAch.Desc(t);
                if (!string.IsNullOrEmpty(desc)) tip += "\n" + desc;
                if (hover)
                    _hoverTip = tip;

                Rect actionRect = new Rect(r.xMax - 112f, r.y + 7f, 104f, 28f);
                string action = on ? (Ru ? "Отозвать" : "Revoke") : (Ru ? "Выдать" : "Unlock");
                if (GUI.Button(actionRect, action, on ? Theme.LinkBtn : Theme.DonateBtn))
                {
                    SteamAch.Toggle(t);
                    RefreshBadgeCache(true);
                    ActionTracker.Track(on ? "badge_revoke_one" : "badge_unlock_one", null,
                        new Dictionary<string, object> { ["badge"] = t.ToString() });
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static void RefreshBadgeCache(bool force)
        {
            var all = SteamAch.AllTypes;
            if (!force
                && _badgeTypeCache == all
                && _badgeUnlockedCache != null
                && _badgeUnlockedCache.Length == all.Length
                && Time.realtimeSinceStartup - _badgeCacheTime < 0.75f)
                return;

            _badgeTypeCache = all;
            if (_badgeUnlockedCache == null || _badgeUnlockedCache.Length != all.Length)
                _badgeUnlockedCache = new bool[all.Length];

            for (int i = 0; i < all.Length; i++)
                _badgeUnlockedCache[i] = SteamAch.IsUnlocked(all[i]);

            _badgeCacheTime = Time.realtimeSinceStartup;
        }

        private static void DrawTooltipOverlayModern()
        {
            string tip = HoverTooltipText();
            if (string.IsNullOrEmpty(tip))
                return;

            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            DrawTooltipBox(tip, mouse, false);
        }

        private static bool ToggleModern(string key, bool value)
        {
            Rect r = GUILayoutUtility.GetRect(10, 40, GUILayout.ExpandWidth(true));
            bool hover = Event.current != null && r.Contains(Event.current.mousePosition);

            if (hover && Event.current.type == EventType.Repaint)
                GUI.Box(r, GUIContent.none, Theme.RowHover);
            if (hover)
                _hoverTip = Localization.D(key);

            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            GUI.Label(new Rect(r.x + 12, r.y, r.width - 72, r.height), L(key), Theme.RowLabel);

            const float sw = 46f;
            const float sh = 24f;
            Rect track = new Rect(r.xMax - sw - 12f, r.y + (r.height - sh) * 0.5f, sw, sh);
            GUI.Box(track, GUIContent.none, value ? Theme.SwitchOn : Theme.SwitchOff);

            float kn = sh - 6f;
            float kx = value ? track.xMax - kn - 3f : track.x + 3f;
            if (Theme.KnobTex != null)
                GUI.DrawTexture(new Rect(kx, track.y + 3f, kn, kn), Theme.KnobTex);

            if (clicked) value = !value;
            return value;
        }

        private static bool ToggleRaw(string label, string tip, bool value)
        {
            Rect r = GUILayoutUtility.GetRect(10, 40, GUILayout.ExpandWidth(true));
            bool hover = Event.current != null && r.Contains(Event.current.mousePosition);

            if (hover && Event.current.type == EventType.Repaint)
                GUI.Box(r, GUIContent.none, Theme.RowHover);
            if (hover)
                _hoverTip = tip;

            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            GUI.Label(new Rect(r.x + 12, r.y, r.width - 72, r.height), label, Theme.RowLabel);

            const float sw = 46f;
            const float sh = 24f;
            Rect track = new Rect(r.xMax - sw - 12f, r.y + (r.height - sh) * 0.5f, sw, sh);
            GUI.Box(track, GUIContent.none, value ? Theme.SwitchOn : Theme.SwitchOff);

            float kn = sh - 6f;
            float kx = value ? track.xMax - kn - 3f : track.x + 3f;
            if (Theme.KnobTex != null)
                GUI.DrawTexture(new Rect(kx, track.y + 3f, kn, kn), Theme.KnobTex);

            if (clicked) value = !value;
            return value;
        }

        private static Rect DefaultWindowRect()
        {
            return new Rect(80f, 80f, 680f, 580f);
        }

        private static void CenterWindow()
        {
            _rect.x = Mathf.Round((Screen.width - _rect.width) * 0.5f);
            _rect.y = Mathf.Round((Screen.height - _rect.height) * 0.5f);
            ClampWindowToScreen();
        }

        private static void ClampWindowToScreen()
        {
            float margin = 8f;
            float maxWidth = Mathf.Max(420f, Screen.width - margin * 2f);
            float maxHeight = Mathf.Max(340f, Screen.height - margin * 2f);
            float minWidth = Mathf.Min(MinWindowWidth, maxWidth);
            float minHeight = Mathf.Min(MinWindowHeight, maxHeight);

            _rect.width = Mathf.Clamp(_rect.width, minWidth, maxWidth);
            _rect.height = Mathf.Clamp(_rect.height, minHeight, maxHeight);
            _rect.x = Mathf.Clamp(_rect.x, margin, Screen.width - _rect.width - margin);
            _rect.y = Mathf.Clamp(_rect.y, margin, Screen.height - _rect.height - margin);
        }

        private static void HandleWindowResize(Event e)
        {
            if (e == null)
                return;

            if (_resizeEdge != ResizeEdge.None && !Input.GetMouseButton(0))
            {
                _resizeEdge = ResizeEdge.None;
                return;
            }

            if (_resizeEdge == ResizeEdge.None)
            {
                if (e.type != EventType.MouseDown || e.button != 0)
                    return;

                ResizeEdge hit = HitResizeEdge(e.mousePosition);
                if (hit == ResizeEdge.None)
                    return;

                _resizeEdge = hit;
                _resizeStartRect = _rect;
                _resizeStartMouse = e.mousePosition;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseUp)
            {
                _resizeEdge = ResizeEdge.None;
                e.Use();
                return;
            }

            if (e.type != EventType.MouseDrag)
                return;

            float margin = 8f;
            float maxWidth = Mathf.Max(420f, Screen.width - margin * 2f);
            float maxHeight = Mathf.Max(340f, Screen.height - margin * 2f);
            float minWidth = Mathf.Min(MinWindowWidth, maxWidth);
            float minHeight = Mathf.Min(MinWindowHeight, maxHeight);
            Vector2 delta = e.mousePosition - _resizeStartMouse;

            float xMin = _resizeStartRect.xMin;
            float xMax = _resizeStartRect.xMax;
            float yMin = _resizeStartRect.yMin;
            float yMax = _resizeStartRect.yMax;

            if ((_resizeEdge & ResizeEdge.Left) != 0)
                xMin = Mathf.Clamp(_resizeStartRect.xMin + delta.x, margin, _resizeStartRect.xMax - minWidth);
            if ((_resizeEdge & ResizeEdge.Right) != 0)
                xMax = Mathf.Clamp(_resizeStartRect.xMax + delta.x, _resizeStartRect.xMin + minWidth, Screen.width - margin);
            if ((_resizeEdge & ResizeEdge.Top) != 0)
                yMin = Mathf.Clamp(_resizeStartRect.yMin + delta.y, margin, _resizeStartRect.yMax - minHeight);
            if ((_resizeEdge & ResizeEdge.Bottom) != 0)
                yMax = Mathf.Clamp(_resizeStartRect.yMax + delta.y, _resizeStartRect.yMin + minHeight, Screen.height - margin);

            _rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            _rect.width = Mathf.Clamp(_rect.width, minWidth, maxWidth);
            _rect.height = Mathf.Clamp(_rect.height, minHeight, maxHeight);
            e.Use();
        }

        private static ResizeEdge HitResizeEdge(Vector2 mouse)
        {
            bool withinX = mouse.x >= _rect.xMin - ResizeGrip && mouse.x <= _rect.xMax + ResizeGrip;
            bool withinY = mouse.y >= _rect.yMin - ResizeGrip && mouse.y <= _rect.yMax + ResizeGrip;
            if (!withinX || !withinY)
                return ResizeEdge.None;

            ResizeEdge hit = ResizeEdge.None;
            if (Mathf.Abs(mouse.x - _rect.xMin) <= ResizeGrip) hit |= ResizeEdge.Left;
            if (Mathf.Abs(mouse.x - _rect.xMax) <= ResizeGrip) hit |= ResizeEdge.Right;
            if (Mathf.Abs(mouse.y - _rect.yMin) <= ResizeGrip) hit |= ResizeEdge.Top;
            if (Mathf.Abs(mouse.y - _rect.yMax) <= ResizeGrip) hit |= ResizeEdge.Bottom;
            return hit;
        }

        private static void DrawTooltipOverlay()
        {
            if (UseModernUi)
            {
                DrawTooltipOverlayModern();
                return;
            }
            string tip = GUI.tooltip;
            if (string.IsNullOrEmpty(tip))
                return;

            Vector2 mouse = Event.current != null ? Event.current.mousePosition : new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            RememberTooltip(tip);
            DrawTooltipBox(tip, mouse, true);
        }

        /// <summary>One-time donation reminder, shown once per game session unless disabled in config.</summary>
        public static void DrawDonateNotice()
        {
            Theme.EnsureBuilt();
            SyncThemeTextureCache();
            if (_noticeDismissed || !ModConfig.ShowDonateNotice.Value)
                return;
            ApplyFont();
            float noticeWidth = 420f;
            float noticeHeight = _langOpen ? Mathf.Min(660f, Screen.height - 80f) : Mathf.Min(520f, Screen.height - 80f);
            _noticeRect = GUILayout.Window(1, _noticeRect, DrawNoticeWindowModern, GUIContent.none, Theme.Window,
                GUILayout.Width(noticeWidth), GUILayout.Height(noticeHeight));
        }

        private static void DrawNoticeWindow(int id)
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal(); GUILayout.Space(12);
            GUILayout.BeginVertical();
            GUILayout.Label("PEAK-MX ♥", Theme.Title);
            GUILayout.Label(SupportText(), Theme.DonateText);
            GUILayout.Space(6);
            GUILayout.Label(L("ui.language"), Theme.LabelDim);
            DrawLanguagePicker();
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(DonateTextLabel(), Theme.DonateBtn, GUILayout.Width(150)))
            { OpenUrl(UrlDonate); _noticeDismissed = true; }
            if (GUILayout.Button(CloseTextLabel(), Theme.CloseBtn, GUILayout.Width(90)))
                _noticeDismissed = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
            DrawQr(_langOpen ? 118 : 92);
            DrawDonationSupportPanel(true);
            GUILayout.Space(8);
            GUILayout.EndVertical(); GUILayout.Space(12);
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        private static void DrawNoticeWindowModern(int id)
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUILayout.BeginVertical();
            GUILayout.Label("PEAK-MX ♥", Theme.Title);
            GUILayout.Label(SupportText(), Theme.DonateText);
            GUILayout.Space(6);
            GUILayout.Label(L("ui.language"), Theme.LabelDim);
            DrawLanguagePicker();
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(DonateTextLabel(), Theme.DonateBtn, GUILayout.Width(150)))
            {
                OpenUrl(UrlDonate);
                _noticeDismissed = true;
            }
            if (GUILayout.Button(Ru ? "Закрыть" : "Close", Theme.CloseBtn, GUILayout.Width(90)))
                _noticeDismissed = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
            DrawQr(_langOpen ? 118 : 92);
            DrawDonationSupportPanel(true);
            GUILayout.Space(8);
            GUILayout.EndVertical();
            GUILayout.Space(12);
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        // ---------- widgets ----------
        private static bool BeginSection(string key, string title, bool defaultOpen)
        {
            if (!_sectionOpen.TryGetValue(key, out bool open))
                open = defaultOpen;

            GUILayout.BeginVertical(Theme.Card);
            string prefix = open ? "▼ " : "► ";
            var style = open ? Theme.FoldoutBtnOpen : Theme.FoldoutBtn;
            if (GUILayout.Button(prefix + title, style, GUILayout.Height(32)))
                open = !open;
            _sectionOpen[key] = open;

            if (!open)
            {
                GUILayout.EndVertical();
                return false;
            }

            GUILayout.Space(8);
            return true;
        }

        private static void EndSection()
        {
            GUILayout.EndVertical();
        }

        // Modern iOS-style switch row: label on the left, pill+knob toggle on the right,
        // full-row click target, hover highlight, and a tooltip pulled from the description.
        private static bool Toggle(string key, bool value)
        {
            if (UseModernUi)
                return ToggleModern(key, value);
            Rect r = GUILayoutUtility.GetRect(10, 40, GUILayout.ExpandWidth(true));
            bool hover = r.Contains(Event.current.mousePosition);

            if (hover && Event.current.type == EventType.Repaint)
                GUI.Box(r, GUIContent.none, Theme.RowHover);

            bool clicked = GUI.Button(r, new GUIContent("", Localization.D(key)), GUIStyle.none);

            GUI.Label(new Rect(r.x + 12, r.y, r.width - 72, r.height), L(key), Theme.RowLabel);

            const float sw = 46f, sh = 24f;
            Rect track = new Rect(r.xMax - sw - 12f, r.y + (r.height - sh) * 0.5f, sw, sh);
            GUI.Box(track, GUIContent.none, value ? Theme.SwitchOn : Theme.SwitchOff);

            float kn = sh - 6f;
            float kx = value ? track.xMax - kn - 3f : track.x + 3f;
            if (Theme.KnobTex != null)
                GUI.DrawTexture(new Rect(kx, track.y + 3f, kn, kn), Theme.KnobTex);

            if (clicked) value = !value;
            return value;
        }

        private static float NumberField(string label, float value, float min, float max)
        {
            string key = NumberKey(label, min, max);
            if (!_numberText.TryGetValue(key, out string text))
                text = value.ToString("0.##");

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, Theme.LabelDim, GUILayout.Width(120));
            string next = GUILayout.TextField(text, Theme.LinkBtn, GUILayout.Width(96), GUILayout.Height(28));
            GUILayout.Label(min.ToString("0.##") + " - " + max.ToString("0.##"), Theme.LabelDim);
            GUILayout.EndHorizontal();

            if (float.TryParse(next, out float parsed))
                value = Mathf.Clamp(parsed, min, max);

            string normalized = value.ToString("0.##");
            _numberText[key] = next == text && next != normalized ? normalized : next;
            return value;
        }

        private static string NumberKey(string label, float min, float max)
            => label + "|" + min.ToString("0.###") + "|" + max.ToString("0.###");

        private static void SetNumberText(string label, float min, float max, float value)
        {
            _numberText[NumberKey(label, min, max)] = value.ToString("0.##");
        }

        private static float Slider(string label, float value, float min, float max)
        {
            Rect r = GUILayoutUtility.GetRect(10, 34, GUILayout.ExpandWidth(true), GUILayout.Height(34));
            GUI.Label(new Rect(r.x, r.y, 120f, r.height), $"{label}: {value:0.##}", Theme.LabelDim);

            Rect track = new Rect(r.x + 124f, r.y + (r.height - 8f) * 0.5f, Mathf.Max(40f, r.width - 140f), 8f);
            Rect hit = new Rect(track.x, r.y + 4f, track.width, r.height - 8f);
            Event e = Event.current;
            if (e != null && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && hit.Contains(e.mousePosition))
            {
                float t = Mathf.Clamp01((e.mousePosition.x - track.x) / Mathf.Max(1f, track.width));
                value = Mathf.Lerp(min, max, t);
                e.Use();
            }

            GUI.Box(track, GUIContent.none, Theme.SliderBar);
            float normalized = Mathf.InverseLerp(min, max, value);
            Rect fill = new Rect(track.x, track.y, Mathf.Clamp01(normalized) * track.width, track.height);
            if (_sliderFillTex == null) _sliderFillTex = Theme.Tex(Theme.Accent);
            GUI.DrawTexture(fill, _sliderFillTex);

            float thumb = 18f;
            Rect knob = new Rect(track.x + normalized * track.width - thumb * 0.5f, track.center.y - thumb * 0.5f, thumb, thumb);
            GUI.Box(knob, GUIContent.none, Theme.SliderThumb);
            return Mathf.Clamp(value, min, max);
        }

        private static void TipLast(string tip)
        {
            if (Event.current == null || string.IsNullOrWhiteSpace(tip))
                return;
            Rect r = GUILayoutUtility.GetLastRect();
            if (r.Contains(Event.current.mousePosition))
                _hoverTip = tip;
        }

        private static bool DrawItemListRow(int index, string name, bool active)
        {
            Rect r = GUILayoutUtility.GetRect(10, 32, GUILayout.ExpandWidth(true), GUILayout.Height(32));
            bool clicked = GUI.Button(r, GUIContent.none, active ? Theme.ListItemActive : Theme.ListItem);
            Texture2D icon = GameApi.ItemIcon(index);
            Rect iconRect = new Rect(r.x + 7f, r.y + 4f, 24f, 24f);
            float textX = r.x + 38f;
            if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
            }
            else
            {
                GUI.Box(iconRect, GUIContent.none, active ? Theme.ChipActive : Theme.Chip);
                string letter = string.IsNullOrWhiteSpace(name) ? "?" : name.Substring(0, 1).ToUpperInvariant();
                GUI.Label(iconRect, letter, ItemIconFallbackStyle());
            }

            GUI.Label(new Rect(textX, r.y, r.width - (textX - r.x) - 10f, r.height), name, active ? Theme.RowLabel : Theme.Label);
            if (r.Contains(Event.current.mousePosition))
                _hoverTip = Ru ? "Выбрать предмет для спавна или приколов." : "Select this item for spawning or pranks.";
            return clicked;
        }

        private static GUIStyle ItemIconFallbackStyle()
        {
            if (_itemIconText == null)
            {
                _itemIconText = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                };
            }
            return _itemIconText;
        }

        private static void DrawTooltipFooter(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, Theme.Panel9);
            string text = FooterTooltipText();
            Rect textRect = new Rect(rect.x + 12f, rect.y + 7f, rect.width - 24f, rect.height - 12f);
            DrawScrollableTooltipText(textRect, text, Theme.TipText);
        }

        private static string DefaultTooltipText()
        {
            return Ru
                ? "Наведи курсор на кнопку, предмет или игрока - подсказка появится здесь."
                : "Hover a button, item, or player - the hint appears here.";
        }

        private static string HoverTooltipText()
        {
            if (string.IsNullOrWhiteSpace(_hoverTip))
                return null;
            RememberTooltip(_hoverTip);
            return _hoverTip;
        }

        private static string FooterTooltipText()
        {
            string hover = HoverTooltipText();
            if (!string.IsNullOrWhiteSpace(hover))
                return hover;
            if (!string.IsNullOrWhiteSpace(_lastTip) && Time.realtimeSinceStartup <= _lastTipUntil)
                return _lastTip;
            return DefaultTooltipText();
        }

        private static void RememberTooltip(string tip)
        {
            if (string.IsNullOrWhiteSpace(tip))
                return;
            if (!string.Equals(_lastTip, tip, StringComparison.Ordinal))
                _tipScroll = Vector2.zero;
            _lastTip = tip;
            _lastTipUntil = Time.realtimeSinceStartup + 0.30f;
        }

        private static void DrawTooltipBox(string tip, Vector2 mouse, bool avoidWindow)
        {
            if (string.IsNullOrWhiteSpace(tip))
                return;

            float width = Mathf.Min(560f, Mathf.Max(320f, Screen.width * 0.44f));
            float maxHeight = Mathf.Max(140f, Screen.height * 0.72f);
            float innerWidth = width - 24f;
            float needed = Theme.Tooltip.CalcHeight(new GUIContent(tip), innerWidth) + 22f;
            float height = Mathf.Clamp(needed, 44f, maxHeight);

            float x = mouse.x + 18f;
            float y = mouse.y + 20f;
            if (x + width > Screen.width - 10f) x = mouse.x - width - 18f;
            if (y + height > Screen.height - 10f) y = mouse.y - height - 18f;

            Rect tipRect = new Rect(x, y, width, height);
            if (avoidWindow && tipRect.Overlaps(_rect))
            {
                float rightX = _rect.xMax + 14f;
                float leftX = _rect.xMin - width - 14f;
                float belowY = _rect.yMax + 12f;
                float aboveY = _rect.yMin - height - 12f;

                if (rightX + width <= Screen.width - 10f) tipRect.x = rightX;
                else if (leftX >= 10f) tipRect.x = leftX;
                else if (belowY + height <= Screen.height - 10f) tipRect.y = belowY;
                else if (aboveY >= 10f) tipRect.y = aboveY;
            }

            tipRect.x = Mathf.Clamp(tipRect.x, 10f, Screen.width - width - 10f);
            tipRect.y = Mathf.Clamp(tipRect.y, 10f, Screen.height - height - 10f);

            int prevDepth = GUI.depth;
            GUI.depth = -10000;
            GUI.Box(tipRect, GUIContent.none, Theme.Tooltip);
            DrawScrollableTooltipText(new Rect(tipRect.x + 12f, tipRect.y + 10f, tipRect.width - 24f, tipRect.height - 18f), tip, Theme.Tooltip);
            GUI.depth = prevDepth;
        }

        private static void DrawScrollableTooltipText(Rect rect, string text, GUIStyle style)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            float scrollBar = 16f;
            float textHeight = style.CalcHeight(new GUIContent(text), Mathf.Max(1f, rect.width - scrollBar));
            if (textHeight <= rect.height)
            {
                GUI.Label(rect, text, style);
                return;
            }

            Rect view = new Rect(0f, 0f, Mathf.Max(1f, rect.width - scrollBar), textHeight);
            _tipScroll = GUI.BeginScrollView(rect, _tipScroll, view, GUIStyle.none, GUI.skin.verticalScrollbar);
            GUI.Label(view, text, style);
            GUI.EndScrollView();
        }

        private static void LinkButton(string label, string url)
        {
            if (GUILayout.Button(label, Theme.LinkBtn, GUILayout.Height(28)))
                OpenUrl(url);
        }

        // Target row: «Себе» + each non-local player + refresh. Returns the chosen
        // Character (the local character when «Себе» is selected). Wraps onto multiple
        // rows so long names never squash the buttons.
        private static Character TargetPicker(ref int sel)
        {
            if (sel >= GameApi.PlayerChars.Count) sel = -1;
            GUILayout.BeginHorizontal();
            GUILayout.Label(Ru ? "Цель:" : "Target:", Theme.LabelDim, GUILayout.Width(54));
            if (GUILayout.Button(TargetName(sel), Theme.LinkBtn, GUILayout.Height(28)))
                _targetPickerOpen = !_targetPickerOpen;
            TipLast(Ru ? "Открыть список игроков и выбрать цель действия." : "Open the player list and choose a target.");
            if (GUILayout.Button(Ru ? "Обновить" : "Refresh", Theme.LinkBtn, GUILayout.Width(86), GUILayout.Height(28)))
            {
                GameApi.RefreshPlayers();
                ActionTracker.Track("players_refresh_picker");
            }
            GUILayout.EndHorizontal();

            if (_targetPickerOpen)
            {
                if (GUILayout.Button(Ru ? "Себе" : "Me", sel < 0 ? Theme.ListItemActive : Theme.ListItem, GUILayout.Height(28)))
                {
                    sel = -1;
                    _targetPickerOpen = false;
                    ActionTracker.Track("target_select", null, new Dictionary<string, object> { ["target"] = "self" });
                }
                for (int i = 0; i < GameApi.PlayerChars.Count; i++)
                {
                    var pc = GameApi.PlayerChars[i];
                    bool me = false; try { me = pc != null && pc.IsLocal; } catch { }
                    if (me) continue;
                    string nm = i < GameApi.PlayerNames.Count ? GameApi.PlayerNames[i] : "?";
                    if (GUILayout.Button(nm, sel == i ? Theme.ListItemActive : Theme.ListItem, GUILayout.Height(28)))
                    {
                        sel = i;
                        _targetPickerOpen = false;
                        ActionTracker.Track("target_select", null, new Dictionary<string, object> { ["target"] = nm });
                    }
                    TipLast(GameApi.PlayerDetails(pc, Ru));
                }
            }
            return (sel >= 0 && sel < GameApi.PlayerChars.Count) ? GameApi.PlayerChars[sel] : Character.localCharacter;
        }

        private static string TargetName(int sel)
        {
            if (sel >= 0 && sel < GameApi.PlayerNames.Count)
                return GameApi.PlayerNames[sel];
            return Ru ? "Себе" : "Me";
        }

        private static Character InventoryTargetPicker()
        {
            if (_invTarget >= GameApi.PlayerChars.Count) _invTarget = -1;
            GUILayout.BeginHorizontal();
            GUILayout.Label(Ru ? "Кому:" : "Target:", Theme.LabelDim, GUILayout.Width(54));
            if (GUILayout.Button(TargetName(_invTarget), Theme.LinkBtn, GUILayout.Height(28)))
                _inventoryTargetPickerOpen = !_inventoryTargetPickerOpen;
            TipLast(Ru ? "Выбрать, чей инвентарь смотреть и редактировать." : "Choose whose inventory to inspect and edit.");
            if (GUILayout.Button(Ru ? "Обновить" : "Refresh", Theme.LinkBtn, GUILayout.Width(86), GUILayout.Height(28)))
            {
                GameApi.RefreshPlayers();
                ActionTracker.Track("players_refresh_inventory");
            }
            GUILayout.EndHorizontal();

            if (_inventoryTargetPickerOpen)
            {
                if (GUILayout.Button(Ru ? "Себе" : "Me", _invTarget < 0 ? Theme.ListItemActive : Theme.ListItem, GUILayout.Height(28)))
                {
                    _invTarget = -1;
                    _inventoryTargetPickerOpen = false;
                    ActionTracker.Track("inventory_target_select", null, new Dictionary<string, object> { ["target"] = "self" });
                }

                for (int i = 0; i < GameApi.PlayerChars.Count; i++)
                {
                    var pc = GameApi.PlayerChars[i];
                    bool me = false; try { me = pc != null && pc.IsLocal; } catch { }
                    if (me) continue;
                    string nm = i < GameApi.PlayerNames.Count ? GameApi.PlayerNames[i] : "?";
                    if (GUILayout.Button(nm, _invTarget == i ? Theme.ListItemActive : Theme.ListItem, GUILayout.Height(28)))
                    {
                        _invTarget = i;
                        _inventoryTargetPickerOpen = false;
                        ActionTracker.Track("inventory_target_select", null, new Dictionary<string, object> { ["target"] = nm });
                    }
                    TipLast(GameApi.PlayerDetails(pc, Ru));
                }
            }

            return (_invTarget >= 0 && _invTarget < GameApi.PlayerChars.Count) ? GameApi.PlayerChars[_invTarget] : null;
        }

        private static Texture2D _lineTex;
        private static Texture2D _sliderFillTex;
        private static Texture2D _headerTex;
        private static Texture2D _accentTex;
        private static Texture2D _brandIconTex;
        private static GUIStyle _brandIconText;
        private static GUIStyle _itemIconText;

        private static void SyncThemeTextureCache(bool force = false)
        {
            if (!force && _themeVersion == Theme.Version)
                return;

            _themeVersion = Theme.Version;
            _lineTex = null;
            _sliderFillTex = null;
            _headerTex = null;
            _accentTex = null;
            _brandIconTex = null;
            _tileBg = null;
            _frameOn = null;
            _frameOff = null;
            _badgeTileOn = null;
            _badgeTileOff = null;
            _badgeTileBg = null;
            _badgeStripeOn = null;
            _badgeStripeOff = null;
        }

        private static void HLine()
        {
            if (_lineTex == null) _lineTex = Theme.Tex(Theme.PanelLight);
            var r = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(r, _lineTex);
        }

        private static void DrawBrandMark(float size)
        {
            if (_brandIconTex == null)
                _brandIconTex = BuildBrandIcon(64);
            if (_brandIconText == null)
            {
                _brandIconText = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                };
            }

            Rect r = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            GUI.DrawTexture(r, _brandIconTex, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(r.x, r.y + size * 0.45f, r.width, size * 0.34f), "MX", _brandIconText);
        }

        private static Texture2D BuildBrandIcon(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            Color bg = Theme.Bg;
            Color border = Theme.Accent;
            Color mountain = Theme.AccentDim;
            Color peak = new Color(1.000f, 0.851f, 0.400f, 1f);
            float radius = size * 0.18f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - center.x) - (size * 0.5f - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - center.y) - (size * 0.5f - radius), 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    Color col = bg;

                    bool edge = x < 3 || y < 3 || x >= size - 3 || y >= size - 3;
                    if (edge) col = border;

                    float ny = (float)y / (size - 1);
                    float nx = (float)x / (size - 1);
                    float leftSlope = Mathf.InverseLerp(0.12f, 0.50f, nx);
                    float rightSlope = Mathf.InverseLerp(0.88f, 0.50f, nx);
                    float mountainLine = Mathf.Min(leftSlope, rightSlope);
                    if (ny < mountainLine * 0.52f + 0.18f && ny > 0.15f)
                        col = mountain;
                    if (ny < mountainLine * 0.18f + 0.18f && nx > 0.40f && nx < 0.60f)
                        col = peak;

                    col.a *= alpha;
                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply();
            return tex;
        }

        private static void OpenUrl(string url)
        {
            try { Application.OpenURL(url); }
            catch (Exception e) { Plugin.Log?.LogWarning($"OpenURL failed: {e.Message}"); }
        }

        private static void DrawDonationSupportPanel(bool compact)
        {
            const int CollapsedDonationRows = 3;
            DonationSupport.Refresh();
            DonationGoal goal = DonationSupport.Goal;
            var supporters = DonationSupport.Supporters;
            var latest = DonationSupport.Latest;

            GUILayout.Space(6);
            GUILayout.Label(Ru ? "Спасибо за поддержку" : "Thanks for the support", Theme.Section);

            if (goal.Target.HasValue && goal.Target.Value > 0.01d)
            {
                double percent = goal.Percent ?? (goal.Raised / goal.Target.Value * 100d);
                GUILayout.Label(
                    (Ru ? $"За {DonationSupport.HiddenOlderThanDays} дней: " : $"Last {DonationSupport.HiddenOlderThanDays} days: ")
                    + FormatDonationAmount(goal.Raised, goal.Currency)
                    + " / "
                    + FormatDonationAmount(goal.Target.Value, goal.Currency)
                    + " ("
                    + percent.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                    + "%)",
                    Theme.LabelDim);
                DrawDonationProgressBar((float)Mathf.Clamp((float)(percent / 100d), 0f, 1f), compact ? 10f : 12f);
            }
            else if (goal.Raised > 0.01d)
            {
                GUILayout.Label((Ru ? "Уже собрано: " : "Raised: ") + FormatDonationAmount(goal.Raised, goal.Currency), Theme.LabelDim);
            }

            GUILayout.Label(
                Ru
                    ? $"Сбор считает донаты за {DonationSupport.HiddenOlderThanDays} дней; именные донатеры показываются от {DonationSupport.MinPublicAmount:0} RUB суммарно, anonymous - отдельными платежами."
                    : $"The goal counts donations from the last {DonationSupport.HiddenOlderThanDays} days; named supporters are shown from {DonationSupport.MinPublicAmount:0} RUB total, anonymous payments stay separate.",
                Theme.LabelDim);

            if (!DonationSupport.HasLoaded && DonationSupport.IsLoading)
            {
                GUILayout.Label(Ru ? "Загружаю список донатеров..." : "Loading supporters...", Theme.LabelDim);
                return;
            }

            int latestCount = latest != null ? latest.Count : 0;
            if (latestCount > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label(Ru ? "Последние донаты" : "Latest donations", Theme.Label);
                int latestMax = _showAllDonors ? latestCount : Mathf.Min(CollapsedDonationRows, latestCount);
                for (int i = 0; i < latestMax; i++)
                {
                    var donation = latest[i];
                    string when = string.IsNullOrWhiteSpace(donation.LastDonationAt) ? "" : " · " + ShortDonationDate(donation.LastDonationAt);
                    GUILayout.Label(
                        (i + 1)
                        + ". "
                        + donation.Name
                        + " - "
                        + FormatDonationAmount(donation.Amount, donation.Currency)
                        + when,
                        Theme.Label);
                }
            }

            if (latestCount <= 0)
            {
                GUILayout.Label(Ru ? "Пока нет донатов для списка спасибо." : "No recent supporters to show yet.", Theme.LabelDim);
                return;
            }

            if (latestCount > CollapsedDonationRows)
            {
                string text = _showAllDonors
                    ? (Ru ? "Свернуть список" : "Collapse list")
                    : (Ru ? $"Показать всех ({latestCount})" : $"Show all ({latestCount})");
                if (GUILayout.Button(text, Theme.LinkBtn, GUILayout.Height(26)))
                    _showAllDonors = !_showAllDonors;
            }
        }

        private static string ShortDonationDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            string text = value.Replace("T", " ").Replace("Z", "");
            return text.Length > 16 ? text.Substring(0, 16) : text;
        }

        private static string FormatDonationAmount(double amount, string currency)
        {
            string value = amount.ToString(amount >= 100 ? "0" : "0.##", System.Globalization.CultureInfo.InvariantCulture);
            string cur = string.IsNullOrWhiteSpace(currency) ? "RUB" : currency;
            if (string.Equals(cur, "RUB", StringComparison.OrdinalIgnoreCase))
                cur = "RUB";
            return value + " " + cur;
        }

        private static void DrawDonationProgressBar(float fill, float height)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, height, GUILayout.ExpandWidth(true));
            GUI.Box(rect, GUIContent.none, Theme.Panel9);
            Rect inner = new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, (rect.width - 4f) * fill), Mathf.Max(0f, rect.height - 4f));
            if (inner.width > 1f)
                GUI.DrawTexture(inner, Theme.Tex(Theme.DonateHi));
        }

        // ---------- donation QR (lazy-loaded from the web, no extra dependencies) ----------
        private static byte[] _qrBytes;
        private static bool _qrRequested;
        private static Texture2D _qrTex;

        private static Texture2D QrTexture()
        {
            if (_qrTex != null)
                return _qrTex;

            if (!_qrRequested)
            {
                _qrRequested = true;
                Task.Run(() =>
                {
                    try
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        using var c = new WebClient();
                        _qrBytes = c.DownloadData(UrlQr);
                    }
                    catch (Exception e) { Plugin.Log?.LogDebug($"[QR] {e.Message}"); }
                });
            }

            // Texture must be created on the main thread (here, inside OnGUI).
            if (_qrBytes != null)
            {
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                if (LoadImage(t, _qrBytes))
                    _qrTex = t;
                _qrBytes = null;
            }
            return _qrTex;
        }

        // Invoke UnityEngine.ImageConversion.LoadImage via reflection so we don't take a
        // compile-time reference on ImageConversionModule (which would drag in netstandard 2.1).
        private static System.Reflection.MethodInfo _loadImage;
        private static bool _loadImageResolved;
        private static bool LoadImage(Texture2D tex, byte[] data)
        {
            if (!_loadImageResolved)
            {
                _loadImageResolved = true;
                var type = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                _loadImage = type?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });
            }
            if (_loadImage == null)
                return false;
            try { return (bool)_loadImage.Invoke(null, new object[] { tex, data }); }
            catch (Exception e) { Plugin.Log?.LogDebug($"[QR] LoadImage: {e.Message}"); return false; }
        }

        private static void DrawQr(float size)
        {
            var tex = QrTexture();
            if (tex != null)
                GUILayout.Box(tex, GUILayout.Width(size), GUILayout.Height(size));
        }
    }
}
