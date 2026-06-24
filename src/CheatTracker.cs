using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace PeakMX
{
    /// <summary>
    /// Tracks which cheats get toggled and for how long, and reports anonymous usage events.
    /// One event with no value = an activation (a "use"); one event with a value = duration in
    /// seconds on deactivation. Gated by AllowAnonymousStats.
    /// </summary>
    public static class CheatTracker
    {
        private const string EventUrl = "https://peak-mx.rkngov.com/api/event";

        private static readonly Dictionary<string, bool> _prev = new Dictionary<string, bool>();
        private static readonly Dictionary<string, float> _since = new Dictionary<string, float>();
        private static (string name, Func<bool> get)[] _cheats;

        public static void Tick()
        {
            if (!ModConfig.AllowAnonymousStats.Value) return;
            if (_cheats == null) Build();

            foreach (var (name, get) in _cheats)
            {
                bool cur = get();
                bool prev = _prev.TryGetValue(name, out var p) && p;
                if (cur == prev) continue;
                _prev[name] = cur;

                if (cur)
                {
                    _since[name] = Time.realtimeSinceStartup;
                    Send(name, null);                                  // activation = a use
                }
                else
                {
                    float dur = _since.TryGetValue(name, out var t0) ? Time.realtimeSinceStartup - t0 : 0f;
                    Send(name, Math.Max(0, (int)dur));                 // duration in seconds
                }
            }
        }

        private static void Build()
        {
            _cheats = new (string, Func<bool>)[]
            {
                ("speed",        () => ModConfig.SpeedMod),
                ("jump",         () => ModConfig.JumpMod),
                ("infinitejumps", () => ModConfig.InfiniteJumps),
                ("climb",        () => ModConfig.ClimbMod),
                ("vineclimb",    () => ModConfig.VineClimbMod),
                ("ropeclimb",    () => ModConfig.RopeClimbMod),
                ("god",          () => ModConfig.GodMode),
                ("infstamina",   () => ModConfig.InfiniteStamina),
                ("nofall",       () => ModConfig.NoFallDamage),
                ("nofallingragdoll", () => ModConfig.NoFallingRagdoll),
                ("noweight",     () => ModConfig.NoWeight),
                ("lockstatus",   () => ModConfig.LockStatus),
                ("nostatus",     () => ModConfig.NoStatusEffects),
                ("noinjury",      () => ModConfig.NoInjury),
                ("nohunger",      () => ModConfig.NoHunger),
                ("nocold",        () => ModConfig.NoCold),
                ("nopoison",      () => ModConfig.NoPoison),
                ("nocurse",       () => ModConfig.NoCurse),
                ("nodrowsy",      () => ModConfig.NoDrowsy),
                ("nohot",         () => ModConfig.NoHot),
                ("staminaregendelay", () => ModConfig.StaminaRegenDelayMod),
                ("longinteract",  () => ModConfig.LongInteraction),
                ("cinematiccam",  () => ModConfig.CinematicCamera),
                ("noslippery",    () => ModConfig.NoSlipperySurfaces),
                ("gamespeed",     () => ModConfig.GameSpeedMod),
                ("unlimitedlantern", () => ModConfig.UnlimitedLanternFuel),
                ("unlimiteditems", () => ModConfig.UnlimitedItemUses),
                ("infiniteitems", () => ModConfig.InfiniteItems),
                ("teleportping", () => ModConfig.TeleportToPing),
                ("fly",          () => ModConfig.Fly),
            };
        }

        private static void Send(string name, int? value)
        {
            string id = ModConfig.InstallId.Value;
            if (string.IsNullOrEmpty(id)) return;

            string url = $"{EventUrl}?id={Uri.EscapeDataString(id)}&type=cheat"
                       + $"&name={Uri.EscapeDataString(name)}"
                       + $"&t={Uri.EscapeDataString(TelemetryToken.Value)}";
            if (value.HasValue) url += $"&value={value.Value}";

            Task.Run(() =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using var c = new WebClient();
                    c.DownloadString(url);
                }
                catch { }
            });
        }
    }
}
