using System;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace PeakMX
{
    /// <summary>
    /// PEAK-MX overlay drawn with Unity IMGUI (no third-party UI libraries).
    /// Original layout and styling by maxkir041.
    /// </summary>
    public static class Menu
    {
        // maxkir041's links — opened in the default browser on click.
        private const string UrlSteam = "https://steamcommunity.com/id/everyng/";
        private const string UrlTelegram = "https://t.me/maxkir041";
        private const string UrlPlayground = "https://users.playground.ru/7293247/";
        private const string UrlDonate = "https://www.donationalerts.com/r/maxkir041";
        private const string UrlGitHub = "https://github.com/maxkir041/PEAK-MX";
        private const string UrlQr = "https://files.donationalerts.com/uploads/qr/8215314/qr_6de7aa0b93950f375ec25fdfd0bd0c53.png";

        private static readonly string[] Tabs =
            { "tab.character", "tab.cheats", "tab.inventory", "tab.world", "tab.badges", "tab.cosmetics", "tab.about" };

        private static Rect _rect = new Rect(80, 80, 560, 580);
        private static Rect _noticeRect = new Rect(40, 40, 360, 0);
        private static int _tab;
        private static Vector2 _scroll;
        private static bool _noticeDismissed;

        private static string L(string key) => Localization.T(key);
        private static bool Ru => Localization.Current == Lang.Russian;

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
            ApplyFont();
            _rect = GUI.Window(0xAC10, _rect, DrawWindow, GUIContent.none, Theme.Window);
        }

        private static void DrawWindow(int id)
        {
            // Header band
            if (_headerTex == null)
                _headerTex = Theme.GradientTex(new Color(0.118f, 0.302f, 0.220f), new Color(0.078f, 0.157f, 0.122f), 60);
            if (_accentTex == null) _accentTex = Theme.Tex(Theme.Accent);
            GUI.DrawTexture(new Rect(0, 0, _rect.width, 60), _headerTex);
            GUI.DrawTexture(new Rect(0, 60, _rect.width, 2), _accentTex);
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
                case 2: DrawInventory(); break;
                case 3: DrawWorld(); break;
                case 4: DrawBadges(); break;
                case 5: DrawCosmetics(); break;
                case 6: DrawAbout(); break;
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
            GUILayout.Label(Ru ? "🥺 Мод бесплатный и я делаю его один... поддержи хоть капельку — «О моде» → Задонатить ❤️"
                               : "🥺 This mod is free and made solo... please support even a little — About → Donate ❤️", Theme.DonateText);

            GUI.DragWindow(new Rect(0, 0, 100000, 60));
        }

        // ---------- tabs ----------
        private static void DrawCharacter()
        {
            GUILayout.Label(L("tab.character"), Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            ModConfig.SpeedMod = Toggle("feat.speed", ModConfig.SpeedMod);
            if (ModConfig.SpeedMod) ModConfig.SpeedAmount = Slider("x", ModConfig.SpeedAmount, 0.5f, 5f);
            ModConfig.JumpMod = Toggle("feat.jump", ModConfig.JumpMod);
            if (ModConfig.JumpMod) ModConfig.JumpAmount = Slider("x", ModConfig.JumpAmount, 1f, 5f);
            GUILayout.EndVertical();

            GUILayout.Label(L("wip"), Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            ModConfig.ClimbMod = Toggle("feat.climb", ModConfig.ClimbMod);
            if (ModConfig.ClimbMod) ModConfig.ClimbAmount = Slider("x", ModConfig.ClimbAmount, 1f, 5f);
            ModConfig.VineClimbMod = Toggle("feat.vineclimb", ModConfig.VineClimbMod);
            ModConfig.RopeClimbMod = Toggle("feat.ropeclimb", ModConfig.RopeClimbMod);
            GUILayout.EndVertical();
        }

        private static void DrawCheats()
        {
            GUILayout.Label(L("tab.cheats"), Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            ModConfig.GodMode = Toggle("feat.god", ModConfig.GodMode);
            ModConfig.InfiniteStamina = Toggle("feat.infstam", ModConfig.InfiniteStamina);
            ModConfig.NoFallDamage = Toggle("feat.nofall", ModConfig.NoFallDamage);
            ModConfig.NoWeight = Toggle("feat.noweight", ModConfig.NoWeight);
            ModConfig.LockStatus = Toggle("feat.lockstatus", ModConfig.LockStatus);
            GUILayout.EndVertical();

            GUILayout.Label(L("wip"), Theme.Section);
            GUILayout.BeginVertical(Theme.Card);
            ModConfig.TeleportToPing = Toggle("feat.tpping", ModConfig.TeleportToPing);
            ModConfig.Fly = Toggle("feat.fly", ModConfig.Fly);
            if (ModConfig.Fly)
            {
                ModConfig.FlySpeed = Slider(Ru ? "Скорость" : "Speed", ModConfig.FlySpeed, 1f, 50f);
                ModConfig.FlyAcceleration = Slider(Ru ? "Ускорение" : "Accel", ModConfig.FlyAcceleration, 1f, 100f);
            }
            GUILayout.EndVertical();
        }

        private static void DrawInventory()
        {
            GUILayout.Label(L("tab.inventory") + "  (" + L("wip") + ")", Theme.Section);
            ModConfig.RechargeSlot1 = Slider("Slot 1", ModConfig.RechargeSlot1, 0f, 999f);
            ModConfig.RechargeSlot2 = Slider("Slot 2", ModConfig.RechargeSlot2, 0f, 999f);
            ModConfig.RechargeSlot3 = Slider("Slot 3", ModConfig.RechargeSlot3, 0f, 999f);
        }

        private static void DrawWorld()
        {
            GUILayout.Label(L("tab.world") + "  (" + L("wip") + ")", Theme.Section);
            ModConfig.OverrideExpeditionTime = Toggle(Ru ? "Переопределить время" : "Override time", ModConfig.OverrideExpeditionTime);
            ModConfig.ExpeditionTimeSeconds = Slider(Ru ? "Время (сек)" : "Time (sec)", ModConfig.ExpeditionTimeSeconds, 0f, 7200f);
            GUILayout.Label(Ru ? "Только для своих/кооп-каток — не подделывай рекорды."
                               : "For your own/co-op runs only — don't fake records.", Theme.LabelDim);
        }

        private static void DrawBadges()
        {
            GUILayout.Label(L("tab.badges") + "  (" + L("wip") + ")", Theme.Section);
            GUILayout.Label(Ru ? "Список бейджей с переключателями и «разблокировать всё»."
                               : "Badge list with unlock toggles and 'unlock all'.", Theme.LabelDim);
        }

        private static void DrawCosmetics()
        {
            GUILayout.Label(L("tab.cosmetics") + "  (" + L("wip") + ")", Theme.Section);
            GUILayout.Label(Ru ? "«Разблокировать все наряды» для своего персонажа."
                               : "'Unlock all outfits' for your own character.", Theme.LabelDim);
        }

        private static void DrawAbout()
        {
            GUILayout.Label("PEAK-MX  v" + Plugin.Version, Theme.Section);
            GUILayout.Label(Ru ? "Автор: maxkir041" : "Author: maxkir041", Theme.Label);

            GUILayout.Space(6);
            GUILayout.Label(L("ui.language"), Theme.Section);
            DrawLanguagePicker();

            GUILayout.Space(6);
            GUILayout.Label(Ru ? "Ссылки:" : "Links:", Theme.Section);
            GUILayout.BeginHorizontal();
            LinkButton("GitHub", UrlGitHub);
            LinkButton("Steam", UrlSteam);
            LinkButton("Telegram", UrlTelegram);
            LinkButton("Playground", UrlPlayground);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label(Ru
                ? "Я делаю PEAK-MX совсем один и бесплатно, тратя на него всё свободное время и силы... 🥺 "
                  + "Если он тебе хоть немножко помог — пожалуйста, поддержи хоть чуть-чуть. Любая копеечка очень важна и придаёт сил продолжать ❤️"
                : "I make PEAK-MX entirely alone and for free, pouring all my spare time into it... 🥺 "
                  + "If it helped you even a little, please support me however you can. Every bit truly means a lot ❤️",
                Theme.DonateText);
            if (GUILayout.Button(Ru ? "Задонатить" : "Donate", Theme.DonateBtn, GUILayout.Width(160)))
                OpenUrl(UrlDonate);
            GUILayout.Space(6);
            DrawQr(150);
            GUILayout.Label(Ru ? "Сканируй QR для доната" : "Scan the QR to donate", Theme.LabelDim);

            GUILayout.Space(6);
            bool show = ModConfig.ShowDonateNotice.Value;
            bool newShow = GUILayout.Toggle(show, Ru ? " Показывать напоминание при запуске" : " Show reminder on launch", Theme.Label);
            if (newShow != show) ModConfig.ShowDonateNotice.Value = newShow;

            bool stats = ModConfig.AllowAnonymousStats.Value;
            bool newStats = GUILayout.Toggle(stats, Ru ? " Анонимная статистика установок" : " Anonymous install stats", Theme.Label);
            if (newStats != stats) ModConfig.AllowAnonymousStats.Value = newStats;

            string installs = Stats.InstallCount.HasValue ? Stats.InstallCount.Value.ToString() : "…";
            GUILayout.Label((Ru ? "Установок: " : "Installs: ") + installs, Theme.LabelDim);

            GUILayout.Space(6);
            GUILayout.Label(Ru
                ? "Неофициальный фанатский мод. Не связан с разработчиками PEAK. Сторонние библиотеки — под своими лицензиями."
                : "Unofficial fan-made mod. Not affiliated with PEAK's developers. Third-party libraries keep their own licenses.",
                Theme.LabelDim);
        }

        private static void DrawLanguagePicker()
        {
            int perRow = 0;
            GUILayout.BeginHorizontal();
            foreach (var (lang, name) in Localization.Options)
            {
                bool active = lang == Localization.Current;
                if (GUILayout.Button(name, active ? Theme.NavItemActive : Theme.LinkBtn, GUILayout.Height(26)))
                {
                    Localization.Current = lang;
                    ModConfig.Language.Value = (int)lang;
                }
                if (++perRow % 3 == 0) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>One-time donation reminder, shown once per game session unless disabled in config.</summary>
        public static void DrawDonateNotice()
        {
            if (_noticeDismissed || !ModConfig.ShowDonateNotice.Value)
                return;
            Theme.EnsureBuilt();
            ApplyFont();
            _noticeRect = GUILayout.Window(1, _noticeRect, DrawNoticeWindow, GUIContent.none, Theme.Window,
                GUILayout.Width(360));
        }

        private static void DrawNoticeWindow(int id)
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal(); GUILayout.Space(12);
            GUILayout.BeginVertical();
            GUILayout.Label("PEAK-MX ♥", Theme.Title);
            GUILayout.Label(Ru
                ? "Спасибо, что играешь с PEAK-MX 🥺 Мод полностью бесплатный, и я очень стараюсь ради вас, "
                  + "ночами в одиночку... Поддержи меня хоть капельку — это правда помогает не опускать руки ❤️"
                : "Thank you for using PEAK-MX 🥺 It's completely free and I work on it alone, late at night... "
                  + "Please support me even a tiny bit — it truly keeps me going ❤️",
                Theme.DonateText);
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Задонатить" : "Donate", Theme.DonateBtn, GUILayout.Width(150)))
            { OpenUrl(UrlDonate); _noticeDismissed = true; }
            if (GUILayout.Button(Ru ? "Закрыть" : "Close", Theme.CloseBtn, GUILayout.Width(90)))
                _noticeDismissed = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
            DrawQr(140);
            GUILayout.Space(4);
            bool dont = GUILayout.Toggle(!ModConfig.ShowDonateNotice.Value,
                Ru ? " Больше не показывать" : " Don't show again", Theme.Label);
            if (dont == ModConfig.ShowDonateNotice.Value) // changed
            { ModConfig.ShowDonateNotice.Value = !dont; if (dont) _noticeDismissed = true; }
            GUILayout.Space(8);
            GUILayout.EndVertical(); GUILayout.Space(12);
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        // ---------- widgets ----------
        // Modern iOS-style switch row: label on the left, pill+knob toggle on the right,
        // full-row click target, hover highlight, and a tooltip pulled from the description.
        private static bool Toggle(string key, bool value)
        {
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

        private static float Slider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:0.##}", Theme.LabelDim, GUILayout.Width(120));
            value = GUILayout.HorizontalSlider(value, min, max, Theme.SliderBar, Theme.SliderThumb);
            GUILayout.EndHorizontal();
            return value;
        }

        private static void LinkButton(string label, string url)
        {
            if (GUILayout.Button(label, Theme.LinkBtn, GUILayout.Height(28)))
                OpenUrl(url);
        }

        private static Texture2D _lineTex;
        private static Texture2D _headerTex;
        private static Texture2D _accentTex;
        private static void HLine()
        {
            if (_lineTex == null) _lineTex = Theme.Tex(Theme.PanelLight);
            var r = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(r, _lineTex);
        }

        private static void OpenUrl(string url)
        {
            try { Application.OpenURL(url); }
            catch (Exception e) { Plugin.Log?.LogWarning($"OpenURL failed: {e.Message}"); }
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
