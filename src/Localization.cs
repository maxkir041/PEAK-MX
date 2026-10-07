using System.Collections.Generic;

namespace PeakMX
{
    /// <summary>Languages offered by PEAK-MX.</summary>
    public enum Lang
    {
        English = 0,
        Russian = 1,
        Ukrainian = 2,
        ChineseSimplified = 3,
        ChineseTraditional = 4,
        Japanese = 5,
        Korean = 6,
        Spanish = 7,
        PortugueseBR = 8,
        German = 9,
        French = 10,
        Italian = 11,
        Polish = 12,
        Turkish = 13,
    }

    public static class Localization
    {
        public static Lang Current = Lang.Russian;

        public static readonly (Lang lang, string name)[] Options =
        {
            (Lang.English, "English"),
            (Lang.Russian, "Русский"),
            (Lang.Ukrainian, "Українська"),
            (Lang.ChineseSimplified, "简体中文"),
            (Lang.ChineseTraditional, "繁體中文"),
            (Lang.Japanese, "日本語"),
            (Lang.Korean, "한국어"),
            (Lang.Spanish, "Español"),
            (Lang.PortugueseBR, "Português (BR)"),
            (Lang.German, "Deutsch"),
            (Lang.French, "Français"),
            (Lang.Italian, "Italiano"),
            (Lang.Polish, "Polski"),
            (Lang.Turkish, "Türkçe"),
        };

        // Stable key order shared by every language table below.
        private static readonly string[] Keys =
        {
            "tab.character", "tab.cheats", "tab.admin", "tab.inventory", "tab.world", "tab.badges",
            "tab.cosmetics", "tab.about", "tab.settings", "tab.anticheat",
            "feat.speed", "feat.jump", "feat.climb", "feat.vineclimb", "feat.ropeclimb",
            "feat.god", "feat.infstam", "feat.nofall", "feat.noweight", "feat.lockstatus",
            "feat.tpping", "feat.fly", "wip", "ui.language",
        };

        private static readonly Dictionary<Lang, string[]> Values = new()
        {
            [Lang.English] = new[]
            {
                "Character", "Cheats", "Admin", "Inventory", "World", "Badges", "Cosmetics", "About", "Settings", "Anti-cheat",
                "Speed", "Jump", "Climb speed", "Vine climb", "Rope climb",
                "God mode", "Infinite stamina", "No fall damage", "No weight", "Lock status (freeze afflictions)",
                "Teleport to ping", "Fly", "work-in-progress", "Language",
            },
            [Lang.Russian] = new[]
            {
                "Персонаж", "Читы", "Админ", "Инвентарь", "Мир", "Ачивки", "Косметика", "О моде", "Настройки", "Античит",
                "Скорость", "Прыжок", "Скорость лазания", "Лазание по лозам", "Лазание по верёвкам",
                "Бессмертие", "Бесконечная стамина", "Без урона от падения", "Без веса", "Заморозка статусов",
                "Телепорт к пингу", "Полёт", "в разработке", "Язык",
            },
            [Lang.Ukrainian] = new[]
            {
                "Персонаж", "Чити", "Адмін", "Інвентар", "Світ", "Ачивки", "Косметика", "Про мод", "Налаштування", "Античит",
                "Швидкість", "Стрибок", "Швидкість лазіння", "Лазіння по ліанах", "Лазіння по мотузках",
                "Безсмертя", "Нескінченна витривалість", "Без шкоди від падіння", "Без ваги", "Заморозка статусів",
                "Телепорт до пінгу", "Політ", "у розробці", "Мова",
            },
            [Lang.ChineseSimplified] = new[]
            {
                "角色", "作弊", "管理", "物品栏", "世界", "成就", "外观", "关于", "设置", "反作弊",
                "速度", "跳跃", "攀爬速度", "藤蔓攀爬", "绳索攀爬",
                "无敌模式", "无限体力", "无坠落伤害", "无重量", "锁定状态",
                "传送到标记", "飞行", "开发中", "语言",
            },
            [Lang.ChineseTraditional] = new[]
            {
                "角色", "作弊", "管理", "物品欄", "世界", "成就", "外觀", "關於", "設定", "反作弊",
                "速度", "跳躍", "攀爬速度", "藤蔓攀爬", "繩索攀爬",
                "無敵模式", "無限體力", "無墜落傷害", "無重量", "鎖定狀態",
                "傳送到標記", "飛行", "開發中", "語言",
            },
            [Lang.Japanese] = new[]
            {
                "キャラクター", "チート", "管理", "インベントリ", "ワールド", "実績", "外見", "情報", "設定", "アンチチート",
                "スピード", "ジャンプ", "登坂速度", "ツル登り", "ロープ登り",
                "無敵モード", "無限スタミナ", "落下ダメージ無効", "重量なし", "ステータス固定",
                "ピンへテレポート", "飛行", "開発中", "言語",
            },
            [Lang.Korean] = new[]
            {
                "캐릭터", "치트", "관리", "인벤토리", "월드", "업적", "외형", "정보", "설정", "안티치트",
                "속도", "점프", "등반 속도", "덩굴 등반", "밧줄 등반",
                "무적 모드", "무한 스태미나", "낙하 데미지 없음", "무게 없음", "상태 고정",
                "핑으로 순간이동", "비행", "개발 중", "언어",
            },
            [Lang.Spanish] = new[]
            {
                "Personaje", "Trucos", "Admin", "Inventario", "Mundo", "Logros", "Cosméticos", "Acerca de", "Ajustes", "Antitrampas",
                "Velocidad", "Salto", "Velocidad de escalada", "Escalada de lianas", "Escalada de cuerdas",
                "Modo dios", "Resistencia infinita", "Sin daño por caída", "Sin peso", "Bloquear estado",
                "Teletransporte al ping", "Volar", "en desarrollo", "Idioma",
            },
            [Lang.PortugueseBR] = new[]
            {
                "Personagem", "Trapaças", "Admin", "Inventário", "Mundo", "Conquistas", "Cosméticos", "Sobre", "Configurações", "Antitrapaça",
                "Velocidade", "Pulo", "Velocidade de escalada", "Escalada de cipó", "Escalada de corda",
                "Modo deus", "Estamina infinita", "Sem dano de queda", "Sem peso", "Travar status",
                "Teleportar para o ping", "Voar", "em desenvolvimento", "Idioma",
            },
            [Lang.German] = new[]
            {
                "Charakter", "Cheats", "Admin", "Inventar", "Welt", "Erfolge", "Kosmetik", "Über", "Einstellungen", "Anti-Cheat",
                "Geschwindigkeit", "Sprung", "Klettergeschwindigkeit", "Rankenklettern", "Seilklettern",
                "Gottmodus", "Unendliche Ausdauer", "Kein Fallschaden", "Kein Gewicht", "Status einfrieren",
                "Zum Ping teleportieren", "Fliegen", "in Arbeit", "Sprache",
            },
            [Lang.French] = new[]
            {
                "Personnage", "Triches", "Admin", "Inventaire", "Monde", "Succès", "Cosmétiques", "À propos", "Paramètres", "Anti-triche",
                "Vitesse", "Saut", "Vitesse d'escalade", "Escalade de lianes", "Escalade de corde",
                "Mode dieu", "Endurance infinie", "Aucun dégât de chute", "Sans poids", "Verrouiller le statut",
                "Téléportation au ping", "Voler", "en cours", "Langue",
            },
            [Lang.Italian] = new[]
            {
                "Personaggio", "Trucchi", "Admin", "Inventario", "Mondo", "Obiettivi", "Cosmetici", "Info", "Impostazioni", "Anti-cheat",
                "Velocità", "Salto", "Velocità di arrampicata", "Arrampicata su liane", "Arrampicata su corda",
                "Modalità dio", "Stamina infinita", "Nessun danno da caduta", "Senza peso", "Blocca stato",
                "Teletrasporto al ping", "Vola", "in lavorazione", "Lingua",
            },
            [Lang.Polish] = new[]
            {
                "Postać", "Cheaty", "Admin", "Ekwipunek", "Świat", "Osiągnięcia", "Kosmetyki", "O modzie", "Ustawienia", "Antycheat",
                "Prędkość", "Skok", "Prędkość wspinania", "Wspinaczka po pnączach", "Wspinaczka po linie",
                "Tryb boga", "Nieskończona wytrzymałość", "Brak obrażeń od upadku", "Bez wagi", "Zablokuj status",
                "Teleport do pingu", "Latanie", "w trakcie prac", "Język",
            },
            [Lang.Turkish] = new[]
            {
                "Karakter", "Hileler", "Admin", "Envanter", "Dünya", "Başarımlar", "Kozmetikler", "Hakkında", "Ayarlar", "Anti-hile",
                "Hız", "Zıplama", "Tırmanma hızı", "Sarmaşık tırmanışı", "Halat tırmanışı",
                "Tanrı modu", "Sınırsız dayanıklılık", "Düşme hasarı yok", "Ağırlık yok", "Durumu kilitle",
                "Ping'e ışınlan", "Uçuş", "geliştiriliyor", "Dil",
            },
        };

        private static readonly Dictionary<string, int> Index = BuildIndex();

        private static Dictionary<string, int> BuildIndex()
        {
            var d = new Dictionary<string, int>();
            for (int i = 0; i < Keys.Length; i++)
                d[Keys[i]] = i;
            return d;
        }

        public static string T(string key)
        {
            if (!Index.TryGetValue(key, out int i))
                return key;
            if (Values.TryGetValue(Current, out var arr) && i < arr.Length && !string.IsNullOrEmpty(arr[i]))
                return arr[i];
            return Values[Lang.English][i];
        }

        public static string Pick(
            Lang lang,
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
            return lang switch
            {
                Lang.Russian => ru,
                Lang.Ukrainian => uk ?? en,
                Lang.ChineseSimplified => zhCn ?? en,
                Lang.ChineseTraditional => zhTw ?? zhCn ?? en,
                Lang.Japanese => ja ?? en,
                Lang.Korean => ko ?? en,
                Lang.Spanish => es ?? en,
                Lang.PortugueseBR => ptBr ?? en,
                Lang.German => de ?? en,
                Lang.French => fr ?? en,
                Lang.Italian => it ?? en,
                Lang.Polish => pl ?? en,
                Lang.Turkish => tr ?? en,
                _ => en,
            };
        }

        // Per-feature descriptions shown as tooltips. EN + RU; other languages fall back to EN.
        private static readonly Dictionary<Lang, Dictionary<string, string>> Desc = new()
        {
            [Lang.English] = new()
            {
                ["feat.speed"] = "Increases your movement speed by the chosen multiplier.",
                ["feat.jump"] = "Increases your jump force/height by the chosen multiplier.",
                ["feat.climb"] = "Increases climbing speed by the chosen multiplier.",
                ["feat.vineclimb"] = "Climb vines faster by the chosen multiplier.",
                ["feat.ropeclimb"] = "Climb ropes faster by the chosen multiplier.",
                ["feat.god"] = "Invincibility — you take no damage from anything.",
                ["feat.infstam"] = "Your stamina never runs out.",
                ["feat.nofall"] = "Removes all fall damage.",
                ["feat.noweight"] = "Carried items no longer slow you down.",
                ["feat.lockstatus"] = "Freezes all status effects (hunger, cold, poison, weight...) at their current values.",
                ["feat.tpping"] = "Teleports you to your ping marker.",
                ["feat.fly"] = "Free-fly noclip movement through the world.",
            },
            [Lang.Russian] = new()
            {
                ["feat.speed"] = "Увеличивает скорость передвижения на выбранный множитель.",
                ["feat.jump"] = "Увеличивает силу и высоту прыжка на выбранный множитель.",
                ["feat.climb"] = "Увеличивает скорость лазания на выбранный множитель.",
                ["feat.vineclimb"] = "Быстрее лазать по лозам на выбранный множитель.",
                ["feat.ropeclimb"] = "Быстрее лазать по верёвкам на выбранный множитель.",
                ["feat.god"] = "Неуязвимость — вы не получаете никакого урона.",
                ["feat.infstam"] = "Стамина никогда не заканчивается.",
                ["feat.nofall"] = "Полностью убирает урон от падения.",
                ["feat.noweight"] = "Переносимые предметы больше не замедляют вас.",
                ["feat.lockstatus"] = "Замораживает все статусы (голод, холод, яд, вес...) на текущих значениях.",
                ["feat.tpping"] = "Телепортирует к метке пинга.",
                ["feat.fly"] = "Свободный полёт сквозь геометрию.",
            },
        };

        /// <summary>Tooltip/description for a feature key (EN fallback).</summary>
        public static string D(string key)
        {
            var lang = Desc.ContainsKey(Current) ? Current : Lang.English;
            if (Desc[lang].TryGetValue(key, out var s))
                return s;
            return Desc[Lang.English].TryGetValue(key, out var e) ? e : "";
        }

        /// <summary>OS font name needed to render this language, or null if the default font suffices.</summary>
        public static string RequiredOsFont(Lang lang) => lang switch
        {
            Lang.ChineseSimplified => "Microsoft YaHei",
            Lang.ChineseTraditional => "Microsoft JhengHei",
            Lang.Japanese => "Yu Gothic",
            Lang.Korean => "Malgun Gothic",
            _ => null,
        };
    }
}
