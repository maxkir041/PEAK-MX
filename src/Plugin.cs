using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace PeakMX
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.maxkir041.peakmx";
        public const string Name = "PEAK-MX";
        public const string Version = "1.0.12";

        internal static ManualLogSource Log;
        private static bool _menuOpen;
#if !THUNDERSTORE_NO_ANALYTICS
        private bool _nickSent;
        private bool _diagSent;
        private float _lobbyNext;
#endif
        private float _lastMenuToggleAt = -999f;
        private bool _loadedLobbyItems;
        private Harmony _harmony;

        private CursorLockMode _savedLock;
        private bool _savedCursor;

        private void Awake()
        {
            Log = Logger;

#if !THUNDERSTORE_NO_ANALYTICS
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
#endif

            ModConfig.Init(Config);
            Localization.Current = (Lang)ModConfig.Language.Value;
#if !THUNDERSTORE_NO_ANALYTICS
            Stats.Init();
            Diagnostics.HookCrashes();
#endif

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{Name} v{Version} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private void Update()
        {
#if !THUNDERSTORE_NO_ANALYTICS
            if (!ModConfig.AllowAnonymousStats.Value)
                ModConfig.AllowAnonymousStats.Value = true;
#endif

            if (!Menu.IsCapturingHotkey &&
                (Input.GetKeyDown(ModConfig.MenuToggleKey.Value) || (_menuOpen && Input.GetKeyDown(KeyCode.Escape))))
                ToggleMenu();

#if !THUNDERSTORE_NO_ANALYTICS
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
#endif

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
#if THUNDERSTORE_NO_ANALYTICS
            return;
#else
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
#endif
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
                if (!Menu.IsCapturingHotkey && e != null && e.type == EventType.KeyDown && e.keyCode == ModConfig.MenuToggleKey.Value)
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
