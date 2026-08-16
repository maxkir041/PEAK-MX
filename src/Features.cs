using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace PeakMX
{
    /// <summary>Per-frame local feature updates.</summary>
    public static class Features
    {
        // Reflection accessor for the internal CharacterData.isInvincible flag.
        private static readonly AccessTools.FieldRef<CharacterData, bool> InvincibleRef =
            AccessTools.FieldRefAccess<CharacterData, bool>("isInvincible");

        // Speed-multiplier fields on the three traversal components (resolve lazily, may be null).
        private static readonly FieldInfo ClimbSpeedF = AccessTools.Field(typeof(CharacterClimbing), "climbSpeedMod");
        private static readonly FieldInfo VineSpeedF = AccessTools.Field(typeof(CharacterVineClimbing), "climbSpeedMod");
        private static readonly FieldInfo RopeSpeedF = AccessTools.Field(typeof(CharacterRopeHandling), "climbSpeedMod");

        private static float _baseJumpImpulse = float.NaN;
        private static float[] _lockedStatuses;
        private static bool _godApplied, _speedApplied, _jumpApplied;
        private static bool _climbApplied, _vineApplied, _ropeApplied;
        private static float _itemTimer, _timeOverrideTimer, _adminTimer, _cosmeticShuffleTimer;
        private static float _baseInteractionDistance = -1f, _baseInteractionArea = -1f;
        private static bool _timeScaleApplied;
        private static readonly Dictionary<PointPing, float> PingBaseFrustumSizes = new Dictionary<PointPing, float>();
        private static GameObject _cinematicObject;
        private static CameraOverride _cinematicOverride;
        private static Vector2 _cinematicLook;

        public static void Tick()
        {
            var c = Character.localCharacter;
            if (c == null || c.data == null)
                return;

            try { ApplyMovement(c); } catch (Exception e) { Warn("movement", e); }
            try { ApplyCheats(c); } catch (Exception e) { Warn("cheats", e); }
            try { ApplyInfiniteItems(); } catch (Exception e) { Warn("items", e); }
            try { ApplyWorldOverrides(); } catch (Exception e) { Warn("world", e); }
            try { ApplyUtilityModifiers(c); } catch (Exception e) { Warn("utility", e); }
            try { ApplyAdminProtection(); } catch (Exception e) { Warn("admin", e); }
            try { ApplyCosmeticShuffle(); } catch (Exception e) { Warn("cosmetics", e); }
            try { GameApi.ApplyFrozenPlayers(Time.deltaTime); } catch (Exception e) { Warn("freeze", e); }
            try { GameApi.ApplyInventoryLocks(Time.deltaTime); } catch (Exception e) { Warn("inventory-lock", e); }
        }

        // Infinite items: periodically top every held item back up to full charge.
        private static void ApplyInfiniteItems()
        {
            if (!ModConfig.InfiniteItems) return;
            _itemTimer += Time.deltaTime;
            if (_itemTimer < 0.5f) return;
            _itemTimer = 0f;
            float v = ModConfig.RechargeValue > 0f ? ModConfig.RechargeValue : 999f;
            int n = GameApi.SlotCount();
            for (int s = 0; s < n; s++) GameApi.RechargeSlot(s, v);
        }

        // Hold the expedition timer near the chosen value while the override is enabled.
        private static void ApplyWorldOverrides()
        {
            if (!ModConfig.OverrideExpeditionTime)
            {
                _timeOverrideTimer = 0f;
                return;
            }

            _timeOverrideTimer += Time.deltaTime;
            if (_timeOverrideTimer < 0.5f) return;
            _timeOverrideTimer = 0f;
            if (ModConfig.ExpeditionTimeSeconds <= 0.01f)
            {
                float current = GameApi.ExpeditionTimeSeconds();
                if (current > 0.01f) ModConfig.ExpeditionTimeSeconds = current;
                else return;
            }
            GameApi.SetExpeditionTime(ModConfig.ExpeditionTimeSeconds);
        }

        private static void ApplyAdminProtection()
        {
            _adminTimer += Time.deltaTime;
            if (_adminTimer < 2f) return;
            _adminTimer = 0f;
            GameApi.EnforceAdminProtection();
        }

        private static void ApplyCosmeticShuffle()
        {
            if (!ModConfig.RapidRandomOutfitColor)
            {
                _cosmeticShuffleTimer = 0f;
                return;
            }

            _cosmeticShuffleTimer += Time.unscaledDeltaTime;
            float interval = Mathf.Clamp(ModConfig.RapidRandomOutfitColorInterval, 0.08f, 2f);
            if (_cosmeticShuffleTimer < interval)
                return;

            _cosmeticShuffleTimer = 0f;
            GameApi.RandomizeOutfitAndColor(false);
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

            if (ModConfig.InfiniteJumps)
            {
                c.data.jumpsRemaining = Mathf.Max(c.data.jumpsRemaining, 1);
                if (c.data.sinceJump > 0.28f)
                    c.data.sinceGrounded = Mathf.Min(c.data.sinceGrounded, 0.18f);
            }

            // Climb / vine / rope speed — set the component's multiplier while on, reset to 1 when off.
            ApplyClimbField<CharacterClimbing>(c, ClimbSpeedF, ModConfig.ClimbMod, ModConfig.ClimbAmount, ref _climbApplied);
            ApplyClimbField<CharacterVineClimbing>(c, VineSpeedF, ModConfig.VineClimbMod, ModConfig.VineClimbAmount, ref _vineApplied);
            ApplyClimbField<CharacterRopeHandling>(c, RopeSpeedF, ModConfig.RopeClimbMod, ModConfig.RopeClimbAmount, ref _ropeApplied);
        }

        // Sets a float "speed multiplier" field on the character's traversal component.
        private static void ApplyClimbField<T>(Character c, FieldInfo f, bool on, float amount, ref bool applied)
            where T : Component
        {
            if (f == null) return;
            var comp = ((Component)c).GetComponent<T>();
            if (comp == null) return;
            if (on) { f.SetValue(comp, amount); applied = true; }
            else if (applied) { f.SetValue(comp, 1f); applied = false; }
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

            if (ModConfig.NoFallingRagdoll)
                data.fallSeconds = 0f;

            if (ModConfig.NoSlipperySurfaces)
            {
                data.slippy = 0f;
                data.sinceFallSlide = Mathf.Max(data.sinceFallSlide, 2f);
            }

            if (affl != null)
            {
                if (ModConfig.NoStatusEffects)
                {
                    for (int i = 0; i < CharacterAfflictions.NumStatusTypes; i++)
                        affl.SetStatus((CharacterAfflictions.STATUSTYPE)i, 0f, false);
                    try { affl.RemoveAllThorns(); } catch { }
                }
                else
                {
                    ClearBlockedStatus(affl, CharacterAfflictions.STATUSTYPE.Injury, ModConfig.NoInjury);
                    ClearBlockedStatus(affl, CharacterAfflictions.STATUSTYPE.Hunger, ModConfig.NoHunger);
                    ClearBlockedStatus(affl, CharacterAfflictions.STATUSTYPE.Cold, ModConfig.NoCold);
                    ClearBlockedStatus(affl, CharacterAfflictions.STATUSTYPE.Poison, ModConfig.NoPoison);
                    ClearBlockedStatus(affl, CharacterAfflictions.STATUSTYPE.Curse, ModConfig.NoCurse);
                    ClearBlockedStatus(affl, CharacterAfflictions.STATUSTYPE.Drowsy, ModConfig.NoDrowsy);
                    ClearBlockedStatus(affl, CharacterAfflictions.STATUSTYPE.Hot, ModConfig.NoHot);
                }

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

        private static void ClearBlockedStatus(CharacterAfflictions affl, CharacterAfflictions.STATUSTYPE type, bool blocked)
        {
            if (blocked)
                affl.SetStatus(type, 0f, false);
        }

        private static void ApplyUtilityModifiers(Character c)
        {
            ApplyInteractionRange();
            ApplyGameSpeed();
            ApplyCinematicCamera(c);
            ApplyPingHandScale();
        }

        private static void ApplyInteractionRange()
        {
            var inst = Interaction.instance;
            if (inst == null) return;

            if (_baseInteractionDistance < 0f)
            {
                _baseInteractionDistance = inst.distance;
                _baseInteractionArea = inst.area;
            }

            if (ModConfig.LongInteraction)
            {
                inst.distance = Mathf.Max(_baseInteractionDistance, ModConfig.InteractionDistance);
                inst.area = Mathf.Max(_baseInteractionArea, 1.25f);
            }
            else if (_baseInteractionDistance >= 0f)
            {
                inst.distance = _baseInteractionDistance;
                inst.area = _baseInteractionArea;
            }
        }

        private static void ApplyGameSpeed()
        {
            if (ModConfig.GameSpeedMod)
            {
                Time.timeScale = Mathf.Clamp(ModConfig.GameSpeed, 0.05f, 5f);
                _timeScaleApplied = true;
            }
            else if (_timeScaleApplied)
            {
                Time.timeScale = 1f;
                _timeScaleApplied = false;
            }
        }

        private static void ApplyCinematicCamera(Character c)
        {
            if (!ModConfig.CinematicCamera)
            {
                DisableCinematicCamera();
                return;
            }

            if (_cinematicObject == null)
            {
                _cinematicObject = new GameObject("PEAK-MX Cinematic Camera");
                UnityEngine.Object.DontDestroyOnLoad(_cinematicObject);
                _cinematicOverride = _cinematicObject.AddComponent<CameraOverride>();
                Transform source = MainCamera.instance != null ? MainCamera.instance.transform : ((Component)c).transform;
                _cinematicObject.transform.position = source.position;
                _cinematicObject.transform.rotation = source.rotation;
                Vector3 euler = _cinematicObject.transform.rotation.eulerAngles;
                _cinematicLook = new Vector2(euler.y, euler.x);
                ActionTracker.Track("cinematic_camera_on");
            }

            _cinematicOverride.fov = Mathf.Clamp(ModConfig.CinematicCameraFov, 1f, 120f);

            float dt = Time.unscaledDeltaTime;
            _cinematicLook.x += Input.GetAxis("Mouse X") * 140f * dt;
            _cinematicLook.y -= Input.GetAxis("Mouse Y") * 140f * dt;
            _cinematicLook.y = Mathf.Clamp(_cinematicLook.y, -89f, 89f);
            _cinematicObject.transform.rotation = Quaternion.Euler(_cinematicLook.y, _cinematicLook.x, 0f);

            Vector3 local = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) local.z += 1f;
            if (Input.GetKey(KeyCode.S)) local.z -= 1f;
            if (Input.GetKey(KeyCode.D)) local.x += 1f;
            if (Input.GetKey(KeyCode.A)) local.x -= 1f;
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E)) local.y += 1f;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.Q)) local.y -= 1f;
            float speed = Mathf.Max(0.1f, ModConfig.CinematicCameraSpeed) * (Input.GetKey(KeyCode.LeftShift) ? 4f : 1f);
            _cinematicObject.transform.position += _cinematicObject.transform.TransformDirection(local.normalized) * speed * dt;

            if (MainCamera.instance != null)
                MainCamera.instance.SetCameraOverride(_cinematicOverride);
        }

        private static void DisableCinematicCamera()
        {
            if (_cinematicObject == null) return;
            try { if (MainCamera.instance != null) MainCamera.instance.SetCameraOverride(null); } catch { }
            UnityEngine.Object.Destroy(_cinematicObject);
            _cinematicObject = null;
            _cinematicOverride = null;
            ActionTracker.Track("cinematic_camera_off");
        }

        private static void ApplyPingHandScale()
        {
            float scale = Mathf.Clamp(ModConfig.PingHandSizeMultiplier, 0.1f, 10f);
            var pings = UnityEngine.Object.FindObjectsByType<PointPing>(FindObjectsSortMode.None);
            for (int i = 0; i < pings.Length; i++)
            {
                var ping = pings[i];
                if (ping == null) continue;
                if (!PingBaseFrustumSizes.ContainsKey(ping))
                    PingBaseFrustumSizes[ping] = Mathf.Max(0.001f, ping.sizeOfFrustum);
                ping.sizeOfFrustum = PingBaseFrustumSizes[ping] * scale;
            }

            if (Mathf.Abs(scale - 1f) < 0.01f && PingBaseFrustumSizes.Count > 0)
            {
                foreach (var pair in PingBaseFrustumSizes)
                    if (pair.Key != null)
                        pair.Key.sizeOfFrustum = pair.Value;
                PingBaseFrustumSizes.Clear();
            }
        }

        private static void Warn(string where, Exception e) =>
            Plugin.Log?.LogWarning($"[Features:{where}] {e.Message}");
    }
}
