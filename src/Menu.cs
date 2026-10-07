using System;
using System.Net;
using System.Collections.Generic;
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
        private const string EmbeddedDonateQrPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAfQAAAH0CAAAAADuvYBWAAAABGdBTUEAALGPC/xhBQAAACBjSFJNAAB6JgAAgIQAAPoAAACA6AAAdTAAAOpgAAA6mAAAF3CculE8AAAAAmJLR0QA/4ePzL8AAA4bSURBVHja7Z19bFbVGcDft28LNGV8DRZkM5QxMMYSZxW3MdC8urLsgw+zqDi3OoRtycJWs4Qt6DTZktIlJMYiMZkr8pEYcImj4GI2HDBlLpFq3ULNQkVoQ2QmbWw7C+1K3/fd3/c5JM89p+e+99729/vvPHnOc849v/feW3Iv52YycdFcClLwUrW+pNFm9GlT+9TrAx8qWdMc19JXZGDKgXSkA9IB6YB0QDogHZAOSAekA9IB6YB0QDogHVQqJ9sB/VQGnsmVZdw331RTdmeTK30winFe36BecxzGHayVkc5OGXnag/STasalFXqVVlX60bujWPs5Yc702fH8/hyGHUzM1C7FtgYqQ9zTAelIB6QD0gHpgHRAOiAdkA5IB6QD0gHpMGFCvERx+T2Huvd6+DWdHpWROx0ePf5NT3lNzWiIx07xhEOnWxZ5kP7eWoeRh2smfshPt8vIWVV6fasIPKXPfqs+lfFcLNJHXJb+uA/p6WI1V2/u6YB0QDrSAemAdEA6IB2QDkgHpAPSAemAdPBOyh6t9sc07oAMfDqL9EhYdbOMrPBRdocMtMhA/ssi8JcFMqVnMdIjYbsMHPZSdvOyYPt9Q3pmp5TOPR2QDkgHpAPSAemAdEA6IB2QDkgHpAPSAelTm0n3/9NLPlJKSE8Tnfqla7kMzJWBUz6uf3ORnmR2bQm2r03zUfXuI9zTAemAdEA6IB2QDkgHpAPSAemAdKQD0gHpgHRIL0l+nv7Lj5I7t6f0lEdkYH82PdLvHXao6+ETLpnudhk5uyTY7q+VGfVv2I8zXbSr9APe2yQjh9aJwEyj0z5r6TUuS1/tQ3pFTXJOMDGV/mh+bTUeOo1Gcbzc0wHpgHRAOiAd6YB0QDogHZAOSAekA9IB6RAT13m0+npMUzmmp1y4YF9lnXiK3WXU+HxdsF16xai6PjlrEI30DfE4L5rjdoj2eSOldZUIrDRSxqqC7be2yow2IX3cnMl4rixrsCG2Mz051M0Q0s2UO4LNAtdu7umAdEA60gHpgHRAOiAdkA5IB6QD0gHpgHSIgEQ/Wh3wUaS/TJP9T4qkN8c18s16yiIZMCb7BR9FakU71+xwQXzIYQXiWvtsYj5MVAzxcsrZOiWhEOLK1b1s4pPd3eTQqZCUeyn3dP6QA6QD0gHpgHRAOiAdkA5IB6QD0gHpgHSwpbLo8MPw0icb28ctiglZ+1KpPKegcbyVIZ5in14dbPcv0PsMi28SHDM2WWh+XASWGEUu+jhko+zySKpeVDMMWp6QkaNRbHMyNMeQnpwbjbEXzH1epJ8T24/s3eqj6qnFwXbXCplx6xHu6YB0QDogHZAOSAekA9JBl15iDaae9CxrwOUdkA5IB6QD0iEdxPc8veM++z5PysD6zZPKxr59asofxT+3dv3DQfqInjNdtOeH6CM+xJFZJ/sMLNKLyD7txh4fedHOmVMT71BkfvCwCFwyXqXJv2p9fC784uci0LXSvshAu5qybZeU7jJ7hz5Zp1WaEUGPXC5TFqUhTrhyXWZncE8HpCMdkA5IB6QD0gHpgHRAOiAdkA5IB6TDBKk8bIS+I55AXzjjUPd+8QDz3LsORdqte5Re8rIqh9WMb33KvsimpEi/zmvvg7OD7dfWOtTVtx9x4awMzP1ssF0o1yPqHrETxeh5mWFsTaF/zuNt4yWKxu0yIj9o8niLzDh+gwhUL5VnerouTHVJndgMObPR+A5Y7cM9nT/kAOmAdEA6IB2QDsmVzk4UU1A6O1FweQekA9IB6YB0SAnXebQ6OOihbn//JF+4Xg9rdKU8w/aGkF4rA63247zjUKQpo/aZq164WiMxfKBTXSQHnn9CPeB5MnDbgNpHbvEx5DTX0yVrGo0iHVqXgjnwSCkh1Hv56RRE1WYj46g6E/Nn36d1GeSeDkhHOiAdkA5IB6QD0gHpgHRAOiAdkA5IhwkT6v+ny6eexnvTBYciGR99HN7gLhUzPg4nGuTI2YoySTceHq+RgR07xTpW6kUcvlSRqVYzWn9mXfSFrTLStiXYvjZNPxydTocDNrbraDwgAkuNn2zWi/STPrYfeSOS7Udi40zOtsdodSQT6eCeDkgHpAPSAelIB6QD0gHpgHRAOiAdkA5IB6RDWaicXIdTaHDo9OKLiT2eS/dEIn3MCInv9mS+Nubh8rBuzL7PA+32fU7pKd21wXbP8igWdsaYw1X16DeC7bGZ0ZzpVWpKtsrDOF6KeKIquaOITmPRTI17On/IAdIB6YB0QDogHZAOSAekA9IB6YB0QDrYYr5EcazPR93Dk2uZ9vsoskXN6Nsbk/TX9viouzUeObluGfnxKfsq+d+JwCYvh7NZXFYfvV8kDBl7tDT+Siu6r0VGjteKwHJderpZlqAqGgsXisDbfmYi+gxxTwekIx2QDkgHpAPSAemAdEA6IB2QDkgHpMOEmWyPVt/3lBMFH8jA0gr7uS6LS/qOpWVZpD+1y0ibDMj3AwrmrhJqnxA0XVNTjNcsNn5bBJqNgS/foFU9eFBGitm4zvQt5ZFuRL5UZ1+lUezu4PJGUqOa0aUvUjP3dEA6IB2QDkgHpAPSAemAdEA60gHpgHRAOqQWt0er+uNlucV9aTxj3cfLRMLl2FIsxDNuJjMel/SWFjVluCbYfmWD2mXJBeuJ9NbqOdPUjLwMLFS77GlSU9qnqePIy2xlXq36T72qPHeyeT9n+uTi4fK8E5L5q3Yv/eJJtcY8I/KH+UqXWSe5pwPSkQ5IB6QD0gHpgHRAOiAdkA5IB6QD0gHpYE3l7cmZiz6VRyIZ97nn1JQzuVhW5Mpdes7X1Yx7dknpnXrZ019RfzpqjfX6az6VF9UUfbL1Z7SM/Vvtq8aGOTW5jk/qbzGtsteVyWS0n3nJQ41M0c8y5TKTmwoPe85wT+cPOUA6IB2QDkgHpAPSAemAdEA6IB2QDkgHW1w2JbjapufsjeuAdk9yYc96WJMwT2dPrw62+xfIjLnvisBTB+2ntuSUCDzWrvZplbua1OrjdIsdPMZCfNRlXDym//gTkfDJCtll4zMysth+TXpl4LYBGelRi9T6ONOvw+K4iog+BYc+Lh9vmie3AemKZk1C1KjRth8Z4p4OSEc6IB2QDkgHpAPSAemAdEA6IB2QDkiHCRPq0WpXeebSVZ4+//NwOB8OOEytTrQvDflYtY8+EoEbZ2tdsl50zf1YBF79r32Rh/SUQ2qf+u0i8Pwp+5nkfyQjD4p12t3ksEoFcVnd+UQk587xhmD72stOZ7o937TvUgwhvU6cLL1myiYp3WX6mybTxbxqE/d0QDrSAemAdEA6IB3SI73EGkw96VnWgMs7IB2QDkgHpEM68PRodTQxBzSa3KJjMS1BrsqH9I0y0F4dyeyNcarULp3VahGDdjXjq50+qlaXx/laGdj2rI+yO0pBihFNf6Sk0BPm/NKKdBtd8jKl3kjp0aqOJOdqvk3OjXs6f8gB0gHpgHRAOiAdkA5IB6QD0gHpgHRAOiAdVCpZAifyMvDbB1IkveDQSf4HiWzBx1Ry9l0Wy4FL0fyIO2Rgj7H9SIgXSexXaWSmntM3z1ZXptLL9d1HkaKPgQvRnBoViSoz4arc0/lDDpAOSAekA9IB6YB0QDogHZAOSAekA9LBlsqdcY18831qytMy8P0bg+1+h+82nHhLzzEWZYe6x+IF+4U88m8ZaVg5yX9uzXJ/hMuSjUafsx52omhzmey4KNLqUqQgijQbGUdFxnCIqn0laxL0utQNXHe5pwPSAemAdEA6IB2QjnRAOiAdkA5IB6QD0iGhpGsnitG3Yxr43fSs0eB5F+lHI5nLBvsuP9ksAleNF0taayc+jEn+MRH49Up1kRwGbqiTkVv0TsbAs0S7a42L9LtnR+D8mEOfBhk4bOasDzY97USxXko3Mm5dLJbaYRSnd6PWefg4Lvd0/pADpAPSAemAdEA6JFd6iTWYetKzrAGXd0A6IB2QDkiHdBDiJYriiEPdGvsuV1zmfyWSVYmm6oiPIlc9rHQI6SfWOsxt2HouxRCfrmiUgSb5WY1648WErH3VgzPVlGkOazLTh3SjSN/8YLu60Yf0BLFdKO09aKQcsK9615Zg+5pZ9YVcatbodn0FuKfzhxwgHZAOSAekA9IB6YB0QDogHZAOSAekA9JBJV3P0++KpOr27WrKghQrHlqSKukj04Ptlx7yUHTLoyJwfrlMyZ8QgTs6Izm+ywuD7XeMvSka96tF9DeDBtJ1pmdjKppNzPFFMhPu6fwhB0gHpAPSAemAdEiudLYfmYLS2X6EyzsgHZAOSAekQ0pI9KPV30RR9M9vpMnP4zKwU7QPnEuz9Io+Gflhi9qp9bsioP8L9MMWH7Ptsd/q5c6L9sOYe2I0iyM815LqM31+2TrFNlvu6YB0QDogHZAOSAekA9IB6YB0pAPSAemAdEgpCXq0+vdoiqwuy+Q/+ZeMTF+pdvrgA/uB3iyP9FuOO9Sttu5RXGOE9IEXiXbBLDJWJX4EetUqGdhVkJHPiHavMfDGIyLw+6IIdKxRZ9L4PRF4UO9jHN9aB+mLFsV17q+ZEUHRm26y73OPj4HvlYGOEJ0aHAYSfYa4pwPSkQ5IB6QD0gHpgHRAOiAdkA5IB6QD0mHCXO/R6lBSJ3vtqkMno8+sMm2dZyzjbB9FIpI+J7G/0Jf1z3nUrxKB94zD6V5Wlsm2GwMXxGX1c9vUInsOqilGkVlS8bbkGGwuBSmYKSMi5ZBetV50KeWNlO5SBJwNccQF66rDIar22U+Wezp/yAHSAemAdEA6IB2QDkgHpAPSAemAdEA6IB1U/g8haNnaidoBbwAAAABJRU5ErkJggg==";

        private static readonly string[] Tabs =
            { "tab.character", "tab.cheats", "tab.admin", "tab.inventory", "tab.world", "tab.badges", "tab.cosmetics", "tab.about", "tab.anticheat" };
        private static readonly string[][] SimpleSectionKeyMap =
        {
            new[] { "character.redesign.move", "character.redesign.status" },
            new[] { "cheats.redesign.core", "cheats.redesign.esp", "cheats.redesign.status", "cheats.redesign.move", "cheats.redesign.pranks" },
            new[] { "admin.players", "admin.protection", "admin.many_players", "admin.lists" },
            new[] { "inventory.redesign.spawn", "inventory.redesign.backpack", "inventory.redesign.entities" },
            new[] { "world.time", "world.modifiers", "world.navigation", "world.finish", "world.luggage" },
            Array.Empty<string>(),
            Array.Empty<string>(),
            new[] { "about.main", "about.controls", "about.links", "about.support" },
            new[] { "anticheat.detection", "anticheat.mods", "anticheat.events" },
        };
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
        private static Rect _lastSavedRect = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
        private static bool _windowRectLoaded;
        private static Rect _noticeRect = new Rect(40, 40, 360, 0);
        private static int _tab;
        private static Vector2 _scroll;
        private static bool _noticeDismissed;
        private static string _itemSearch = "";
        private static int _selItem = -1;
        private static string _smartSpawnResult = "";
        private static int _selSlot = 0;
        private static int _selBackpackSlot = 0;
        private static Vector2 _itemScroll;
        private static Vector2 _itemEspScroll;
        private static string _itemEspSearch = "";
        private static int _invTarget = -1; // -1 = себе; иначе индекс в GameApi.PlayerChars
        private static bool _excludeSelf = true;
        private static string _tpX = "0", _tpY = "0", _tpZ = "0";
        private static Vector2 _luggageScroll;
        private static int _selLuggage = -1;
        private static string _worldObjectSearch = "bell tower item luggage gloom campfire statue";
        private static float _worldObjectRange = 1200f;
        private static int _selWorldObject = -1;
        private static Vector2 _worldObjectScroll;
        private static int _selStatus;
        private static float _statusAmount = 0.5f;
        private static float _statusCustomDeltaPercent = 25f;
        private static int _statusTarget = -1; // -1 = себе
        private static int _prankTarget = -1;
        private static int _adminTarget = -1;
        private static int _mxAhgTargetActor = -1;
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
        private static readonly HashSet<ushort> _favoriteItemIds = new HashSet<ushort>();
        private static string _favoriteItemSource;
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
        private static int _quickActionCaptureSlot = -1;
        private static int _quickActionPickerSlot = -1;
        private static bool _itemFavoritesOnly;
        private static int _itemCategory;
        private static int _itemEspCategory;
        private static string _nicknameInput = "";
        private static float _compatCopiedUntil;
        private static float _quickActionCopiedUntil;
        private static float _petrifyAmount = 50f;
        private static int _themeVersion = -1;
        private static string _accentHexText;
        private static string _actionHexText;
        private static readonly int[] _simpleSection = new int[Tabs.Length];
        private static Lang _simpleSectionLabelLanguage = (Lang)(-1);
        private static string[][] _simpleSectionLabelMap;
        private static bool _favoritesOpen;
        private static string _favoriteActionSearch = "";
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
        private static bool ZhCn => Localization.Current == Lang.ChineseSimplified;
        private static bool ZhTw => Localization.Current == Lang.ChineseTraditional;
        private static string Tx(
            string ru,
            string en,
            string zhCn = null,
            string zhTw = null,
            string uk = null,
            string ja = null,
            string ko = null,
            string es = null,
            string ptBr = null,
            string de = null,
            string fr = null,
            string it = null,
            string pl = null,
            string tr = null)
        {
            string selected = Localization.Pick(Localization.Current, ru, en, zhCn, zhTw, uk, ja, ko, es, ptBr, de, fr, it, pl, tr);
            if (!string.Equals(selected, en, StringComparison.Ordinal))
                return selected;

            string known = KnownTx(en);
            return string.IsNullOrEmpty(known) ? selected : known;
        }

        private static string KnownTx(string en)
        {
            return Localization.Current switch
            {
                Lang.Ukrainian => en switch
                {
                    "Many players" => "Багато гравців",
                    "Enable larger lobbies" => "Увімкнути великі лобі",
                    "Max players" => "Макс. гравців",
                    "Host-only start" => "Старт лише хостом",
                    "Join/leave log" => "Журнал входів/виходів",
                    "Voice group fix" => "Фікс голосових груп",
                    "Player UI fix" => "Фікс UI гравців",
                    "Give food" => "Роздати їжу",
                    "Give backpacks" => "Роздати рюкзаки",
                    "No falling ragdoll" => "Без ragdoll від падіння",
                    "Global voice hearing" => "Голос на всю мапу",
                    "Player ESP" => "ESP гравців",
                    "ESP distance" => "Дистанція ESP",
                    "No status effects" => "Без ефектів статусу",
                    "No injury" => "Без травм",
                    "No hunger" => "Без голоду",
                    "No cold" => "Без холоду",
                    "No poison" => "Без отрути",
                    "No curse" => "Без прокляття",
                    "No drowsy" => "Без сонливості",
                    "No hot" => "Без спеки",
                    "Fly & teleport" => "Політ і телепорт",
                    "Long interaction" => "Далека взаємодія",
                    "Distance" => "Дистанція",
                    "Cinematic camera" => "Кінематографічна камера",
                    "Camera speed" => "Швидкість камери",
                    "Noclip through geometry" => "Прохід крізь геометрію",
                    "Speed" => "Швидкість",
                    "Accel" => "Прискорення",
                    "Coords:" => "Коорд.:",
                    "TP" => "ТП",
                    "Pranks" => "Розіграші",
                    "Pile on afflictions" => "Накласти недуги",
                    "Cure" => "Вилікувати",
                    "Push" => "Штовхнути",
                    "Explode" => "Підірвати",
                    "Petrify" => "Окаменити",
                    "Clear petrify" => "Зняти окаменіння",
                    "Add arrow" => "Додати стрілу",
                    "5 arrows" => "5 стріл",
                    "Remove arrows" => "Витягти стріли",
                    "Stuff with selected item" => "Забити вибраним предметом",
                    "Fill with gold" => "Забити золотом",
                    "Fill with webs" => "Забити павутиною",
                    "Random items" => "Випадкові предмети",
                    "Clear inventory" => "Очистити інвентар",
                    "7 balloons" => "7 кульок",
                    "Drop items" => "Викинути речі",
                    "Plugin DLL check" => "Перевірка DLL модів",
                    "Enable DLL check" => "Увімкнути перевірку DLL",
                    "Kick mismatches" => "Кікати невідповідних",
                    "Refresh signature" => "Оновити підпис",
                    "Copy report" => "Скопіювати звіт",
                    "Your set: " => "Твій набір: ",
                    "Error: " => "Помилка: ",
                    _ => null,
                },
                Lang.Japanese => en switch
                {
                    "Many players" => "大人数",
                    "Enable larger lobbies" => "大人数ロビーを有効化",
                    "Max players" => "最大プレイヤー",
                    "Host-only start" => "ホストのみ開始",
                    "Join/leave log" => "参加/退出ログ",
                    "Voice group fix" => "ボイスグループ修正",
                    "Player UI fix" => "プレイヤーUI修正",
                    "Give food" => "食料を配る",
                    "Give backpacks" => "バックパックを配る",
                    "No falling ragdoll" => "落下ラグドールなし",
                    "Global voice hearing" => "全体ボイス受信",
                    "Player ESP" => "プレイヤーESP",
                    "ESP distance" => "ESP距離",
                    "No status effects" => "状態異常なし",
                    "No injury" => "負傷なし",
                    "No hunger" => "空腹なし",
                    "No cold" => "寒さなし",
                    "No poison" => "毒なし",
                    "No curse" => "呪いなし",
                    "No drowsy" => "眠気なし",
                    "No hot" => "暑さなし",
                    "Fly & teleport" => "飛行とテレポート",
                    "Long interaction" => "長距離インタラクト",
                    "Distance" => "距離",
                    "Cinematic camera" => "シネマカメラ",
                    "Camera speed" => "カメラ速度",
                    "Noclip through geometry" => "地形をすり抜ける",
                    "Speed" => "速度",
                    "Accel" => "加速",
                    "Coords:" => "座標:",
                    "TP" => "TP",
                    "Pranks" => "いたずら",
                    "Pile on afflictions" => "状態異常を付与",
                    "Cure" => "治療",
                    "Push" => "押す",
                    "Explode" => "爆発",
                    "Petrify" => "石化",
                    "Clear petrify" => "石化解除",
                    "Add arrow" => "矢を追加",
                    "5 arrows" => "矢5本",
                    "Remove arrows" => "矢を抜く",
                    "Stuff with selected item" => "選択アイテムで満たす",
                    "Fill with gold" => "金で満たす",
                    "Fill with webs" => "クモ糸で満たす",
                    "Random items" => "ランダムアイテム",
                    "Clear inventory" => "インベントリ消去",
                    "7 balloons" => "風船7個",
                    "Drop items" => "アイテムを落とす",
                    "Plugin DLL check" => "プラグインDLL検査",
                    "Enable DLL check" => "DLL検査を有効化",
                    "Kick mismatches" => "不一致をキック",
                    "Refresh signature" => "署名を更新",
                    "Copy report" => "レポートをコピー",
                    "Your set: " => "自分のセット: ",
                    "Error: " => "エラー: ",
                    _ => null,
                },
                Lang.Korean => en switch
                {
                    "Many players" => "다인 플레이",
                    "Enable larger lobbies" => "대형 로비 켜기",
                    "Max players" => "최대 플레이어",
                    "Host-only start" => "호스트만 시작",
                    "Join/leave log" => "입장/퇴장 로그",
                    "Voice group fix" => "음성 그룹 수정",
                    "Player UI fix" => "플레이어 UI 수정",
                    "Give food" => "음식 지급",
                    "Give backpacks" => "배낭 지급",
                    "No falling ragdoll" => "낙하 래그돌 없음",
                    "Global voice hearing" => "전체 맵 음성 듣기",
                    "Player ESP" => "플레이어 ESP",
                    "ESP distance" => "ESP 거리",
                    "No status effects" => "상태 효과 없음",
                    "No injury" => "부상 없음",
                    "No hunger" => "허기 없음",
                    "No cold" => "추위 없음",
                    "No poison" => "독 없음",
                    "No curse" => "저주 없음",
                    "No drowsy" => "졸림 없음",
                    "No hot" => "더위 없음",
                    "Fly & teleport" => "비행 및 순간이동",
                    "Long interaction" => "긴 상호작용",
                    "Distance" => "거리",
                    "Cinematic camera" => "시네마틱 카메라",
                    "Camera speed" => "카메라 속도",
                    "Noclip through geometry" => "지형 통과",
                    "Speed" => "속도",
                    "Accel" => "가속",
                    "Coords:" => "좌표:",
                    "TP" => "TP",
                    "Pranks" => "장난",
                    "Pile on afflictions" => "상태 이상 부여",
                    "Cure" => "치료",
                    "Push" => "밀기",
                    "Explode" => "폭발",
                    "Petrify" => "석화",
                    "Clear petrify" => "석화 해제",
                    "Add arrow" => "화살 추가",
                    "5 arrows" => "화살 5개",
                    "Remove arrows" => "화살 제거",
                    "Stuff with selected item" => "선택 아이템으로 채우기",
                    "Fill with gold" => "금으로 채우기",
                    "Fill with webs" => "거미줄로 채우기",
                    "Random items" => "무작위 아이템",
                    "Clear inventory" => "인벤토리 비우기",
                    "7 balloons" => "풍선 7개",
                    "Drop items" => "아이템 떨어뜨리기",
                    "Plugin DLL check" => "플러그인 DLL 검사",
                    "Enable DLL check" => "DLL 검사 켜기",
                    "Kick mismatches" => "불일치 킥",
                    "Refresh signature" => "서명 새로고침",
                    "Copy report" => "보고서 복사",
                    "Your set: " => "내 세트: ",
                    "Error: " => "오류: ",
                    _ => null,
                },
                Lang.Spanish => en switch
                {
                    "Many players" => "Muchos jugadores",
                    "Enable larger lobbies" => "Activar lobbies grandes",
                    "Max players" => "Máx. jugadores",
                    "Host-only start" => "Inicio solo host",
                    "Join/leave log" => "Registro entradas/salidas",
                    "Voice group fix" => "Arreglo de voz",
                    "Player UI fix" => "Arreglo de UI",
                    "Give food" => "Dar comida",
                    "Give backpacks" => "Dar mochilas",
                    "No falling ragdoll" => "Sin ragdoll al caer",
                    "Global voice hearing" => "Voz en todo el mapa",
                    "Player ESP" => "ESP de jugadores",
                    "ESP distance" => "Distancia ESP",
                    "No status effects" => "Sin estados",
                    "No injury" => "Sin heridas",
                    "No hunger" => "Sin hambre",
                    "No cold" => "Sin frío",
                    "No poison" => "Sin veneno",
                    "No curse" => "Sin maldición",
                    "No drowsy" => "Sin sueño",
                    "No hot" => "Sin calor",
                    "Fly & teleport" => "Vuelo y teletransporte",
                    "Long interaction" => "Interacción lejana",
                    "Distance" => "Distancia",
                    "Cinematic camera" => "Cámara cinematográfica",
                    "Camera speed" => "Velocidad cámara",
                    "Noclip through geometry" => "Atravesar geometría",
                    "Speed" => "Velocidad",
                    "Accel" => "Aceleración",
                    "Coords:" => "Coords:",
                    "TP" => "TP",
                    "Pranks" => "Bromas",
                    "Pile on afflictions" => "Aplicar estados",
                    "Cure" => "Curar",
                    "Push" => "Empujar",
                    "Explode" => "Explotar",
                    "Petrify" => "Petrificar",
                    "Clear petrify" => "Quitar petrificación",
                    "Add arrow" => "Añadir flecha",
                    "5 arrows" => "5 flechas",
                    "Remove arrows" => "Quitar flechas",
                    "Stuff with selected item" => "Llenar con objeto elegido",
                    "Fill with gold" => "Llenar con oro",
                    "Fill with webs" => "Llenar con telarañas",
                    "Random items" => "Objetos aleatorios",
                    "Clear inventory" => "Vaciar inventario",
                    "7 balloons" => "7 globos",
                    "Drop items" => "Soltar objetos",
                    "Plugin DLL check" => "Comprobar DLL de mods",
                    "Enable DLL check" => "Activar comprobación DLL",
                    "Kick mismatches" => "Expulsar diferencias",
                    "Refresh signature" => "Actualizar firma",
                    "Copy report" => "Copiar informe",
                    "Your set: " => "Tu conjunto: ",
                    "Error: " => "Error: ",
                    _ => null,
                },
                Lang.PortugueseBR => en switch
                {
                    "Many players" => "Muitos jogadores",
                    "Enable larger lobbies" => "Ativar lobbies grandes",
                    "Max players" => "Máx. jogadores",
                    "Host-only start" => "Início só host",
                    "Join/leave log" => "Log entrar/sair",
                    "Voice group fix" => "Correção de voz",
                    "Player UI fix" => "Correção da UI",
                    "Give food" => "Dar comida",
                    "Give backpacks" => "Dar mochilas",
                    "No falling ragdoll" => "Sem ragdoll ao cair",
                    "Global voice hearing" => "Voz no mapa todo",
                    "Player ESP" => "ESP de jogadores",
                    "ESP distance" => "Distância ESP",
                    "No status effects" => "Sem status",
                    "No injury" => "Sem ferimentos",
                    "No hunger" => "Sem fome",
                    "No cold" => "Sem frio",
                    "No poison" => "Sem veneno",
                    "No curse" => "Sem maldição",
                    "No drowsy" => "Sem sono",
                    "No hot" => "Sem calor",
                    "Fly & teleport" => "Voo e teleporte",
                    "Long interaction" => "Interação longa",
                    "Distance" => "Distância",
                    "Cinematic camera" => "Câmera cinematográfica",
                    "Camera speed" => "Velocidade câmera",
                    "Noclip through geometry" => "Atravessar geometria",
                    "Speed" => "Velocidade",
                    "Accel" => "Aceleração",
                    "Coords:" => "Coords:",
                    "TP" => "TP",
                    "Pranks" => "Pegadinhas",
                    "Pile on afflictions" => "Aplicar aflições",
                    "Cure" => "Curar",
                    "Push" => "Empurrar",
                    "Explode" => "Explodir",
                    "Petrify" => "Petrificar",
                    "Clear petrify" => "Remover petrificação",
                    "Add arrow" => "Adicionar flecha",
                    "5 arrows" => "5 flechas",
                    "Remove arrows" => "Remover flechas",
                    "Stuff with selected item" => "Encher com item escolhido",
                    "Fill with gold" => "Encher com ouro",
                    "Fill with webs" => "Encher com teias",
                    "Random items" => "Itens aleatórios",
                    "Clear inventory" => "Limpar inventário",
                    "7 balloons" => "7 balões",
                    "Drop items" => "Soltar itens",
                    "Plugin DLL check" => "Verificação DLL de mods",
                    "Enable DLL check" => "Ativar verificação DLL",
                    "Kick mismatches" => "Expulsar divergentes",
                    "Refresh signature" => "Atualizar assinatura",
                    "Copy report" => "Copiar relatório",
                    "Your set: " => "Seu conjunto: ",
                    "Error: " => "Erro: ",
                    _ => null,
                },
                Lang.German => en switch
                {
                    "Many players" => "Viele Spieler",
                    "Enable larger lobbies" => "Große Lobbys aktivieren",
                    "Max players" => "Max. Spieler",
                    "Host-only start" => "Start nur Host",
                    "Join/leave log" => "Beitritt/Verlassen-Log",
                    "Voice group fix" => "Sprachgruppen-Fix",
                    "Player UI fix" => "Spieler-UI-Fix",
                    "Give food" => "Essen geben",
                    "Give backpacks" => "Rucksäcke geben",
                    "No falling ragdoll" => "Kein Fall-Ragdoll",
                    "Global voice hearing" => "Stimme überall hören",
                    "Player ESP" => "Spieler-ESP",
                    "ESP distance" => "ESP-Distanz",
                    "No status effects" => "Keine Statuseffekte",
                    "No injury" => "Keine Verletzung",
                    "No hunger" => "Kein Hunger",
                    "No cold" => "Keine Kälte",
                    "No poison" => "Kein Gift",
                    "No curse" => "Kein Fluch",
                    "No drowsy" => "Keine Müdigkeit",
                    "No hot" => "Keine Hitze",
                    "Fly & teleport" => "Flug und Teleport",
                    "Long interaction" => "Weite Interaktion",
                    "Distance" => "Distanz",
                    "Cinematic camera" => "Filmische Kamera",
                    "Camera speed" => "Kameratempo",
                    "Noclip through geometry" => "Durch Geometrie noclippen",
                    "Speed" => "Tempo",
                    "Accel" => "Beschl.",
                    "Coords:" => "Koord.:",
                    "TP" => "TP",
                    "Pranks" => "Streiche",
                    "Pile on afflictions" => "Leiden stapeln",
                    "Cure" => "Heilen",
                    "Push" => "Schubsen",
                    "Explode" => "Explodieren",
                    "Petrify" => "Versteinern",
                    "Clear petrify" => "Versteinerung entfernen",
                    "Add arrow" => "Pfeil hinzufügen",
                    "5 arrows" => "5 Pfeile",
                    "Remove arrows" => "Pfeile entfernen",
                    "Stuff with selected item" => "Mit gewähltem Item füllen",
                    "Fill with gold" => "Mit Gold füllen",
                    "Fill with webs" => "Mit Netzen füllen",
                    "Random items" => "Zufällige Items",
                    "Clear inventory" => "Inventar leeren",
                    "7 balloons" => "7 Ballons",
                    "Drop items" => "Items fallen lassen",
                    "Plugin DLL check" => "Plugin-DLL-Prüfung",
                    "Enable DLL check" => "DLL-Prüfung aktivieren",
                    "Kick mismatches" => "Abweichungen kicken",
                    "Refresh signature" => "Signatur aktualisieren",
                    "Copy report" => "Bericht kopieren",
                    "Your set: " => "Dein Satz: ",
                    "Error: " => "Fehler: ",
                    _ => null,
                },
                Lang.French => en switch
                {
                    "Many players" => "Beaucoup de joueurs",
                    "Enable larger lobbies" => "Activer les grands salons",
                    "Max players" => "Joueurs max",
                    "Host-only start" => "Départ par l'hôte",
                    "Join/leave log" => "Journal entrées/sorties",
                    "Voice group fix" => "Correctif voix",
                    "Player UI fix" => "Correctif UI joueurs",
                    "Give food" => "Donner nourriture",
                    "Give backpacks" => "Donner sacs",
                    "No falling ragdoll" => "Pas de ragdoll en chute",
                    "Global voice hearing" => "Voix sur toute la carte",
                    "Player ESP" => "ESP joueurs",
                    "ESP distance" => "Distance ESP",
                    "No status effects" => "Sans statuts",
                    "No injury" => "Sans blessure",
                    "No hunger" => "Sans faim",
                    "No cold" => "Sans froid",
                    "No poison" => "Sans poison",
                    "No curse" => "Sans malédiction",
                    "No drowsy" => "Sans somnolence",
                    "No hot" => "Sans chaleur",
                    "Fly & teleport" => "Vol et téléportation",
                    "Long interaction" => "Interaction longue",
                    "Distance" => "Distance",
                    "Cinematic camera" => "Caméra cinématique",
                    "Camera speed" => "Vitesse caméra",
                    "Noclip through geometry" => "Traverser la géométrie",
                    "Speed" => "Vitesse",
                    "Accel" => "Accél.",
                    "Coords:" => "Coord.:",
                    "TP" => "TP",
                    "Pranks" => "Farces",
                    "Pile on afflictions" => "Ajouter des afflictions",
                    "Cure" => "Soigner",
                    "Push" => "Pousser",
                    "Explode" => "Exploser",
                    "Petrify" => "Pétrifier",
                    "Clear petrify" => "Retirer pétrification",
                    "Add arrow" => "Ajouter flèche",
                    "5 arrows" => "5 flèches",
                    "Remove arrows" => "Retirer flèches",
                    "Stuff with selected item" => "Remplir avec l'objet choisi",
                    "Fill with gold" => "Remplir d'or",
                    "Fill with webs" => "Remplir de toiles",
                    "Random items" => "Objets aléatoires",
                    "Clear inventory" => "Vider inventaire",
                    "7 balloons" => "7 ballons",
                    "Drop items" => "Lâcher objets",
                    "Plugin DLL check" => "Vérification DLL mods",
                    "Enable DLL check" => "Activer vérification DLL",
                    "Kick mismatches" => "Expulser différences",
                    "Refresh signature" => "Actualiser signature",
                    "Copy report" => "Copier rapport",
                    "Your set: " => "Ton ensemble : ",
                    "Error: " => "Erreur : ",
                    _ => null,
                },
                Lang.Italian => en switch
                {
                    "Many players" => "Molti giocatori",
                    "Enable larger lobbies" => "Attiva lobby grandi",
                    "Max players" => "Giocatori max",
                    "Host-only start" => "Avvio solo host",
                    "Join/leave log" => "Log entrate/uscite",
                    "Voice group fix" => "Fix gruppi voce",
                    "Player UI fix" => "Fix UI giocatori",
                    "Give food" => "Dai cibo",
                    "Give backpacks" => "Dai zaini",
                    "No falling ragdoll" => "Niente ragdoll caduta",
                    "Global voice hearing" => "Voce su tutta la mappa",
                    "Player ESP" => "ESP giocatori",
                    "ESP distance" => "Distanza ESP",
                    "No status effects" => "Nessun effetto stato",
                    "No injury" => "Nessuna ferita",
                    "No hunger" => "Niente fame",
                    "No cold" => "Niente freddo",
                    "No poison" => "Niente veleno",
                    "No curse" => "Niente maledizione",
                    "No drowsy" => "Niente sonnolenza",
                    "No hot" => "Niente caldo",
                    "Fly & teleport" => "Volo e teletrasporto",
                    "Long interaction" => "Interazione lunga",
                    "Distance" => "Distanza",
                    "Cinematic camera" => "Camera cinematica",
                    "Camera speed" => "Velocità camera",
                    "Noclip through geometry" => "Attraversa geometria",
                    "Speed" => "Velocità",
                    "Accel" => "Accel.",
                    "Coords:" => "Coord.:",
                    "TP" => "TP",
                    "Pranks" => "Scherzi",
                    "Pile on afflictions" => "Aggiungi afflizioni",
                    "Cure" => "Cura",
                    "Push" => "Spingi",
                    "Explode" => "Esplodi",
                    "Petrify" => "Pietrifica",
                    "Clear petrify" => "Rimuovi pietrificazione",
                    "Add arrow" => "Aggiungi freccia",
                    "5 arrows" => "5 frecce",
                    "Remove arrows" => "Rimuovi frecce",
                    "Stuff with selected item" => "Riempi con oggetto scelto",
                    "Fill with gold" => "Riempi d'oro",
                    "Fill with webs" => "Riempi di ragnatele",
                    "Random items" => "Oggetti casuali",
                    "Clear inventory" => "Svuota inventario",
                    "7 balloons" => "7 palloncini",
                    "Drop items" => "Lascia oggetti",
                    "Plugin DLL check" => "Controllo DLL mod",
                    "Enable DLL check" => "Attiva controllo DLL",
                    "Kick mismatches" => "Espelli differenze",
                    "Refresh signature" => "Aggiorna firma",
                    "Copy report" => "Copia report",
                    "Your set: " => "Il tuo set: ",
                    "Error: " => "Errore: ",
                    _ => null,
                },
                Lang.Polish => en switch
                {
                    "Many players" => "Wielu graczy",
                    "Enable larger lobbies" => "Włącz większe lobby",
                    "Max players" => "Maks. graczy",
                    "Host-only start" => "Start tylko host",
                    "Join/leave log" => "Log wejść/wyjść",
                    "Voice group fix" => "Naprawa grup głosu",
                    "Player UI fix" => "Naprawa UI graczy",
                    "Give food" => "Daj jedzenie",
                    "Give backpacks" => "Daj plecaki",
                    "No falling ragdoll" => "Bez ragdoll po upadku",
                    "Global voice hearing" => "Głos na całej mapie",
                    "Player ESP" => "ESP graczy",
                    "ESP distance" => "Dystans ESP",
                    "No status effects" => "Bez efektów statusu",
                    "No injury" => "Bez obrażeń",
                    "No hunger" => "Bez głodu",
                    "No cold" => "Bez zimna",
                    "No poison" => "Bez trucizny",
                    "No curse" => "Bez klątwy",
                    "No drowsy" => "Bez senności",
                    "No hot" => "Bez gorąca",
                    "Fly & teleport" => "Lot i teleport",
                    "Long interaction" => "Daleka interakcja",
                    "Distance" => "Dystans",
                    "Cinematic camera" => "Kamera filmowa",
                    "Camera speed" => "Prędkość kamery",
                    "Noclip through geometry" => "Przenikanie przez geometrię",
                    "Speed" => "Prędkość",
                    "Accel" => "Przysp.",
                    "Coords:" => "Współrz.:",
                    "TP" => "TP",
                    "Pranks" => "Żarty",
                    "Pile on afflictions" => "Nałóż dolegliwości",
                    "Cure" => "Ulecz",
                    "Push" => "Popchnij",
                    "Explode" => "Wysadź",
                    "Petrify" => "Skamień",
                    "Clear petrify" => "Usuń skamienienie",
                    "Add arrow" => "Dodaj strzałę",
                    "5 arrows" => "5 strzał",
                    "Remove arrows" => "Usuń strzały",
                    "Stuff with selected item" => "Wypełnij wybranym przedmiotem",
                    "Fill with gold" => "Wypełnij złotem",
                    "Fill with webs" => "Wypełnij pajęczyną",
                    "Random items" => "Losowe przedmioty",
                    "Clear inventory" => "Wyczyść ekwipunek",
                    "7 balloons" => "7 balonów",
                    "Drop items" => "Upuść przedmioty",
                    "Plugin DLL check" => "Kontrola DLL modów",
                    "Enable DLL check" => "Włącz kontrolę DLL",
                    "Kick mismatches" => "Wyrzucaj różnice",
                    "Refresh signature" => "Odśwież podpis",
                    "Copy report" => "Kopiuj raport",
                    "Your set: " => "Twój zestaw: ",
                    "Error: " => "Błąd: ",
                    _ => null,
                },
                Lang.Turkish => en switch
                {
                    "Many players" => "Çok oyuncu",
                    "Enable larger lobbies" => "Büyük lobileri aç",
                    "Max players" => "Maks. oyuncu",
                    "Host-only start" => "Sadece host başlatır",
                    "Join/leave log" => "Giriş/çıkış kaydı",
                    "Voice group fix" => "Ses grubu düzeltmesi",
                    "Player UI fix" => "Oyuncu UI düzeltmesi",
                    "Give food" => "Yemek ver",
                    "Give backpacks" => "Sırt çantası ver",
                    "No falling ragdoll" => "Düşüş ragdoll yok",
                    "Global voice hearing" => "Harita geneli ses",
                    "Player ESP" => "Oyuncu ESP",
                    "ESP distance" => "ESP mesafesi",
                    "No status effects" => "Durum etkisi yok",
                    "No injury" => "Yaralanma yok",
                    "No hunger" => "Açlık yok",
                    "No cold" => "Soğuk yok",
                    "No poison" => "Zehir yok",
                    "No curse" => "Lanet yok",
                    "No drowsy" => "Uykululuk yok",
                    "No hot" => "Sıcak yok",
                    "Fly & teleport" => "Uçuş ve ışınlanma",
                    "Long interaction" => "Uzak etkileşim",
                    "Distance" => "Mesafe",
                    "Cinematic camera" => "Sinematik kamera",
                    "Camera speed" => "Kamera hızı",
                    "Noclip through geometry" => "Geometriden geç",
                    "Speed" => "Hız",
                    "Accel" => "İvme",
                    "Coords:" => "Koord.:",
                    "TP" => "TP",
                    "Pranks" => "Şakalar",
                    "Pile on afflictions" => "Durum etkisi ekle",
                    "Cure" => "İyileştir",
                    "Push" => "İt",
                    "Explode" => "Patlat",
                    "Petrify" => "Taşa çevir",
                    "Clear petrify" => "Taşlaşmayı kaldır",
                    "Add arrow" => "Ok ekle",
                    "5 arrows" => "5 ok",
                    "Remove arrows" => "Okları çıkar",
                    "Stuff with selected item" => "Seçili eşyayla doldur",
                    "Fill with gold" => "Altınla doldur",
                    "Fill with webs" => "Ağla doldur",
                    "Random items" => "Rastgele eşyalar",
                    "Clear inventory" => "Envanteri temizle",
                    "7 balloons" => "7 balon",
                    "Drop items" => "Eşyaları düşür",
                    "Plugin DLL check" => "Mod DLL kontrolü",
                    "Enable DLL check" => "DLL kontrolünü aç",
                    "Kick mismatches" => "Uyumsuzları at",
                    "Refresh signature" => "İmzayı yenile",
                    "Copy report" => "Raporu kopyala",
                    "Your set: " => "Senin setin: ",
                    "Error: " => "Hata: ",
                    _ => null,
                },
                _ => null,
            };
        }
        private static bool UseModernUi => true;
        public static bool IsCapturingHotkey => _waitingForMenuKey || _quickActionCaptureSlot >= 0;

        private static bool IsAntiCheatAllowedTab(int index)
        {
            if (index < 0 || index >= Tabs.Length)
                return false;
            return Tabs[index] == "tab.about" || Tabs[index] == "tab.anticheat";
        }

        private static void EnsureAntiCheatTabAllowed()
        {
            if (!AntiCheat.ClientToolsLocked)
                return;
            _favoritesOpen = false;
            if (!IsAntiCheatAllowedTab(_tab))
                _tab = Array.IndexOf(Tabs, "tab.anticheat");
            if (_tab < 0)
                _tab = Array.IndexOf(Tabs, "tab.about");
            if (_tab < 0)
                _tab = 0;
        }

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

        private static float UiScale()
        {
            try
            {
                if (ModConfig.UiScale != null)
                    return Mathf.Clamp(ModConfig.UiScale.Value, 0.6f, 2f);
            }
            catch { }
            return 1f;
        }

        private static float VirtualScreenWidth()
        {
            return Mathf.Max(1f, Screen.width / UiScale());
        }

        private static float VirtualScreenHeight()
        {
            return Mathf.Max(1f, Screen.height / UiScale());
        }

        private static void BeginScaledGui(out Matrix4x4 previous)
        {
            previous = GUI.matrix;
            float scale = UiScale();
            if (Mathf.Abs(scale - 1f) > 0.001f)
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        }

        private static void EndScaledGui(Matrix4x4 previous)
        {
            GUI.matrix = previous;
        }

        public static void Draw()
        {
            BeginScaledGui(out Matrix4x4 oldMatrix);
            try
            {
                EnsureWindowRectLoaded();
                Theme.EnsureBuilt();
                SyncThemeTextureCache();
                ApplyFont();
                ClampWindowToScreen();
                _hoverTip = null;
                HandleWindowResize(Event.current);
                _rect = GUI.Window(0xAC10, _rect, DrawWindow, GUIContent.none, Theme.Window);
                ClampWindowToScreen();
                PersistWindowRect();
                DrawTooltipOverlay();
            }
            finally
            {
                EndScaledGui(oldMatrix);
            }
        }

        public static void DrawAdminNotice()
        {
            if (string.IsNullOrWhiteSpace(GameApi.AdminNoticeText)
                || Time.realtimeSinceStartup > GameApi.AdminNoticeUntil)
                return;

            BeginScaledGui(out Matrix4x4 oldMatrix);
            try
            {
                Theme.EnsureBuilt();
                ApplyFont();
                float width = Mathf.Min(430f, VirtualScreenWidth() - 32f);
                Rect r = new Rect(VirtualScreenWidth() - width - 18f, 18f, width, 64f);
                GUI.Box(r, GUIContent.none, Theme.Card);
                GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 8f, r.width - 24f, r.height - 16f));
                GUILayout.Label(Ru ? "Античит" : "Anti-cheat", Theme.DonateText);
                GUILayout.Label(GameApi.AdminNoticeText, Theme.LabelDim);
                GUILayout.EndArea();
            }
            finally
            {
                EndScaledGui(oldMatrix);
            }
        }

        public static bool ConsumeCloseRequest()
        {
            bool value = _closeRequested;
            _closeRequested = false;
            return value;
        }

        private static void DrawWindow(int id)
        {
            EnsureAntiCheatTabAllowed();
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
                    bool allowed = !AntiCheat.ClientToolsLocked || IsAntiCheatAllowedTab(i);
                    GUI.enabled = allowed;
                    if (GUILayout.Button(L(Tabs[i]), style, GUILayout.Height(32)) && allowed)
                        _tab = i;
                    GUI.enabled = true;
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label(Ru ? "Тяни за край, чтобы менять размер" : "Drag any edge to resize", Theme.LabelDim);
                GUILayout.EndArea();

                GUILayout.BeginArea(new Rect(contentRect.x + 12f, contentRect.y + 10f, contentRect.width - 24f, contentRect.height - 20f));
                _scroll = BeginVerticalScroll(_scroll);
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
                    case 8: DrawAntiCheatModern(); break;
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
                bool allowed = !AntiCheat.ClientToolsLocked || IsAntiCheatAllowedTab(i);
                GUI.enabled = allowed;
                if (GUILayout.Button(L(Tabs[i]), style, GUILayout.Height(34)) && allowed)
                    _tab = i;
                GUI.enabled = true;
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            GUILayout.BeginVertical();
            _scroll = BeginVerticalScroll(_scroll);
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
                case 8: DrawAntiCheatModern(); break;
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
                        _itemScroll = BeginVerticalScroll(_itemScroll, GUILayout.Height(170));
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
                _itemScroll = BeginVerticalScroll(_itemScroll, GUILayout.Height(170));
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
                        _luggageScroll = BeginVerticalScroll(_luggageScroll, GUILayout.Height(150));
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
                _luggageScroll = BeginVerticalScroll(_luggageScroll, GUILayout.Height(150));
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
            _badgeScroll = BeginVerticalScroll(_badgeScroll, GUILayout.Height(scrollHeight));
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
                DrawTesterThanks();

                GUILayout.Space(6);
                GUILayout.Label(L("ui.language"), Theme.Section);
                DrawLanguagePicker();
                DrawMenuKeyBinding();
                DrawQuickActionSettings();
                DrawUiScaleSettings();
#if !DISABLE_UPDATE_CHECKER
                DrawUpdateChecker();
#endif
                DrawThemeColorSettings();
                DrawWindowSettings();
                DrawCompatibilityPanel();

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
                GUILayout.EndHorizontal();

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
            DrawQuickActionSettings();
            DrawUiScaleSettings();
#if !DISABLE_UPDATE_CHECKER
            DrawUpdateChecker();
#endif
            DrawThemeColorSettings();
            DrawWindowSettings();
            DrawCompatibilityPanel();

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
            GUILayout.EndHorizontal();

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

        private static void DrawQuickActionSettings()
        {
            CaptureQuickActionKeyEvent();
            if (ModConfig.QuickActionKeys == null || ModConfig.QuickActionIds == null)
                return;

            if (!BeginSection("about.quick_actions", Ru ? "Быстрые клавиши" : "Quick actions", false))
                return;

            GUILayout.Label(
                Ru
                    ? "Назначь клавишу на часто используемую функцию. Срабатывает только когда меню закрыто."
                    : "Bind a key to a frequently used action. It only fires while the menu is closed.",
                Theme.LabelDim);

            for (int i = 0; i < ModConfig.QuickActionSlotCount; i++)
            {
                string actionId = QuickActions.NormalizeId(ModConfig.QuickActionIds[i].Value);
                KeyCode key = ModConfig.QuickActionKeys[i].Value;
                bool pickingAction = _quickActionPickerSlot == i;
                bool capturing = _quickActionCaptureSlot == i;

                GUILayout.BeginHorizontal();
                GUILayout.Label("#" + (i + 1), Theme.LabelDim, GUILayout.Width(30));

                if (GUILayout.Button(QuickActions.Label(actionId, Ru), pickingAction ? Theme.ChipActive : Theme.LinkBtn, GUILayout.Height(28)))
                    _quickActionPickerSlot = pickingAction ? -1 : i;
                TipLast(QuickActions.Tip(actionId, Ru));

                string keyText = capturing
                    ? (Ru ? "Нажми..." : "Press...")
                    : (key == KeyCode.None ? (Ru ? "Нет" : "None") : key.ToString());
                if (GUILayout.Button(keyText, capturing ? Theme.DonateBtn : Theme.Chip, GUILayout.Width(120), GUILayout.Height(28)))
                {
                    _quickActionCaptureSlot = i;
                    _waitingForMenuKey = false;
                }
                TipLast(Ru ? "Назначить клавишу. Esc отменяет, Backspace/Delete очищает." : "Bind a key. Esc cancels, Backspace/Delete clears.");

                if (GUILayout.Button("X", Theme.CloseBtn, GUILayout.Width(34), GUILayout.Height(28)))
                {
                    ModConfig.QuickActionKeys[i].Value = KeyCode.None;
                    ModConfig.QuickActionIds[i].Value = "none";
                    if (_quickActionPickerSlot == i) _quickActionPickerSlot = -1;
                    if (_quickActionCaptureSlot == i) _quickActionCaptureSlot = -1;
                }
                GUILayout.EndHorizontal();

                if (pickingAction)
                    DrawQuickActionPicker(i);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Скопировать список" : "Copy list", Theme.LinkBtn, GUILayout.Height(28)))
            {
                GUIUtility.systemCopyBuffer = BuildQuickActionList();
                _quickActionCopiedUntil = Time.realtimeSinceStartup + 2f;
            }
            if (GUILayout.Button(Ru ? "Сбросить по умолчанию" : "Reset defaults", Theme.LinkBtn, GUILayout.Height(28)))
                ResetQuickActions();
            GUILayout.EndHorizontal();
            if (Time.realtimeSinceStartup < _quickActionCopiedUntil)
                GUILayout.Label(Ru ? "Список скопирован." : "List copied.", Theme.LabelDim);

            EndSection();
        }

        private static void DrawQuickActionPicker(int slot)
        {
            GUILayout.BeginVertical(Theme.Panel9);
            for (int i = 0; i < QuickActions.Actions.Length; i++)
            {
                if (i % 2 == 0)
                    GUILayout.BeginHorizontal();

                var action = QuickActions.Actions[i];
                bool active = string.Equals(ModConfig.QuickActionIds[slot].Value, action.Id, StringComparison.OrdinalIgnoreCase);
                if (GUILayout.Button(Ru ? action.Ru : action.En, active ? Theme.ChipActive : Theme.Chip, GUILayout.Height(26)))
                {
                    QuickActions.SetSlotAction(slot, action.Id);
                    _quickActionPickerSlot = -1;
                }
                TipLast(Ru ? action.TipRu : action.TipEn);

                if (i % 2 == 1 || i == QuickActions.Actions.Length - 1)
                    GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private static void CaptureQuickActionKeyEvent()
        {
            Event e = Event.current;
            if (_quickActionCaptureSlot < 0 || e == null || e.type != EventType.KeyDown)
                return;

            int slot = _quickActionCaptureSlot;
            if (e.keyCode == KeyCode.Escape)
            {
                _quickActionCaptureSlot = -1;
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.Backspace || e.keyCode == KeyCode.Delete)
            {
                ModConfig.QuickActionKeys[slot].Value = KeyCode.None;
                _quickActionCaptureSlot = -1;
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.None)
                return;

            ModConfig.QuickActionKeys[slot].Value = e.keyCode;
            _quickActionCaptureSlot = -1;
            GUI.FocusControl(null);
            e.Use();
        }

        private static string BuildQuickActionList()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < ModConfig.QuickActionSlotCount; i++)
            {
                sb.Append(i + 1).Append(". ")
                    .Append(ModConfig.QuickActionKeys[i].Value)
                    .Append(" -> ")
                    .Append(QuickActions.Label(ModConfig.QuickActionIds[i].Value, Ru))
                    .AppendLine();
            }
            return sb.ToString();
        }

        private static void ResetQuickActions()
        {
            KeyCode[] keys = { KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None };
            string[] actions = { "toggle_fly", "full_stamina", "anti_stuck", "none", "none", "none", "none", "none" };
            for (int i = 0; i < ModConfig.QuickActionSlotCount; i++)
            {
                ModConfig.QuickActionKeys[i].Value = keys[i];
                ModConfig.QuickActionIds[i].Value = actions[i];
            }
            _quickActionCaptureSlot = -1;
            _quickActionPickerSlot = -1;
        }

        private static void DrawWindowSettings()
        {
            if (!BeginSection("about.window_editor", Ru ? "Окно и интерфейс" : "Window & interface", false))
                return;

            GUILayout.Label(
                (Ru ? "Размер: " : "Size: ")
                + Mathf.RoundToInt(_rect.width)
                + " x "
                + Mathf.RoundToInt(_rect.height),
                Theme.LabelDim);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Стандартный размер" : "Default size", Theme.LinkBtn, GUILayout.Height(28)))
                _rect = DefaultWindowRect();
            if (GUILayout.Button(Ru ? "По центру" : "Center", Theme.LinkBtn, GUILayout.Height(28)))
                CenterWindow();
            GUILayout.EndHorizontal();
            GUILayout.Label(
                Ru
                    ? "Масштаб и цвета выше сохраняются в конфиге. Окно можно тянуть за шапку и менять размер за края."
                    : "Scale and colors above are saved in the config. Drag the header and resize by the edges.",
                Theme.LabelDim);
            EndSection();
        }

        private static void DrawCompatibilityPanel()
        {
            if (!BeginSection("about.compatibility", Ru ? "Диагностика и совместимость" : "Diagnostics & compatibility", false))
                return;

            GUILayout.Label(
                Ru
                    ? "Быстрая проверка методов и полей игры, от которых зависят функции PEAK-MX после обновлений PEAK."
                    : "Quick check of game methods and fields used by PEAK-MX after PEAK updates.",
                Theme.LabelDim);

            var rows = CompatibilityDiagnostics.Rows(Ru);
            for (int i = 0; i < rows.Count; i++)
            {
                CompatibilityDiagnostics.Row row = rows[i];
                GUILayout.Label((row.Ok ? "[OK] " : "[!] ") + row.Name + ": " + row.Detail,
                    row.Ok ? Theme.LabelDim : Theme.DonateText);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Обновить игроки/предметы" : "Refresh players/items", Theme.LinkBtn, GUILayout.Height(28)))
            {
                GameApi.RefreshPlayers();
                GameApi.LoadItems();
                MxAhgHost.RefreshLocal();
            }
            if (GUILayout.Button(Ru ? "Скопировать отчет" : "Copy report", Theme.LinkBtn, GUILayout.Height(28)))
            {
                GUIUtility.systemCopyBuffer = CompatibilityDiagnostics.BuildReport(Ru);
                _compatCopiedUntil = Time.realtimeSinceStartup + 2f;
            }
            GUILayout.EndHorizontal();
            if (Time.realtimeSinceStartup < _compatCopiedUntil)
                GUILayout.Label(Ru ? "Отчет скопирован." : "Report copied.", Theme.LabelDim);

            EndSection();
        }

        private static void DrawUiScaleSettings()
        {
            GUILayout.Space(6);
            GUILayout.Label(Ru ? "Масштаб меню" : "Menu scale", Theme.Section);
            float sc = UiScale();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", Theme.LinkBtn, GUILayout.Width(40), GUILayout.Height(28)))
                ModConfig.UiScale.Value = Mathf.Clamp(sc - 0.1f, 0.6f, 2f);
            GUILayout.Label((UiScale() * 100f).ToString("0") + "%", Theme.Label, GUILayout.Width(64));
            if (GUILayout.Button("+", Theme.LinkBtn, GUILayout.Width(40), GUILayout.Height(28)))
                ModConfig.UiScale.Value = Mathf.Clamp(sc + 0.1f, 0.6f, 2f);
            if (GUILayout.Button(Ru ? "Сброс" : "Reset", Theme.LinkBtn, GUILayout.Width(80), GUILayout.Height(28)))
                ModConfig.UiScale.Value = 1f;
            GUILayout.EndHorizontal();
        }



#if !DISABLE_UPDATE_CHECKER
        private static void DrawUpdateChecker()
        {
            GUILayout.Label(Ru ? "Обновления" : "Updates", Theme.Section);
            GUILayout.Label((Ru ? "Установлена версия: " : "Installed version: ") + Plugin.Version, Theme.LabelDim);
            GUILayout.Label(UpdateChecker.IsChecking ? (Ru ? "Проверка..." : "Checking...") :
                UpdateChecker.Error ?? (UpdateChecker.UpdateAvailable ?
                (Ru ? "Доступна версия: " : "Available version: ") + UpdateChecker.LatestVersion :
                (UpdateChecker.HasChecked ? (Ru ? "Новых версий нет." : "No newer release.") : "")), Theme.Label);
            if (GUILayout.Button(Ru ? "Проверить" : "Check", Theme.LinkBtn))
                UpdateChecker.CheckAsync(true);
            LinkButton("GitHub Releases", "https://github.com/maxkir041/PEAK-MX/releases");
        }

#endif

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



        private static string SupportText()
        {
            switch (Localization.Current)
            {
                case Lang.Russian:
                    return "PEAK-MX остаётся бесплатным. Ваша поддержка помогает мне уделять больше времени разработке и быстрее выпускать обновления и исправления. Спасибо, что помогаете моду становиться лучше!";
                case Lang.Ukrainian:
                    return "PEAK-MX залишається безкоштовним. Ваша підтримка допомагає мені приділяти більше часу розробці та швидше випускати оновлення й виправлення. Дякую, що допомагаєте робити мод кращим!";
                case Lang.Spanish:
                    return "PEAK-MX sigue siendo gratuito. Tu apoyo me permite dedicar más tiempo al desarrollo y publicar actualizaciones y correcciones más rápido. ¡Gracias por ayudar a mejorar el mod!";
                case Lang.PortugueseBR:
                    return "PEAK-MX continua gratuito. Seu apoio me ajuda a dedicar mais tempo ao desenvolvimento e lançar atualizações e correções mais rápido. Obrigado por ajudar a melhorar o mod!";
                case Lang.German:
                    return "PEAK-MX bleibt kostenlos. Deine Unterstützung hilft mir, mehr Zeit in die Entwicklung zu investieren und Updates und Fehlerbehebungen schneller zu veröffentlichen. Danke, dass du den Mod besser machst!";
                case Lang.French:
                    return "PEAK-MX reste gratuit. Votre soutien me permet de consacrer plus de temps au développement et de publier les mises à jour et les correctifs plus rapidement. Merci de faire progresser le mod !";
                case Lang.Italian:
                    return "PEAK-MX resta gratuito. Il tuo supporto mi aiuta a dedicare più tempo allo sviluppo e a pubblicare aggiornamenti e correzioni più rapidamente. Grazie per aiutarmi a migliorare il mod!";
                case Lang.Polish:
                    return "PEAK-MX pozostaje darmowy. Twoje wsparcie pozwala mi poświęcać więcej czasu na rozwój oraz szybciej wydawać aktualizacje i poprawki. Dziękuję za pomoc w ulepszaniu moda!";
                case Lang.Turkish:
                    return "PEAK-MX ücretsiz kalmaya devam ediyor. Desteğiniz, geliştirmeye daha fazla zaman ayırmama ve güncellemeler ile hata düzeltmelerini daha hızlı yayınlamama yardımcı oluyor. Modu geliştirmeme yardımcı olduğunuz için teşekkürler!";
                case Lang.ChineseSimplified:
                    return "PEAK-MX 依然免费。你的支持让我能投入更多时间开发，更快推出更新和修复补丁。感谢你帮助这个模组变得更好！";
                case Lang.ChineseTraditional:
                    return "PEAK-MX 依然免費。你的支持讓我能投入更多時間開發，更快推出更新和修正檔。感謝你幫助這個模組變得更好！";
                case Lang.Japanese:
                    return "PEAK-MX は引き続き無料です。ご支援のおかげで開発により多くの時間を使い、更新や不具合修正をより早く届けられます。MOD の改善を支えてくださり、ありがとうございます！";
                case Lang.Korean:
                    return "PEAK-MX는 계속 무료입니다. 후원해 주시면 개발에 더 많은 시간을 쏟고 업데이트와 버그 수정을 더 빠르게 제공할 수 있습니다. 모드가 더 좋아질 수 있도록 도와주셔서 감사합니다!";
                default:
                    return "PEAK-MX remains free. Your support helps me spend more time on development and release updates and fixes sooner. Thank you for helping make the mod better!";
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

            _cosmeticScroll = BeginVerticalScroll(_cosmeticScroll, GUILayout.Height(Mathf.Clamp(_rect.height - 260f, 180f, 360f)));
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
            TipLast(Ru ? "Случайно меняет цвет, лицо, одежду, ленту, медаль и остальные категории." : "Randomly changes color, face, outfit, sash, medal, and the other categories.");
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
            int optionCount = 0;
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (option == null) continue;
                optionCount++;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label((Ru ? "Активно: " : "Active: ") + (current >= 0 && current < options.Length && options[current] != null ? CosmeticDisplayName(options[current]) : "-"), Theme.LabelDim);
            GUILayout.FlexibleSpace();
            if (blank >= 0 && GUILayout.Button(Ru ? "Выключить" : "Clear", Theme.LinkBtn, GUILayout.Height(26)))
                GameApi.SetCosmetic(category, blank);
            TipLast(Ru ? "Поставить пустой вариант в текущей категории косметики." : "Select the blank option for the current cosmetic category.");
            GUILayout.EndHorizontal();
            GUILayout.Label((Ru ? "Элементов: " : "Options: ") + optionCount, Theme.LabelDim);
            if (optionCount == 0)
                GUILayout.Label(Ru ? "В этой категории пока нет элементов." : "This category has no items yet.", Theme.LabelDim);

            _cosmeticScroll = BeginVerticalScroll(_cosmeticScroll, GUILayout.Height(Mathf.Clamp(_rect.height - 260f, 180f, 360f)));
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (option == null) continue;
                bool grantable = GameApi.IsCosmeticOptionGrantable(category, option);
                bool active = i == current;
                bool locked = false;
                string label = CosmeticDisplayName(option);
                try
                {
                    locked = grantable && option.IsLocked;
                }
                catch { }
                if (!grantable && !option.isBlank)
                    label += Ru ? " [невыдаваемое]" : " [unobtainable]";

                GUILayout.BeginHorizontal();
                if (DrawCosmeticListRow(category, option, label + (locked ? (Ru ? " [закрыто]" : " [locked]") : ""), active, locked))
                    GameApi.SetCosmetic(category, i);
                TipLast(!grantable && !option.isBlank
                    ? (Ru ? "Можно надеть, но нельзя открывать или закрывать как обычную косметику." : "Can be equipped, but cannot be unlocked or locked like normal cosmetics.")
                    : locked
                    ? (Ru ? "Элемент закрыт. Нажми «Открыть», чтобы разблокировать его отдельно." : "This option is locked. Press Unlock to unlock it separately.")
                    : (Ru ? "Выбрать этот элемент косметики." : "Select this cosmetic."));
                if (GUILayout.Button(Ru ? "Выбрать" : "Select", Theme.LinkBtn, GUILayout.Width(82), GUILayout.Height(30)))
                    GameApi.SetCosmetic(category, i);
                TipLast(Ru ? "Надеть этот элемент косметики." : "Equip this cosmetic option.");
                if (grantable && locked && GUILayout.Button(Ru ? "Открыть" : "Unlock", Theme.LinkBtn, GUILayout.Width(82), GUILayout.Height(30)))
                {
                    GameApi.UnlockCosmeticOption(option);
                    GameApi.SetCosmetic(category, i);
                }
                if (grantable && locked)
                    TipLast(Ru ? "Открыть конкретный элемент без разблокировки всей категории." : "Unlock this specific cosmetic without unlocking the whole category.");
                if (grantable && !locked && !option.isBlank && GUILayout.Button(Ru ? "Закрыть" : "Lock", Theme.DangerBtn, GUILayout.Width(82), GUILayout.Height(30)))
                {
                    if (active && blank >= 0 && blank != i)
                        GameApi.SetCosmetic(category, blank);
                    GameApi.LockCosmeticOption(option);
                }
                if (grantable && !locked && !option.isBlank)
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
            int page = Mathf.Clamp(_simpleSection[7], 0, 3);
            bool showAll = AdvancedUi;

            if (showAll || page == 0)
            {
                GUILayout.Label("PEAK-MX  v" + Plugin.Version, Theme.Section);
                GUILayout.Label(Ru ? "Автор: maxkir041" : "Author: maxkir041", Theme.Label);
                GUILayout.Label(L("ui.language"), Theme.Section);
                DrawLanguagePicker();
#if !DISABLE_UPDATE_CHECKER
                DrawUpdateChecker();
#endif
            }

            if (showAll || page == 1)
            {
                GUILayout.Label(Tx("Управление и вид", "Controls & appearance", "控制与外观", "控制與外觀"), Theme.Section);
                DrawMenuKeyBinding();
                DrawQuickActionSettings();
                DrawUiScaleSettings();
                DrawThemeColorSettings();
                DrawWindowSettings();
                DrawCompatibilityPanel();
            }

            if (showAll || page == 2)
            {
                GUILayout.Label(Ru ? "Ссылки" : "Links", Theme.Section);
                GUILayout.BeginHorizontal();
                LinkButton("GitHub", UrlGitHub);
                LinkButton("Thunderstore", UrlThunderstore);
                LinkButton("Nexus", UrlNexus);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                LinkButton("Steam", UrlSteam);
                LinkButton("Telegram", UrlTelegram);
                GUILayout.EndHorizontal();
            }

            if (showAll || page == 3)
            {
                DrawTesterThanks();
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
            }

        }

        private static void DrawTesterThanks()
        {
            GUILayout.Space(6f);
            GUILayout.BeginVertical(Theme.Panel9);
            GUILayout.Label(Localization.Pick(
                Localization.Current,
                "Отдельная благодарность тестировщикам",
                "Special thanks to the testers",
                "特别感谢测试人员",
                "特別感謝測試人員",
                uk: "Окрема подяка тестувальникам",
                ja: "テスターへの特別な感謝",
                ko: "테스터 특별 감사",
                es: "Agradecimiento especial a los testers",
                ptBr: "Agradecimento especial aos testadores",
                de: "Besonderer Dank an die Tester",
                fr: "Remerciements particuliers aux testeurs",
                it: "Un ringraziamento speciale ai tester",
                pl: "Specjalne podziękowania dla testerów",
                tr: "Test ekibine özel teşekkürler"), Theme.Section);
            GUILayout.Label("SaLaBiDay   |   Noado", Theme.Label);
            GUILayout.Label(Localization.Pick(
                Localization.Current,
                "За тщательное тестирование, подробные отчёты и помощь в развитии PEAK-MX.",
                "For thorough testing, detailed reports, and helping PEAK-MX grow.",
                "感谢细致测试、详细报告以及对 PEAK-MX 发展的帮助。",
                "感謝細緻測試、詳細報告以及對 PEAK-MX 發展的幫助。",
                uk: "За ретельне тестування, докладні звіти та допомогу в розвитку PEAK-MX.",
                ja: "丁寧なテスト、詳細な報告、そして PEAK-MX の改善への協力に感謝します。",
                ko: "꼼꼼한 테스트와 자세한 보고, PEAK-MX 발전에 도움을 주셔서 감사합니다.",
                es: "Por las pruebas exhaustivas, los informes detallados y la ayuda para mejorar PEAK-MX.",
                ptBr: "Pelos testes cuidadosos, relatórios detalhados e pela ajuda no desenvolvimento do PEAK-MX.",
                de: "Für gründliche Tests, detaillierte Berichte und die Hilfe bei der Weiterentwicklung von PEAK-MX.",
                fr: "Pour les tests approfondis, les rapports détaillés et l'aide au développement de PEAK-MX.",
                it: "Per i test accurati, le segnalazioni dettagliate e l'aiuto nello sviluppo di PEAK-MX.",
                pl: "Za dokładne testy, szczegółowe raporty i pomoc w rozwoju PEAK-MX.",
                tr: "Ayrıntılı testler, detaylı raporlar ve PEAK-MX'in gelişimine katkıları için."), Theme.LabelDim);
            GUILayout.EndVertical();
        }

        private static void DrawWindowModern(int id)
        {
            EnsureAntiCheatTabAllowed();
            if (_headerTex == null)
                _headerTex = Theme.GradientTex(Theme.HeaderBg, Theme.HeaderDim, Mathf.RoundToInt(HeaderHeight));
            if (_accentTex == null) _accentTex = Theme.Tex(Theme.Accent);

            GUI.DrawTexture(new Rect(0f, 0f, _rect.width, HeaderHeight), _headerTex);
            GUI.DrawTexture(new Rect(0f, HeaderHeight, _rect.width, 2f), _accentTex);

            float navWidth = Mathf.Clamp(_rect.width * 0.18f, 136f, 170f);
            float bodyTop = HeaderHeight + 12f;
            float bodyHeight = _rect.height - bodyTop - WindowPadding;
            float contentX = WindowPadding + navWidth + 12f;
            float contentWidth = _rect.width - contentX - WindowPadding;
            float footerHeight = Mathf.Clamp(_rect.height * 0.11f, 48f, 78f);

            GUILayout.BeginArea(new Rect(16f, 10f, _rect.width - 32f, HeaderHeight - 18f));
            GUILayout.BeginHorizontal();
            DrawBrandMark(32f);
            GUILayout.Space(8f);
            GUILayout.BeginVertical(GUILayout.Height(40f));
            GUILayout.Label("PEAK-MX", Theme.Title, GUILayout.Height(22f));
            GUILayout.Label($"v{Plugin.Version}  |  by maxkir041  |  {CurrentPageTitle()}", Theme.Subtitle, GUILayout.Height(16f));
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
            GUI.enabled = !AntiCheat.ClientToolsLocked;
            if (GUILayout.Button(Tx("★ Избранное", "★ Favorites", "★ 收藏", "★ 收藏"), _favoritesOpen ? Theme.NavItemActive : Theme.NavItem, GUILayout.Height(32)))
            {
                _favoritesOpen = true;
                _scroll = Vector2.zero;
            }
            GUI.enabled = true;
            GUILayout.Space(4f);
            for (int i = 0; i < Tabs.Length; i++)
            {
                var style = !_favoritesOpen && i == _tab ? Theme.NavItemActive : Theme.NavItem;
                bool allowed = !AntiCheat.ClientToolsLocked || IsAntiCheatAllowedTab(i);
                GUI.enabled = allowed;
                if (GUILayout.Button(L(Tabs[i]), style, GUILayout.Height(32)) && allowed)
                {
                    if (_tab != i || _favoritesOpen)
                        _scroll = Vector2.zero;
                    _tab = i;
                    _favoritesOpen = false;
                }
                GUI.enabled = true;
            }
            GUILayout.FlexibleSpace();
            DrawInterfaceModeToggle();
            GUILayout.Space(6f);
            GUILayout.Label("maxkir041", Theme.LabelDim);
            GUILayout.EndArea();

            float simpleNavHeight = SimpleNavigationHeight();
            if (simpleNavHeight > 0f)
            {
                GUILayout.BeginArea(new Rect(_contentRect.x + 12f, _contentRect.y + 8f, _contentRect.width - 24f, simpleNavHeight));
                DrawSimpleSectionNavigation();
                GUILayout.EndArea();
            }

            GUILayout.BeginArea(new Rect(
                _contentRect.x + 12f,
                _contentRect.y + 10f + simpleNavHeight,
                _contentRect.width - 24f,
                _contentRect.height - 24f - footerHeight - simpleNavHeight));
            _scroll = BeginVerticalScroll(_scroll);
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
                if (_favoritesOpen)
                {
                    DrawFavoritesModern();
                    return;
                }
                switch (_tab)
                {
                    case 0: DrawCharacterRedesign(); break;
                    case 1: DrawCheatsRedesign(); break;
                    case 2: DrawAdminModern(); break;
                    case 3: DrawInventoryRedesign(); break;
                    case 4: DrawWorldModern(); break;
                    case 5: DrawBadgesModern(); break;
                    case 6: DrawCosmeticsModern3(); break;
                    case 7: DrawAboutModern(); break;
                    case 8: DrawAntiCheatModern(); break;
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"[Menu:{CurrentPageTitle()}] {e}");
                GUILayout.Label(Ru ? "Вкладка временно недоступна после обновления игры. Ошибка записана в лог BepInEx." : "This tab hit a game-update compatibility error. Details were written to the BepInEx log.", Theme.LabelDim);
                if (GUILayout.Button(Ru ? "Обновить игроков/предметы" : "Refresh players/items", Theme.LinkBtn, GUILayout.Height(28)))
                {
                    GameApi.RefreshPlayers();
                    GameApi.LoadItems();
                }
            }
        }

        private static bool AdvancedUi => ModConfig.AdvancedUi != null && ModConfig.AdvancedUi.Value;

        private static string CurrentPageTitle()
        {
            return _favoritesOpen
                ? Tx("Избранное", "Favorites", "收藏", "收藏")
                : L(Tabs[Mathf.Clamp(_tab, 0, Tabs.Length - 1)]);
        }

        private static string[] SimpleSectionKeys(int tab)
        {
            return tab >= 0 && tab < SimpleSectionKeyMap.Length
                ? SimpleSectionKeyMap[tab]
                : Array.Empty<string>();
        }

        private static string[] SimpleSectionLabels(int tab)
        {
            if (_simpleSectionLabelMap == null || _simpleSectionLabelLanguage != Localization.Current)
            {
                _simpleSectionLabelLanguage = Localization.Current;
                _simpleSectionLabelMap = new string[Tabs.Length][];
                _simpleSectionLabelMap[0] = new[] { Tx("Движение", "Movement", "移动", "移動"), Tx("Состояние", "Status", "状态", "狀態") };
                _simpleSectionLabelMap[1] = new[]
                {
                    Tx("Основное", "Core", "主要", "主要"), "ESP",
                    Tx("Эффекты", "Effects", "效果", "效果"),
                    Tx("Полёт", "Movement", "移动", "移動"),
                    Tx("Приколы", "Pranks", "恶作剧", "惡作劇"),
                };
                _simpleSectionLabelMap[2] = new[]
                {
                    Tx("Игроки", "Players", "玩家", "玩家"), Tx("Защита", "Protection", "保护", "保護"),
                    Tx("Много игроков", "Many players", "多人大厅", "多人房間"), Tx("Журнал", "Log", "日志", "日誌")
                };
                _simpleSectionLabelMap[3] = new[] { Tx("Предметы", "Items", "物品", "物品"), Tx("Рюкзак", "Backpack", "背包", "背包"), EntitySectionName() };
                _simpleSectionLabelMap[4] = new[]
                {
                    Tx("Время", "Time", "时间", "時間"), Tx("Мир", "World", "世界", "世界"),
                    Tx("Поиск", "Finder", "查找", "搜尋"), Tx("Финиш", "Finish", "终点", "終點"),
                    Tx("Контейнеры", "Containers", "容器", "容器"),
                };
                _simpleSectionLabelMap[5] = Array.Empty<string>();
                _simpleSectionLabelMap[6] = Array.Empty<string>();
                _simpleSectionLabelMap[7] = new[]
                {
                    Tx("Основное", "General", "常规", "一般"), Tx("Управление", "Controls", "控制", "控制"),
                    Tx("Ссылки", "Links", "链接", "リンク"), Tx("Поддержка", "Support", "支持", "支持"),
                };
                _simpleSectionLabelMap[8] = new[]
                {
                    Tx("Обнаружение действий", "Action detection", "行为检测", "行為偵測"),
                    Tx("Контроль модов", "Mod control", "模组控制", "模組控制"),
                    Tx("События", "Events", "事件", "事件"),
                };
            }

            return tab >= 0 && tab < _simpleSectionLabelMap.Length
                ? _simpleSectionLabelMap[tab] ?? Array.Empty<string>()
                : Array.Empty<string>();
        }

        private static float SimpleNavigationHeight()
        {
            if (AdvancedUi || _favoritesOpen)
                return 0f;
            string[] labels = SimpleSectionLabels(_tab);
            if (labels.Length <= 1)
                return 0f;
            int rows = Mathf.CeilToInt(labels.Length / (float)Mathf.Max(1, TileColumns(104f)));
            return rows * 34f + 2f;
        }

        private static void DrawSimpleSectionNavigation()
        {
            string[] labels = SimpleSectionLabels(_tab);
            if (labels.Length <= 1)
                return;
            int old = Mathf.Clamp(_simpleSection[_tab], 0, labels.Length - 1);
            _simpleSection[_tab] = old;
            DrawChipBar(ref _simpleSection[_tab], labels);
            if (old != _simpleSection[_tab])
                _scroll = Vector2.zero;
        }

        private static bool TryGetSimpleSectionIndex(string key, out int index)
        {
            index = -1;
            if (AdvancedUi || string.IsNullOrWhiteSpace(key))
                return false;
            string[] keys = SimpleSectionKeys(_tab);
            for (int i = 0; i < keys.Length; i++)
            {
                if (!string.Equals(keys[i], key, StringComparison.Ordinal))
                    continue;
                index = i;
                return true;
            }
            return false;
        }

        private static void DrawInterfaceModeToggle()
        {
            bool value = AdvancedUi;
            Rect r = GUILayoutUtility.GetRect(80f, 48f, GUILayout.ExpandWidth(true), GUILayout.Height(48f));
            GUI.Label(new Rect(r.x, r.y, r.width, 18f), Tx("Интерфейс", "Interface", "界面", "介面"), Theme.LabelDim);
            Rect row = new Rect(r.x, r.y + 19f, r.width, 27f);
            if (GUI.Button(row, GUIContent.none, GUIStyle.none))
                value = !value;
            GUI.Label(new Rect(row.x, row.y, Mathf.Max(42f, row.width - 48f), row.height),
                value ? Tx("Расшир.", "Advanced", "高级", "進階") : Tx("Простой", "Simple", "简洁", "簡潔"), Theme.LabelDim);
            Rect track = new Rect(row.xMax - 42f, row.y + 2f, 40f, 22f);
            GUI.Box(track, GUIContent.none, value ? Theme.SwitchOn : Theme.SwitchOff);
            float knob = 16f;
            float knobX = value ? track.xMax - knob - 3f : track.x + 3f;
            if (Theme.KnobTex != null)
                GUI.DrawTexture(new Rect(knobX, track.y + 3f, knob, knob), Theme.KnobTex);
            if (value != AdvancedUi && ModConfig.AdvancedUi != null)
            {
                ModConfig.AdvancedUi.Value = value;
                _scroll = Vector2.zero;
            }
            if (Event.current != null && r.Contains(Event.current.mousePosition))
                _hoverTip = Tx(
                    "Простой режим показывает один рабочий блок. Расширенный возвращает точные слоты и все дополнительные параметры.",
                    "Simple mode shows one task group. Advanced mode restores precise slots and every extra setting.",
                    "简洁模式一次显示一个任务组；高级模式显示精确栏位和全部附加设置。",
                    "簡潔模式一次顯示一個工作群組；進階模式顯示精確欄位和全部附加設定。");
        }

        private static void DrawFavoritesModern()
        {
            GUILayout.Label(Tx("Избранные функции", "Favorite actions", "收藏功能", "收藏功能"), Theme.Section);

            bool hasFavorites = QuickActions.FavoriteCount > 0;
            if (BeginSection("favorites.saved", Tx("Быстрый доступ", "Quick access", "快速访问", "快速存取"), true))
            {
                if (!hasFavorites)
                    GUILayout.Label(Tx("Пока пусто", "No favorites yet", "暂无收藏", "尚無收藏"), Theme.LabelDim);
                for (int i = 1; i < QuickActions.Actions.Length; i++)
                {
                    QuickActions.Definition action = QuickActions.Actions[i];
                    if (!QuickActions.IsFavorite(action.Id))
                        continue;
                    DrawFavoriteActionRow(action, true);
                }
                EndSection();
            }

            if (BeginSection("favorites.catalog", Tx("Добавить функции", "Add actions", "添加功能", "新增功能"), !hasFavorites))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Tx("Поиск:", "Search:", "搜索:", "搜尋:"), Theme.LabelDim, GUILayout.Width(54f));
                _favoriteActionSearch = GUILayout.TextField(_favoriteActionSearch ?? "", Theme.TextInput, GUILayout.Height(30f));
                GUILayout.EndHorizontal();
                string query = (_favoriteActionSearch ?? "").Trim();
                for (int i = 1; i < QuickActions.Actions.Length; i++)
                {
                    QuickActions.Definition action = QuickActions.Actions[i];
                    string label = Ru ? action.Ru : action.En;
                    if (query.Length > 0
                        && label.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0
                        && action.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    DrawFavoriteActionRow(action, QuickActions.IsFavorite(action.Id));
                }
                EndSection();
            }
        }

        private static void DrawFavoriteActionRow(QuickActions.Definition action, bool favorite)
        {
            string label = Ru ? action.Ru : action.En;
            string tip = Ru ? action.TipRu : action.TipEn;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(favorite ? "★" : "☆", favorite ? Theme.ChipActive : Theme.Chip, GUILayout.Width(36f), GUILayout.Height(32f)))
                QuickActions.SetFavorite(action.Id, !favorite);
            TipLast(favorite
                ? Tx("Убрать из избранного", "Remove from favorites", "取消收藏", "取消收藏")
                : Tx("Добавить в избранное", "Add to favorites", "添加收藏", "加入收藏"));
            if (GUILayout.Button(label, favorite ? Theme.DonateBtn : Theme.LinkBtn, GUILayout.Height(32f)))
                QuickActions.Execute(action.Id);
            TipLast(tip);
            GUILayout.EndHorizontal();
        }

        private static void DrawCharacterRedesign()
        {
            if (BeginSection("character.redesign.move", L("tab.character"), true))
            {
                DrawToggleTiles(
                    new ToggleTileSpec(L("feat.speed"), Localization.D("feat.speed"), () => ModConfig.SpeedMod, v => ModConfig.SpeedMod = v),
                    new ToggleTileSpec(L("feat.jump"), Localization.D("feat.jump"), () => ModConfig.JumpMod, v => ModConfig.JumpMod = v),
                    new ToggleTileSpec(Tx("Бесконечные прыжки", "Infinite jumps", "无限跳跃", "無限跳躍"), Tx("Позволяет прыгать повторно в воздухе без ожидания земли.", "Allows repeated jumps in the air without touching the ground.", "允许在空中反复跳跃，不必等落地。", "允許在空中反覆跳躍，不必等落地。"), () => ModConfig.InfiniteJumps, v => ModConfig.InfiniteJumps = v),
                    new ToggleTileSpec(Tx("Без скольжения", "No sliding", "防滑", "防滑"), Tx("Снижает соскальзывание и считает крутые поверхности пригодными для стояния.", "Reduces sliding and treats steep surfaces as standable.", "降低滑落并把陡坡视为可站立。", "降低滑落並把陡坡視為可站立。"), () => ModConfig.NoSlipperySurfaces, v => ModConfig.NoSlipperySurfaces = v)
                );

                if (ModConfig.SpeedMod)
                    ModConfig.SpeedAmount = Slider("Speed x", ModConfig.SpeedAmount, 0.5f, 5f);
                if (ModConfig.JumpMod)
                    ModConfig.JumpAmount = Slider("Jump x", ModConfig.JumpAmount, 1f, 5f);

                GUILayout.Space(4f);
                DrawToggleTiles(
                    new ToggleTileSpec(L("feat.climb"), Localization.D("feat.climb"), () => ModConfig.ClimbMod, v => ModConfig.ClimbMod = v),
                    new ToggleTileSpec(L("feat.vineclimb"), Localization.D("feat.vineclimb"), () => ModConfig.VineClimbMod, v => ModConfig.VineClimbMod = v),
                    new ToggleTileSpec(L("feat.ropeclimb"), Localization.D("feat.ropeclimb"), () => ModConfig.RopeClimbMod, v => ModConfig.RopeClimbMod = v)
                );
                if (ModConfig.ClimbMod)
                    ModConfig.ClimbAmount = Slider(Tx("Лазание x", "Climb x", "攀爬 x", "攀爬 x"), ModConfig.ClimbAmount, 1f, 5f);
                if (ModConfig.VineClimbMod)
                    ModConfig.VineClimbAmount = Slider(Tx("Лианы x", "Vines x", "藤蔓 x", "藤蔓 x"), ModConfig.VineClimbAmount, 1f, 5f);
                if (ModConfig.RopeClimbMod)
                    ModConfig.RopeClimbAmount = Slider(Tx("Веревки x", "Ropes x", "绳索 x", "繩索 x"), ModConfig.RopeClimbAmount, 1f, 5f);
                ModConfig.ClimbStaminaConsumptionPercent = Slider(Tx("Расход стамины при лазании %", "Climbing stamina cost %", "攀爬耐力消耗 %", "攀爬耐力消耗 %"), ModConfig.ClimbStaminaConsumptionPercent, 0f, 300f);
                EndSection();
            }

            if (BeginSection("character.redesign.status", Tx("Стамина и недуги", "Stamina & afflictions", "耐力与异常", "耐力與異常"), true))
            {
                Character target = TargetPicker(ref _statusTarget);
                DrawActionTiles(
                    new ActionTileSpec(Tx("Полная стамина", "Full stamina", "补满耐力", "補滿耐力"), Tx("Заполняет обычную стамину выбранной цели.", "Fills the selected target's regular stamina.", "填满所选目标普通耐力。", "填滿所選目標普通耐力。"), () => GameApi.FullStamina(target)),
                    new ActionTileSpec(Tx("Полная экстра", "Full extra", "补满额外耐力", "補滿額外耐力"), Tx("Заполняет дополнительную стамину выбранной цели.", "Fills the selected target's extra stamina.", "填满所选目标额外耐力。", "填滿所選目標額外耐力。"), () =>
                    {
                        ModConfig.ExtraStaminaPercent = 100f;
                        SetNumberText(Tx("Экстра стамина %", "Extra stamina %", "额外耐力 %", "額外耐力 %"), 0f, 100f, ModConfig.ExtraStaminaPercent);
                        GameApi.FullExtraStamina(target);
                    }),
                    new ActionTileSpec(Tx("Снять недуги", "Clear afflictions", "清除异常", "清除異常"), Tx("Очищает все недуги выбранной цели.", "Clears every affliction from the selected target.", "清除所选目标所有异常。", "清除所選目標所有異常。"), () => GameApi.ClearAllStatus(target))
                );

                string extraLabel = Tx("Экстра стамина %", "Extra stamina %", "额外耐力 %", "額外耐力 %");
                ModConfig.ExtraStaminaPercent = NumberField(extraLabel, ModConfig.ExtraStaminaPercent, 0f, 100f);
                if (GUILayout.Button(Tx("Выдать экстрастамину", "Set extra stamina", "设置额外耐力", "設定額外耐力"), Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.SetExtraStamina(target, ModConfig.ExtraStaminaPercent / 100f);

                ModConfig.StaminaConsumptionPercent = Slider(Tx("Расход стамины %", "Stamina cost %", "耐力消耗 %", "耐力消耗 %"), ModConfig.StaminaConsumptionPercent, 0f, 300f);
                ModConfig.StaminaRegenPercent = Slider(Tx("Восстановление %", "Stamina regen %", "耐力恢复 %", "耐力恢復 %"), ModConfig.StaminaRegenPercent, 0f, 300f);
                DrawToggleTiles(new ToggleTileSpec(Tx("Своя задержка восстановления", "Custom regen delay", "自定义恢复延迟", "自訂恢復延遲"), Tx("Заменяет задержку перед восстановлением стамины.", "Overrides the delay before stamina starts regenerating.", "覆盖耐力恢复前的延迟。", "覆蓋耐力恢復前的延遲。"), () => ModConfig.StaminaRegenDelayMod, v => ModConfig.StaminaRegenDelayMod = v));
                if (ModConfig.StaminaRegenDelayMod)
                    ModConfig.StaminaRegenDelay = Slider(Tx("Задержка", "Delay", "延迟", "延遲"), ModConfig.StaminaRegenDelay, 0f, 5f);

                GUILayout.Space(8f);
                GUILayout.Label(Tx("Работа с недугами", "Afflictions", "异常状态", "異常狀態"), Theme.Section);
                _statusCustomDeltaPercent = Slider(
                    Tx("Своё число (-100…+100)", "Custom change (-100…+100)", "自定义变化 (-100…+100)", "自訂變化 (-100…+100)"),
                    _statusCustomDeltaPercent,
                    -100f,
                    100f);
                DrawAfflictionTable(target);
                EndSection();
            }
        }

        private static void DrawAfflictionTable(Character target)
        {
            string[] names = Ru ? GameApi.StatusRu : GameApi.StatusEn;
            int count = Mathf.Min(names.Length, 15);
            bool compact = (_contentRect.width > 0f ? _contentRect.width : 620f) < 610f;
            for (int i = 0; i < count; i++)
            {
                int status = i;
                Color oldContentColor = GUI.contentColor;
                GUI.contentColor = StatusUiColor(i);
                GUILayout.BeginHorizontal();
                GUILayout.Label(names[i], Theme.RowLabel, GUILayout.Width(compact ? 92f : 126f), GUILayout.Height(28f));
                if (GUILayout.Button("+25", Theme.LinkBtn, GUILayout.Width(compact ? 44f : 52f), GUILayout.Height(28f))) GameApi.AdjustStatus(target, status, 0.25f);
                if (GUILayout.Button("+50", Theme.LinkBtn, GUILayout.Width(compact ? 44f : 52f), GUILayout.Height(28f))) GameApi.AdjustStatus(target, status, 0.50f);
                if (GUILayout.Button("-25", Theme.LinkBtn, GUILayout.Width(compact ? 44f : 52f), GUILayout.Height(28f))) GameApi.AdjustStatus(target, status, -0.25f);
                if (GUILayout.Button("-50", Theme.LinkBtn, GUILayout.Width(compact ? 44f : 52f), GUILayout.Height(28f))) GameApi.AdjustStatus(target, status, -0.50f);
                if (!compact)
                {
                    if (GUILayout.Button(Tx("Убрать", "Clear", "清除", "清除"), Theme.LinkBtn, GUILayout.Width(68f), GUILayout.Height(28f))) GameApi.SetStatus(target, status, 0f);
                    if (GUILayout.Button(Tx("Своё", "Custom", "自定义", "自訂"), Theme.LinkBtn, GUILayout.Width(64f), GUILayout.Height(28f))) GameApi.AdjustStatus(target, status, _statusCustomDeltaPercent / 100f);
                }
                GUILayout.EndHorizontal();

                if (compact)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(92f);
                    if (GUILayout.Button(Tx("Убрать", "Clear", "清除", "清除"), Theme.LinkBtn, GUILayout.Width(76f), GUILayout.Height(26f))) GameApi.SetStatus(target, status, 0f);
                    if (GUILayout.Button(Tx("Своё", "Custom", "自定义", "自訂"), Theme.LinkBtn, GUILayout.Width(76f), GUILayout.Height(26f))) GameApi.AdjustStatus(target, status, _statusCustomDeltaPercent / 100f);
                    GUILayout.EndHorizontal();
                }
                GUI.contentColor = oldContentColor;
                GUILayout.Space(3f);
            }
        }

        private static Color StatusUiColor(int index)
        {
            return index switch
            {
                0 => new Color(1f, 0.37f, 0.40f),
                1 => new Color(1f, 0.72f, 0.10f),
                2 => new Color(0.24f, 0.80f, 1f),
                3 => new Color(0.78f, 0.52f, 1f),
                4 => new Color(1f, 0.55f, 0.66f),
                5 => new Color(0.78f, 0.78f, 0.82f),
                6 => new Color(0.92f, 0.66f, 1f),
                7 => new Color(0.78f, 0.82f, 0.88f),
                8 => new Color(1f, 0.48f, 0.20f),
                9 => new Color(0.35f, 0.82f, 0.45f),
                10 => new Color(0.90f, 0.58f, 0.66f),
                11 => new Color(0.86f, 0.90f, 0.94f),
                12 => new Color(1f, 0.62f, 0.24f),
                13 => new Color(0.55f, 0.66f, 0.82f),
                _ => new Color(0.20f, 0.82f, 0.42f),
            };
        }

        private static void DrawCheatsRedesign()
        {
            if (BeginSection("cheats.redesign.core", L("tab.cheats"), true))
            {
                DrawToggleTiles(
                    new ToggleTileSpec(L("feat.god"), Localization.D("feat.god"), () => ModConfig.GodMode, v => ModConfig.GodMode = v),
                    new ToggleTileSpec(L("feat.infstam"), Localization.D("feat.infstam"), () => ModConfig.InfiniteStamina, v => ModConfig.InfiniteStamina = v),
                    new ToggleTileSpec(L("feat.nofall"), Localization.D("feat.nofall"), () => ModConfig.NoFallDamage, v => ModConfig.NoFallDamage = v),
                    new ToggleTileSpec(Tx("Без ragdoll", "No ragdoll", "禁用布娃娃", "停用布娃娃"), Tx("Блокирует принудительный ragdoll от падения и соскальзывания.", "Blocks forced ragdoll from falling and sliding.", "阻止坠落和滑落触发布娃娃。", "阻止墜落和滑落觸發布娃娃。"), () => ModConfig.NoFallingRagdoll, v => ModConfig.NoFallingRagdoll = v),
                    new ToggleTileSpec(L("feat.noweight"), Localization.D("feat.noweight"), () => ModConfig.NoWeight, v => ModConfig.NoWeight = v),
                    new ToggleTileSpec(L("feat.lockstatus"), Localization.D("feat.lockstatus"), () => ModConfig.LockStatus, v => ModConfig.LockStatus = v),
                    new ToggleTileSpec(Tx("Слышно всех", "Hear everyone", "听见所有人", "聽見所有人"), Tx("Локально делает голоса всех игроков слышимыми на карте без 2D-эха.", "Locally makes every player's voice audible across the map without 2D echo.", "本地让所有玩家全图可听且没有 2D 回声。", "本地讓所有玩家全圖可聽且沒有 2D 回聲。"), () => ModConfig.GlobalVoice, v => ModConfig.GlobalVoice = v)
                );
                DrawActionTiles(
                    new ActionTileSpec(Tx("Слышно всем", "Enable for everyone", "为所有人启用", "為所有人啟用"), Tx("Отправляет команду всем клиентам с PEAK-MX.", "Sends the hear-all command to every PEAK-MX client.", "向所有 PEAK-MX 客户端发送全图语音命令。", "向所有 PEAK-MX 客戶端發送全圖語音命令。"), () => VoiceControl.BroadcastHearAll(true)),
                    new ActionTileSpec(Tx("Отключить всем", "Disable for everyone", "为所有人关闭", "為所有人關閉"), Tx("Снимает сетевой режим слышимости у клиентов с PEAK-MX.", "Turns off network hear-all on PEAK-MX clients.", "关闭 PEAK-MX 客户端的全图语音。", "關閉 PEAK-MX 客戶端的全圖語音。"), () => VoiceControl.BroadcastHearAll(false))
                );
                EndSection();
            }

            if (BeginSection("cheats.redesign.esp", Tx("ESP и поиск", "ESP & search", "ESP 与搜索", "ESP 與搜尋"), false))
            {
                DrawToggleTiles(
                    new ToggleTileSpec(Tx("ESP игроков", "Player ESP", "玩家 ESP", "玩家 ESP"), Tx("Показывает игроков через стены экранными метками с расстоянием.", "Shows players through walls with labels and distance.", "用带距离的屏幕标记显示墙后的玩家。", "用帶距離的畫面標記顯示牆後的玩家。"), () => ModConfig.PlayerEsp, v => ModConfig.PlayerEsp = v),
                    new ToggleTileSpec(Tx("ESP предметов", "Item ESP", "物品 ESP", "物品 ESP"), Tx("Показывает выбранные предметы через стены.", "Shows selected items through walls.", "透视显示所选物品。", "透視顯示所選物品。"), () => ModConfig.ItemEsp, v => ModConfig.ItemEsp = v),
                    new ToggleTileSpec(Tx("ESP объектов", "Object ESP", "对象 ESP", "物件 ESP"), Tx("Показывает опасности, костры, предметы на спину и амулеты.", "Shows hazards, campfires, back-slot items and amulets.", "显示危险、营火、背部物品和护符。", "顯示危險、營火、背部物品和護符。"), () => ModConfig.WorldEsp != null && ModConfig.WorldEsp.Value, v => { if (ModConfig.WorldEsp != null) ModConfig.WorldEsp.Value = v; })
                );

                if (ModConfig.PlayerEsp)
                {
                    ModConfig.PlayerEspDistance = Slider(Tx("Дистанция игроков", "Player distance", "玩家距离", "玩家距離"), ModConfig.PlayerEspDistance, 25f, 2500f);
                    GUILayout.BeginHorizontal();
                    ModConfig.PlayerEspBoxes = ToggleCompact(Tx("Рамки", "Boxes", "方框", "方框"), ModConfig.PlayerEspBoxes);
                    ModConfig.PlayerEspLines = ToggleCompact(Tx("Линии", "Lines", "连线", "連線"), ModConfig.PlayerEspLines);
                    GUILayout.EndHorizontal();
                }

                if (ModConfig.ItemEsp)
                {
                    ModConfig.ItemEspDistance = Slider(Tx("Дистанция предметов", "Item distance", "物品距离", "物品距離"), ModConfig.ItemEspDistance, 25f, 2000f);
                    ModConfig.ItemEspLines = ToggleCompact(Tx("Линии к предметам", "Item lines", "物品连线", "物品連線"), ModConfig.ItemEspLines);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Tx("Все предметы", "All items", "全部物品", "全部物品"), ModConfig.ItemEspSelectedIndex < 0 ? Theme.ChipActive : Theme.Chip, GUILayout.Width(120), GUILayout.Height(30)))
                        ModConfig.ItemEspSelectedIndex = -1;
                    GUILayout.Label(Tx("Поиск:", "Search:", "搜索:", "搜尋:"), Theme.LabelDim, GUILayout.Width(54));
                    _itemEspSearch = GUILayout.TextField(_itemEspSearch ?? "", Theme.TextInput, GUILayout.Height(30));
                    ModConfig.ItemEspSearch = _itemEspSearch;
                    GUILayout.EndHorizontal();
                    DrawItemCategoryBar(ref _itemEspCategory);
                    GameApi.EnsureItemsLoaded();
                    _itemEspScroll = BeginVerticalScroll(_itemEspScroll, GUILayout.Height(176));
                    DrawItemTileGrid(_itemEspSearch, _itemEspCategory, ModConfig.ItemEspSelectedIndex, i => ModConfig.ItemEspSelectedIndex = i);
                    GUILayout.EndScrollView();
                }

                if (ModConfig.WorldEsp != null && ModConfig.WorldEsp.Value)
                {
                    ModConfig.WorldEspDistance.Value = Slider(Tx("Дистанция объектов", "Object distance", "对象距离", "物件距離"), ModConfig.WorldEspDistance.Value, 25f, 2500f);
                    GUILayout.BeginHorizontal();
                    ModConfig.WorldEspLines.Value = ToggleCompact(Tx("Линии", "Lines", "连线", "連線"), ModConfig.WorldEspLines.Value);
                    ModConfig.WorldEspDangers.Value = ToggleCompact(Tx("Опасности", "Hazards", "危险", "危險"), ModConfig.WorldEspDangers.Value);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    ModConfig.WorldEspCampfires.Value = ToggleCompact(Tx("Костры", "Campfires", "营火", "營火"), ModConfig.WorldEspCampfires.Value);
                    ModConfig.WorldEspBackItems.Value = ToggleCompact(Tx("Рюкзаки/джеты", "Back items", "背部物品", "背部物品"), ModConfig.WorldEspBackItems.Value);
                    ModConfig.WorldEspAmulets.Value = ToggleCompact(Tx("Амулеты", "Amulets", "护符", "護符"), ModConfig.WorldEspAmulets.Value);
                    GUILayout.EndHorizontal();
                }
                EndSection();
            }

            if (BeginSection("cheats.redesign.status", Tx("Защита от эффектов", "Status protection", "状态保护", "狀態保護"), false))
            {
                DrawToggleTiles(new ToggleTileSpec(Tx("Без всех эффектов", "No status effects", "无状态效果", "無狀態效果"), Tx("Постоянно очищает все недуги локального персонажа.", "Continuously clears every affliction from the local character.", "持续清除本地角色所有负面状态。", "持續清除本地角色所有負面狀態。"), () => ModConfig.NoStatusEffects, v => ModConfig.NoStatusEffects = v));
                if (!ModConfig.NoStatusEffects)
                {
                    DrawToggleTiles(
                        new ToggleTileSpec(Tx("Без травм", "No injury", "无受伤", "無受傷"), "", () => ModConfig.NoInjury, v => ModConfig.NoInjury = v),
                        new ToggleTileSpec(Tx("Без голода", "No hunger", "无饥饿", "無飢餓"), "", () => ModConfig.NoHunger, v => ModConfig.NoHunger = v),
                        new ToggleTileSpec(Tx("Без холода", "No cold", "无寒冷", "無寒冷"), "", () => ModConfig.NoCold, v => ModConfig.NoCold = v),
                        new ToggleTileSpec(Tx("Без яда", "No poison", "无中毒", "無中毒"), "", () => ModConfig.NoPoison, v => ModConfig.NoPoison = v),
                        new ToggleTileSpec(Tx("Без проклятия", "No curse", "无诅咒", "無詛咒"), "", () => ModConfig.NoCurse, v => ModConfig.NoCurse = v),
                        new ToggleTileSpec(Tx("Без сонливости", "No drowsy", "无困倦", "無睏倦"), "", () => ModConfig.NoDrowsy, v => ModConfig.NoDrowsy = v),
                        new ToggleTileSpec(Tx("Без жары", "No hot", "无炎热", "無炎熱"), "", () => ModConfig.NoHot, v => ModConfig.NoHot = v),
                        new ToggleTileSpec(Tx("Без краба", "No crab", "无螃蟹", "無螃蟹"), "", () => ModConfig.NoCrab, v => ModConfig.NoCrab = v),
                        new ToggleTileSpec(Tx("Без шипов", "No thorns", "无刺", "無刺"), "", () => ModConfig.NoThorns, v => ModConfig.NoThorns = v),
                        new ToggleTileSpec(Tx("Без спор", "No spores", "无孢子", "無孢子"), "", () => ModConfig.NoSpores, v => ModConfig.NoSpores = v),
                        new ToggleTileSpec(Tx("Без паутины", "No web", "无蛛网", "無蛛網"), "", () => ModConfig.NoWeb, v => ModConfig.NoWeb = v),
                        new ToggleTileSpec(Tx("Без стрел", "No arrows", "无箭矢", "無箭矢"), "", () => ModConfig.NoArrows, v => ModConfig.NoArrows = v),
                        new ToggleTileSpec(Tx("Без окаменения", "No petrify", "无石化", "無石化"), "", () => ModConfig.NoPetrify, v => ModConfig.NoPetrify = v),
                        new ToggleTileSpec(Tx("Без мухоловки", "No flytrap", "无捕蝇草", "無捕蠅草"), "", () => ModConfig.NoFlyTrap, v => ModConfig.NoFlyTrap = v)
                    );
                }
                EndSection();
            }

            if (BeginSection("cheats.redesign.move", Tx("Полёт и телепорт", "Fly & teleport", "飞行与传送", "飛行與傳送"), false))
            {
                DrawToggleTiles(
                    new ToggleTileSpec(L("feat.tpping"), Localization.D("feat.tpping"), () => ModConfig.TeleportToPing, v => ModConfig.TeleportToPing = v),
                    new ToggleTileSpec(Tx("Дальнее взаимодействие", "Long interaction", "远距离交互", "遠距離互動"), Tx("Увеличивает дистанцию взаимодействия с предметами и объектами.", "Increases interaction distance for items and objects.", "增加与物品和对象交互的距离。", "增加與物品和物件互動的距離。"), () => ModConfig.LongInteraction, v => ModConfig.LongInteraction = v),
                    new ToggleTileSpec(Tx("Кино-камера", "Cinematic camera", "电影镜头", "電影鏡頭"), Tx("Свободная камера для съемки и обзора.", "Free camera for filming and scouting.", "用于拍摄和观察的自由镜头。", "用於拍攝和觀察的自由鏡頭。"), () => ModConfig.CinematicCamera, v => ModConfig.CinematicCamera = v),
                    new ToggleTileSpec(L("feat.fly"), Localization.D("feat.fly"), () => ModConfig.Fly, v => ModConfig.Fly = v)
                );
                if (ModConfig.LongInteraction)
                    ModConfig.InteractionDistance = Slider(Tx("Дистанция", "Distance", "距离", "距離"), ModConfig.InteractionDistance, 2f, 80f);
                if (ModConfig.CinematicCamera)
                {
                    ModConfig.CinematicCameraSpeed = Slider(Tx("Скорость камеры", "Camera speed", "镜头速度", "鏡頭速度"), ModConfig.CinematicCameraSpeed, 0.5f, 40f);
                    ModConfig.CinematicCameraFov = Slider("FOV", ModConfig.CinematicCameraFov, 1f, 120f);
                }
                if (ModConfig.Fly)
                {
                    DrawToggleTiles(new ToggleTileSpec(Tx("Ноклип", "Noclip", "穿墙", "穿牆"), Tx("Во время полёта отключает столкновения локального персонажа.", "Disables local character collisions while flying.", "飞行时关闭本地角色碰撞。", "飛行時關閉本地角色碰撞。"), () => ModConfig.Noclip, v => ModConfig.Noclip = v));
                    ModConfig.FlySpeed = Slider(Tx("Скорость", "Speed", "速度", "速度"), ModConfig.FlySpeed, 1f, 50f);
                    ModConfig.FlyAcceleration = Slider(Tx("Ускорение", "Accel", "加速度", "加速度"), ModConfig.FlyAcceleration, 1f, 100f);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(Tx("Коорд.:", "Coords:", "坐标:", "座標:"), Theme.LabelDim, GUILayout.Width(54));
                GUILayout.Label("X", Theme.LabelDim, GUILayout.Width(12)); _tpX = GUILayout.TextField(_tpX ?? "0", Theme.TextInput, GUILayout.Width(76), GUILayout.Height(30));
                GUILayout.Label("Y", Theme.LabelDim, GUILayout.Width(12)); _tpY = GUILayout.TextField(_tpY ?? "0", Theme.TextInput, GUILayout.Width(76), GUILayout.Height(30));
                GUILayout.Label("Z", Theme.LabelDim, GUILayout.Width(12)); _tpZ = GUILayout.TextField(_tpZ ?? "0", Theme.TextInput, GUILayout.Width(76), GUILayout.Height(30));
                if (GUILayout.Button(Tx("ТП", "TP", "传送", "傳送"), Theme.DonateBtn, GUILayout.Width(64), GUILayout.Height(30)))
                {
                    if (float.TryParse(_tpX, out float x) & float.TryParse(_tpY, out float y) & float.TryParse(_tpZ, out float z))
                        GameApi.TeleportToCoords(x, y, z);
                }
                GUILayout.EndHorizontal();
                EndSection();
            }

            if (BeginSection("cheats.redesign.pranks", Tx("Приколы", "Pranks", "恶作剧", "惡作劇"), false))
            {
                Character target = TargetPicker(ref _prankTarget);
                DrawActionTiles(
                    new ActionTileSpec(Tx("Наслать недуги", "Pile afflictions", "施加异常", "施加異常"), Tx("Накладывает набор неприятных эффектов на выбранную цель.", "Adds several nasty effects to the selected target.", "给目标施加多个异常。", "給目標施加多個異常。"), () => GameApi.PrankAfflict(target)),
                    new ActionTileSpec(Tx("Вылечить", "Cure", "治愈", "治癒"), Tx("Очищает все недуги выбранной цели.", "Clears all afflictions from the selected target.", "清除目标所有异常。", "清除目標所有異常。"), () => GameApi.ClearAllStatus(target)),
                    new ActionTileSpec(Tx("Пнуть", "Push", "推开", "推開"), Tx("Толкает выбранную цель от тебя.", "Pushes the selected target away from you.", "把目标从你身边推开。", "把目標從你身邊推開。"), () => GameApi.PushPlayer(target)),
                    new ActionTileSpec(Tx("Взорвать", "Explode", "爆炸", "爆炸"), Tx("Создает взрыв прямо в выбранной цели.", "Creates an explosion directly inside the selected target.", "在目标身上制造爆炸。", "在目標身上製造爆炸。"), () => GameApi.ExplodePlayer(target), 2),
                    new ActionTileSpec(Tx("Окаменить", "Petrify", "石化", "石化"), Tx("Выставляет окаменение выбранной цели на 100%.", "Sets the target's petrify amount to 100%.", "把目标石化值设为 100%。", "把目標石化值設為 100%。"), () => GameApi.PetrifyPlayer(target)),
                    new ActionTileSpec(Tx("Снять камень", "Clear petrify", "解除石化", "解除石化"), Tx("Сбрасывает окаменение выбранной цели.", "Clears petrify from the selected target.", "清除目标石化。", "清除目標石化。"), () => GameApi.ClearPetrify(target)),
                    new ActionTileSpec(Tx("Стрела", "Arrow", "箭矢", "箭矢"), Tx("Добавляет стрелу на своего персонажа; для чужого игрока нужен его клиент.", "Adds an arrow to your own character; remote targets need their client.", "给自己角色添加箭矢；远程目标需要对方客户端。", "給自己角色新增箭矢；遠端目標需要對方客戶端。"), () => GameApi.AddArrowPrank(target, 1)),
                    new ActionTileSpec(Tx("5 стрел", "5 arrows", "5 支箭", "5 支箭"), Tx("Добавляет несколько стрел.", "Adds several arrows.", "添加多支箭。", "新增多支箭。"), () => GameApi.AddArrowPrank(target, 5)),
                    new ActionTileSpec(Tx("Поножовщина", "Knife Fight", "匕首混战", "匕首混戰"), Tx("Через 5 секунд выдаёт каждому игроку ритуальный кинжал в третий слот.", "After 5 seconds, gives every player a ritual dagger in slot three.", "5 秒后给每位玩家的第三栏发放仪式匕首。", "5 秒後給每位玩家的第三欄發放儀式匕首。"), () => GameApi.ScheduleKnifeFight(), 2)
                );

                _petrifyAmount = Slider(Tx("Окаменение %", "Petrify %", "石化 %", "石化 %"), _petrifyAmount, 0f, 100f);
                DrawActionTiles(
                    new ActionTileSpec(Tx("Поставить %", "Set %", "设置 %", "設定 %"), Tx("Выставляет окаменение на выбранное значение.", "Sets petrify to the selected value.", "把石化设为所选数值。", "把石化設為所選數值。"), () => GameApi.SetPetrifyAmount(target, Mathf.RoundToInt(_petrifyAmount))),
                    new ActionTileSpec(Tx("Мини-взрыв", "Mini explosion", "小爆炸", "小爆炸"), Tx("Создает слабый взрыв в цели.", "Creates a weaker explosion inside the target.", "在目标身上制造较弱爆炸。", "在目標身上製造較弱爆炸。"), () => GameApi.ExplodePlayer(target, 14f, 4f)),
                    new ActionTileSpec(Tx("Легкий пинок", "Light push", "轻推", "輕推"), Tx("Слабый толчок.", "A softer push.", "较弱推力。", "較弱推力。"), () => GameApi.PushPlayer(target, 7f))
                );

                DrawActionTiles(
                    new ActionTileSpec(Tx("Забить выбранным", "Stuff selected", "塞满所选", "塞滿所選"), Tx("Заполняет слоты целью выбранным предметом из вкладки Инвентарь.", "Fills target slots with the Inventory-selected item.", "用物品栏所选物品填满目标栏位。", "用物品欄所選物品填滿目標欄位。"), () => GameApi.FillInventoryWith(target, _selItem), 1),
                    new ActionTileSpec(Tx("Забить золотом", "Fill gold", "塞满黄金", "塞滿黃金"), Tx("Заполняет слоты золотом.", "Fills slots with gold.", "用黄金填满栏位。", "用黃金填滿欄位。"), () => GameApi.FillInventoryWithGold(target)),
                    new ActionTileSpec(Tx("Забить паутиной", "Fill webs", "塞满蛛网", "塞滿蛛網"), Tx("Заполняет/накладывает паутину.", "Fills or applies web.", "填充或施加蛛网。", "填充或施加蛛網。"), () => GameApi.FillInventoryWithWebs(target)),
                    new ActionTileSpec(Tx("Случайные", "Random items", "随机物品", "隨機物品"), Tx("Заполняет слоты случайными предметами.", "Fills slots with random items.", "用随机物品填满栏位。", "用隨機物品填滿欄位。"), () => GameApi.FillInventoryWithRandom(target)),
                    new ActionTileSpec(Tx("Очистить", "Clear inventory", "清空", "清空"), Tx("Очищает инвентарь цели.", "Clears the target inventory.", "清空目标物品栏。", "清空目標物品欄。"), () => GameApi.ClearInventory(target)),
                    new ActionTileSpec(Tx("Высыпать", "Drop items", "丢出物品", "丟出物品"), Tx("Выбрасывает предметы рядом с целью.", "Drops items near the target.", "把物品丢到目标附近。", "把物品丟到目標附近。"), () => GameApi.DropAllInventory(target))
                );
                EndSection();
            }
        }

        private static string SmartSpawnResultText(string result)
        {
            if (string.IsNullOrWhiteSpace(result))
                return "";
            if (result.StartsWith("slot:", StringComparison.Ordinal))
                return Tx("Добавлено в основной слот ", "Added to inventory slot ", "已放入主栏位 ", "已放入主欄位 ") + result.Substring(5);
            if (result.StartsWith("backpack:", StringComparison.Ordinal))
                return Tx("Добавлено в рюкзак, слот ", "Added to backpack slot ", "已放入背包栏位 ", "已放入背包欄位 ") + result.Substring(9);
            return result switch
            {
                "back" => Tx("Надето на спину", "Equipped on back", "已装备到背部", "已裝備到背部"),
                "temp" => Tx("Передано в руки/временный слот", "Placed in hand/temporary slot", "已放入手中/临时栏位", "已放入手中/暫時欄位"),
                "hand_or_ground" => Tx("Передано в руки или создано рядом", "Put in hand or spawned nearby", "已放入手中或生成在附近", "已放入手中或生成在附近"),
                "ground" => Tx("Создано рядом с игроком", "Spawned near the player", "已生成在玩家附近", "已生成在玩家附近"),
                _ => Tx("Не удалось выдать предмет", "Could not give the item", "无法给予物品", "無法給予物品"),
            };
        }

        private static void DrawInventoryRedesign()
        {
            if (BeginSection("inventory.redesign.spawn", Tx("Инвентарь и предметы", "Inventory & items", "物品栏与物品", "物品欄與物品"), true))
            {
                GameApi.EnsureItemsLoaded();
                if (_invTarget >= GameApi.PlayerChars.Count) _invTarget = -1;
                Character invChar = InventoryTargetPicker();

                int slots = invChar != null ? GameApi.SlotCountFor(invChar) : GameApi.SlotCount();
                if (slots <= 0) slots = 3;
                if (_selSlot >= slots) _selSlot = Mathf.Max(0, slots - 1);

                GUILayout.BeginHorizontal();
                if (AdvancedUi)
                {
                    GUILayout.Label(Tx("Слот:", "Slot:", "栏位:", "欄位:"), Theme.LabelDim, GUILayout.Width(54));
                    for (int i = 0; i < slots; i++)
                        if (GUILayout.Button((i + 1).ToString(), _selSlot == i ? Theme.ChipActive : Theme.Chip, GUILayout.Width(36), GUILayout.Height(30)))
                            _selSlot = i;
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Label((Ru ? "Предметов: " : "Items: ") + GameApi.ItemNames.Count, Theme.LabelDim);
                if (GUILayout.Button(Tx("Обновить", "Reload", "刷新", "重新整理"), Theme.LinkBtn, GUILayout.Width(94), GUILayout.Height(30)))
                    GameApi.LoadItems();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(Tx("Поиск:", "Search:", "搜索:", "搜尋:"), Theme.LabelDim, GUILayout.Width(54));
                _itemSearch = GUILayout.TextField(_itemSearch ?? "", Theme.TextInput, GUILayout.Height(30));
                GUILayout.EndHorizontal();
                DrawItemCategoryBar(ref _itemCategory);
                DrawItemFavoriteControls();

                _itemScroll = BeginVerticalScroll(_itemScroll, GUILayout.Height(260));
                DrawItemTileGrid(_itemSearch, _itemCategory, _selItem, i => _selItem = i);
                GUILayout.EndScrollView();

                if (_selItem >= 0 && _selItem < GameApi.ItemNames.Count)
                    GUILayout.Label((Ru ? "Выбрано: " : "Selected: ") + GameApi.ItemNames[_selItem], Theme.LabelDim);
                GUILayout.Label((Ru ? "На спине: " : "Back slot: ") + GameApi.DescribeBackSlot(invChar) + "   |   " + (Ru ? "Амулет/временный: " : "Amulet/temp: ") + GameApi.DescribeTempSlot(invChar), Theme.LabelDim);

                DrawActionTiles(
                    new ActionTileSpec(
                        Tx("Выдать предмет", "Give item", "给予物品", "給予物品"),
                        Tx("Автоматически выбирает свободное место; при заполненном инвентаре передает предмет в руки или кладет рядом.", "Automatically chooses free space; when inventory is full, puts the item in hand or nearby.", "自动选择空位；物品栏已满时放到手中或附近。", "自動選擇空位；物品欄已滿時放到手中或附近。"),
                        () => { if (_selItem >= 0) _smartSpawnResult = GameApi.SmartSpawnItem(invChar, _selItem); },
                        1)
                );
                if (!string.IsNullOrWhiteSpace(_smartSpawnResult))
                    GUILayout.Label(SmartSpawnResultText(_smartSpawnResult), Theme.LabelDim);

                if (AdvancedUi)
                {
                    DrawActionTiles(
                        new ActionTileSpec(Tx("В слот", "To slot", "放入栏位", "放入欄位"), Tx("Кладет выбранный предмет в выбранный основной слот.", "Puts the selected item into the selected main slot.", "把所选物品放入所选主栏位。", "把所選物品放入所選主欄位。"), () => { if (_selItem >= 0) GameApi.SpawnToSlotFor(invChar, _selItem, _selSlot); }),
                        new ActionTileSpec(Tx("Очистить слот", "Clear slot", "清空栏位", "清空欄位"), Tx("Очищает выбранный основной слот.", "Clears the selected main slot.", "清空所选主栏位。", "清空所選主欄位。"), () => GameApi.ClearSlotFor(invChar, _selSlot)),
                        new ActionTileSpec(Tx("Во все слоты", "Spawn to all", "填满全部", "填滿全部"), Tx("Кладет выбранный предмет во все основные слоты и рюкзак.", "Puts the selected item into every main slot and backpack.", "把所选物品放入全部主栏位和背包。", "把所選物品放入全部主欄位和背包。"), () => { if (_selItem >= 0) GameApi.FillInventoryWith(invChar, _selItem); }),
                        new ActionTileSpec(Tx("Очистить всё", "Clear all", "全部清空", "全部清空"), Tx("Очищает основные слоты и рюкзак цели.", "Clears main slots and backpack.", "清空主栏位和背包。", "清空主欄位和背包。"), () => GameApi.ClearInventory(invChar), 2),
                        new ActionTileSpec(Tx("На спину", "Equip back", "装备背部", "裝備背部"), Tx("Надевает выбранный предмет на спину.", "Equips the selected item to the back slot.", "把所选物品装备到背部栏位。", "把所選物品裝備到背部欄位。"), () => { if (_selItem >= 0) GameApi.SpawnToBackSlotFor(invChar, _selItem); }),
                        new ActionTileSpec(Tx("В амулет", "To amulet", "放入护符", "放入護符"), Tx("Кладет выбранный предмет во временный слот 250.", "Puts the selected item into temporary slot 250.", "把所选物品放入临时栏位 250。", "把所選物品放入暫時欄位 250。"), () => { if (_selItem >= 0) GameApi.SpawnToTempSlotFor(invChar, _selItem); })
                    );
                    DrawActionTiles(
                        new ActionTileSpec(Tx("Еда", "Food", "食物", "食物"), Tx("Заполнить несколько слотов случайной едой.", "Fill several slots with random food.", "用随机食物填充多个栏位。", "用隨機食物填充多個欄位。"), () => GameApi.SpawnItemSet(invChar, "food")),
                        new ActionTileSpec(Tx("Спасение", "Rescue", "救援", "救援"), Tx("Выдать веревку, лечение и источник света.", "Give rope, healing and a light source.", "给予绳索、治疗和光源。", "給予繩索、治療和光源。"), () => GameApi.SpawnItemSet(invChar, "rescue")),
                        new ActionTileSpec(Tx("Свет", "Light", "照明", "照明"), Tx("Выдать фонарь/факел/топливо.", "Give lantern/torch/fuel.", "给予灯笼/火把/燃料。", "給予燈籠/火把/燃料。"), () => GameApi.SpawnItemSet(invChar, "light")),
                        new ActionTileSpec(Tx("Мобильность", "Mobility", "机动", "機動"), Tx("Выдать предметы перемещения и слот на спине.", "Give movement tools and a back item.", "给予移动工具和背部物品。", "給予移動工具和背部物品。"), () => GameApi.SpawnItemSet(invChar, "mobility"))
                    );
                }
                EndSection();
            }

            if (BeginSection("inventory.redesign.backpack", Tx("Рюкзак и заряд", "Backpack & charge", "背包与充能", "背包與充能"), false))
            {
                Character invChar = (_invTarget >= 0 && _invTarget < GameApi.PlayerChars.Count) ? GameApi.PlayerChars[_invTarget] : null;
                int backpackSlots = GameApi.BackpackSlotCount(invChar);
                if (backpackSlots > 0)
                {
                    if (AdvancedUi)
                    {
                        if (_selBackpackSlot >= backpackSlots) _selBackpackSlot = Mathf.Max(0, backpackSlots - 1);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(Tx("Рюкзак:", "Backpack:", "背包:", "背包:"), Theme.LabelDim, GUILayout.Width(70));
                        for (int i = 0; i < backpackSlots; i++)
                            if (GUILayout.Button((i + 1).ToString(), _selBackpackSlot == i ? Theme.ChipActive : Theme.Chip, GUILayout.Width(36), GUILayout.Height(30)))
                                _selBackpackSlot = i;
                        GUILayout.EndHorizontal();
                        for (int i = 0; i < backpackSlots; i++)
                            GUILayout.Label($"B{i + 1}. {GameApi.DescribeBackpackSlot(invChar, i)}", Theme.LabelDim);
                    }
                    else
                    {
                        GUILayout.Label(
                            Tx("Ячеек рюкзака: ", "Backpack slots: ", "背包栏位: ", "背包欄位: ") + backpackSlots,
                            Theme.LabelDim);
                    }
                }
                else
                {
                    GUILayout.Label(Tx("Рюкзак не надет или еще не синхронизирован.", "No backpack equipped or not synced yet.", "未装备背包或尚未同步。", "未裝備背包或尚未同步。"), Theme.LabelDim);
                }

                DrawToggleTiles(
                    new ToggleTileSpec(Tx("Бесконечные использования", "Unlimited uses", "无限使用", "無限使用"), Tx("Не дает предметам тратить количество использований.", "Prevents items from spending uses.", "阻止物品消耗使用次数。", "阻止物品消耗使用次數。"), () => ModConfig.UnlimitedItemUses, v => ModConfig.UnlimitedItemUses = v),
                    new ToggleTileSpec(Tx("Автоперезарядка", "Auto recharge", "自动充能", "自動充能"), Tx("Периодически выставляет заряд предметов.", "Periodically restores item charge.", "定期恢复物品充能。", "定期恢復物品充能。"), () => ModConfig.InfiniteItems, v => ModConfig.InfiniteItems = v),
                    new ToggleTileSpec(Tx("Топливо фонаря", "Lantern fuel", "灯笼燃料", "燈籠燃料"), Tx("Фонари не расходуют топливо и держатся заполненными.", "Lanterns do not spend fuel and stay filled.", "灯笼不消耗燃料并保持满。", "燈籠不消耗燃料並保持滿。"), () => ModConfig.UnlimitedLanternFuel, v => ModConfig.UnlimitedLanternFuel = v)
                );
                if (!ModConfig.UnlimitedLanternFuel)
                    ModConfig.LanternFuelConsumptionPercent = Slider(Tx("Расход топлива фонаря %", "Lantern fuel cost %", "灯笼燃料消耗 %", "燈籠燃料消耗 %"), ModConfig.LanternFuelConsumptionPercent, 0f, 300f);
                ModConfig.RechargeValue = NumberField(Tx("Заряд", "Charge", "充能", "充能"), ModConfig.RechargeValue, 0f, 999f);

                if (AdvancedUi)
                {
                    DrawActionTiles(
                        new ActionTileSpec(Tx("Зарядить слот", "Recharge slot", "充能栏位", "充能欄位"), Tx("Выставить заряд выбранного основного слота.", "Sets charge for the selected main slot.", "设置所选主栏位充能。", "設定所選主欄位充能。"), () => GameApi.RechargeSlotFor(invChar, _selSlot, ModConfig.RechargeValue)),
                        new ActionTileSpec(Tx("Все слоты", "All slots", "全部栏位", "全部欄位"), Tx("Зарядить все основные слоты.", "Recharge all main slots.", "充能全部主栏位。", "充能全部主欄位。"), () => RechargeAllMainSlots(invChar)),
                        new ActionTileSpec(Tx("Предмет на спине", "Back item", "背部物品", "背部物品"), Tx("Зарядить предмет на спине.", "Recharge the equipped back item.", "充能背部物品。", "充能背部物品。"), () => GameApi.RechargeBackSlotFor(invChar, ModConfig.RechargeValue)),
                        new ActionTileSpec(Tx("Амулет", "Amulet", "护符", "護符"), Tx("Зарядить временный слот 250.", "Recharge temporary slot 250.", "充能临时栏位 250。", "充能暫時欄位 250。"), () => GameApi.RechargeTempSlotFor(invChar, ModConfig.RechargeValue)),
                        new ActionTileSpec(Tx("Слот рюкзака", "Backpack slot", "背包栏位", "背包欄位"), Tx("Зарядить выбранную ячейку рюкзака.", "Recharge selected backpack slot.", "充能所选背包栏位。", "充能所選背包欄位。"), () => GameApi.RechargeBackpackSlotFor(invChar, _selBackpackSlot, ModConfig.RechargeValue)),
                        new ActionTileSpec(Tx("Весь рюкзак", "All backpack", "整个背包", "整個背包"), Tx("Зарядить весь рюкзак.", "Recharge the whole backpack.", "充能整个背包。", "充能整個背包。"), () => RechargeAllBackpackSlots(invChar))
                    );
                }
                else
                {
                    DrawActionTiles(
                        new ActionTileSpec(
                            Tx("Зарядить всё", "Recharge everything", "全部充能", "全部充能"),
                            Tx("Заряжает основные слоты, предмет на спине, амулет и весь рюкзак.", "Recharges main slots, back item, amulet, and the whole backpack.", "为主栏位、背部物品、护符和整个背包充能。", "為主欄位、背部物品、護符和整個背包充能。"),
                            () => RechargeEverything(invChar),
                            1)
                    );
                }
                EndSection();
            }
            DrawEntitySpawner();
        }

        private static void RechargeAllMainSlots(Character target)
        {
            int slots = target != null ? GameApi.SlotCountFor(target) : GameApi.SlotCount();
            if (slots <= 0) slots = 3;
            for (int slot = 0; slot < slots; slot++)
                GameApi.RechargeSlotFor(target, slot, ModConfig.RechargeValue);
        }

        private static void RechargeAllBackpackSlots(Character target)
        {
            int slots = GameApi.BackpackSlotCount(target);
            for (int slot = 0; slot < slots; slot++)
                GameApi.RechargeBackpackSlotFor(target, slot, ModConfig.RechargeValue);
        }

        private static void RechargeEverything(Character target)
        {
            RechargeAllMainSlots(target);
            GameApi.RechargeBackSlotFor(target, ModConfig.RechargeValue);
            GameApi.RechargeTempSlotFor(target, ModConfig.RechargeValue);
            RechargeAllBackpackSlots(target);
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
                    _itemScroll = BeginVerticalScroll(_itemScroll, GUILayout.Height(170));
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
                    _itemScroll = BeginVerticalScroll(_itemScroll, GUILayout.Height(170));
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
                    Tx("Без падения в ragdoll", "No falling ragdoll", "禁用摔倒布娃娃", "停用摔倒布娃娃"),
                    Tx("Блокирует принудительный ragdoll от падения и соскальзывания.", "Blocks forced ragdoll from falling and sliding.", "阻止坠落和滑落触发强制布娃娃状态。", "阻止墜落和滑落觸發強制布娃娃狀態。"),
                    ModConfig.NoFallingRagdoll);
                ModConfig.NoWeight = Toggle("feat.noweight", ModConfig.NoWeight);
                ModConfig.LockStatus = Toggle("feat.lockstatus", ModConfig.LockStatus);
                ModConfig.GlobalVoice = ToggleRaw(
                    Tx("Слышно всех", "Hear everyone", "听见所有人", "聽見所有人"),
                    Tx("Локально делает голоса всех игроков слышимыми на карте без 2D-эха: рядом остается обычный голос игры.", "Locally makes every player's voice audible across the map without 2D echo: nearby voices stay as normal game proximity voice.", "在本地让所有玩家的语音全图可听且没有 2D 回声：附近仍保持游戏原本的近距离语音。", "在本地讓所有玩家的語音全圖可聽且沒有 2D 回聲：附近仍保持遊戲原本的近距離語音。"),
                    ModConfig.GlobalVoice);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Tx("Включить всем", "Enable for everyone", "为所有人启用", "為所有人啟用"), Theme.LinkBtn, GUILayout.Height(28)))
                    VoiceControl.BroadcastHearAll(true);
                TipLast(Tx("Отправляет команду всем клиентам с PEAK-MX: все игроки будут слышны на всей карте без эха.", "Sends a command to every PEAK-MX client: all players become audible across the map without echo.", "向所有 PEAK-MX 客户端发送命令：所有玩家全图可听且没有回声。", "向所有 PEAK-MX 客戶端發送命令：所有玩家全圖可聽且沒有回聲。"));
                if (GUILayout.Button(Tx("Выключить всем", "Disable for everyone", "为所有人关闭", "為所有人關閉"), Theme.LinkBtn, GUILayout.Width(140), GUILayout.Height(28)))
                    VoiceControl.BroadcastHearAll(false);
                TipLast(Tx("Снимает сетевой режим слышимости у клиентов с PEAK-MX. Локальный переключатель выше остается твоей настройкой.", "Turns off the network voice-hearing mode on PEAK-MX clients. The local toggle above remains your own setting.", "关闭 PEAK-MX 客户端上的联网语音可听模式。上方本地开关仍是你的个人设置。", "關閉 PEAK-MX 客戶端上的連網語音可聽模式。上方本地開關仍是你的個人設定。"));
                GUILayout.EndHorizontal();
                ModConfig.PlayerEsp = ToggleRaw(
                    Tx("ESP игроков", "Player ESP", "玩家 ESP", "玩家 ESP"),
                    Tx("Показывает игроков через стены экранными метками с расстоянием.", "Shows players through walls with screen labels and distance.", "用带距离的屏幕标记显示墙后的玩家。", "用帶距離的畫面標記顯示牆後的玩家。"),
                    ModConfig.PlayerEsp);
                if (ModConfig.PlayerEsp)
                {
                    ModConfig.PlayerEspDistance = Slider(Tx("Дистанция ESP", "ESP distance", "ESP 距离", "ESP 距離"), ModConfig.PlayerEspDistance, 25f, 2500f);
                    GUILayout.BeginHorizontal();
                    ModConfig.PlayerEspBoxes = ToggleCompact(Tx("Рамки", "Boxes", "方框", "方框"), ModConfig.PlayerEspBoxes);
                    ModConfig.PlayerEspLines = ToggleCompact(Tx("Линии", "Lines", "连线", "連線"), ModConfig.PlayerEspLines);
                    GUILayout.EndHorizontal();
                }
                ModConfig.ItemEsp = ToggleRaw(
                    Tx("ESP предметов", "Item ESP", "物品 ESP", "物品 ESP"),
                    Tx("Показывает выбранные предметы через стены экранными метками.", "Shows selected items through walls with screen labels.", "用屏幕标记显示墙后的所选物品。", "用畫面標記顯示牆後的所選物品。"),
                    ModConfig.ItemEsp);
                if (ModConfig.ItemEsp)
                {
                    ModConfig.ItemEspDistance = Slider(Tx("Дистанция предметов", "Item distance", "物品距离", "物品距離"), ModConfig.ItemEspDistance, 25f, 2000f);
                    ModConfig.ItemEspLines = ToggleCompact(Tx("Линии к предметам", "Item lines", "物品连线", "物品連線"), ModConfig.ItemEspLines);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Tx("Все предметы", "All items", "全部物品", "全部物品"), ModConfig.ItemEspSelectedIndex < 0 ? Theme.ChipActive : Theme.Chip, GUILayout.Width(110), GUILayout.Height(26)))
                        ModConfig.ItemEspSelectedIndex = -1;
                    GUILayout.Label(Tx("Поиск:", "Search:", "搜索:", "搜尋:"), Theme.LabelDim, GUILayout.Width(54));
                    _itemEspSearch = GUILayout.TextField(_itemEspSearch ?? "", Theme.TextInput, GUILayout.Height(26));
                    ModConfig.ItemEspSearch = _itemEspSearch;
                    GUILayout.EndHorizontal();

                    GameApi.EnsureItemsLoaded();
                    string q = (_itemEspSearch ?? "").Trim().ToLowerInvariant();
                    _itemEspScroll = BeginVerticalScroll(_itemEspScroll, GUILayout.Height(126));
                    for (int i = 0; i < GameApi.ItemNames.Count; i++)
                    {
                        string nm = GameApi.ItemNames[i] ?? "";
                        if (q.Length > 0 && nm.ToLowerInvariant().IndexOf(q, StringComparison.Ordinal) < 0)
                            continue;
                        if (DrawItemListRow(i, nm, ModConfig.ItemEspSelectedIndex == i))
                            ModConfig.ItemEspSelectedIndex = i;
                    }
                    GUILayout.EndScrollView();
                }
                if (ModConfig.WorldEsp != null)
                {
                    ModConfig.WorldEsp.Value = ToggleRaw(
                        Tx("ESP опасностей и объектов", "Danger/object ESP", "危险/对象 ESP", "危險/物件 ESP"),
                        Tx("Показывает через стены опасности, костры, предметы на спину и амулеты.", "Shows hazards, campfires, back-slot items and amulets through walls.", "透视显示危险、营火、背部物品和护符。", "透視顯示危險、營火、背部物品和護符。"),
                        ModConfig.WorldEsp.Value);
                    if (ModConfig.WorldEsp.Value)
                    {
                        ModConfig.WorldEspDistance.Value = Slider(Tx("Дистанция объектов", "Object distance", "对象距离", "物件距離"), ModConfig.WorldEspDistance.Value, 25f, 2500f);
                        GUILayout.BeginHorizontal();
                        ModConfig.WorldEspLines.Value = ToggleCompact(Tx("Линии", "Lines", "连线", "連線"), ModConfig.WorldEspLines.Value);
                        ModConfig.WorldEspDangers.Value = ToggleCompact(Tx("Опасности", "Hazards", "危险", "危險"), ModConfig.WorldEspDangers.Value);
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        ModConfig.WorldEspCampfires.Value = ToggleCompact(Tx("Костры", "Campfires", "营火", "營火"), ModConfig.WorldEspCampfires.Value);
                        ModConfig.WorldEspBackItems.Value = ToggleCompact(Tx("Рюкзаки/джеты", "Back items", "背部物品", "背部物品"), ModConfig.WorldEspBackItems.Value);
                        ModConfig.WorldEspAmulets.Value = ToggleCompact(Tx("Амулеты", "Amulets", "护符", "護符"), ModConfig.WorldEspAmulets.Value);
                        GUILayout.EndHorizontal();
                    }
                }
                ModConfig.NoStatusEffects = ToggleRaw(
                    Tx("Без эффектов статуса", "No status effects", "无状态效果", "無狀態效果"),
                    Tx("Постоянно очищает все недуги локального персонажа.", "Continuously clears every affliction from the local character.", "持续清除本地角色的所有负面状态。", "持續清除本地角色的所有負面狀態。"),
                    ModConfig.NoStatusEffects);
                if (!ModConfig.NoStatusEffects)
                {
                    ModConfig.NoInjury = ToggleRaw(Tx("Без травм", "No injury", "无受伤", "無受傷"), Tx("Постоянно очищает травмы.", "Continuously clears injury.", "持续清除受伤状态。", "持續清除受傷狀態。"), ModConfig.NoInjury);
                    ModConfig.NoHunger = ToggleRaw(Tx("Без голода", "No hunger", "无饥饿", "無飢餓"), Tx("Постоянно очищает голод.", "Continuously clears hunger.", "持续清除饥饿。", "持續清除飢餓。"), ModConfig.NoHunger);
                    ModConfig.NoCold = ToggleRaw(Tx("Без холода", "No cold", "无寒冷", "無寒冷"), Tx("Постоянно очищает холод.", "Continuously clears cold.", "持续清除寒冷。", "持續清除寒冷。"), ModConfig.NoCold);
                    ModConfig.NoPoison = ToggleRaw(Tx("Без яда", "No poison", "无中毒", "無中毒"), Tx("Постоянно очищает яд.", "Continuously clears poison.", "持续清除中毒。", "持續清除中毒。"), ModConfig.NoPoison);
                    ModConfig.NoCurse = ToggleRaw(Tx("Без проклятия", "No curse", "无诅咒", "無詛咒"), Tx("Постоянно очищает проклятие.", "Continuously clears curse.", "持续清除诅咒。", "持續清除詛咒。"), ModConfig.NoCurse);
                    ModConfig.NoDrowsy = ToggleRaw(Tx("Без сонливости", "No drowsy", "无困倦", "無睏倦"), Tx("Постоянно очищает сонливость.", "Continuously clears drowsiness.", "持续清除困倦。", "持續清除睏倦。"), ModConfig.NoDrowsy);
                    ModConfig.NoHot = ToggleRaw(Tx("Без жары", "No hot", "无炎热", "無炎熱"), Tx("Постоянно очищает жару.", "Continuously clears heat.", "持续清除炎热。", "持續清除炎熱。"), ModConfig.NoHot);
                    ModConfig.NoCrab = ToggleRaw(Tx("Без краба", "No crab", "无螃蟹", "無螃蟹"), Tx("Постоянно очищает эффект краба.", "Continuously clears crab status.", "持续清除螃蟹状态。", "持續清除螃蟹狀態。"), ModConfig.NoCrab);
                    ModConfig.NoThorns = ToggleRaw(Tx("Без шипов", "No thorns", "无刺", "無刺"), Tx("Постоянно очищает шипы.", "Continuously clears thorns.", "持续清除刺。", "持續清除刺。"), ModConfig.NoThorns);
                    ModConfig.NoSpores = ToggleRaw(Tx("Без спор", "No spores", "无孢子", "無孢子"), Tx("Постоянно очищает споры.", "Continuously clears spores.", "持续清除孢子。", "持續清除孢子。"), ModConfig.NoSpores);
                    ModConfig.NoWeb = ToggleRaw(Tx("Без паутины", "No web", "无蛛网", "無蛛網"), Tx("Постоянно очищает паутину.", "Continuously clears web.", "持续清除蛛网。", "持續清除蛛網。"), ModConfig.NoWeb);
                    ModConfig.NoArrows = ToggleRaw(Tx("Без стрел", "No arrows", "无箭矢", "無箭矢"), Tx("Постоянно очищает стрелы.", "Continuously clears arrows.", "持续清除箭矢。", "持續清除箭矢。"), ModConfig.NoArrows);
                    ModConfig.NoPetrify = ToggleRaw(Tx("Без окаменения", "No petrify", "无石化", "無石化"), Tx("Постоянно очищает окаменение, включая цену эффектов амулетов.", "Continuously clears petrify, including the cost from amulet effects.", "持续清除石化，包括护符效果的代价。", "持續清除石化，包括護符效果的代價。"), ModConfig.NoPetrify);
                    ModConfig.NoFlyTrap = ToggleRaw(Tx("Без мухоловки", "No flytrap", "无捕蝇草", "無捕蠅草"), Tx("Постоянно очищает эффект мухоловки.", "Continuously clears flytrap status.", "持续清除捕蝇草状态。", "持續清除捕蠅草狀態。"), ModConfig.NoFlyTrap);
                }
                EndSection();
            }

            if (BeginSection("cheats.fly3", Tx("Полёт и телепорт", "Fly & teleport", "飞行与传送", "飛行與傳送"), false))
            {
                ModConfig.TeleportToPing = Toggle("feat.tpping", ModConfig.TeleportToPing);
                ModConfig.LongInteraction = ToggleRaw(
                    Tx("Дальнее взаимодействие", "Long interaction", "远距离交互", "遠距離互動"),
                    Tx("Увеличивает дистанцию взаимодействия с предметами и объектами.", "Increases interaction distance for items and objects.", "增加与物品和对象交互的距离。", "增加與物品和物件互動的距離。"),
                    ModConfig.LongInteraction);
                if (ModConfig.LongInteraction)
                    ModConfig.InteractionDistance = Slider(Tx("Дистанция", "Distance", "距离", "距離"), ModConfig.InteractionDistance, 2f, 80f);
                ModConfig.CinematicCamera = ToggleRaw(
                    Tx("Кинематографичная камера", "Cinematic camera", "电影镜头", "電影鏡頭"),
                    Tx("Свободная камера: WASD, Space/E вверх, Ctrl/Q вниз, Shift быстрее, мышь поворачивает.", "Free camera: WASD, Space/E up, Ctrl/Q down, Shift faster, mouse rotates.", "自由镜头：WASD 移动，Space/E 上升，Ctrl/Q 下降，Shift 加速，鼠标转向。", "自由鏡頭：WASD 移動，Space/E 上升，Ctrl/Q 下降，Shift 加速，滑鼠轉向。"),
                    ModConfig.CinematicCamera);
                if (ModConfig.CinematicCamera)
                {
                    ModConfig.CinematicCameraSpeed = Slider(Tx("Скорость камеры", "Camera speed", "镜头速度", "鏡頭速度"), ModConfig.CinematicCameraSpeed, 0.5f, 40f);
                    ModConfig.CinematicCameraFov = Slider("FOV", ModConfig.CinematicCameraFov, 1f, 120f);
                }
                ModConfig.Fly = Toggle("feat.fly", ModConfig.Fly);
                if (ModConfig.Fly)
                {
                    ModConfig.Noclip = ToggleRaw(
                        Tx("Проходить сквозь стены", "Noclip through geometry", "穿墙飞行", "穿牆飛行"),
                        Tx("Во время полёта отключает столкновения локального персонажа с объектами и невидимыми стенами.", "Disables local character collisions while flying, including invisible blockers.", "飞行时关闭本地角色与物体和隐形墙的碰撞。", "飛行時關閉本地角色與物件和隱形牆的碰撞。"),
                        ModConfig.Noclip);
                    ModConfig.FlySpeed = Slider(Tx("Скорость", "Speed", "速度", "速度"), ModConfig.FlySpeed, 1f, 50f);
                    ModConfig.FlyAcceleration = Slider(Tx("Ускорение", "Accel", "加速度", "加速度"), ModConfig.FlyAcceleration, 1f, 100f);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(Tx("Коорд.:", "Coords:", "坐标:", "座標:"), Theme.LabelDim, GUILayout.Width(54));
                GUILayout.Label("X", Theme.LabelDim, GUILayout.Width(12)); _tpX = GUILayout.TextField(_tpX ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                GUILayout.Label("Y", Theme.LabelDim, GUILayout.Width(12)); _tpY = GUILayout.TextField(_tpY ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                GUILayout.Label("Z", Theme.LabelDim, GUILayout.Width(12)); _tpZ = GUILayout.TextField(_tpZ ?? "0", Theme.LinkBtn, GUILayout.Width(70), GUILayout.Height(26));
                if (GUILayout.Button(Tx("ТП", "TP", "传送", "傳送"), Theme.DonateBtn, GUILayout.Width(54), GUILayout.Height(26)))
                {
                    if (float.TryParse(_tpX, out float x) & float.TryParse(_tpY, out float y) & float.TryParse(_tpZ, out float z))
                        GameApi.TeleportToCoords(x, y, z);
                }
                GUILayout.EndHorizontal();
                EndSection();
            }

            if (BeginSection("cheats.pranks3", Tx("Приколы", "Pranks", "恶作剧", "惡作劇"), false))
            {
                Character target = TargetPicker(ref _prankTarget);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Tx("Наслать недуги", "Pile on afflictions", "施加异常", "施加異常"), Theme.LinkBtn, GUILayout.Height(30))) GameApi.PrankAfflict(target);
                TipLast(Tx("Накладывает яд, проклятие, краба, сонливость и паутину на выбранную цель. Часть сетевых эффектов может зависеть от владельца цели.", "Adds poison, curse, crab, drowsy, and web to the target. Some networked effects may depend on the target owner.", "给目标施加中毒、诅咒、螃蟹、困倦和蛛网。部分网络效果可能取决于目标所有者。", "給目標施加中毒、詛咒、螃蟹、睏倦和蛛網。部分網路效果可能取決於目標擁有者。"));
                if (GUILayout.Button(Tx("Вылечить", "Cure", "治愈", "治癒"), Theme.LinkBtn, GUILayout.Height(30))) GameApi.ClearAllStatus(target);
                TipLast(Tx("Очищает все недуги выбранной цели.", "Clears every affliction on the selected target.", "清除所选目标的所有异常状态。", "清除所選目標的所有異常狀態。"));
                if (GUILayout.Button(Tx("Пнуть", "Push", "推开", "推開"), Theme.LinkBtn, GUILayout.Height(30))) GameApi.PushPlayer(target);
                TipLast(Tx("Толкает выбранную цель от тебя физическим импульсом.", "Pushes the selected target away from you with a physics impulse.", "用物理冲量把所选目标从你身边推开。", "用物理衝量把所選目標從你身邊推開。"));
                if (GUILayout.Button(Tx("Взорвать", "Explode", "爆炸", "爆炸"), Theme.DangerBtn, GUILayout.Height(30))) GameApi.ExplodePlayer(target);
                TipLast(Tx("Создает взрыв прямо в выбранной цели и подбрасывает игроков рядом.", "Creates an explosion directly inside the selected target and throws nearby players.", "在所选目标身上直接制造爆炸并击飞附近玩家。", "在所選目標身上直接製造爆炸並擊飛附近玩家。"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Tx("Окаменить", "Petrify", "石化", "石化"), Theme.LinkBtn, GUILayout.Height(28))) GameApi.PetrifyPlayer(target);
                TipLast(Tx("Выставляет окаменение выбранной цели на 100%. На чужих игроках результат зависит от сетевого владельца.", "Sets the selected target's petrify amount to 100%. On remote players, the result depends on network ownership.", "把所选目标的石化值设为 100%。远程玩家的结果取决于网络所有权。", "把所選目標的石化值設為 100%。遠端玩家的結果取決於網路擁有權。"));
                if (GUILayout.Button(Tx("Снять окаменение", "Clear petrify", "解除石化", "解除石化"), Theme.LinkBtn, GUILayout.Height(28))) GameApi.ClearPetrify(target);
                TipLast(Tx("Сбрасывает окаменение выбранной цели.", "Clears petrify from the selected target.", "清除所选目标的石化。", "清除所選目標的石化。"));
                GUILayout.EndHorizontal();

                _petrifyAmount = Slider(Tx("Окаменение %", "Petrify %", "石化 %", "石化 %"), _petrifyAmount, 0f, 100f);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Tx("Поставить %", "Set %", "设置 %", "設定 %"), Theme.LinkBtn, GUILayout.Height(28)))
                    GameApi.SetPetrifyAmount(target, Mathf.RoundToInt(_petrifyAmount));
                TipLast(Tx("Выставляет окаменение выбранной цели на указанное значение.", "Sets the selected target's petrify amount to the selected value.", "把所选目标的石化值设为指定数值。", "把所選目標的石化值設為指定數值。"));
                if (GUILayout.Button(Tx("Мини-взрыв", "Mini explosion", "小爆炸", "小爆炸"), Theme.LinkBtn, GUILayout.Height(28)))
                    GameApi.ExplodePlayer(target, 14f, 4f);
                TipLast(Tx("Создает слабый взрыв в цели без намеренного убийства.", "Creates a weaker explosion inside the target without intentionally killing them.", "在目标身上制造较弱爆炸，不刻意击杀。", "在目標身上製造較弱爆炸，不刻意擊殺。"));
                if (GUILayout.Button(Tx("Легкий пинок", "Light push", "轻推", "輕推"), Theme.LinkBtn, GUILayout.Height(28)))
                    GameApi.PushPlayer(target, 7f);
                TipLast(Tx("Слабый толчок для мягкого розыгрыша.", "A weaker push for a softer prank.", "用于轻度恶作剧的较弱推力。", "用於輕度惡作劇的較弱推力。"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Tx("Добавить стрелу", "Add arrow", "添加箭矢", "新增箭矢"), Theme.LinkBtn, GUILayout.Height(28))) GameApi.AddArrowPrank(target, 1);
                TipLast(Tx("Надежно работает на своем персонаже; чужому игроку стрелу должен добавить его клиент.", "Works reliably on your own character; a remote target's client owns arrow placement.", "在自己角色上最可靠；远程目标的箭矢位置由对方客户端拥有。", "在自己角色上最可靠；遠端目標的箭矢位置由對方客戶端擁有。"));
                if (GUILayout.Button(Tx("5 стрел", "5 arrows", "5 支箭", "5 支箭"), Theme.LinkBtn, GUILayout.Height(28))) GameApi.AddArrowPrank(target, 5);
                TipLast(Tx("Добавляет несколько стрел на твоего персонажа; для чужого игрока нужен код на его стороне.", "Adds several arrows to your character; remote targets need code on their side.", "给你的角色添加多支箭；远程目标需要对方客户端执行代码。", "給你的角色新增多支箭；遠端目標需要對方客戶端執行程式碼。"));
                if (GUILayout.Button(Tx("Вытащить стрелы", "Remove arrows", "移除箭矢", "移除箭矢"), Theme.LinkBtn, GUILayout.Height(28))) GameApi.ClearArrows(target);
                TipLast(Tx("Снимает физические стрелы с выбранной цели. У хоста может запросить снятие у владельца цели.", "Removes physical arrows from the selected target. As host, asks the target owner to remove them.", "移除所选目标身上的实体箭矢。房主可请求目标所有者移除。", "移除所選目標身上的實體箭矢。房主可請求目標擁有者移除。"));
                GUILayout.EndHorizontal();

                if (GUILayout.Button(Tx("Забить выбранным предметом", "Stuff with selected item", "塞满所选物品", "塞滿所選物品"), Theme.DonateBtn, GUILayout.Height(32)))
                    GameApi.FillInventoryWith(target, _selItem);
                TipLast(Tx("Заполняет все обычные слоты и рюкзак предметом из вкладки «Инвентарь».", "Fills every regular slot and backpack slot with the selected Inventory item.", "用“物品栏”标签中选中的物品填满普通栏位和背包栏位。", "用「物品欄」分頁中選中的物品填滿普通欄位和背包欄位。"));

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Tx("Забить золотом", "Fill with gold", "塞满黄金", "塞滿黃金"), Theme.LinkBtn, GUILayout.Height(28)))
                {
                    GameApi.FillInventoryWithGold(target);
                }
                TipLast(Tx("Ищет золотой идол по игровому тегу и кладёт его во все слоты цели, включая рюкзак.", "Finds the golden idol by game tag and fills the target's slots, including backpack.", "按游戏标签查找金色神像，并填入目标所有栏位，包括背包。", "按遊戲標籤查找金色神像，並填入目標所有欄位，包括背包。"));
                if (GUILayout.Button(Tx("Забить паутиной", "Fill with webs", "塞满蛛网", "塞滿蛛網"), Theme.LinkBtn, GUILayout.Height(28)))
                {
                    GameApi.FillInventoryWithWebs(target);
                }
                TipLast(Tx("Пытается найти предмет паутины/паука; если такого предмета нет, накладывает эффект паутины.", "Tries to find a web/spider item; if none exists, applies the web status.", "尝试查找蛛网/蜘蛛物品；如果没有，则施加蛛网状态。", "嘗試查找蛛網/蜘蛛物品；如果沒有，則施加蛛網狀態。"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Tx("Случайные предметы", "Random items", "随机物品", "隨機物品"), Theme.LinkBtn, GUILayout.Height(28))) GameApi.FillInventoryWithRandom(target);
                TipLast(Tx("Заполняет слоты цели случайными предметами из загруженного списка, включая рюкзак.", "Fills the target's slots with random loaded items, including backpack.", "用已加载列表中的随机物品填充目标栏位，包括背包。", "用已載入列表中的隨機物品填充目標欄位，包括背包。"));
                if (GUILayout.Button(Tx("Очистить инвентарь", "Clear inventory", "清空物品栏", "清空物品欄"), Theme.LinkBtn, GUILayout.Height(28))) GameApi.ClearInventory(target);
                TipLast(Tx("Очищает обычные слоты и рюкзак выбранной цели.", "Clears the selected target's regular slots and backpack.", "清空所选目标的普通栏位和背包。", "清空所選目標的普通欄位和背包。"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Tx("7 шариков", "7 balloons", "7 个气球", "7 個氣球"), Theme.LinkBtn, GUILayout.Height(28))) GameApi.TieBalloons(target, 7);
                TipLast(Tx("Привязывает к цели семь игровых шариков, чтобы подъём был заметнее.", "Ties seven in-game balloons to the target for a stronger lift.", "给目标绑上 7 个游戏气球，让升力更明显。", "給目標綁上 7 個遊戲氣球，讓升力更明顯。"));
                if (GUILayout.Button(Tx("Высыпать вещи", "Drop items", "丢出物品", "丟出物品"), Theme.LinkBtn, GUILayout.Height(28))) GameApi.DropAllInventory(target);
                TipLast(Tx("Выбрасывает содержимое основных слотов и надетый рюкзак рядом с целью.", "Drops main-slot items and the equipped backpack near the target.", "把主栏位物品和已装备背包丢到目标附近。", "把主欄位物品和已裝備背包丟到目標附近。"));
                GUILayout.EndHorizontal();

                GUILayout.Label(Tx("Предмет для первого прикола выбирается во вкладке «Инвентарь». Список предметов загружается автоматически.", "The selected item comes from the Inventory tab. The item list loads automatically.", "第一个恶作剧使用“物品栏”标签中选中的物品。物品列表会自动加载。", "第一個惡作劇使用「物品欄」分頁中選中的物品。物品列表會自動載入。"), Theme.LabelDim);
                EndSection();
            }
        }

        private static void DrawAdminModern()
        {
            GUILayout.Label(Ru ? "Администрирование" : "Administration", Theme.Section);
            if (!GameApi.IsHost())
                GUILayout.Label(Ru ? "Функции меню доступны всем пользователям, но кик и часть сетевых действий игра всё равно принимает только от хоста комнаты." : "Menu actions are available to every user, but kick and some network actions are still accepted only from the room host by the game.", Theme.LabelDim);

            if (BeginSection("admin.players", Ru ? "Игроки" : "Players", true))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label((Ru ? "В списке: " : "Listed: ") + GameApi.PlayerChars.Count, Theme.LabelDim);
                GUILayout.FlexibleSpace();
                _excludeSelf = ToggleCompact(Ru ? "Не трогать себя" : "Exclude me", _excludeSelf);
                GUILayout.EndHorizontal();

                DrawActionTiles(
                    new ActionTileSpec(Ru ? "Обновить" : "Refresh", Ru ? "Обновить список игроков, не меняя размер панели." : "Refresh the player list without changing the panel size.", () =>
                    {
                        GameApi.RefreshPlayers();
                    }),
                    new ActionTileSpec(Ru ? "Воскресить всех" : "Revive all", Ru ? "Воскресить всех игроков." : "Revive every player.", () => GameApi.ReviveAll()),
                    new ActionTileSpec(Ru ? "Только мёртвых" : "Dead only", Ru ? "Воскресить только игроков, которые сейчас отмечены мёртвыми." : "Revive only players currently marked as dead.", () => GameApi.ReviveAllDeadOnly()),
                    new ActionTileSpec(Ru ? "Притянуть всех" : "Bring all", Ru ? "Телепортировать остальных игроков к тебе." : "Teleport the other players to you.", () => GameApi.WarpAllToMe()),
                    new ActionTileSpec(Ru ? "Убить всех" : "Kill all", Ru ? "Убить всех игроков. Переключатель выше исключает тебя из действия." : "Kill all players. The switch above excludes you.", () => GameApi.KillAll(_excludeSelf), 2)
                );

                if (GameApi.PlayerChars.Count == 0)
                    GUILayout.Label(Ru ? "Список пуст. Нажми «Обновить список» в лобби или забеге." : "Empty. Press Refresh in a lobby or run.", Theme.LabelDim);

                _adminScroll = BeginVerticalScroll(_adminScroll, GUILayout.Height(GameApi.PlayerChars.Count > 4 ? 150 : 112));
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

                DrawToggleTiles(
                    new ToggleTileSpec(Ru ? "Защита" : "Protection", Ru ? "Включает проверки для других игроков. На пользователя панели проверки не действуют." : "Enables checks for other players. The panel user is exempt.", () => ModConfig.AdminProtectionEnabled, v => ModConfig.AdminProtectionEnabled = v),
                    new ToggleTileSpec(Ru ? "Только лог" : "Warn only", Ru ? "Писать подозрительные события в лог, но не кикать автоматически." : "Write suspicious events to the log without automatic kicks.", () => ModConfig.AdminWarnOnly, v => ModConfig.AdminWarnOnly = v),
                    new ToggleTileSpec(Ru ? "Скорость" : "Movement", Ru ? "Проверяет скорость других игроков и отмечает слишком резкие скачки." : "Measures other players' speed and flags extreme jumps.", () => ModConfig.AdminDetectExtremeMovement, v => ModConfig.AdminDetectExtremeMovement = v),
                    new ToggleTileSpec(Ru ? "Игнор падения" : "Ignore falling", Ru ? "При проверке скорости не считать чистое движение вниз." : "Ignores pure downward motion in speed checks.", () => ModConfig.AdminIgnoreDownwardMovement, v => ModConfig.AdminIgnoreDownwardMovement = v),
                    new ToggleTileSpec(Ru ? "Редкие предметы" : "Rare items", Ru ? "Отслеживает частое появление проклятых предметов и амулетов в слотах игроков." : "Tracks frequent cursed item and amulet appearances in player slots.", () => ModConfig.AdminDetectRareItemBursts, v => ModConfig.AdminDetectRareItemBursts = v),
                    new ToggleTileSpec(Ru ? "Сессионные баны" : "Session bans", Ru ? "Если забаненный игрок вернётся в комнату, хост снова его кикнет." : "If a session-banned player rejoins, the host kicks them again.", () => ModConfig.AdminAutoKickSessionBans, v => ModConfig.AdminAutoKickSessionBans = v)
                );

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

        private static void DrawAntiCheatModern()
        {
            GUILayout.Label(L("tab.anticheat"), Theme.Section);
            bool host = GameApi.IsHost();
            if (BeginSection("anticheat.detection", Tx("Обнаружение действий", "Action detection", "行为检测", "行為偵測"), true))
            {
                GUILayout.Label(AntiCheat.StatusText(Localization.Current), Theme.LabelDim);
                if (host)
                {
                    DrawToggleTiles(
                        new ToggleTileSpec(
                            Tx("Античит", "Anti-cheat", "反作弊", "反作弊"),
                            Tx(
                                "Включает обнаружение подозрительных действий. Настройки ниже управляют тем, что именно проверять и как реагировать.",
                                "Enables suspicious-action detection. The settings below control what is checked and how the host responds.",
                                "启用可疑行为检测。下方设置决定检查内容及房主响应方式。",
                                "啟用可疑行為偵測。下方設定決定檢查內容及房主回應方式。"),
                            () => ModConfig.AntiCheatEnabled,
                            v => AntiCheat.SetHostEnabled(v))
                    );

                    if (ModConfig.AntiCheatEnabled)
                    {
                        DrawToggleTiles(
                            new ToggleTileSpec(
                                Tx("Блокировать инструменты PEAK-MX у клиентов", "Lock PEAK-MX tools on clients", "锁定客户端 PEAK-MX 工具", "鎖定用戶端 PEAK-MX 工具"),
                                Tx("Не-хосты смогут открыть только вкладки «О моде» и «Античит».", "Non-hosts can only open the About and Anti-cheat tabs.", "非房主只能打开“关于”和“反作弊”标签。", "非房主只能打開「關於」和「反作弊」分頁。"),
                                () => ModConfig.AntiCheatLockClients == null || ModConfig.AntiCheatLockClients.Value,
                                v =>
                                {
                                    if (ModConfig.AntiCheatLockClients == null) return;
                                    ModConfig.AntiCheatLockClients.Value = v;
                                    AntiCheat.SetHostEnabled(true);
                                }),
                            new ToggleTileSpec(
                                Tx("Только предупреждать", "Warn only", "仅警告", "僅警告"),
                                Tx("Записывает подозрительные события, но не кикает автоматически.", "Logs suspicious events without automatic kicks.", "记录可疑事件，但不会自动踢出。", "記錄可疑事件，但不會自動踢出。"),
                                () => ModConfig.AdminWarnOnly,
                                v => ModConfig.AdminWarnOnly = v),
                            new ToggleTileSpec(
                                Tx("Проверять скорость", "Detect movement", "检测移动", "偵測移動"),
                                Tx("Отмечает слишком быстрое движение других игроков.", "Flags excessively fast movement by other players.", "标记其他玩家的异常高速移动。", "標記其他玩家的異常高速移動。"),
                                () => ModConfig.AdminDetectExtremeMovement,
                                v => ModConfig.AdminDetectExtremeMovement = v),
                            new ToggleTileSpec(
                                Tx("Игнорировать движение вниз", "Ignore downward movement", "忽略向下移动", "忽略向下移動"),
                                Tx("Падение вниз не учитывается при проверке скорости.", "Downward falling is excluded from movement checks.", "速度检测不计算向下坠落。", "速度偵測不計算向下墜落。"),
                                () => ModConfig.AdminIgnoreDownwardMovement,
                                v => ModConfig.AdminIgnoreDownwardMovement = v,
                                ModConfig.AdminDetectExtremeMovement),
                            new ToggleTileSpec(
                                Tx("Редкие предметы", "Rare items", "稀有物品", "稀有物品"),
                                Tx("Отслеживает подозрительное появление проклятых предметов и одинаковых амулетов.", "Tracks suspicious cursed-item appearances and duplicate amulets.", "检测可疑的诅咒物品和重复护符。", "偵測可疑的詛咒物品和重複護符。"),
                                () => ModConfig.AdminDetectRareItemBursts,
                                v => ModConfig.AdminDetectRareItemBursts = v),
                            new ToggleTileSpec(
                                Tx("Сессионные баны", "Session bans", "会话封禁", "工作階段封鎖"),
                                Tx("Повторно кикает заблокированного игрока при возврате в текущую комнату.", "Kicks a session-banned player again if they return to the current room.", "玩家返回当前房间时再次踢出。", "玩家返回目前房間時再次踢出。"),
                                () => ModConfig.AdminAutoKickSessionBans,
                                v => ModConfig.AdminAutoKickSessionBans = v)
                        );

                        if (ModConfig.AdminDetectExtremeMovement)
                        {
                            ModConfig.AdminMaxSpeed = NumberField(Tx("Макс. скорость", "Max speed", "最大速度", "最大速度"), ModConfig.AdminMaxSpeed, 5f, 300f);
                            ModConfig.AdminSpeedStrikes = Mathf.RoundToInt(NumberField(Tx("Срабатываний", "Strikes", "触发次数", "觸發次數"), ModConfig.AdminSpeedStrikes, 1f, 10f));
                        }
                    }
                }
                else
                {
                    GUILayout.Label(Tx(
                        "Проверки выполняет хост. На этом клиенте показано только состояние политики комнаты.",
                        "Checks are run by the host. This client only shows the room policy state.",
                        "检查由房主执行。此客户端仅显示房间策略状态。",
                        "檢查由房主執行。此用戶端僅顯示房間策略狀態。"),
                        Theme.LabelDim);
                    GUILayout.Label(Tx("Хост: ", "Host: ", "房主: ", "房主: ") + (AntiCheat.HostActor > 0 ? AntiCheat.HostActor.ToString() : "?"), Theme.LabelDim);
                    if (!string.IsNullOrWhiteSpace(AntiCheat.HostVersion))
                        GUILayout.Label("PEAK-MX: " + AntiCheat.HostVersion, Theme.LabelDim);
                }

                if (AntiCheat.ClientToolsLocked)
                {
                    GUILayout.Label(Tx(
                        "Инструменты отключены политикой античита хоста.",
                        "Tools are disabled by the host anti-cheat policy.",
                        "工具已被房主反作弊策略禁用。",
                        "工具已被房主反作弊策略停用。"),
                        Theme.DonateText);
                }
                EndSection();
            }

            if (BeginSection("anticheat.mods", Tx("Контроль модов", "Mod control", "模组控制", "模組控制"), true))
            {
                DrawMxAhgAdmin();
                EndSection();
            }

            if (BeginSection("anticheat.events", Tx("События", "Events", "事件", "事件"), true))
            {
                if (GameApi.AdminProtectionLog.Count == 0)
                    GUILayout.Label(Ru ? "Лог пока пуст." : "No log events yet.", Theme.LabelDim);
                for (int i = 0; i < GameApi.AdminProtectionLog.Count; i++)
                    GUILayout.Label(GameApi.AdminProtectionLog[i], Theme.LabelDim);
                EndSection();
            }
        }

        private static void DrawMxAhgAdmin()
        {
            GUILayout.Label(Tx(
                "Отдельный лёгкий агент проверяет DLL в BepInEx/plugins, реально загруженные плагины и владельцев Harmony-патчей. Личные файлы и SteamID не собираются.",
                "The standalone lightweight agent checks DLLs under BepInEx/plugins, loaded plugins and Harmony patch owners. It does not collect personal files or SteamID.",
                "独立轻量代理检查 BepInEx/plugins 中的 DLL、已加载插件和 Harmony 补丁所有者，不收集个人文件或 SteamID。",
                "獨立輕量代理檢查 BepInEx/plugins 中的 DLL、已載入外掛和 Harmony 修補擁有者，不收集個人檔案或 SteamID。"),
                Theme.LabelDim);
            GUILayout.Label(Tx(
                "Строгий режим: хост ставит PEAK-MX, игроки ставят MX-AHG и тот же набор обычных модов. PEAK-MX хоста и MX-AHG не входят в эталон.",
                "Strict mode: the host runs PEAK-MX, players run MX-AHG plus the same regular mod pack. Host PEAK-MX and MX-AHG are excluded from the baseline.",
                "严格模式：房主使用 PEAK-MX，玩家使用 MX-AHG 和相同的普通模组包。房主 PEAK-MX 和 MX-AHG 不计入基准。",
                "嚴格模式：房主使用 PEAK-MX，玩家使用 MX-AHG 和相同的普通模組包。房主 PEAK-MX 和 MX-AHG 不計入基準。"),
                Theme.LabelDim);

            if (!GameApi.IsHost())
            {
                GUILayout.Label(Tx("Управление MX-AHG доступно хосту комнаты.", "MX-AHG controls are available to the room host.", "MX-AHG 控制仅供房主使用。", "MX-AHG 控制僅供房主使用。"), Theme.LabelDim);
                return;
            }

            bool requireAgent = ModConfig.MxAhgRequireAgent != null && ModConfig.MxAhgRequireAgent.Value;
            bool exactPack = ModConfig.MxAhgExactPack == null || ModConfig.MxAhgExactPack.Value;
            DrawToggleTiles(
                new ToggleTileSpec(
                    Tx("Требовать MX-AHG", "Require MX-AHG", "要求 MX-AHG", "要求 MX-AHG"),
                    Tx("После времени на подключение игрок должен прислать действительный отчёт агента.", "After the join grace period, every player must provide a valid agent report.", "加入宽限期后，每位玩家都必须提供有效的代理报告。", "加入寬限期後，每位玩家都必須提供有效的代理報告。"),
                    () => requireAgent,
                    v =>
                    {
                        if (ModConfig.MxAhgRequireAgent == null) return;
                        ModConfig.MxAhgRequireAgent.Value = v;
                        if (!v) return;
                        if (ModConfig.MxAhgExactPack != null) ModConfig.MxAhgExactPack.Value = true;
                        if (ModConfig.MxAhgKickMissing != null) ModConfig.MxAhgKickMissing.Value = true;
                        if (ModConfig.MxAhgKickMismatch != null) ModConfig.MxAhgKickMismatch.Value = true;
                    })
            );

            if (!requireAgent)
            {
                GUILayout.Label(Tx(
                    "Включи требование агента, чтобы настроить точное сравнение, реакцию на несовпадения и отчёты игроков.",
                    "Enable the agent requirement to configure exact matching, mismatch handling and player reports.",
                    "启用代理要求后可配置精确匹配、不匹配处理和玩家报告。",
                    "啟用代理要求後可設定精確比對、不相符處理和玩家報告。"), Theme.LabelDim);
                return;
            }

            DrawToggleTiles(
                new ToggleTileSpec(
                    Tx("Точный набор хоста", "Exact host pack", "与房主完全一致", "與房主完全一致"),
                    Tx("Сравнивает DLL, загруженные плагины и Harmony-патчи с набором хоста.", "Compares DLLs, loaded plugins and Harmony patches against the host set.", "将 DLL、已加载插件和 Harmony 补丁与房主集合进行比较。", "將 DLL、已載入外掛和 Harmony 修補與房主集合進行比較。"),
                    () => exactPack,
                    v => { if (ModConfig.MxAhgExactPack != null) ModConfig.MxAhgExactPack.Value = v; }),
                new ToggleTileSpec(
                    Tx("Кик без агента", "Kick missing agent", "踢出无代理者", "踢出無代理者"),
                    Tx("Кикает игрока без heartbeat или действительного отчёта после ожидания.", "Kicks a player without a heartbeat or valid report after the grace period.", "宽限期后踢出没有心跳或有效报告的玩家。", "寬限期後踢出沒有心跳或有效報告的玩家。"),
                    () => ModConfig.MxAhgKickMissing == null || ModConfig.MxAhgKickMissing.Value,
                    v => { if (ModConfig.MxAhgKickMissing != null) ModConfig.MxAhgKickMissing.Value = v; },
                    requireAgent),
                new ToggleTileSpec(
                    Tx("Кик несовпадений", "Kick mismatches", "踢出不匹配者", "踢出不相符者"),
                    Tx("Кикает игрока, если его проверенный набор отличается от набора хоста.", "Kicks a player when their verified set differs from the host set.", "若验证集合与房主不同则踢出玩家。", "若驗證集合與房主不同則踢出玩家。"),
                    () => ModConfig.MxAhgKickMismatch == null || ModConfig.MxAhgKickMismatch.Value,
                    v => { if (ModConfig.MxAhgKickMismatch != null) ModConfig.MxAhgKickMismatch.Value = v; },
                    requireAgent && exactPack),
                new ToggleTileSpec(
                    Tx("Кик подозрительных", "Kick suspicious", "踢出可疑者", "踢出可疑者"),
                    Tx("Кикает по стоп-словам. По умолчанию выключено из-за риска ложных срабатываний.", "Kicks on denied keywords. Off by default because names can cause false positives.", "根据禁用关键词踢出。默认关闭以避免误报。", "根據禁用關鍵詞踢出。預設關閉以避免誤報。"),
                    () => ModConfig.MxAhgKickSuspicious != null && ModConfig.MxAhgKickSuspicious.Value,
                    v => { if (ModConfig.MxAhgKickSuspicious != null) ModConfig.MxAhgKickSuspicious.Value = v; },
                    requireAgent),
                new ToggleTileSpec(
                    Tx("Кик за потерю heartbeat", "Kick stale heartbeat", "心跳丢失时踢出", "心跳遺失時踢出"),
                    Tx("Кикает, если агент перестал отвечать более чем на 20 секунд.", "Kicks when the agent stops responding for more than 20 seconds.", "代理停止响应超过 20 秒时踢出。", "代理停止回應超過 20 秒時踢出。"),
                    () => ModConfig.MxAhgKickStale != null && ModConfig.MxAhgKickStale.Value,
                    v => { if (ModConfig.MxAhgKickStale != null) ModConfig.MxAhgKickStale.Value = v; },
                    requireAgent)
            );

            if (ModConfig.MxAhgGraceSeconds != null)
                ModConfig.MxAhgGraceSeconds.Value = NumberField(Tx("Ожидание после входа", "Join grace", "加入宽限", "加入寬限"), ModConfig.MxAhgGraceSeconds.Value, 8f, 90f);
            if (ModConfig.MxAhgRecheckSeconds != null)
                ModConfig.MxAhgRecheckSeconds.Value = NumberField(Tx("Повторная проверка", "Recheck interval", "复查间隔", "複查間隔"), ModConfig.MxAhgRecheckSeconds.Value, 20f, 300f);

            GUILayout.BeginHorizontal();
            GUILayout.Label(Tx("Стоп-слова", "Denied keywords", "禁用关键词", "禁用關鍵詞"), Theme.LabelDim);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Tx("Сброс", "Reset", "重置", "重設"), Theme.LinkBtn, GUILayout.Width(72f), GUILayout.Height(24f))
                && ModConfig.MxAhgDeniedKeywords != null)
                ModConfig.MxAhgDeniedKeywords.Value = ModConfig.DefaultMxAhgDeniedKeywords;
            TipLast(Tx("Вернуть стандартный список стоп-слов.", "Restore the default denied-keyword list.", "恢复默认禁用关键词列表。", "還原預設禁用關鍵詞清單。"));
            GUILayout.EndHorizontal();
            if (ModConfig.MxAhgDeniedKeywords != null)
                ModConfig.MxAhgDeniedKeywords.Value = GUILayout.TextField(ModConfig.MxAhgDeniedKeywords.Value ?? "", Theme.TextInput, GUILayout.Height(30));

            DrawActionTiles(
                new ActionTileSpec(Tx("Обновить набор хоста", "Rescan host", "重新扫描房主", "重新掃描房主"), Tx("Повторно считает локальные DLL и загруженные плагины.", "Rescans local DLLs and loaded plugins.", "重新扫描本地 DLL 和已加载插件。", "重新掃描本地 DLL 和已載入外掛。"), () => MxAhgHost.RefreshLocal()),
                new ActionTileSpec(Tx("Перепроверить всех", "Recheck all", "复查所有人", "複查所有人"), Tx("Отправляет новый одноразовый challenge всем агентам.", "Sends a fresh one-time challenge to every agent.", "向所有代理发送新的单次挑战。", "向所有代理傳送新的單次挑戰。"), () => MxAhgHost.RequestAll()),
                new ActionTileSpec(Tx("Копировать отчёт", "Copy report", "复制报告", "複製報告"), Tx("Копирует подробный отчёт по всем игрокам.", "Copies the detailed report for every player.", "复制所有玩家的详细报告。", "複製所有玩家的詳細報告。"), () => GUIUtility.systemCopyBuffer = MxAhgHost.BuildReport(Localization.Current)),
                new ActionTileSpec(Tx("Кикнуть отклонённых", "Kick rejected", "踢出未通过者", "踢出未通過者"), Tx("Сразу кикает всех игроков, которые сейчас не прошли проверку и не отмечены доверенными.", "Immediately kicks every player currently rejected and not trusted.", "立即踢出当前未通过且未受信任的玩家。", "立即踢出目前未通過且未受信任的玩家。"), () => MxAhgHost.KickRejected(), 2)
            );

            GUILayout.Label(Tx("Эталон хоста: ", "Host baseline: ", "房主基准: ", "房主基準: ") + MxAhgHost.LocalSignature + $"  ({MxAhgHost.LocalFileCount} DLL)", Theme.LabelDim);
            if (!string.IsNullOrWhiteSpace(MxAhgHost.LocalError))
                GUILayout.Label(Tx("Примечание сканера: ", "Scanner note: ", "扫描提示: ", "掃描提示: ") + MxAhgHost.LocalError, Theme.DonateText);

            IReadOnlyList<MxAhgRow> rows = MxAhgHost.Rows;
            if (rows.Count == 0)
            {
                GUILayout.Label(Tx("В комнате пока нет других игроков.", "No other players are in the room yet.", "房间内暂时没有其他玩家。", "房間內暫時沒有其他玩家。"), Theme.LabelDim);
            }
            else
            {
                bool selectedExists = false;
                for (int i = 0; i < rows.Count; i++)
                {
                    MxAhgRow row = rows[i];
                    if (row.ActorNumber == _mxAhgTargetActor) selectedExists = true;
                    string age = row.HeartbeatAge < 0f ? "--" : row.HeartbeatAge.ToString("0") + "s";
                    string label = $"{row.PlayerName}   |   {row.StatusText(Localization.Current)}   |   {age}";
                    GUIStyle style = row.ActorNumber == _mxAhgTargetActor ? Theme.ListItemActive : Theme.ListItem;
                    if (GUILayout.Button(label, style, GUILayout.Height(34)))
                        _mxAhgTargetActor = row.ActorNumber;
                }
                if (!selectedExists && rows.Count > 0)
                    _mxAhgTargetActor = rows[0].ActorNumber;
            }

            MxAhgRow selected = null;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].ActorNumber == _mxAhgTargetActor)
                {
                    selected = rows[i];
                    break;
                }
            }

            if (selected != null)
            {
                GUILayout.Space(6f);
                GUILayout.Label(selected.PlayerName + " — " + selected.StatusText(Localization.Current), Theme.Section);
                GUILayout.Label("MX-AHG " + (string.IsNullOrWhiteSpace(selected.AgentVersion) ? "--" : selected.AgentVersion)
                    + "   |   " + Tx("подпись ", "signature ", "签名 ", "簽名 ")
                    + (string.IsNullOrWhiteSpace(selected.DeclaredSignature) ? "--" : selected.DeclaredSignature)
                    + $" ({selected.DeclaredFileCount} DLL)", Theme.LabelDim);

                DrawActionTiles(
                    new ActionTileSpec(Tx("Перепроверить", "Recheck", "重新检查", "重新檢查"), Tx("Запрашивает новый полный отчёт только у этого игрока.", "Requests a fresh full report from this player.", "仅向该玩家请求新的完整报告。", "僅向該玩家請求新的完整報告。"), () => MxAhgHost.RequestReport(selected.ActorNumber)),
                    new ActionTileSpec(selected.Trusted ? Tx("Снять доверие", "Remove trust", "取消信任", "取消信任") : Tx("Доверить", "Trust", "信任", "信任"), Tx("Временное исключение действует только до выхода из текущей комнаты.", "The temporary exception lasts only until leaving the current room.", "临时例外仅持续到离开当前房间。", "臨時例外僅持續到離開目前房間。"), () => MxAhgHost.SetTrusted(selected.ActorNumber, !selected.Trusted), selected.Trusted ? 2 : 1),
                    new ActionTileSpec(Tx("Копировать всё", "Copy all", "复制全部", "複製全部"), Tx("Копирует полный MX-AHG отчёт.", "Copies the complete MX-AHG report.", "复制完整 MX-AHG 报告。", "複製完整 MX-AHG 報告。"), () => GUIUtility.systemCopyBuffer = MxAhgHost.BuildReport(Localization.Current)),
                    new ActionTileSpec(Tx("Кикнуть", "Kick", "踢出", "踢出"), Tx("Кикает выбранного игрока с причиной MX-AHG.", "Kicks the selected player with an MX-AHG reason.", "以 MX-AHG 原因踢出所选玩家。", "以 MX-AHG 原因踢出所選玩家。"), () => MxAhgHost.Kick(selected.ActorNumber), 2)
                );

                if (!string.IsNullOrWhiteSpace(selected.InvalidReason))
                    GUILayout.Label(Tx("Ошибка отчёта: ", "Report error: ", "报告错误: ", "報告錯誤: ") + selected.InvalidReason, Theme.DonateText);
                DrawMxAhgList(Tx("Отсутствует", "Missing", "缺少", "缺少"), selected.Missing);
                DrawMxAhgList(Tx("Лишнее", "Extra", "额外", "額外"), selected.Extra);
                DrawMxAhgList(Tx("Изменено", "Changed", "已更改", "已更改"), selected.Changed);
                DrawMxAhgList("Harmony", selected.PatchDifferences);
                DrawMxAhgList(Tx("Подозрительное", "Suspicious", "可疑", "可疑"), selected.SuspiciousFindings);
            }

        }

        private static void DrawMxAhgList(string title, IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
                return;
            GUILayout.Label(title + ":", Theme.LabelDim);
            int count = Mathf.Min(values.Count, 20);
            for (int i = 0; i < count; i++)
                GUILayout.Label("- " + values[i], Theme.LabelDim);
            if (values.Count > count)
                GUILayout.Label("+" + (values.Count - count), Theme.LabelDim);
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
            float distance = GameApi.DistanceToLocal(c);
            if (!local && distance >= 0f)
                label += "  " + distance.ToString("0") + "m";
            string pmx = GameApi.PlayerModVersion(c);
            if (!string.IsNullOrWhiteSpace(pmx))
                label += "  MX " + pmx;
            GUIStyle style = _adminTarget == index ? Theme.ListItemActive : (dead ? Theme.DangerBtn : Theme.ListItem);
            if (GUILayout.Button(label, style, GUILayout.Height(30)))
            {
                _adminTarget = index;
            }
            TipLast(GameApi.PlayerDetails(c, Ru));
        }

        private static void DrawAdminSelectedPlayer(Character c)
        {
            GUILayout.Space(6);
            GUILayout.Label((Ru ? "Выбран: " : "Selected: ") + GameApi.PlayerDisplayName(c), Theme.Section);
            GUILayout.Label(GameApi.PlayerDetails(c, Ru), Theme.LabelDim);
            bool local = false; try { local = c.IsLocal; } catch { }

            if (string.IsNullOrWhiteSpace(_nicknameInput))
                _nicknameInput = GameApi.LocalNickname();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Ru ? "Ник:" : "Nick:", Theme.LabelDim, GUILayout.Width(54));
            _nicknameInput = GUILayout.TextField(_nicknameInput ?? "", Theme.LinkBtn, GUILayout.Height(28));
            if (GUILayout.Button(Ru ? "Сменить" : "Set", Theme.LinkBtn, GUILayout.Width(88), GUILayout.Height(28)))
                GameApi.SetLocalNickname(_nicknameInput);
            GUILayout.EndHorizontal();
            TipLast(Ru ? "Меняет твой отображаемый Photon-ник. SteamID не меняется." : "Changes your displayed Photon nickname. SteamID is unchanged.");

            bool frozen = GameApi.IsFrozen(c);
            bool muted = GameApi.IsMuted(c);
            bool invLock = GameApi.IsInventoryLocked(c);
            bool voiceForced = VoiceControl.IsActorForced(c);
            bool banned = GameApi.IsSessionBanned(c);

            DrawActionTiles(
                AdminTile(Ru ? "Скопировать вид" : "Clone look", Ru ? "Копирует одежду, цвет и косметику выбранного игрока на тебя." : "Copies the selected player's outfit, color, and cosmetics onto you.", () => GameApi.CloneLocalAppearanceFrom(c, false)),
                AdminTile(Ru ? "Клон + ник" : "Clone + nick", Ru ? "Копирует внешний вид выбранного игрока и ставит тебе такой же отображаемый ник." : "Copies the selected player's appearance and applies the same displayed nickname to you.", () => GameApi.CloneLocalAppearanceFrom(c, true)),
                AdminTile(Ru ? "Телепорт к нему" : "Teleport to", Ru ? "Переместиться к выбранному игроку." : "Move yourself to the selected player.", () => GameApi.WarpToPlayer(c)),
                AdminTile(Ru ? "К себе" : "Bring to me", Ru ? "Притянуть выбранного игрока к тебе." : "Bring the selected player to you.", () => GameApi.BringPlayer(c)),
                AdminTile(Ru ? "На спавн" : "To spawn", Ru ? "Отправить выбранного игрока на его точку спавна." : "Send the selected player to their spawn point.", () => GameApi.WarpPlayerToSpawn(c)),
                AdminTile(Ru ? "Анти-застр." : "Anti-stuck", Ru ? "Сбросить скорость/падение и поднять выбранного игрока вверх." : "Reset velocity/fall state and lift the selected player up.", () => GameApi.AntiStuck(c))
            );

            DrawActionTiles(
                AdminTile(Ru ? "Найти" : "Find", Ru ? "Включить ESP игроков и выделить выбранного игрока линией." : "Enable player ESP and highlight the selected player with a line.", () => PlayerEsp.Focus(c), 1),
                AdminTile(Ru ? "Снять поиск" : "Clear find", Ru ? "Убрать выделение выбранного игрока в ESP." : "Clear the player ESP focus.", () => PlayerEsp.ClearFocus()),
                AdminTile(Ru ? "Убить" : "Kill", Ru ? "Убить выбранного игрока." : "Kill the selected player.", () => GameApi.KillPlayer(c), 2),
                AdminTile(Ru ? "Воскресить" : "Revive", Ru ? "Воскресить выбранного игрока." : "Revive the selected player.", () => GameApi.RevivePlayer(c)),
                AdminTile(Ru ? "Оживить тут" : "Revive here", Ru ? "Воскресить выбранного игрока рядом с тобой." : "Revive the selected player near you.", () => GameApi.RevivePlayerHere(c)),
                AdminTile(Ru ? "Скаут" : "Scoutmaster", Ru ? "Призвать скаутмастера рядом с выбранным игроком. Работает у хоста." : "Spawn a Scoutmaster near the selected player. Host only.", () => GameApi.SpawnScoutmaster(c))
            );

            DrawActionTiles(
                AdminTile(frozen ? (Ru ? "Фриз: вкл" : "Freeze: on") : (Ru ? "Фриз: выкл" : "Freeze: off"), Ru ? "Первое нажатие замораживает, второе снимает фриз." : "First click freezes, second click unfreezes.", () => GameApi.ToggleFreeze(c), frozen ? 2 : 0),
                AdminTile(muted ? (Ru ? "Мут: вкл" : "Mute: on") : (Ru ? "Мут: выкл" : "Mute: off"), local ? (Ru ? "Мут на себя может игнорироваться игрой/Photon. Эта кнопка в основном для других игроков." : "Self mute may be ignored by the game/Photon. This is mainly for other players.") : (Ru ? "Переключить мут выбранного игрока через сетевое свойство Photon." : "Toggle this player's mute state through Photon properties."), () =>
                {
                    if (local) GameApi.AddAdminLog(Ru ? "Мут на себя: игра может игнорировать действие" : "Self mute: game may ignore it");
                    GameApi.ToggleMute(c);
                }, muted ? 2 : 0),
                AdminTile(invLock ? (Ru ? "Инвентарь: запрет" : "Inventory: locked") : (Ru ? "Инвентарь: можно" : "Inventory: allowed"), Ru ? "Запрещает цель держать предметы: найденные предметы будут выкидываться." : "Prevents the target from keeping items by dropping them repeatedly.", () => GameApi.ToggleInventoryLock(c), invLock ? 2 : 0),
                AdminTile(voiceForced ? (Ru ? "Слышимость: вкл" : "Voice: global") : (Ru ? "Слышно всем" : "Hear target"), voiceForced ? (Ru ? "Снять сетевую команду: выбранного игрока больше не форсируем как слышимого на всей карте." : "Remove the network command: the selected player is no longer forced audible across the map.") : (Ru ? "Отправить клиентам с PEAK-MX команду: выбранного игрока слышно на всей карте без эха." : "Tell PEAK-MX clients to hear the selected player across the map without echo."), () => VoiceControl.BroadcastHearActor(c, !voiceForced), voiceForced ? 2 : 0),
                AdminTile(VoiceControl.ForceAllActors ? (Ru ? "Все слышны: вкл" : "All voices: on") : (Ru ? "Слышно всех" : "Hear everyone"), Ru ? "Переключить сетевой режим: у клиентов с PEAK-MX все игроки слышны на всей карте без эха." : "Toggle the network mode: PEAK-MX clients hear every player across the map without echo.", () => VoiceControl.BroadcastHearAll(!VoiceControl.ForceAllActors), VoiceControl.ForceAllActors ? 2 : 0),
                AdminTile(Ru ? "Кикнуть" : "Kick", Ru ? "Попробовать кикнуть игрока из текущей комнаты." : "Try to kick this player from the current room.", () => GameApi.KickPlayer(c), 2),
                AdminTile(banned ? (Ru ? "Бан: вкл" : "Ban: on") : (Ru ? "Бан: выкл" : "Ban: off"), local ? (Ru ? "Сессионный бан нужен для других игроков. Самого себя игра обычно не кикает." : "Session ban is for other players. The game usually does not kick yourself.") : (Ru ? "Сессионный бан: игрок будет снова кикнут при входе в эту комнату, если твой клиент может применить действие." : "Session ban: the player is kicked again if they rejoin, if your client can apply the action."), () =>
                {
                    if (local) GameApi.AddAdminLog(Ru ? "Бан на себя: добавлен в список, самокик может игнорироваться" : "Self ban: listed, self-kick may be ignored");
                    GameApi.ToggleSessionBan(c);
                }, banned ? 2 : 0)
            );

            if (local)
                GUILayout.Label(Ru ? "Кик, мут и бан предназначены для других игроков. На себе они могут выглядеть как бездействие." : "Kick, mute, and ban are meant for other players. On yourself they can look like no-op actions.", Theme.LabelDim);

            if (!GameApi.IsHost())
                GUILayout.Label(Ru ? "Если игра отклонит сетевое действие без прав хоста, оно просто не применится." : "If the game rejects a network action without host authority, it will simply not apply.", Theme.LabelDim);
        }

        private static bool AdminActionButton(string label, string tip, GUIStyle style = null)
        {
            bool clicked = GUILayout.Button(label, style ?? Theme.LinkBtn, GUILayout.Height(30));
            TipLast(tip);
            if (clicked)
                GameApi.AddAdminLog("action: " + label);
            return clicked;
        }

        private static ActionTileSpec AdminTile(string label, string tip, Action run, int style = 0)
        {
            return new ActionTileSpec(label, tip, () =>
            {
                GameApi.AddAdminLog("action: " + label);
                run?.Invoke();
            }, style);
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

        private struct ToggleTileSpec
        {
            public string Label;
            public string Tip;
            public Func<bool> Get;
            public Action<bool> Set;
            public bool Enabled;

            public ToggleTileSpec(string label, string tip, Func<bool> get, Action<bool> set, bool enabled = true)
            {
                Label = label;
                Tip = tip;
                Get = get;
                Set = set;
                Enabled = enabled;
            }
        }

        private struct ActionTileSpec
        {
            public string Label;
            public string Tip;
            public Action Run;
            public int Style;

            public ActionTileSpec(string label, string tip, Action run, int style = 0)
            {
                Label = label;
                Tip = tip;
                Run = run;
                Style = style;
            }
        }

        private static Vector2 BeginVerticalScroll(Vector2 scroll, params GUILayoutOption[] options)
        {
            scroll.x = 0f;
            return GUILayout.BeginScrollView(scroll, GUIStyle.none, GUI.skin.verticalScrollbar, options);
        }

        private static int TileColumns(float minTileWidth = 150f)
        {
            float width = Mathf.Max(280f, _contentRect.width > 0f ? _contentRect.width - 52f : 620f);
            int columns = Mathf.FloorToInt((width + 8f) / Mathf.Max(72f, minTileWidth + 8f));
            return Mathf.Clamp(columns, 1, 10);
        }

        private static float TileWidth(int columns, float gap = 8f)
        {
            float width = Mathf.Max(280f, _contentRect.width > 0f ? _contentRect.width - 52f : 620f);
            return Mathf.Floor((width - gap * Mathf.Max(0, columns - 1)) / Mathf.Max(1, columns));
        }

        private static void DrawToggleTiles(params ToggleTileSpec[] specs)
        {
            int columns = TileColumns();
            float width = TileWidth(columns);
            for (int i = 0; i < specs.Length; i += columns)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < columns; col++)
                {
                    int index = i + col;
                    if (index >= specs.Length)
                    {
                        GUILayout.Space(width);
                        continue;
                    }

                    ToggleTileSpec spec = specs[index];
                    bool value = spec.Get != null && spec.Get();
                    bool guiEnabled = GUI.enabled;
                    GUI.enabled = guiEnabled && spec.Enabled;
                    bool clicked = DrawTile(spec.Label, spec.Tip, value, width, 52f, value ? Theme.ToggleOn : Theme.Toggle);
                    GUI.enabled = guiEnabled;
                    if (clicked)
                        spec.Set?.Invoke(!value);

                    if (col < columns - 1)
                        GUILayout.Space(8f);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
            }
        }

        private static void DrawActionTiles(params ActionTileSpec[] specs)
        {
            int columns = TileColumns();
            float width = TileWidth(columns);
            for (int i = 0; i < specs.Length; i += columns)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < columns; col++)
                {
                    int index = i + col;
                    if (index >= specs.Length)
                    {
                        GUILayout.Space(width);
                        continue;
                    }

                    ActionTileSpec spec = specs[index];
                    GUIStyle style = spec.Style == 2 ? Theme.DangerBtn : spec.Style == 1 ? Theme.DonateBtn : Theme.LinkBtn;
                    if (DrawTile(spec.Label, spec.Tip, false, width, 42f, style))
                        spec.Run?.Invoke();

                    if (col < columns - 1)
                        GUILayout.Space(8f);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
            }
        }

        private static bool DrawTile(string label, string tip, bool active, float width, float height, GUIStyle style)
        {
            Rect r = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            bool clicked = GUI.Button(r, new GUIContent(label, tip ?? ""), style);
            if (Event.current != null && r.Contains(Event.current.mousePosition) && !string.IsNullOrWhiteSpace(tip))
                _hoverTip = tip;
            return clicked;
        }

        private static void DrawChipBar(ref int selected, string[] labels, string[] tips = null)
        {
            int columns = TileColumns(104f);
            float width = TileWidth(columns);
            for (int i = 0; i < labels.Length; i += columns)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < columns; col++)
                {
                    int index = i + col;
                    if (index >= labels.Length)
                    {
                        GUILayout.Space(width);
                        continue;
                    }

                    bool active = selected == index;
                    if (GUILayout.Button(labels[index], active ? Theme.ChipActive : Theme.Chip, GUILayout.Width(width), GUILayout.Height(30f)))
                        selected = index;
                    if (Event.current != null && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && tips != null && index < tips.Length)
                        _hoverTip = tips[index];
                    if (col < columns - 1)
                        GUILayout.Space(8f);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4f);
            }
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
            DrawStatusIncreaseRow(target, 4, Ru ? "Краб %" : "Crab %", ref ModConfig.CrabIncreasePercent);
            DrawStatusIncreaseRow(target, 5, Ru ? "Проклятие %" : "Curse %", ref ModConfig.CurseIncreasePercent);
            DrawStatusIncreaseRow(target, 6, Ru ? "Сонливость %" : "Drowsy %", ref ModConfig.DrowsyIncreasePercent);
            DrawStatusIncreaseRow(target, 8, Ru ? "Жара %" : "Hot %", ref ModConfig.HotIncreasePercent);
            DrawStatusIncreaseRow(target, 9, Ru ? "Шипы %" : "Thorns %", ref ModConfig.ThornsIncreasePercent);
            DrawStatusIncreaseRow(target, 10, Ru ? "Споры %" : "Spores %", ref ModConfig.SporesIncreasePercent);
            DrawStatusIncreaseRow(target, 11, Ru ? "Паутина %" : "Web %", ref ModConfig.WebIncreasePercent);
            DrawStatusIncreaseRow(target, 12, Ru ? "Стрелы %" : "Arrows %", ref ModConfig.ArrowsIncreasePercent);
            DrawStatusIncreaseRow(target, 13, Ru ? "Окаменение %" : "Petrify %", ref ModConfig.PetrifyIncreasePercent);
            DrawStatusIncreaseRow(target, 14, Ru ? "Мухоловка %" : "Flytrap %", ref ModConfig.FlyTrapIncreasePercent);
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
                GUILayout.Label((Ru ? "На спине: " : "Back slot: ") + GameApi.DescribeBackSlot(invChar), Theme.LabelDim);
                GUILayout.Label((Ru ? "Амулет/временный: " : "Amulet/temp: ") + GameApi.DescribeTempSlot(invChar), Theme.LabelDim);

                GUILayout.BeginHorizontal();
                GUILayout.Label((Ru ? "Предметов: " : "Items: ") + GameApi.ItemNames.Count, Theme.LabelDim);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Ru ? "Перезагрузить список" : "Reload list", Theme.LinkBtn, GUILayout.Height(26)))
                    GameApi.LoadItems();
                TipLast(Ru ? "Заново собрать список предметов из игровых ресурсов." : "Reload the item list from game resources.");
                GUILayout.EndHorizontal();

                _itemSearch = GUILayout.TextField(_itemSearch ?? "", Theme.LinkBtn, GUILayout.Height(26));
                DrawItemFavoriteControls();
                _itemScroll = BeginVerticalScroll(_itemScroll, GUILayout.Height(170));
                string q = (_itemSearch ?? "").Trim().ToLowerInvariant();
                for (int i = 0; i < GameApi.ItemNames.Count; i++)
                {
                    string nm = GameApi.ItemNames[i];
                    if (_itemFavoritesOnly && !IsFavoriteItem(i)) continue;
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

                GUILayout.Space(4);
                GUILayout.Label(Ru ? "Быстрые наборы" : "Quick item sets", Theme.LabelDim);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Еда" : "Food", Theme.LinkBtn, GUILayout.Height(28)))
                    GameApi.SpawnItemSet(invChar, "food");
                TipLast(Ru ? "Заполнить несколько слотов случайной едой." : "Fill several slots with random food.");
                if (GUILayout.Button(Ru ? "Спасение" : "Rescue", Theme.LinkBtn, GUILayout.Height(28)))
                    GameApi.SpawnItemSet(invChar, "rescue");
                TipLast(Ru ? "Попробовать выдать веревку, лечение и источник света." : "Try to give rope, healing and a light source.");
                if (GUILayout.Button(Ru ? "Свет" : "Light", Theme.LinkBtn, GUILayout.Height(28)))
                    GameApi.SpawnItemSet(invChar, "light");
                TipLast(Ru ? "Попробовать выдать фонарь/факел/топливо." : "Try to give lantern/torch/fuel.");
                if (GUILayout.Button(Ru ? "Мобильность" : "Mobility", Theme.LinkBtn, GUILayout.Height(28)))
                    GameApi.SpawnItemSet(invChar, "mobility");
                TipLast(Ru ? "Попробовать выдать предмет на спину и средства передвижения." : "Try to give a back item and movement tools.");
                GUILayout.EndHorizontal();
                EndSection();
            }

            if (BeginSection("inventory.special3", Ru ? "Слот на спине и амулет" : "Back slot & amulet", false))
            {
                Character invChar = (_invTarget >= 0 && _invTarget < GameApi.PlayerChars.Count) ? GameApi.PlayerChars[_invTarget] : null;
                GUILayout.Label(Ru ? "Новые предметы на спине используют отдельный слот игры. Амулеты и переполненный предмет используют временный слот 250." : "New back items use the game's back slot. Amulets and overflow items use temporary slot 250.", Theme.LabelDim);
                GUILayout.Label((Ru ? "На спине: " : "Back slot: ") + GameApi.DescribeBackSlot(invChar), Theme.LabelDim);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Надеть на спину" : "Equip back item", Theme.DonateBtn, GUILayout.Height(30)) && _selItem >= 0)
                    GameApi.SpawnToBackSlotFor(invChar, _selItem);
                TipLast(Ru ? "Работает с Backpack, Fannypack, Jetpack и Rocketpack из списка предметов." : "Works with Backpack, Fannypack, Jetpack and Rocketpack from the item list.");
                if (GUILayout.Button(Ru ? "Снять" : "Clear", Theme.LinkBtn, GUILayout.Width(110), GUILayout.Height(30)))
                    GameApi.ClearBackSlotFor(invChar);
                TipLast(Ru ? "Очищает предмет на спине." : "Clears the equipped back item.");
                GUILayout.EndHorizontal();

                GUILayout.Label((Ru ? "Амулет/временный: " : "Amulet/temp: ") + GameApi.DescribeTempSlot(invChar), Theme.LabelDim);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "В слот 250" : "To slot 250", Theme.DonateBtn, GUILayout.Height(30)) && _selItem >= 0)
                    GameApi.SpawnToTempSlotFor(invChar, _selItem);
                TipLast(Ru ? "Подходит для амулетов и обычных предметов, которые игра держит во временном слоте." : "For amulets and regular items the game stores in the temporary slot.");
                if (GUILayout.Button(Ru ? "Очистить" : "Clear", Theme.LinkBtn, GUILayout.Width(110), GUILayout.Height(30)))
                    GameApi.ClearTempSlotFor(invChar);
                TipLast(Ru ? "Очищает временный слот 250." : "Clears temporary slot 250.");
                GUILayout.EndHorizontal();
                EndSection();
            }

            if (BeginSection("inventory.backpack3", Ru ? "Рюкзак" : "Backpack", false))
            {
                Character invChar = (_invTarget >= 0 && _invTarget < GameApi.PlayerChars.Count) ? GameApi.PlayerChars[_invTarget] : null;
                GUILayout.Label(Ru ? "Ячейки рюкзака доступны только у предметов со встроенным хранилищем." : "Backpack slots are available only on back items with internal storage.", Theme.LabelDim);
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
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Зарядить предмет на спине" : "Recharge back item", Theme.LinkBtn, GUILayout.Height(30)))
                    GameApi.RechargeBackSlotFor(invChar, ModConfig.RechargeValue);
                TipLast(Ru ? "Выставить заряд/прочность предмета на спине, если у него есть такие данные." : "Set charge/durability for the equipped back item if it has those data.");
                if (GUILayout.Button(Ru ? "Зарядить амулет" : "Recharge amulet", Theme.LinkBtn, GUILayout.Width(130), GUILayout.Height(30)))
                    GameApi.RechargeTempSlotFor(invChar, ModConfig.RechargeValue);
                TipLast(Ru ? "Выставить заряд/прочность временного слота 250." : "Set charge/durability for temporary slot 250.");
                GUILayout.EndHorizontal();

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

            DrawWorldNavigationTools();

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
                    _luggageScroll = BeginVerticalScroll(_luggageScroll, GUILayout.Height(150));
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

        private static void DrawWorldNavigationTools()
        {
            if (!BeginSection("world.navigation", Ru ? "Биомы и поиск объектов" : "Biomes & object finder", false))
                return;

            GUILayout.Label((Ru ? "Текущий сегмент: " : "Current segment: ") + GameApi.CurrentSegmentLabel(Ru), Theme.LabelDim);
            var segmentTiles = new ActionTileSpec[GameApi.SegmentCount];
            for (int i = 0; i < GameApi.SegmentCount; i++)
            {
                int segmentIndex = i;
                string label = GameApi.SegmentLabel(segmentIndex, Ru);
                string biome = GameApi.SegmentBiomeLabel(segmentIndex, Ru);
                if (!string.IsNullOrWhiteSpace(biome) && !string.Equals(biome, label, StringComparison.OrdinalIgnoreCase))
                    label += " / " + biome;
                segmentTiles[i] = new ActionTileSpec(
                    label,
                    Ru ? "Перейти к выбранному сегменту через MapHandler игры." : "Jump to this segment through the game's MapHandler.",
                    () => GameApi.WarpToSegmentIndex(segmentIndex));
            }
            DrawActionTiles(segmentTiles);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Ru ? "Поиск:" : "Search:", Theme.LabelDim, GUILayout.Width(54));
            _worldObjectSearch = GUILayout.TextField(_worldObjectSearch ?? "", Theme.TextInput, GUILayout.Height(28));
            GUILayout.EndHorizontal();
            _worldObjectRange = NumberField(Ru ? "Радиус" : "Radius", _worldObjectRange, 50f, 5000f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Ru ? "Сканировать" : "Scan", Theme.DonateBtn, GUILayout.Height(28)))
            {
                GameApi.RefreshWorldObjects(_worldObjectSearch, _worldObjectRange);
                _selWorldObject = -1;
            }
            TipLast(Ru ? "Ищет активные объекты по имени, типу компонента и названию предмета." : "Finds active objects by name, component type, and item name.");
            if (GUILayout.Button(Ru ? "Gloom/Bell" : "Gloom/Bell", Theme.LinkBtn, GUILayout.Width(110), GUILayout.Height(28)))
            {
                _worldObjectSearch = "gloom bell belltower tower jester";
                GameApi.RefreshWorldObjects(_worldObjectSearch, _worldObjectRange);
                _selWorldObject = -1;
            }
            if (GUILayout.Button(Ru ? "Предметы" : "Items", Theme.LinkBtn, GUILayout.Width(100), GUILayout.Height(28)))
            {
                _worldObjectSearch = "item luggage chest container";
                GameApi.RefreshWorldObjects(_worldObjectSearch, _worldObjectRange);
                _selWorldObject = -1;
            }
            GUILayout.EndHorizontal();

            if (_selWorldObject >= GameApi.WorldObjectLabels.Count)
                _selWorldObject = -1;

            int count = GameApi.WorldObjectLabels.Count;
            GUILayout.Label((Ru ? "Найдено: " : "Found: ") + count, Theme.LabelDim);
            if (count > 0)
            {
                _worldObjectScroll = BeginVerticalScroll(_worldObjectScroll, GUILayout.Height(150));
                for (int i = 0; i < count; i++)
                {
                    if (GUILayout.Button(GameApi.WorldObjectLabels[i], _selWorldObject == i ? Theme.ListItemActive : Theme.ListItem, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                        _selWorldObject = i;
                }
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Ru ? "Телепорт к выбранному" : "Warp to selected", Theme.LinkBtn, GUILayout.Height(28)))
                    GameApi.WarpToWorldObject(_selWorldObject);
                if (GUILayout.Button(Ru ? "В координаты" : "To coords", Theme.LinkBtn, GUILayout.Width(110), GUILayout.Height(28)))
                {
                    Vector3 pos = GameApi.WorldObjectPosition(_selWorldObject);
                    _tpX = pos.x.ToString("0.##");
                    _tpY = (pos.y + 2f).ToString("0.##");
                    _tpZ = pos.z.ToString("0.##");
                }
                GUILayout.EndHorizontal();
            }

            EndSection();
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

            _badgeScroll = BeginVerticalScroll(_badgeScroll, GUILayout.Height(scrollHeight));
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

        private static void EnsureWindowRectLoaded()
        {
            if (_windowRectLoaded)
                return;
            _windowRectLoaded = true;
            if (ModConfig.WindowX == null || ModConfig.WindowY == null
                || ModConfig.WindowWidth == null || ModConfig.WindowHeight == null)
                return;

            _rect = new Rect(
                ModConfig.WindowX.Value,
                ModConfig.WindowY.Value,
                ModConfig.WindowWidth.Value,
                ModConfig.WindowHeight.Value);
            _lastSavedRect = _rect;
        }

        private static void PersistWindowRect()
        {
            if (!_windowRectLoaded || Input.GetMouseButton(0)
                || ModConfig.WindowX == null || ModConfig.WindowY == null
                || ModConfig.WindowWidth == null || ModConfig.WindowHeight == null)
                return;
            if (Mathf.Abs(_rect.x - _lastSavedRect.x) < 0.5f
                && Mathf.Abs(_rect.y - _lastSavedRect.y) < 0.5f
                && Mathf.Abs(_rect.width - _lastSavedRect.width) < 0.5f
                && Mathf.Abs(_rect.height - _lastSavedRect.height) < 0.5f)
                return;

            ModConfig.WindowX.Value = Mathf.Round(_rect.x);
            ModConfig.WindowY.Value = Mathf.Round(_rect.y);
            ModConfig.WindowWidth.Value = Mathf.Round(_rect.width);
            ModConfig.WindowHeight.Value = Mathf.Round(_rect.height);
            _lastSavedRect = _rect;
        }

        private static float EffectiveMinWindowWidth()
        {
            float t = Mathf.Clamp01((UiScale() - 1f) / 0.5f);
            return Mathf.Lerp(MinWindowWidth, 820f, t);
        }

        private static float EffectiveMinWindowHeight()
        {
            float t = Mathf.Clamp01((UiScale() - 1f) / 0.5f);
            return Mathf.Lerp(MinWindowHeight, 640f, t);
        }

        private static void CenterWindow()
        {
            _rect.x = Mathf.Round((VirtualScreenWidth() - _rect.width) * 0.5f);
            _rect.y = Mathf.Round((VirtualScreenHeight() - _rect.height) * 0.5f);
            ClampWindowToScreen();
        }

        private static void ClampWindowToScreen()
        {
            float margin = 8f;
            float screenWidth = VirtualScreenWidth();
            float screenHeight = VirtualScreenHeight();
            float maxWidth = Mathf.Max(420f, screenWidth - margin * 2f);
            float maxHeight = Mathf.Max(340f, screenHeight - margin * 2f);
            float minWidth = Mathf.Min(EffectiveMinWindowWidth(), maxWidth);
            float minHeight = Mathf.Min(EffectiveMinWindowHeight(), maxHeight);

            _rect.width = Mathf.Clamp(_rect.width, minWidth, maxWidth);
            _rect.height = Mathf.Clamp(_rect.height, minHeight, maxHeight);
            _rect.x = Mathf.Clamp(_rect.x, margin, screenWidth - _rect.width - margin);
            _rect.y = Mathf.Clamp(_rect.y, margin, screenHeight - _rect.height - margin);
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
            float screenWidth = VirtualScreenWidth();
            float screenHeight = VirtualScreenHeight();
            float maxWidth = Mathf.Max(420f, screenWidth - margin * 2f);
            float maxHeight = Mathf.Max(340f, screenHeight - margin * 2f);
            float minWidth = Mathf.Min(EffectiveMinWindowWidth(), maxWidth);
            float minHeight = Mathf.Min(EffectiveMinWindowHeight(), maxHeight);
            Vector2 delta = e.mousePosition - _resizeStartMouse;

            float xMin = _resizeStartRect.xMin;
            float xMax = _resizeStartRect.xMax;
            float yMin = _resizeStartRect.yMin;
            float yMax = _resizeStartRect.yMax;

            if ((_resizeEdge & ResizeEdge.Left) != 0)
                xMin = Mathf.Clamp(_resizeStartRect.xMin + delta.x, margin, _resizeStartRect.xMax - minWidth);
            if ((_resizeEdge & ResizeEdge.Right) != 0)
                xMax = Mathf.Clamp(_resizeStartRect.xMax + delta.x, _resizeStartRect.xMin + minWidth, screenWidth - margin);
            if ((_resizeEdge & ResizeEdge.Top) != 0)
                yMin = Mathf.Clamp(_resizeStartRect.yMin + delta.y, margin, _resizeStartRect.yMax - minHeight);
            if ((_resizeEdge & ResizeEdge.Bottom) != 0)
                yMax = Mathf.Clamp(_resizeStartRect.yMax + delta.y, _resizeStartRect.yMin + minHeight, screenHeight - margin);

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
            BeginScaledGui(out Matrix4x4 oldMatrix);
            try
            {
                Theme.EnsureBuilt();
                SyncThemeTextureCache();
                if (_noticeDismissed || !ModConfig.ShowDonateNotice.Value)
                    return;
                ApplyFont();
                float noticeWidth = 420f;
                float noticeHeight = _langOpen ? Mathf.Min(660f, VirtualScreenHeight() - 80f) : Mathf.Min(520f, VirtualScreenHeight() - 80f);
                ClampNoticeToScreen(noticeWidth, noticeHeight);
                _noticeRect = GUILayout.Window(1, _noticeRect, DrawNoticeWindowModern, GUIContent.none, Theme.Window,
                    GUILayout.Width(noticeWidth), GUILayout.Height(noticeHeight));
            }
            finally
            {
                EndScaledGui(oldMatrix);
            }
        }

        private static void ClampNoticeToScreen(float width, float height)
        {
            float margin = 12f;
            float screenWidth = VirtualScreenWidth();
            float screenHeight = VirtualScreenHeight();
            _noticeRect.width = width;
            _noticeRect.height = height;
            _noticeRect.x = Mathf.Clamp(_noticeRect.x, margin, Mathf.Max(margin, screenWidth - width - margin));
            _noticeRect.y = Mathf.Clamp(_noticeRect.y, margin, Mathf.Max(margin, screenHeight - height - margin));
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
            if (TryGetSimpleSectionIndex(key, out int simpleIndex))
            {
                if (simpleIndex != _simpleSection[_tab])
                    return false;
                GUILayout.BeginVertical(Theme.Card);
                GUILayout.Label(title, Theme.Section);
                GUILayout.Space(6f);
                return true;
            }

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

        private static void DrawItemFavoriteControls()
        {
            GUILayout.BeginHorizontal();
            _itemFavoritesOnly = ToggleCompact(Ru ? "Только избранные" : "Favorites only", _itemFavoritesOnly);
            bool hasSelection = _selItem >= 0 && _selItem < GameApi.ItemNames.Count;
            GUI.enabled = hasSelection;
            bool favorite = hasSelection && IsFavoriteItem(_selItem);
            if (GUILayout.Button(favorite ? (Ru ? "Убрать из избранного" : "Remove favorite") : (Ru ? "В избранное" : "Add favorite"), Theme.LinkBtn, GUILayout.Width(156f), GUILayout.Height(28)))
                SetFavoriteItem(_selItem, !favorite);
            GUI.enabled = true;
            if (GUILayout.Button(Ru ? "Очистить" : "Clear", Theme.LinkBtn, GUILayout.Width(90), GUILayout.Height(28)))
                ModConfig.FavoriteItemIds.Value = "";
            GUILayout.EndHorizontal();
            TipLast(Ru ? "Избранные предметы сохраняются по игровому itemID." : "Favorite items are saved by the game's itemID.");
        }

        private static bool IsFavoriteItem(int itemIndex)
        {
            if (ModConfig.FavoriteItemIds == null)
                return false;
            ushort id = GameApi.ItemIdAt(itemIndex);
            if (id == ushort.MaxValue)
                return false;
            EnsureFavoriteItems();
            return _favoriteItemIds.Contains(id);
        }

        private static void SetFavoriteItem(int itemIndex, bool enabled)
        {
            if (ModConfig.FavoriteItemIds == null)
                return;
            ushort id = GameApi.ItemIdAt(itemIndex);
            if (id == ushort.MaxValue)
                return;

            EnsureFavoriteItems();
            if (enabled)
                _favoriteItemIds.Add(id);
            else
                _favoriteItemIds.Remove(id);

            var ordered = new List<ushort>(_favoriteItemIds);
            ordered.Sort();
            var values = new string[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
                values[i] = ordered[i].ToString();
            string source = string.Join(",", values);
            ModConfig.FavoriteItemIds.Value = source;
            _favoriteItemSource = source;
        }

        private static void EnsureFavoriteItems()
        {
            string source = ModConfig.FavoriteItemIds?.Value ?? "";
            if (string.Equals(source, _favoriteItemSource, StringComparison.Ordinal))
                return;

            _favoriteItemIds.Clear();
            string[] values = source.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < values.Length; i++)
            {
                if (ushort.TryParse(values[i].Trim(), out ushort id))
                    _favoriteItemIds.Add(id);
            }
            _favoriteItemSource = source;
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

        private static void DrawItemCategoryBar(ref int selected)
        {
            string[] labels = Ru
                ? new[] { "Все", "Избранное", "Еда", "Расход", "Снаряж.", "Спина", "Мистика", "Другое" }
                : new[] { "All", "Favorites", "Food", "Consum.", "Gear", "Back", "Mystic", "Other" };
            DrawChipBar(ref selected, labels);
        }

        private static bool DrawItemTileGrid(string search, int category, int selectedIndex, Action<int> select)
        {
            bool changed = false;
            int columns = TileColumns(116f);
            float width = TileWidth(columns);
            int col = 0;
            bool rowOpen = false;
            string q = (search ?? "").Trim().ToLowerInvariant();

            for (int i = 0; i < GameApi.ItemNames.Count; i++)
            {
                string name = GameApi.ItemNames[i] ?? "";
                if (!ItemMatchesFilter(i, name, q, category))
                    continue;

                if (!rowOpen)
                {
                    GUILayout.BeginHorizontal();
                    rowOpen = true;
                }

                if (DrawItemTile(i, name, selectedIndex == i, width))
                {
                    select?.Invoke(i);
                    changed = true;
                }

                col++;
                if (col >= columns)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.Space(8f);
                    col = 0;
                    rowOpen = false;
                }
                else
                {
                    GUILayout.Space(8f);
                }
            }

            if (rowOpen)
            {
                while (col < columns)
                {
                    GUILayout.Space(width);
                    col++;
                    if (col < columns)
                        GUILayout.Space(8f);
                }
                GUILayout.EndHorizontal();
            }

            return changed;
        }

        private static bool DrawItemTile(int index, string name, bool active, float width)
        {
            Rect r = GUILayoutUtility.GetRect(width, 88f, GUILayout.Width(width), GUILayout.Height(88f));
            bool clicked = GUI.Button(r, GUIContent.none, active ? Theme.ListItemActive : Theme.ListItem);
            Texture2D icon = GameApi.ItemIcon(index);
            Rect iconRect = new Rect(r.x + (r.width - 38f) * 0.5f, r.y + 8f, 38f, 38f);
            if (icon != null)
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
            else
                GUI.Label(iconRect, ItemFallbackLetter(name), ItemIconFallbackStyle());

            GUI.Label(new Rect(r.x + 7f, r.y + 50f, r.width - 14f, 32f), name, active ? ItemTileLabelActiveStyle() : ItemTileLabelStyle());
            if (Event.current != null && r.Contains(Event.current.mousePosition))
                _hoverTip = Ru ? "Выбрать предмет для спавна, ESP или приколов." : "Select this item for spawning, ESP, or pranks.";
            return clicked;
        }

        private static bool ItemMatchesFilter(int index, string name, string search, int category)
        {
            if (search.Length > 0 && (name ?? "").ToLowerInvariant().IndexOf(search, StringComparison.Ordinal) < 0)
                return false;
            if (category == 0)
                return true;
            if (category == 1)
                return IsFavoriteItem(index);

            string haystack = ItemHaystack(index, name);
            switch (category)
            {
                case 2:
                    return ContainsAny(haystack, "food", "berry", "mushroom", "marshmallow", "hot dog", "hotdog", "coconut", "banana", "apple", "soup", "ration", "еда", "ягод", "гриб");
                case 3:
                    return ContainsAny(haystack, "drink", "cure", "heal", "medical", "bandage", "antidote", "fuel", "flare", "lantern", "torch", "consum", "леч", "топлив", "фонар", "факел");
                case 4:
                    return ContainsAny(haystack, "rope", "piton", "climb", "hook", "compass", "map", "radio", "bugle", "equipment", "снаряж", "верев", "крюк", "компас");
                case 5:
                    return ContainsAny(haystack, "backpack", "fannypack", "jetpack", "rocketpack", "pack", "рюкзак", "ранец");
                case 6:
                    return ContainsAny(haystack, "cursed", "curse", "skull", "pandora", "effigy", "faerie", "amulet", "medallion", "talisman", "charm", "mystical", "прокля", "череп", "амулет", "медальон", "талисман");
                default:
                    return !ItemMatchesFilter(index, name, "", 2)
                        && !ItemMatchesFilter(index, name, "", 3)
                        && !ItemMatchesFilter(index, name, "", 4)
                        && !ItemMatchesFilter(index, name, "", 5)
                        && !ItemMatchesFilter(index, name, "", 6);
            }
        }

        private static string ItemHaystack(int index, string name)
        {
            try
            {
                Item item = index >= 0 && index < GameApi.Items.Count ? GameApi.Items[index] : null;
                return ((name ?? "") + " "
                    + (item != null ? item.name : "") + " "
                    + (item != null && item.gameObject != null ? item.gameObject.name : "") + " "
                    + (item != null ? item.GetType().Name : "") + " "
                    + (item != null ? item.itemTags.ToString() : "")).ToLowerInvariant();
            }
            catch
            {
                return (name ?? "").ToLowerInvariant();
            }
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (string.IsNullOrWhiteSpace(value) || needles == null)
                return false;
            for (int i = 0; i < needles.Length; i++)
            {
                string needle = needles[i];
                if (!string.IsNullOrWhiteSpace(needle) && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static string ItemFallbackLetter(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";
            return name.Substring(0, 1).ToUpperInvariant();
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

        private static GUIStyle ItemTileLabelStyle()
        {
            if (_itemTileLabel == null)
                _itemTileLabel = new GUIStyle(Theme.LabelDim)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = 10,
                    wordWrap = true,
                    clipping = TextClipping.Clip,
                };
            return _itemTileLabel;
        }

        private static GUIStyle ItemTileLabelActiveStyle()
        {
            if (_itemTileLabelActive == null)
                _itemTileLabelActive = new GUIStyle(Theme.Label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true,
                    clipping = TextClipping.Clip,
                };
            return _itemTileLabelActive;
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

            float screenWidth = VirtualScreenWidth();
            float screenHeight = VirtualScreenHeight();
            float width = Mathf.Min(560f, Mathf.Max(320f, screenWidth * 0.44f));
            float maxHeight = Mathf.Max(140f, screenHeight * 0.72f);
            float innerWidth = width - 24f;
            float needed = Theme.Tooltip.CalcHeight(new GUIContent(tip), innerWidth) + 22f;
            float height = Mathf.Clamp(needed, 44f, maxHeight);

            float x = mouse.x + 18f;
            float y = mouse.y + 20f;
            if (x + width > screenWidth - 10f) x = mouse.x - width - 18f;
            if (y + height > screenHeight - 10f) y = mouse.y - height - 18f;

            Rect tipRect = new Rect(x, y, width, height);
            if (avoidWindow && tipRect.Overlaps(_rect))
            {
                float rightX = _rect.xMax + 14f;
                float leftX = _rect.xMin - width - 14f;
                float belowY = _rect.yMax + 12f;
                float aboveY = _rect.yMin - height - 12f;

                if (rightX + width <= screenWidth - 10f) tipRect.x = rightX;
                else if (leftX >= 10f) tipRect.x = leftX;
                else if (belowY + height <= screenHeight - 10f) tipRect.y = belowY;
                else if (aboveY >= 10f) tipRect.y = aboveY;
            }

            tipRect.x = Mathf.Clamp(tipRect.x, 10f, screenWidth - width - 10f);
            tipRect.y = Mathf.Clamp(tipRect.y, 10f, screenHeight - height - 10f);

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
            }
            GUILayout.EndHorizontal();

            if (_targetPickerOpen)
            {
                if (GUILayout.Button(Ru ? "Себе" : "Me", sel < 0 ? Theme.ListItemActive : Theme.ListItem, GUILayout.Height(28)))
                {
                    sel = -1;
                    _targetPickerOpen = false;
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
            }
            GUILayout.EndHorizontal();

            if (_inventoryTargetPickerOpen)
            {
                if (GUILayout.Button(Ru ? "Себе" : "Me", _invTarget < 0 ? Theme.ListItemActive : Theme.ListItem, GUILayout.Height(28)))
                {
                    _invTarget = -1;
                    _inventoryTargetPickerOpen = false;
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
        private static GUIStyle _itemTileLabel;
        private static GUIStyle _itemTileLabelActive;

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
            GUILayout.Label(Ru ? "Спасибо всем, кто поддерживает разработку PEAK-MX!" : "Thank you to everyone supporting PEAK-MX!", Theme.Label);
        }

        // ---------- donation QR ----------
        private static byte[] _qrBytes;
        private static Texture2D _qrTex;

        private static Texture2D QrTexture()
        {
            if (_qrTex != null)
                return _qrTex;

            if (_qrBytes == null)
            {
                try { _qrBytes = Convert.FromBase64String(EmbeddedDonateQrPngBase64); }
                catch (Exception e) { Plugin.Log?.LogDebug($"[QR] embedded decode failed: {e.Message}"); }
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
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            if (tex != null)
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
        }
    }
}
