using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace PeakMX
{
    internal static class VoiceControl
    {
        private const byte EventCode = 171;
        private const string Magic = "PMX_VOICE";
        private const int CommandSetActor = 1;
        private const int CommandSetAll = 2;
        private const float GlobalMaxDistance = 5000f;

        private struct SourceState
        {
            public float MinDistance;
            public float MaxDistance;
            public AudioRolloffMode RolloffMode;
            public bool HasValue;
        }

        private static readonly HashSet<int> ForcedActors = new HashSet<int>();
        private static readonly Dictionary<int, SourceState> SourceStates = new Dictionary<int, SourceState>();
        private static bool _forceAllActors;
        private static bool _subscribed;

        public static bool ForceAllActors => _forceAllActors;

        public static void Init()
        {
            if (_subscribed)
                return;
            try
            {
                if (PhotonNetwork.NetworkingClient == null)
                    return;
                PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
                _subscribed = true;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[VoiceControl] subscribe failed: {e.Message}");
            }
        }

        public static void Dispose()
        {
            if (!_subscribed)
                return;
            try { PhotonNetwork.NetworkingClient.EventReceived -= OnEvent; }
            catch { }
            _subscribed = false;
        }

        public static bool IsActorForced(Character character)
        {
            int actor = ActorNumber(character);
            return actor > 0 && ForcedActors.Contains(actor);
        }

        public static bool ShouldForceAudible(Character character)
        {
            if (character == null)
                return false;
            try { if (character.IsLocal) return false; } catch { }
            if (ModConfig.GlobalVoice || _forceAllActors)
                return true;
            int actor = ActorNumber(character);
            return actor > 0 && ForcedActors.Contains(actor);
        }

        public static void BroadcastHearActor(Character character, bool enabled)
        {
            int actor = ActorNumber(character);
            if (actor <= 0)
                return;
            ApplyActor(actor, enabled);
            Raise(CommandSetActor, actor, enabled);
        }

        public static void BroadcastHearAll(bool enabled)
        {
            ApplyAll(enabled);
            Raise(CommandSetAll, -1, enabled);
        }

        public static void ApplyVoiceSource(AudioSource source, float audioLevel)
        {
            if (source == null)
                return;

            int id = source.GetInstanceID();
            if (!SourceStates.ContainsKey(id))
            {
                SourceStates[id] = new SourceState
                {
                    MinDistance = source.minDistance,
                    MaxDistance = source.maxDistance,
                    RolloffMode = source.rolloffMode,
                    HasValue = true,
                };
            }

            source.spatialBlend = 1f;
            source.bypassReverbZones = true;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = Mathf.Max(1f, source.minDistance);
            source.maxDistance = Mathf.Max(source.maxDistance, GlobalMaxDistance);
            source.volume = Mathf.Clamp01(audioLevel);
        }

        public static void RestoreVoiceSource(AudioSource source)
        {
            if (source == null)
                return;

            int id = source.GetInstanceID();
            if (!SourceStates.TryGetValue(id, out SourceState state) || !state.HasValue)
                return;

            try
            {
                source.minDistance = state.MinDistance;
                source.maxDistance = state.MaxDistance;
                source.rolloffMode = state.RolloffMode;
            }
            catch { }
            SourceStates.Remove(id);
        }

        public static void DisableEcho(VoiceObscuranceFilter filter)
        {
            try
            {
                if (filter == null || filter.echo == null)
                    return;
                filter.echo.enabled = true;
                filter.echo.wetMix = 0f;
                filter.echo.dryMix = 1f;
                filter.echo.decayRatio = 0f;
                filter.echo.delay = 10f;
            }
            catch { }
        }

        private static void Raise(int command, int actor, bool enabled)
        {
            try
            {
                if (!PhotonNetwork.InRoom || PhotonNetwork.NetworkingClient == null)
                    return;
                object[] payload = { Magic, command, actor, enabled };
                var options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                PhotonNetwork.RaiseEvent(EventCode, payload, options, SendOptions.SendReliable);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[VoiceControl] raise failed: {e.Message}");
            }
        }

        private static void OnEvent(EventData data)
        {
            try
            {
                if (data == null || data.Code != EventCode)
                    return;
                if (!(data.CustomData is object[] payload) || payload.Length < 4)
                    return;
                if (!(payload[0] is string magic) || !string.Equals(magic, Magic, StringComparison.Ordinal))
                    return;

                int command = Convert.ToInt32(payload[1]);
                int actor = Convert.ToInt32(payload[2]);
                bool enabled = Convert.ToBoolean(payload[3]);

                if (command == CommandSetActor)
                    ApplyActor(actor, enabled);
                else if (command == CommandSetAll)
                    ApplyAll(enabled);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[VoiceControl] event failed: {e.Message}");
            }
        }

        private static void ApplyActor(int actor, bool enabled)
        {
            if (actor <= 0)
                return;
            if (enabled)
                ForcedActors.Add(actor);
            else
                ForcedActors.Remove(actor);
        }

        private static void ApplyAll(bool enabled)
        {
            _forceAllActors = enabled;
            if (!enabled)
                ForcedActors.Clear();
        }

        private static int ActorNumber(Character character)
        {
            try
            {
                if (character == null)
                    return 0;
                PhotonView view = ((MonoBehaviourPun)character).photonView;
                if (view != null && view.Owner != null)
                    return view.Owner.ActorNumber;
            }
            catch { }
            return 0;
        }
    }
}
