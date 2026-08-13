using System;
using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace PeakMX
{
    /// <summary>Local free-flight patch.</summary>
    [HarmonyPatch(typeof(Character), "Update")]
    public static class FlyPatch
    {
        private static bool _flying;
        private static Vector3 _vel = Vector3.zero;
        private static readonly List<Collider> _noclipColliders = new List<Collider>();
        private static readonly List<Rigidbody> _noclipBodies = new List<Rigidbody>();
        private static CharacterController _noclipController;
        private static bool _noclipApplied;

        private static void Postfix(Character __instance)
        {
            try
            {
                if (__instance == null || !__instance.IsLocal) return;

                if (!ModConfig.Fly)
                {
                    if (_flying) { _flying = false; _vel = Vector3.zero; }
                    RestoreColliders();
                    return;
                }
                if (!_flying) { _flying = true; _vel = Vector3.zero; }

                // Keep the character "grounded" so the game doesn't fight the fly velocity.
                __instance.data.isGrounded = true;
                __instance.data.sinceGrounded = 0f;
                __instance.data.sinceJump = 0f;

                Vector3 input = __instance.input.movementInput;          // Vector2 -> Vector3 (implicit)
                Vector3 fwd = __instance.data.lookDirection_Flat.normalized;
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
                Vector3 dir = fwd * input.y + right * input.x;
                if (__instance.input.jumpIsPressed) dir += Vector3.up;
                if (__instance.input.crouchIsPressed) dir += Vector3.down;

                float speed = ModConfig.FlySpeed;
                float accel = ModConfig.FlyAcceleration;
                _vel = Vector3.Lerp(_vel, dir.normalized * speed, Time.deltaTime * accel);

                List<Bodypart> parts = __instance.refs.ragdoll.partList;
                for (int i = 0; i < parts.Count; i++)
                {
                    var rig = parts[i] != null ? parts[i].Rig : null;
                    if (rig != null) rig.linearVelocity = _vel;
                }

                if (ModConfig.Noclip) ApplyNoclip(__instance);
                else RestoreColliders();
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[FlyPatch] {e.Message}"); }
        }

        private static void ApplyNoclip(Character c)
        {
            if (!_noclipApplied)
            {
                _noclipApplied = true;
                _noclipColliders.Clear();
                _noclipBodies.Clear();
                _noclipController = ((Component)c).GetComponent<CharacterController>();
            }

            DisableColliders(((Component)c).GetComponentsInChildren<Collider>(true));
            DisableBodies(((Component)c).GetComponentsInChildren<Rigidbody>(true));

            var parts = c.refs?.ragdoll?.partList;
            if (parts != null)
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    var part = parts[i];
                    if (part == null) continue;
                    if (part.Rig != null)
                    {
                        TrackBody(part.Rig);
                        part.Rig.detectCollisions = false;
                    }
                    DisableColliders(((Component)part).GetComponentsInChildren<Collider>(true));
                }
            }

            if (_noclipController != null)
                _noclipController.enabled = false;
        }

        private static void DisableColliders(Collider[] colliders)
        {
            if (colliders == null) return;
            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (col == null) continue;
                if (!_noclipColliders.Contains(col))
                    _noclipColliders.Add(col);
                col.enabled = false;
            }
        }

        private static void DisableBodies(Rigidbody[] bodies)
        {
            if (bodies == null) return;
            for (int i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i];
                if (body == null) continue;
                TrackBody(body);
                body.detectCollisions = false;
            }
        }

        private static void TrackBody(Rigidbody body)
        {
            if (body != null && !_noclipBodies.Contains(body))
                _noclipBodies.Add(body);
        }

        private static void RestoreColliders()
        {
            if (!_noclipApplied) return;
            _noclipApplied = false;
            for (int i = 0; i < _noclipColliders.Count; i++)
            {
                var col = _noclipColliders[i];
                if (col != null) col.enabled = true;
            }
            for (int i = 0; i < _noclipBodies.Count; i++)
            {
                var body = _noclipBodies[i];
                if (body != null) body.detectCollisions = true;
            }
            if (_noclipController != null)
                _noclipController.enabled = true;
            _noclipColliders.Clear();
            _noclipBodies.Clear();
            _noclipController = null;
        }
    }

    /// <summary>Teleports the local player to their own ping.</summary>
    [HarmonyPatch(typeof(PointPinger), "ReceivePoint_Rpc")]
    public static class PointPingPatch
    {
        private static void Postfix(Vector3 point, Vector3 hitNormal, PointPinger __instance)
        {
            try
            {
                if (!ModConfig.TeleportToPing) return;
                var pinger = __instance != null ? __instance.character : null;
                if (pinger == null) return;
                var owner = ((MonoBehaviourPun)pinger).photonView?.Owner;
                if (owner == null || owner != PhotonNetwork.LocalPlayer) return;

                var me = Character.localCharacter;
                if (me == null || me.data.dead) return;
                Vector3 dest = point + Vector3.up;
                ((MonoBehaviourPun)me).photonView.RPC("WarpPlayerRPC", RpcTarget.All, new object[] { dest, true });
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[PointPingPatch] {e.Message}"); }
        }
    }

    [HarmonyPatch(typeof(RunManager), "Update")]
    public static class RunTimerHoldPatch
    {
        private static void Postfix(RunManager __instance)
        {
            try
            {
                if (__instance == null || !ModConfig.OverrideExpeditionTime)
                    return;
                if (ModConfig.ExpeditionTimeSeconds <= 0.01f)
                    ModConfig.ExpeditionTimeSeconds = GameApi.RunTimeSeconds(__instance);
                GameApi.SetRunTimeLocal(__instance, ModConfig.ExpeditionTimeSeconds);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[RunTimerHoldPatch] {e.Message}"); }
        }
    }

    [HarmonyPatch(typeof(CustomizationOption), "get_IsLocked")]
    public static class CosmeticUnlockPatch
    {
        private static void Postfix(CustomizationOption __instance, ref bool __result)
        {
            try
            {
                if (GameApi.IsCosmeticLocallyUnlocked(__instance))
                    __result = false;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[CosmeticUnlockPatch] {e.Message}"); }
        }
    }

    [HarmonyPatch(typeof(Character), "UseStamina")]
    public static class StaminaUsePatch
    {
        private static bool Prefix(Character __instance, ref float usage, ref bool __result)
        {
            try
            {
                if (__instance == null || !__instance.IsLocal)
                    return true;

                if (ModConfig.InfiniteStamina || usage <= 0f)
                {
                    __result = true;
                    return false;
                }

                bool climbing = __instance.data != null &&
                    (__instance.data.isClimbing || __instance.data.isRopeClimbing || __instance.data.isVineClimbing || __instance.data.currentClimbHandle != null);
                float percent = climbing ? ModConfig.ClimbStaminaConsumptionPercent : ModConfig.StaminaConsumptionPercent;
                usage *= Mathf.Clamp(percent, 0f, 500f) / 100f;
                if (usage <= 0f)
                {
                    __result = true;
                    return false;
                }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[StaminaUsePatch] {e.Message}"); }
            return true;
        }
    }

    [HarmonyPatch(typeof(Character), "AddStamina")]
    public static class StaminaRegenPatch
    {
        private static void Prefix(Character __instance, ref float add)
        {
            try
            {
                if (__instance == null || !__instance.IsLocal || add <= 0f)
                    return;
                add *= Mathf.Clamp(ModConfig.StaminaRegenPercent, 0f, 500f) / 100f;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[StaminaRegenPatch] {e.Message}"); }
        }
    }

    [HarmonyPatch(typeof(Character), "CanRegenStamina")]
    public static class StaminaRegenDelayPatch
    {
        private static void Postfix(Character __instance, ref bool __result)
        {
            try
            {
                if (!ModConfig.StaminaRegenDelayMod || __instance == null || !__instance.IsLocal || __instance.data == null)
                    return;
                if (__instance.data.currentClimbHandle != null || __instance.IsStuck())
                    return;
                if (__instance.data.sinceUseStamina < Mathf.Clamp(ModConfig.StaminaRegenDelay, 0f, 10f))
                    __result = false;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[StaminaRegenDelayPatch] {e.Message}"); }
        }
    }

    [HarmonyPatch(typeof(Character), "Fall")]
    public static class NoFallingRagdollPatch
    {
        private static bool Prefix(Character __instance)
        {
            try
            {
                if (ModConfig.NoFallingRagdoll && __instance != null && __instance.IsLocal)
                {
                    if (__instance.data != null) __instance.data.fallSeconds = 0f;
                    return false;
                }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[NoFallingRagdollPatch] {e.Message}"); }
            return true;
        }
    }

    [HarmonyPatch(typeof(CharacterMovement), "CheckFallDamage")]
    public static class NoFallDamagePatch
    {
        private static bool Prefix(Character ___character)
        {
            try
            {
                if (ModConfig.NoFallDamage && ___character != null && ___character.IsLocal)
                {
                    if (___character.data != null) ___character.data.fallSeconds = 0f;
                    return false;
                }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[NoFallDamagePatch] {e.Message}"); }
            return true;
        }
    }

    [HarmonyPatch(typeof(CharacterMovement), "AcceptableAngle")]
    public static class NoSlipperySurfacesPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (ModConfig.NoSlipperySurfaces)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(Lantern), "UpdateFuel")]
    public static class LanternFuelPatch
    {
        private static readonly AccessTools.FieldRef<Lantern, float> FuelRef =
            AccessTools.FieldRefAccess<Lantern, float>("fuel");

        private static bool Prefix(Lantern __instance)
        {
            try
            {
                if (!ModConfig.UnlimitedLanternFuel || __instance == null)
                    return true;
                SetFuel(__instance, Mathf.Max(FuelRef(__instance), __instance.startingFuel));
                return false;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[LanternFuelPatch:Prefix] {e.Message}"); return true; }
        }

        private static void Postfix(Lantern __instance)
        {
            try
            {
                if (__instance == null || ModConfig.UnlimitedLanternFuel)
                    return;
                float percent = Mathf.Clamp(ModConfig.LanternFuelConsumptionPercent, 0f, 500f);
                if (Mathf.Abs(percent - 100f) < 0.01f)
                    return;
                float fuel = FuelRef(__instance);
                fuel += Time.deltaTime * (1f - percent / 100f);
                SetFuel(__instance, Mathf.Clamp(fuel, 0f, Mathf.Max(0.01f, __instance.startingFuel)));
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[LanternFuelPatch:Postfix] {e.Message}"); }
        }

        private static void SetFuel(Lantern lantern, float fuel)
        {
            FuelRef(lantern) = fuel;
            try { lantern.GetData<FloatItemData>(DataEntryKey.Fuel).Value = fuel; } catch { }
            try
            {
                var item = ((ItemComponent)lantern).item;
                if (item != null)
                    item.SetUseRemainingPercentage(fuel / Mathf.Max(0.01f, lantern.startingFuel));
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(Item), "CanUsePrimary")]
    public static class UnlimitedItemUsePrimaryPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (ModConfig.UnlimitedItemUses)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(Item), "CanUseSecondary")]
    public static class UnlimitedItemUseSecondaryPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (ModConfig.UnlimitedItemUses)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(Action_ReduceUses), "RunAction")]
    public static class UnlimitedItemReduceUsesPatch
    {
        private static bool Prefix()
        {
            return !ModConfig.UnlimitedItemUses;
        }
    }
}
