using BepInEx.Configuration;
using UnityEngine;

namespace PeakMX
{
    /// <summary>Config entries and runtime flags.</summary>
    public static class ModConfig
    {
        // ---- General ----
        public static ConfigEntry<KeyCode> MenuToggleKey;
        public static ConfigEntry<int> Language;
        public static ConfigEntry<bool> ShowDonateNotice;
        public static ConfigEntry<bool> ManyPlayersEnabled;
        public static ConfigEntry<int> ManyPlayersMaxPlayers;
        public static ConfigEntry<bool> ManyPlayersHostOnlyKiosk;
        public static ConfigEntry<bool> ManyPlayersLobbyDetails;
        public static ConfigEntry<bool> ManyPlayersVoiceFix;
        public static ConfigEntry<bool> ManyPlayersUiFix;
        public static ConfigEntry<bool> ManyPlayersPluginAudit;
        public static ConfigEntry<bool> ManyPlayersPluginAuditKick;
        public static ConfigEntry<bool> AntiCheatEnabledEntry;
        public static ConfigEntry<bool> AntiCheatLockClients;
        public static ConfigEntry<bool> AntiCheatKickMissingData;
        public static ConfigEntry<bool> MxAhgRequireAgent;
        public static ConfigEntry<bool> MxAhgExactPack;
        public static ConfigEntry<bool> MxAhgKickMissing;
        public static ConfigEntry<bool> MxAhgKickMismatch;
        public static ConfigEntry<bool> MxAhgKickSuspicious;
        public static ConfigEntry<bool> MxAhgKickStale;
        public static ConfigEntry<float> MxAhgGraceSeconds;
        public static ConfigEntry<float> MxAhgRecheckSeconds;
        public static ConfigEntry<string> MxAhgDeniedKeywords;
        public const string DefaultMxAhgDeniedKeywords = "cheat;trainer;hack;godmode;exploit;injector";
        public const int QuickActionSlotCount = 8;
        public static ConfigEntry<KeyCode>[] QuickActionKeys;
        public static ConfigEntry<string>[] QuickActionIds;


        // ---- Movement ----
        public static bool SpeedMod;
        public static float SpeedAmount = 1f;
        public static bool JumpMod;
        public static float JumpAmount = 1.5f;
        public static bool InfiniteJumps;
        public static bool ClimbMod;
        public static float ClimbAmount = 1f;
        public static bool VineClimbMod;
        public static float VineClimbAmount = 1f;
        public static bool RopeClimbMod;
        public static float RopeClimbAmount = 1f;

        // ---- Cheats ----
        public static bool GodMode;
        public static bool InfiniteStamina;
        public static bool NoFallDamage;
        public static bool NoFallingRagdoll;
        public static bool NoWeight;
        public static bool LockStatus;
        public static bool NoStatusEffects;
        public static bool NoInjury;
        public static bool NoHunger;
        public static bool NoCold;
        public static bool NoPoison;
        public static bool NoCurse;
        public static bool NoDrowsy;
        public static bool NoHot;
        public static bool NoCrab;
        public static bool NoThorns;
        public static bool NoSpores;
        public static bool NoWeb;
        public static bool NoArrows;
        public static bool NoPetrify;
        public static bool NoFlyTrap;
        public static bool TeleportToPing;
        public static bool NoSlipperySurfaces;
        public static bool LongInteraction;
        public static float InteractionDistance = 12f;
        public static bool CinematicCamera;
        public static float CinematicCameraSpeed = 8f;
        public static float CinematicCameraFov = 65f;
        public static bool GlobalVoice;
        public static bool PlayerEsp;
        public static float PlayerEspDistance = 1000f;
        public static bool PlayerEspBoxes = true;
        public static bool PlayerEspLines = true;
        public static bool ItemEsp;
        public static float ItemEspDistance = 350f;
        public static bool ItemEspLines = true;
        public static int ItemEspSelectedIndex = -1;
        public static string ItemEspSearch = "";
        public static ConfigEntry<bool> WorldEsp;
        public static ConfigEntry<float> WorldEspDistance;
        public static ConfigEntry<bool> WorldEspLines;
        public static ConfigEntry<bool> WorldEspDangers;
        public static ConfigEntry<bool> WorldEspCampfires;
        public static ConfigEntry<bool> WorldEspBackItems;
        public static ConfigEntry<bool> WorldEspAmulets;

        // ---- Stamina tuning ----
        public static float StaminaConsumptionPercent = 100f;
        public static float StaminaRegenPercent = 100f;
        public static bool StaminaRegenDelayMod;
        public static float StaminaRegenDelay = 1f;
        public static float ClimbStaminaConsumptionPercent = 100f;
        public static float ExtraStaminaPercent = 100f;

        // ---- Status tuning ----
        public static float StatusIncreasePercent = 25f;
        public static float InjuryIncreasePercent = 25f;
        public static float HungerIncreasePercent = 25f;
        public static float ColdIncreasePercent = 25f;
        public static float PoisonIncreasePercent = 25f;
        public static float CrabIncreasePercent = 25f;
        public static float CurseIncreasePercent = 25f;
        public static float DrowsyIncreasePercent = 25f;
        public static float HotIncreasePercent = 25f;
        public static float ThornsIncreasePercent = 25f;
        public static float SporesIncreasePercent = 25f;
        public static float WebIncreasePercent = 25f;
        public static float ArrowsIncreasePercent = 25f;
        public static float PetrifyIncreasePercent = 25f;
        public static float FlyTrapIncreasePercent = 25f;

        // ---- World modifiers ----
        public static bool GameSpeedMod;
        public static float GameSpeed = 1f;
        public static float PingHandSizeMultiplier = 1f;

        // ---- Host admin protection ----
        public static ConfigEntry<bool> AdminProtectionEnabledEntry;
        public static ConfigEntry<bool> AdminWarnOnlyEntry;
        public static ConfigEntry<bool> AdminDetectExtremeMovementEntry;
        public static ConfigEntry<bool> AdminIgnoreDownwardMovementEntry;
        public static ConfigEntry<bool> AdminDetectRareItemBurstsEntry;
        public static ConfigEntry<bool> AdminAutoKickSessionBansEntry;
        public static ConfigEntry<float> AdminMaxSpeedEntry;
        public static ConfigEntry<int> AdminSpeedStrikesEntry;
        private static bool _adminProtectionEnabled;
        private static bool _adminWarnOnly = true;
        private static bool _adminDetectExtremeMovement = true;
        private static bool _adminIgnoreDownwardMovement = true;
        private static bool _adminDetectRareItemBursts = true;
        private static bool _adminAutoKickSessionBans = true;
        private static float _adminMaxSpeed = 25f;
        private static int _adminSpeedStrikes = 3;

        public static bool AntiCheatEnabled
        {
            get => AntiCheatEnabledEntry != null ? AntiCheatEnabledEntry.Value : false;
            set { if (AntiCheatEnabledEntry != null) AntiCheatEnabledEntry.Value = value; }
        }

        public static bool AdminProtectionEnabled
        {
            get => AdminProtectionEnabledEntry != null ? AdminProtectionEnabledEntry.Value : _adminProtectionEnabled;
            set { _adminProtectionEnabled = value; if (AdminProtectionEnabledEntry != null) AdminProtectionEnabledEntry.Value = value; }
        }

        public static bool AdminWarnOnly
        {
            get => AdminWarnOnlyEntry != null ? AdminWarnOnlyEntry.Value : _adminWarnOnly;
            set { _adminWarnOnly = value; if (AdminWarnOnlyEntry != null) AdminWarnOnlyEntry.Value = value; }
        }

        public static bool AdminDetectExtremeMovement
        {
            get => AdminDetectExtremeMovementEntry != null ? AdminDetectExtremeMovementEntry.Value : _adminDetectExtremeMovement;
            set { _adminDetectExtremeMovement = value; if (AdminDetectExtremeMovementEntry != null) AdminDetectExtremeMovementEntry.Value = value; }
        }

        public static bool AdminIgnoreDownwardMovement
        {
            get => AdminIgnoreDownwardMovementEntry != null ? AdminIgnoreDownwardMovementEntry.Value : _adminIgnoreDownwardMovement;
            set { _adminIgnoreDownwardMovement = value; if (AdminIgnoreDownwardMovementEntry != null) AdminIgnoreDownwardMovementEntry.Value = value; }
        }

        public static bool AdminDetectRareItemBursts
        {
            get => AdminDetectRareItemBurstsEntry != null ? AdminDetectRareItemBurstsEntry.Value : _adminDetectRareItemBursts;
            set { _adminDetectRareItemBursts = value; if (AdminDetectRareItemBurstsEntry != null) AdminDetectRareItemBurstsEntry.Value = value; }
        }

        public static bool AdminAutoKickSessionBans
        {
            get => AdminAutoKickSessionBansEntry != null ? AdminAutoKickSessionBansEntry.Value : _adminAutoKickSessionBans;
            set { _adminAutoKickSessionBans = value; if (AdminAutoKickSessionBansEntry != null) AdminAutoKickSessionBansEntry.Value = value; }
        }

        public static float AdminMaxSpeed
        {
            get => AdminMaxSpeedEntry != null ? AdminMaxSpeedEntry.Value : _adminMaxSpeed;
            set { _adminMaxSpeed = value; if (AdminMaxSpeedEntry != null) AdminMaxSpeedEntry.Value = value; }
        }

        public static int AdminSpeedStrikes
        {
            get => AdminSpeedStrikesEntry != null ? AdminSpeedStrikesEntry.Value : _adminSpeedStrikes;
            set { _adminSpeedStrikes = value; if (AdminSpeedStrikesEntry != null) AdminSpeedStrikesEntry.Value = value; }
        }

        // ---- Fly ----
        public static bool Fly;
        public static bool Noclip;
        public static float FlySpeed = 10f;
        public static float FlyAcceleration = 30f;

        // ---- Inventory ----
        public static float RechargeValue = 100f;
        public static bool InfiniteItems;
        public static bool UnlimitedLanternFuel;
        public static float LanternFuelConsumptionPercent = 100f;
        public static bool UnlimitedItemUses;

        // ---- UI ----
        public static ConfigEntry<float> UiScale;
        public static ConfigEntry<bool> AdvancedUi;
        public static ConfigEntry<float> WindowX;
        public static ConfigEntry<float> WindowY;
        public static ConfigEntry<float> WindowWidth;
        public static ConfigEntry<float> WindowHeight;
        public static ConfigEntry<string> AccentColorHex;
        public static ConfigEntry<string> ActionColorHex;
        public static ConfigEntry<string> FavoriteItemIds;
        public static ConfigEntry<string> FavoriteActionIds;
        public static bool RapidRandomOutfitColor;
        public static float RapidRandomOutfitColorInterval = 0.15f;

        // ---- World / Time ----
        public static bool OverrideExpeditionTime;
        public static float ExpeditionTimeSeconds;

        public static void Init(ConfigFile cfg)
        {
            MenuToggleKey = cfg.Bind("General", "MenuToggleKey", KeyCode.Insert,
                "Key to toggle the PEAK-MX overlay.");
            ShowDonateNotice = cfg.Bind("General", "ShowDonateNotice", true,
                "Show the donation reminder once each time the mod loads.");
            ManyPlayersEnabled = cfg.Bind("ManyPlayers", "Enabled", false,
                "Allow rooms above the base game player limit.");
            ManyPlayersMaxPlayers = cfg.Bind("ManyPlayers", "MaxPlayers", 20,
                new ConfigDescription("Maximum lobby size.",
                    new AcceptableValueRange<int>(1, 30)));
            ManyPlayersHostOnlyKiosk = cfg.Bind("ManyPlayers", "HostOnlyKiosk", true,
                "Only the room host can start the expedition from the airport kiosk.");
            ManyPlayersLobbyDetails = cfg.Bind("ManyPlayers", "LobbyDetails", true,
                "Write extra join/leave details to the PEAK-MX admin log.");
            ManyPlayersVoiceFix = cfg.Bind("ManyPlayers", "VoiceFix", true,
                "Reuse voice mixer groups safely when more than four players are in a room.");
            ManyPlayersUiFix = cfg.Bind("ManyPlayers", "UiFix", true,
                "Expand simple lobby/name UI arrays for larger rooms where possible.");
            ManyPlayersPluginAudit = cfg.Bind("ManyPlayers", "PluginAudit", false,
                "Publish and compare a local hash of BepInEx plugin DLLs. Everyone needs PEAK-MX for this check.");
            ManyPlayersPluginAuditKick = cfg.Bind("ManyPlayers", "PluginAuditKick", false,
                "Host kicks players whose PEAK-MX plugin hash differs from the host.");

            AntiCheatEnabledEntry = cfg.Bind("AntiCheat", "Enabled", false,
                "Host master switch for movement, rare-item and session protection. MX-AHG validation is an optional strict mode.");
            AntiCheatLockClients = cfg.Bind("AntiCheat", "LockClientTools", true,
                "When host anti-cheat is enabled, non-host PEAK-MX clients can only use About and Anti-cheat tabs.");
            AntiCheatKickMissingData = cfg.Bind("AntiCheat", "KickMissingDllData", true,
                "Legacy setting retained for config compatibility. MX-AHG KickMissingAgent is used instead.");
            MxAhgRequireAgent = cfg.Bind("MX-AHG", "RequireAgent", false,
                "Require remote players to run the standalone MX-AHG agent.");
            MxAhgExactPack = cfg.Bind("MX-AHG", "ExactHostPack", true,
                "Compare client DLLs, loaded plugins and Harmony patch owners against the host set.");
            MxAhgKickMissing = cfg.Bind("MX-AHG", "KickMissingAgent", true,
                "Kick players without a responding MX-AHG agent when RequireAgent is enabled.");
            MxAhgKickMismatch = cfg.Bind("MX-AHG", "KickPackMismatch", true,
                "Kick MX-AHG clients whose mod set differs from the host when ExactHostPack is enabled.");
            MxAhgKickSuspicious = cfg.Bind("MX-AHG", "KickSuspiciousNames", false,
                "Kick clients whose reported plugin data contains a denied keyword. Disabled by default to avoid false positives.");
            MxAhgKickStale = cfg.Bind("MX-AHG", "KickStaleHeartbeat", false,
                "Kick an MX-AHG client whose heartbeat stops for more than 20 seconds.");
            MxAhgGraceSeconds = cfg.Bind("MX-AHG", "JoinGraceSeconds", 18f,
                new ConfigDescription("Time allowed for the agent to answer after a player joins.",
                    new AcceptableValueRange<float>(8f, 90f)));
            MxAhgRecheckSeconds = cfg.Bind("MX-AHG", "RecheckSeconds", 60f,
                new ConfigDescription("How often the host requests a fresh full report.",
                    new AcceptableValueRange<float>(20f, 300f)));
            MxAhgDeniedKeywords = cfg.Bind("MX-AHG", "DeniedKeywords", DefaultMxAhgDeniedKeywords,
                "Semicolon-separated words that mark a DLL, plugin or Harmony owner as suspicious.");

            AdminProtectionEnabledEntry = cfg.Bind("AntiCheat", "AdminProtection", false,
                "Enable host-side checks for suspicious behavior from other players.");
            AdminWarnOnlyEntry = cfg.Bind("AntiCheat", "WarnOnly", true,
                "Only write suspicious events to the admin log without automatic kicks.");
            AdminDetectExtremeMovementEntry = cfg.Bind("AntiCheat", "DetectExtremeMovement", true,
                "Detect suspiciously large remote movement jumps.");
            AdminIgnoreDownwardMovementEntry = cfg.Bind("AntiCheat", "IgnoreDownwardMovement", true,
                "When detecting movement cheats, ignore pure downward falling movement so the threshold can be stricter.");
            AdminDetectRareItemBurstsEntry = cfg.Bind("AntiCheat", "DetectRareItemBursts", true,
                "Detect suspicious bursts of cursed items and amulets appearing in player inventories.");
            AdminAutoKickSessionBansEntry = cfg.Bind("AntiCheat", "AutoKickSessionBans", true,
                "Keep session bans enforced while the banned player remains or rejoins.");
            AdminMaxSpeedEntry = cfg.Bind("AntiCheat", "MaxSpeed", 25f,
                new ConfigDescription("Movement speed threshold for admin protection.",
                    new AcceptableValueRange<float>(5f, 300f)));
            AdminSpeedStrikesEntry = cfg.Bind("AntiCheat", "SpeedStrikes", 3,
                new ConfigDescription("Number of speed detections before kicking when WarnOnly is false.",
                    new AcceptableValueRange<int>(1, 10)));

            QuickActionKeys = new ConfigEntry<KeyCode>[QuickActionSlotCount];
            QuickActionIds = new ConfigEntry<string>[QuickActionSlotCount];
            for (int i = 0; i < QuickActionSlotCount; i++)
            {
                KeyCode defaultKey = i switch
                {
                    0 => KeyCode.F6,
                    1 => KeyCode.F7,
                    2 => KeyCode.F8,
                    _ => KeyCode.None,
                };
                string defaultAction = i switch
                {
                    0 => "toggle_fly",
                    1 => "full_stamina",
                    2 => "anti_stuck",
                    _ => "none",
                };

                int slot = i + 1;
                QuickActionKeys[i] = cfg.Bind("QuickActions", $"Slot{slot}Key", defaultKey,
                    "Keyboard shortcut for this quick action slot. Set to None to disable.");
                QuickActionIds[i] = cfg.Bind("QuickActions", $"Slot{slot}Action", defaultAction,
                    "Action id executed by this quick action slot.");
            }


            UiScale = cfg.Bind("UI", "Scale", 1f,
                new ConfigDescription("Menu size multiplier (0.6–2.0).",
                    new AcceptableValueRange<float>(0.6f, 2f)));
            AdvancedUi = cfg.Bind("UI", "AdvancedMode", false,
                "Show precise slots, tuning controls and uncommon actions. Simple mode displays one task group at a time.");
            WindowX = cfg.Bind("UI", "WindowX", 80f, "Saved horizontal menu position in scaled screen coordinates.");
            WindowY = cfg.Bind("UI", "WindowY", 80f, "Saved vertical menu position in scaled screen coordinates.");
            WindowWidth = cfg.Bind("UI", "WindowWidth", 680f, "Saved menu width in scaled screen coordinates.");
            WindowHeight = cfg.Bind("UI", "WindowHeight", 580f, "Saved menu height in scaled screen coordinates.");
            AccentColorHex = cfg.Bind("UI", "AccentColor", "#41D58A",
                "Main menu accent color in #RRGGBB format.");
            ActionColorHex = cfg.Bind("UI", "ActionColor", "#599FF4",
                "Primary action button color in #RRGGBB format.");
            FavoriteItemIds = cfg.Bind("Inventory", "FavoriteItemIds", "",
                "Comma-separated PEAK item ids marked as favorites in the inventory UI.");
            FavoriteActionIds = cfg.Bind("UI", "FavoriteActions", "full_stamina;anti_stuck;toggle_fly",
                "Semicolon-separated quick action ids pinned to the Favorites page.");
            Language = cfg.Bind("UI", "Language", 1,
                new ConfigDescription("0=English, 1=Russian, 2=Ukrainian, 3=zh-CN, 4=zh-TW, 5=ja, " +
                    "6=ko, 7=es, 8=pt-BR, 9=de, 10=fr, 11=it, 12=pl, 13=tr",
                    new AcceptableValueRange<int>(0, 13)));

            WorldEsp = cfg.Bind("ESP", "WorldEsp", false,
                "Show danger/campfire/back-item/amulet markers through walls.");
            WorldEspDistance = cfg.Bind("ESP", "WorldEspDistance", 600f,
                new ConfigDescription("Maximum distance for world ESP markers.",
                    new AcceptableValueRange<float>(25f, 2500f)));
            WorldEspLines = cfg.Bind("ESP", "WorldEspLines", true,
                "Draw lines from the screen bottom to world ESP markers.");
            WorldEspDangers = cfg.Bind("ESP", "WorldEspDangers", true,
                "Include hazard-like objects in world ESP.");
            WorldEspCampfires = cfg.Bind("ESP", "WorldEspCampfires", true,
                "Include campfires in world ESP.");
            WorldEspBackItems = cfg.Bind("ESP", "WorldEspBackItems", true,
                "Include backpacks, fannypacks, jetpacks and rocketpacks in world ESP.");
            WorldEspAmulets = cfg.Bind("ESP", "WorldEspAmulets", true,
                "Include amulets and charm-like items in world ESP.");
        }
    }
}
