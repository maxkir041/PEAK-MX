using BepInEx.Configuration;
using UnityEngine;

namespace PeakMX
{
    /// <summary>
    /// Central store for every toggle/value in PEAK-MX.
    /// Persistent options are backed by BepInEx config; pure runtime flags live as plain fields.
    /// </summary>
    public static class ModConfig
    {
        // ---- General ----
        public static ConfigEntry<KeyCode> MenuToggleKey;
        public static ConfigEntry<int> Language;
        public static ConfigEntry<bool> ShowDonateNotice;

        // ---- Anonymous install counter (telemetry) ----
        public static ConfigEntry<bool> AllowAnonymousStats;
        public static ConfigEntry<string> InstallId;
        public static ConfigEntry<bool> InstallReported;

        // ---- Movement ----
        public static bool SpeedMod;
        public static float SpeedAmount = 1f;
        public static bool JumpMod;
        public static float JumpAmount = 1.5f;
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
        public static bool NoWeight;
        public static bool LockStatus;
        public static bool TeleportToPing;

        // ---- Fly ----
        public static bool Fly;
        public static float FlySpeed = 10f;
        public static float FlyAcceleration = 30f;

        // ---- Inventory ----
        public static float RechargeSlot1 = 100f;
        public static float RechargeSlot2 = 100f;
        public static float RechargeSlot3 = 100f;

        // ---- World / Time (implemented in a later pass) ----
        public static bool OverrideExpeditionTime;
        public static float ExpeditionTimeSeconds;

        public static void Init(ConfigFile cfg)
        {
            MenuToggleKey = cfg.Bind("General", "MenuToggleKey", KeyCode.Insert,
                "Key to toggle the PEAK-MX overlay.");
            ShowDonateNotice = cfg.Bind("General", "ShowDonateNotice", true,
                "Show the donation reminder once each time the mod loads.");

            AllowAnonymousStats = cfg.Bind("Telemetry", "AllowAnonymousStats", true,
                "Send a single anonymous install ping (random ID only, no personal data) so the " +
                "author can show an install counter. Set to false to disable completely.");
            InstallId = cfg.Bind("Telemetry", "InstallId", "",
                "Random anonymous identifier for this install. Generated locally; contains no personal data.");
            InstallReported = cfg.Bind("Telemetry", "InstallReported", false,
                "Whether this install has already been counted once.");
            Language = cfg.Bind("UI", "Language", 1,
                new ConfigDescription("0=English, 1=Russian, 2=Ukrainian, 3=zh-CN, 4=zh-TW, 5=ja, " +
                    "6=ko, 7=es, 8=pt-BR, 9=de, 10=fr, 11=it, 12=pl, 13=tr",
                    new AcceptableValueRange<int>(0, 13)));
        }
    }
}
