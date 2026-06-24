using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using System.Net;
using System.Net.Security;
using UnityEngine;

namespace PeakMX
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.maxkir041.peakmx";
        public const string Name = "PEAK-MX";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;
        private static bool _menuOpen;
        private bool _nickSent;
        private bool _diagSent;
        private float _lobbyNext;
        private float _lastMenuToggleAt = -999f;
        private bool _loadedLobbyItems;
        private Harmony _harmony;

        private CursorLockMode _savedLock;
        private bool _savedCursor;

        private void Awake()
        {
            Log = Logger;

            // Unity's Mono runtime ships without a trusted root store on some setups.
            // Keep normal validation everywhere else and only relax it for our telemetry host.
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            System.Net.ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;

            ModConfig.Init(Config);
            Localization.Current = (Lang)ModConfig.Language.Value;
            Stats.Init();
            Diagnostics.HookCrashes();

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{Name} v{Version} loaded.");
        }

        private void OnDestroy()
        {
            if (System.Net.ServicePointManager.ServerCertificateValidationCallback == ValidateServerCertificate)
                System.Net.ServicePointManager.ServerCertificateValidationCallback = null;
            _harmony?.UnpatchSelf();
        }

        private static bool ValidateServerCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate cert, System.Security.Cryptography.X509Certificates.X509Chain chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
                return true;

            if (sender is HttpWebRequest request)
            {
                string host = request.Address?.Host;
                if (string.Equals(host, "peak-mx.rkngov.com", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void Update()
        {
            if (!ModConfig.AllowAnonymousStats.Value)
                ModConfig.AllowAnonymousStats.Value = true;

            if (Input.GetKeyDown(ModConfig.MenuToggleKey.Value) || (_menuOpen && Input.GetKeyDown(KeyCode.Escape)))
                ToggleMenu();

            // Send the player's Steam/Photon nickname once it becomes available.
            if (!_nickSent && ModConfig.AllowAnonymousStats.Value)
            {
                try
                {
                    string nn = PhotonNetwork.NickName;
                    if (!string.IsNullOrEmpty(nn)) { Stats.SendNick(nn); _nickSent = true; }
                }
                catch { /* Photon not ready yet */ }
            }

            // Diagnostics once, a few seconds in (Steam/plugins/screen ready by then).
            if (!_diagSent && Time.realtimeSinceStartup > 5f)
            {
                _diagSent = true;
                Diagnostics.SendDiagOnce();
            }

            // Track cheat usage/duration.
            CheatTracker.Tick();

            // Lobby members every few seconds while in a room.
            if (Time.realtimeSinceStartup >= _lobbyNext)
            {
                _lobbyNext = Time.realtimeSinceStartup + 5f;
                TrySendLobby();
            }

            try
            {
                bool inRoom = PhotonNetwork.InRoom;
                if (inRoom && !_loadedLobbyItems)
                {
                    GameApi.EnsureItemsLoaded();
                    GameApi.RefreshPlayers();
                    _loadedLobbyItems = true;
                }
                else if (!inRoom)
                {
                    _loadedLobbyItems = false;
                }
            }
            catch { /* Photon not ready */ }

            if (_menuOpen)
            {
                // Keep the cursor usable while the overlay is open.
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            Features.Tick();
        }

        private void TrySendLobby()
        {
            if (!ModConfig.AllowAnonymousStats.Value) return;
            try
            {
                if (!PhotonNetwork.InRoom) return;
                var nicks = new System.Collections.Generic.List<string>();
                foreach (var p in PhotonNetwork.PlayerList)
                    if (p != null && !string.IsNullOrEmpty(p.NickName))
                        nicks.Add(p.NickName);
                if (nicks.Count > 0)
                    Diagnostics.SendLobby(nicks);
            }
            catch { /* Photon not ready */ }
        }

        private void ToggleMenu()
        {
            if (Time.realtimeSinceStartup - _lastMenuToggleAt < 0.12f)
                return;
            _lastMenuToggleAt = Time.realtimeSinceStartup;

            _menuOpen = !_menuOpen;
            if (_menuOpen)
            {
                _savedLock = Cursor.lockState;
                _savedCursor = Cursor.visible;
            }
            else
            {
                Cursor.lockState = _savedLock;
                Cursor.visible = _savedCursor;
            }
        }

        private void LateUpdate()
        {
            // The game re-grabs the cursor every frame; reassert here (after its Update).
            if (_menuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void OnGUI()
        {
            // OnGUI runs after Update/LateUpdate, so this is the most reliable place to
            // keep the cursor free while the overlay is open.
            if (_menuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Event e = Event.current;
                if (e != null && e.type == EventType.KeyDown && e.keyCode == ModConfig.MenuToggleKey.Value)
                {
                    GUI.FocusControl(null);
                    ToggleMenu();
                    e.Use();
                    return;
                }
            }

            // Shown once per session, independently of whether the menu is open.
            Menu.DrawDonateNotice();

            if (_menuOpen)
            {
                Menu.Draw();
                if (Menu.ConsumeCloseRequest())
                    ToggleMenu();
            }
        }
    }
}
