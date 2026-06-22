using System;
using HarmonyLib;
using UnityEngine;

namespace PeakMX
{
    /// <summary>
    /// Applies the active toggles to the local character every frame.
    /// Everything here targets the player's own character only.
    /// </summary>
    public static class Features
    {
        // Reflection accessor for the internal CharacterData.isInvincible flag.
        private static readonly AccessTools.FieldRef<CharacterData, bool> InvincibleRef =
            AccessTools.FieldRefAccess<CharacterData, bool>("isInvincible");

        private static float _baseJumpImpulse = float.NaN;
        private static float[] _lockedStatuses;
        private static bool _godApplied, _speedApplied, _jumpApplied;

        public static void Tick()
        {
            var c = Character.localCharacter;
            if (c == null || c.data == null)
                return;

            try { ApplyMovement(c); } catch (Exception e) { Warn("movement", e); }
            try { ApplyCheats(c); } catch (Exception e) { Warn("cheats", e); }
        }

        private static void ApplyMovement(Character c)
        {
            var move = c.refs?.movement;
            if (move == null)
                return;

            // Speed — reset to the game's neutral modifier (1) when turned off.
            if (ModConfig.SpeedMod)
            {
                move.movementModifier = ModConfig.SpeedAmount;
                _speedApplied = true;
            }
            else if (_speedApplied)
            {
                move.movementModifier = 1f;
                _speedApplied = false;
            }

            // Jump — capture the natural impulse on first enable, restore it on disable.
            if (ModConfig.JumpMod)
            {
                if (float.IsNaN(_baseJumpImpulse) || _baseJumpImpulse <= 0f)
                    _baseJumpImpulse = move.jumpImpulse > 0f ? move.jumpImpulse : 4f;
                move.jumpImpulse = _baseJumpImpulse * ModConfig.JumpAmount;
                _jumpApplied = true;
            }
            else if (_jumpApplied)
            {
                if (!float.IsNaN(_baseJumpImpulse) && _baseJumpImpulse > 0f)
                    move.jumpImpulse = _baseJumpImpulse;
                _jumpApplied = false;
            }
        }

        private static void ApplyCheats(Character c)
        {
            var data = c.data;
            var affl = c.refs != null ? c.refs.afflictions : null;

            // God mode — force invincibility while on; on disable, restore the game's
            // natural value (otherwise the forced flag stays stuck on).
            if (ModConfig.GodMode)
            {
                InvincibleRef(data) = true;
                _godApplied = true;
            }
            else if (_godApplied)
            {
                InvincibleRef(data) = false;
                data.RecalculateInvincibility();
                _godApplied = false;
            }

            if (ModConfig.InfiniteStamina)
            {
                data.currentStamina = 1f;
                data.extraStamina = Mathf.Max(data.extraStamina, 0f);
            }

            if (ModConfig.NoFallDamage)
                data.fallSeconds = 0f;

            if (affl != null)
            {
                if (ModConfig.NoWeight)
                    affl.SetStatus(CharacterAfflictions.STATUSTYPE.Weight, 0f);

                if (ModConfig.LockStatus)
                {
                    if (_lockedStatuses == null && affl.currentStatuses != null)
                        _lockedStatuses = (float[])affl.currentStatuses.Clone();
                    if (_lockedStatuses != null && affl.currentStatuses != null)
                        Array.Copy(_lockedStatuses, affl.currentStatuses,
                            Mathf.Min(_lockedStatuses.Length, affl.currentStatuses.Length));
                }
                else
                {
                    _lockedStatuses = null;
                }
            }
        }

        private static void Warn(string where, Exception e) =>
            Plugin.Log?.LogWarning($"[Features:{where}] {e.Message}");
    }
}
