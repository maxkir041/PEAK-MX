using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

namespace PeakMX
{
    internal static class PlayerMeta
    {
        public const string VersionKey = "pmx_ver";
        private static float _nextPublish;

        public static void Tick()
        {
            try
            {
                if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
                {
                    _nextPublish = 0f;
                    return;
                }

                if (Time.realtimeSinceStartup < _nextPublish)
                    return;
                _nextPublish = Time.realtimeSinceStartup + 10f;

                object current;
                if (PhotonNetwork.LocalPlayer.CustomProperties != null
                    && PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(VersionKey, out current)
                    && string.Equals(current as string, Plugin.Version, StringComparison.Ordinal))
                    return;

                var props = new Hashtable { { VersionKey, Plugin.Version } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug("[PlayerMeta] " + e.Message);
            }
        }
    }
}
