using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using Zorro.Core;

namespace PeakMX
{
    /// <summary>Steam achievement and icon helpers used by the menu.</summary>
    public static class SteamAch
    {
        private readonly struct LinkedAchievement
        {
            public readonly STEAMSTATTYPE Stat;
            public readonly int RequiredValue;

            public LinkedAchievement(STEAMSTATTYPE stat, int requiredValue)
            {
                Stat = stat;
                RequiredValue = requiredValue;
            }
        }

        private static ACHIEVEMENTTYPE[] _all;
        private static readonly Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<ACHIEVEMENTTYPE, string> _nameCache = new Dictionary<ACHIEVEMENTTYPE, string>();
        private static readonly Dictionary<ACHIEVEMENTTYPE, string> _descCache = new Dictionary<ACHIEVEMENTTYPE, string>();
        private static readonly Dictionary<ACHIEVEMENTTYPE, LinkedAchievement> _linked = new Dictionary<ACHIEVEMENTTYPE, LinkedAchievement>
        {
            { ACHIEVEMENTTYPE.ForestryBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_Forestry, 1) },
            { ACHIEVEMENTTYPE.TreadLightlyBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_TreadLightly, 1) },
            { ACHIEVEMENTTYPE.WebSecurityBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_WebSecurity, 1) },
            { ACHIEVEMENTTYPE.UndeadEncounterBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_UndeadEncounter, 1) },
            { ACHIEVEMENTTYPE.AdvancedMycologyBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_AdvancedMycology, 1) },
            { ACHIEVEMENTTYPE.DisasterResponseBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_DisasterResponse, 1) },
            { ACHIEVEMENTTYPE.CalciumIntakeBadge, new LinkedAchievement(STEAMSTATTYPE.DamageBlockedByMilk, 100) },
            { ACHIEVEMENTTYPE.CompetitiveEatingBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_CompetitiveEating, 1) },
            { ACHIEVEMENTTYPE.AppliedEsotericaBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_AppliedEsoterica, 1) },
            { ACHIEVEMENTTYPE.MycoacrobaticsBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_Mycoacrobatics, 1) },
            { ACHIEVEMENTTYPE.CryptogastronomyBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_Cryptogastronomy, 1) },
            { ACHIEVEMENTTYPE.WandererBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_Wanderer, 1) },
            { ACHIEVEMENTTYPE.BellringerBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_Bellringer, 1) },
            { ACHIEVEMENTTYPE.WellRestedBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_WellRested, 1) },
            { ACHIEVEMENTTYPE.JesterBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_Jester, 1) },
            { ACHIEVEMENTTYPE.HangGlidingBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_HangGliding, 1) },
            { ACHIEVEMENTTYPE.MedievalHistoryBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_MedievalHistory, 1) },
            { ACHIEVEMENTTYPE.LastResortBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_LastResort, 1) },
            { ACHIEVEMENTTYPE.ExorcistBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_Exorcist, 1) },
            { ACHIEVEMENTTYPE.ArcheryBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_Archery, 1) },
            { ACHIEVEMENTTYPE.RuleZeroBadge, new LinkedAchievement(STEAMSTATTYPE.GotBadge_RuleZero, 1) },
        };
        private static readonly Dictionary<ACHIEVEMENTTYPE, string> _ruNames = new Dictionary<ACHIEVEMENTTYPE, string>
        {
            { ACHIEVEMENTTYPE.TriedYourBestBadge, "Ты старался" },
            { ACHIEVEMENTTYPE.BeachcomberBadge, "Пляжный сборщик" },
            { ACHIEVEMENTTYPE.TrailblazerBadge, "Первопроходец" },
            { ACHIEVEMENTTYPE.AlpinistBadge, "Альпинист" },
            { ACHIEVEMENTTYPE.VolcanologyBadge, "Вулканолог" },
            { ACHIEVEMENTTYPE.CookingBadge, "Повар" },
            { ACHIEVEMENTTYPE.HappyCamperBadge, "Счастливый турист" },
            { ACHIEVEMENTTYPE.BoulderingBadge, "Боулдеринг" },
            { ACHIEVEMENTTYPE.ToxicologyBadge, "Токсиколог" },
            { ACHIEVEMENTTYPE.ForagingBadge, "Собиратель" },
            { ACHIEVEMENTTYPE.EsotericaBadge, "Эзотерика" },
            { ACHIEVEMENTTYPE.PeakBadge, "Покоритель пика" },
            { ACHIEVEMENTTYPE.LoneWolfBadge, "Одинокий волк" },
            { ACHIEVEMENTTYPE.ClutchBadge, "Спасение в последний момент" },
            { ACHIEVEMENTTYPE.BalloonBadge, "Воздухоплаватель" },
            { ACHIEVEMENTTYPE.LeaveNoTraceBadge, "Не оставляй следов" },
            { ACHIEVEMENTTYPE.SpeedClimberBadge, "Скоростной альпинист" },
            { ACHIEVEMENTTYPE.BingBongBadge, "Бинг-Бонг" },
            { ACHIEVEMENTTYPE.NaturalistBadge, "Натуралист" },
            { ACHIEVEMENTTYPE.GourmandBadge, "Гурман" },
            { ACHIEVEMENTTYPE.MycologyBadge, "Микология" },
            { ACHIEVEMENTTYPE.FirstAidBadge, "Первая помощь" },
            { ACHIEVEMENTTYPE.SurvivalistBadge, "Выживальщик" },
            { ACHIEVEMENTTYPE.AnimalSerenadingBadge, "Серенада животным" },
            { ACHIEVEMENTTYPE.ArboristBadge, "Древовед" },
            { ACHIEVEMENTTYPE.MentorshipBadge, "Наставник" },
            { ACHIEVEMENTTYPE.KnotTyingBadge, "Вязание узлов" },
            { ACHIEVEMENTTYPE.EmergencyPreparednessBadge, "Готовность к ЧС" },
            { ACHIEVEMENTTYPE.AscenderBadge, "Восхождение" },
            { ACHIEVEMENTTYPE.PlundererBadge, "Расхититель" },
            { ACHIEVEMENTTYPE.BookwormBadge, "Книжный червь" },
            { ACHIEVEMENTTYPE.EnduranceBadge, "Выносливость" },
            { ACHIEVEMENTTYPE.ResourcefulnessBadge, "Находчивость" },
            { ACHIEVEMENTTYPE.NomadBadge, "Кочевник" },
            { ACHIEVEMENTTYPE.UltimateBadge, "Абсолют" },
            { ACHIEVEMENTTYPE.CoolCucumberBadge, "Спокойствие" },
            { ACHIEVEMENTTYPE.NeedlepointBadge, "Игла" },
            { ACHIEVEMENTTYPE.AeronauticsBadge, "Аэронавтика" },
            { ACHIEVEMENTTYPE.TwentyFourKaratBadge, "Чистое золото" },
            { ACHIEVEMENTTYPE.DaredevilBadge, "Сорвиголова" },
            { ACHIEVEMENTTYPE.MegaentomologyBadge, "Мегаэнтомология" },
            { ACHIEVEMENTTYPE.AstronomyBadge, "Астрономия" },
            { ACHIEVEMENTTYPE.BundledUpBadge, "Теплее некуда" },
            { ACHIEVEMENTTYPE.ForestryBadge, "Лесничий" },
            { ACHIEVEMENTTYPE.TreadLightlyBadge, "Лёгкий шаг" },
            { ACHIEVEMENTTYPE.WebSecurityBadge, "Защита от паутины" },
            { ACHIEVEMENTTYPE.UndeadEncounterBadge, "Встреча с нежитью" },
            { ACHIEVEMENTTYPE.AdvancedMycologyBadge, "Продвинутая микология" },
            { ACHIEVEMENTTYPE.DisasterResponseBadge, "Реакция на бедствие" },
            { ACHIEVEMENTTYPE.CalciumIntakeBadge, "Кальциевая диета" },
            { ACHIEVEMENTTYPE.CompetitiveEatingBadge, "Соревновательное обжорство" },
            { ACHIEVEMENTTYPE.AppliedEsotericaBadge, "Прикладная эзотерика" },
            { ACHIEVEMENTTYPE.MycoacrobaticsBadge, "Микоакробатика" },
            { ACHIEVEMENTTYPE.CryptogastronomyBadge, "Криптогастрономия" },
            { ACHIEVEMENTTYPE.WandererBadge, "Странник" },
            { ACHIEVEMENTTYPE.BellringerBadge, "Звонарь" },
            { ACHIEVEMENTTYPE.WellRestedBadge, "Хорошо отдохнул" },
            { ACHIEVEMENTTYPE.JesterBadge, "Шут" },
            { ACHIEVEMENTTYPE.HangGlidingBadge, "Дельтапланеризм" },
            { ACHIEVEMENTTYPE.MedievalHistoryBadge, "Средневековая история" },
            { ACHIEVEMENTTYPE.LastResortBadge, "Последний шанс" },
            { ACHIEVEMENTTYPE.ExorcistBadge, "Экзорцист" },
            { ACHIEVEMENTTYPE.ArcheryBadge, "Стрельба из лука" },
            { ACHIEVEMENTTYPE.RuleZeroBadge, "Правило ноль" },
        };
        /// <summary>All real achievements (enum values except NONE).</summary>
        public static ACHIEVEMENTTYPE[] AllTypes
        {
            get
            {
                if (_all != null) return _all;
                var list = new List<ACHIEVEMENTTYPE>();
                foreach (ACHIEVEMENTTYPE t in Enum.GetValues(typeof(ACHIEVEMENTTYPE)))
                    if (IsVisibleAchievement(t)) list.Add(t);
                _all = list.ToArray();
                return _all;
            }
        }

        public static string Id(ACHIEVEMENTTYPE t) => t.ToString();

        private static bool IsVisibleAchievement(ACHIEVEMENTTYPE t)
        {
            switch (t)
            {
                case ACHIEVEMENTTYPE.NONE:
                case ACHIEVEMENTTYPE.Ascent1:
                case ACHIEVEMENTTYPE.Ascent2:
                case ACHIEVEMENTTYPE.Ascent3:
                case ACHIEVEMENTTYPE.Ascent4:
                case ACHIEVEMENTTYPE.Ascent5:
                case ACHIEVEMENTTYPE.Ascent6:
                case ACHIEVEMENTTYPE.Ascent7:
                case ACHIEVEMENTTYPE.Ascent8:
                    return false;
                default:
                    return true;
            }
        }

        private static bool Ready
        {
            get { try { return SteamAPI.IsSteamRunning() && SteamUser.BLoggedOn(); } catch { return false; } }
        }

        private static AchievementManager Manager
        {
            get
            {
                try { return Singleton<AchievementManager>.Instance; }
                catch { return null; }
            }
        }

        public static bool IsUnlocked(ACHIEVEMENTTYPE t)
        {
            if (!Ready) return false;
            try
            {
                if (TryGetLinked(t, out var linked))
                    return TryGetStat(linked.Stat, out int value) && value >= linked.RequiredValue;

                return SteamUserStats.GetAchievement(Id(t), out bool a) && a;
            }
            catch { return false; }
        }

        public static void Unlock(ACHIEVEMENTTYPE t, bool track = true)
        {
            if (!Ready) return;
            try
            {
                if (IsUnlocked(t))
                    return;

                if (TryGetLinked(t, out var linked))
                {
                    SetStat(linked.Stat, linked.RequiredValue);
                    return;
                }

                SteamUserStats.SetAchievement(Id(t));
                SteamUserStats.StoreStats();
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[Ach] Unlock {t}: {e.Message}"); }
        }

        public static void Revoke(ACHIEVEMENTTYPE t, bool track = true)
        {
            if (!Ready) return;
            try
            {
                if (!IsUnlocked(t))
                    return;

                if (TryGetLinked(t, out var linked))
                    SetStat(linked.Stat, 0);

                SteamUserStats.ClearAchievement(Id(t));
                SteamUserStats.StoreStats();
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[Ach] Revoke {t}: {e.Message}"); }
        }

        public static void Toggle(ACHIEVEMENTTYPE t)
        {
            if (IsUnlocked(t)) Revoke(t); else Unlock(t);
        }

        public static void UnlockAll()
        {
            if (!Ready) return;
            try
            {
                int changed = 0;
                foreach (var t in AllTypes)
                {
                    if (IsUnlocked(t)) continue;
                    Unlock(t, false);
                    changed++;
                }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[Ach] UnlockAll: {e.Message}"); }
        }

        public static void RevokeAll()
        {
            if (!Ready) return;
            try
            {
                int changed = 0;
                foreach (var t in AllTypes)
                {
                    if (!IsUnlocked(t)) continue;
                    Revoke(t, false);
                    changed++;
                }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[Ach] RevokeAll: {e.Message}"); }
        }

        public static int UnlockedCount()
        {
            int n = 0;
            foreach (var t in AllTypes) if (IsUnlocked(t)) n++;
            return n;
        }

        public static string DisplayName(ACHIEVEMENTTYPE t)
        {
            if (Localization.Current == Lang.Russian && _ruNames.TryGetValue(t, out var ruName))
                return ruName;

            if (_nameCache.TryGetValue(t, out var cached))
                return cached;

            string value = null;
            if (Ready)
                try
                {
                    string s = SteamUserStats.GetAchievementDisplayAttribute(Id(t), "name");
                    if (!string.IsNullOrEmpty(s)) value = s;
                }
                catch { }

            value ??= Prettify(Id(t));
            _nameCache[t] = value;
            return value;
        }

        public static string Desc(ACHIEVEMENTTYPE t)
        {
            if (Localization.Current == Lang.Russian)
            {
                return "Достижение PEAK: " + DisplayName(t) + ".";
            }

            if (_descCache.TryGetValue(t, out var cached))
                return cached;

            string value = "";
            if (Ready)
                try { value = SteamUserStats.GetAchievementDisplayAttribute(Id(t), "desc") ?? ""; }
                catch { }

            _descCache[t] = value;
            return value;
        }

        /// <summary>
        /// Steam achievement icon as a Texture2D. Steam loads icons asynchronously: the first
        /// call(s) may return null (handle 0) — retry on later frames. Cached once built.
        /// Must be called on the main thread (it is — from OnGUI).
        /// </summary>
        public static Texture2D Icon(ACHIEVEMENTTYPE t)
        {
            string id = Id(t);
            if (_iconCache.TryGetValue(id, out var cached)) return cached;
            if (!Ready) return null;
            try
            {
                int handle = SteamUserStats.GetAchievementIcon(id);
                if (handle == 0) return null; // still downloading; try again next frame
                if (!SteamUtils.GetImageSize(handle, out uint w, out uint h) || w == 0 || h == 0)
                    return null;
                var buf = new byte[w * h * 4];
                if (!SteamUtils.GetImageRGBA(handle, buf, buf.Length))
                    return null;

                var tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false)
                { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };

                // Steam image is top-down; Unity SetPixels expects bottom-up — flip rows.
                int iw = (int)w, ih = (int)h;
                var pixels = new Color32[iw * ih];
                for (int y = 0; y < ih; y++)
                {
                    int src = y * iw * 4;
                    int dstRow = (ih - 1 - y) * iw;
                    for (int x = 0; x < iw; x++)
                    {
                        int s = src + x * 4;
                        pixels[dstRow + x] = new Color32(buf[s], buf[s + 1], buf[s + 2], buf[s + 3]);
                    }
                }
                tex.SetPixels32(pixels);
                tex.Apply();
                _iconCache[id] = tex;
                return tex;
            }
            catch (Exception e) { Plugin.Log?.LogDebug($"[Ach] Icon {t}: {e.Message}"); return null; }
        }

        /// <summary>Bump the MaxAscent stat so ascent-gated / Goat / Crown cosmetics unlock.</summary>
        public static void SetMaxAscent(int value)
        {
            SetStat(STEAMSTATTYPE.MaxAscent, value);
        }

        public static void SetStatValue(STEAMSTATTYPE stat, int value)
        {
            SetStat(stat, value);
        }

        public static void UnlockAllCosmetics()
        {
            UnlockAll();
            if (TryGetStat(STEAMSTATTYPE.MaxAscent, out int current) && current >= 8)
            {
                return;
            }

            SetMaxAscent(8);
            SetStat(STEAMSTATTYPE.LoadedCosmeticsPreviously, 1);
        }

        private static bool TryGetLinked(ACHIEVEMENTTYPE t, out LinkedAchievement linked)
            => _linked.TryGetValue(t, out linked);

        private static bool TryGetStat(STEAMSTATTYPE stat, out int value)
        {
            value = 0;

            var manager = Manager;
            if (manager != null)
            {
                try { return manager.GetSteamStatInt(stat, out value); }
                catch { }
            }

            if (!Ready) return false;
            try { return SteamUserStats.GetStat(stat.ToString(), out value); }
            catch { return false; }
        }

        private static void SetStat(STEAMSTATTYPE stat, int value)
        {
            bool handled = false;
            var manager = Manager;
            if (manager != null)
            {
                try
                {
                    manager.SetSteamStat(stat, value);
                    handled = true;
                }
                catch (Exception e)
                {
                    Plugin.Log?.LogDebug($"[Ach] Manager SetStat {stat}: {e.Message}");
                }
            }

            if (!Ready) return;
            try
            {
                if (!handled)
                    SteamUserStats.SetStat(stat.ToString(), value);
                SteamUserStats.StoreStats();
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[Ach] SetStat {stat}: {e.Message}"); }
        }

        private static string Prettify(string enumName)
        {
            if (string.IsNullOrEmpty(enumName)) return enumName;
            if (enumName.EndsWith("Badge")) enumName = enumName.Substring(0, enumName.Length - 5);
            var sb = new System.Text.StringBuilder(enumName.Length + 6);
            for (int i = 0; i < enumName.Length; i++)
            {
                char c = enumName[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(enumName[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
