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
        public static bool TeleportToPing;
        public static bool NoSlipperySurfaces;
        public static bool LongInteraction;
        public static float InteractionDistance = 12f;
        public static bool CinematicCamera;
        public static float CinematicCameraSpeed = 8f;
        public static float CinematicCameraFov = 65f;

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
        public static float CurseIncreasePercent = 25f;
        public static float DrowsyIncreasePercent = 25f;
        public static float HotIncreasePercent = 25f;

        // ---- World modifiers ----
        public static bool GameSpeedMod;
        public static float GameSpeed = 1f;
        public static float PingHandSizeMultiplier = 1f;

        // ---- Host admin protection ----
        public static bool AdminProtectionEnabled;
        public static bool AdminWarnOnly = true;
        public static bool AdminDetectExtremeMovement = true;
        public static bool AdminAutoKickSessionBans = true;
        public static float AdminMaxSpeed = 80f;
        public static int AdminSpeedStrikes = 3;

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

        // ---- World / Time ----
        public static bool OverrideExpeditionTime;
        public static float ExpeditionTimeSeconds;

        public static void Init(ConfigFile cfg)
        {
            MenuToggleKey = cfg.Bind("General", "MenuToggleKey", KeyCode.Insert,
                "Key to toggle the PEAK-MX overlay.");
            ShowDonateNotice = cfg.Bind("General", "ShowDonateNotice", true,
                "Show the donation reminder once each time the mod loads.");

            AllowAnonymousStats = cfg.Bind("Telemetry", "AllowAnonymousStats", true,
                "Send usage statistics so the author can show an install counter and improve the mod. " +
                "Set to false to disable completely.");
            InstallId = cfg.Bind("Telemetry", "InstallId", "",
                "Identifier for this install, generated locally.");
            InstallReported = cfg.Bind("Telemetry", "InstallReported", false,
                "Whether this install has already been counted once.");
            AllowAnonymousStats.Value = true;
            UiScale = cfg.Bind("UI", "Scale", 1f,
                new ConfigDescription("Menu size multiplier (0.6–2.0).",
                    new AcceptableValueRange<float>(0.6f, 2f)));
            Language = cfg.Bind("UI", "Language", 1,
                new ConfigDescription("0=English, 1=Russian, 2=Ukrainian, 3=zh-CN, 4=zh-TW, 5=ja, " +
                    "6=ko, 7=es, 8=pt-BR, 9=de, 10=fr, 11=it, 12=pl, 13=tr",
                    new AcceptableValueRange<int>(0, 13)));
        }
    }
}
